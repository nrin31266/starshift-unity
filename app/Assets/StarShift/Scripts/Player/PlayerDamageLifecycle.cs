// PlayerDamageLifecycle — Nhận sát thương, mất mạng và hồi sinh; điều phối qua PlayerLives và trạng thái tàu.
using System;
using StarShift.Gameplay;
using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(PlayerGameplayGate), typeof(PlayerLives), typeof(PlayerRespawnEntrance))]
    [RequireComponent(typeof(RelativeDragMovement))]
    [AddComponentMenu("StarShift/Player/Player Damage Lifecycle")]
    public sealed class PlayerDamageLifecycle : MonoBehaviour
    {
        [SerializeField] private EnemyFireGate enemyFireGate;
        // Thời gian chờ của pha chết trước khi xét hồi sinh hoặc hết mạng.
        [SerializeField, Min(.02f)] private float deathDuration = .6f;
        // Thời gian bất tử sau khi tàu đã bay vào lại; lúc này được điều khiển và bắn.
        [SerializeField, Min(.02f)] private float invincibilityDuration = 2.5f;
        // Khoảng địch chờ trước khi được mở bắn lại sau đường bay hồi sinh.
        [SerializeField, Min(0)] private float enemyFireGrace = 1f;
        [SerializeField, Min(.02f)] private float shieldBreakInvincibilityDuration = .7f;
        private PlayerGameplayGate gate;
        private PlayerLives lives;
        private PlayerRespawnEntrance entrance;
        private PulseLaserWeapon weapon;
        private RelativeDragMovement movement;
        private float remaining;
        private PlayerTemporaryEffects effects;
        public event Action GameOver;

        private void Awake()
        {
            gate = GetComponent<PlayerGameplayGate>();
            effects = GetComponent<PlayerTemporaryEffects>();
            lives = GetComponent<PlayerLives>();
            entrance = GetComponent<PlayerRespawnEntrance>();
            weapon = GetComponent<PulseLaserWeapon>();
            movement = GetComponent<RelativeDragMovement>();
            if (enemyFireGate == null) enemyFireGate = FindAnyObjectByType<EnemyFireGate>();
            if (enemyFireGate != null && weapon != null && movement != null) return;
            Debug.LogError("PlayerDamageLifecycle requires an enemy fire gate and active Pulse weapon.", this);
            enabled = false;
        }

        private void OnEnable()
        {
            remaining = 0;
            lives.ResetForSession();
            entrance.Cancel();
            try { enemyFireGate.ResumeAfter(0); }
            finally { gate.SetState(PlayerGameplayState.Active); }
        }

        public void RestoreCheckpoint(int restoredLives)
        {
            remaining = 0;
            entrance.Cancel();
            lives.RestoreCheckpoint(restoredLives);
            gate.SetState(PlayerGameplayState.Active);
            enemyFireGate.ResumeAfter(0);
        }

        // Bảo vệ ngắn sau khiên hoặc tên lửa; không rút ngắn khoảng bảo vệ đang còn.
        public void GrantBriefProtection(float seconds)
        {
            if (gate.State != PlayerGameplayState.Active && gate.State != PlayerGameplayState.PostEntryInvincibility) return;
            remaining = Mathf.Max(remaining, seconds);
            gate.SetState(PlayerGameplayState.PostEntryInvincibility);
        }

        // Cửa xét hit hợp lệ: kiểm tra bảo vệ và khiên trước khi trừ mạng, hạ cấp súng.
        public bool TryReceiveHit(GameObject owner)
        {
            if (!isActiveAndEnabled || !gate.CanTakeDamage || lives.CurrentLives <= 0) return false;
            // Khiên hấp thụ hit trước bước mất mạng nên không hạ cấp súng.
            if (effects != null && effects.TryAbsorb())
            {
                GrantBriefProtection(shieldBreakInvincibilityDuration);
                return true;
            }
            remaining = deathDuration;
            // State khóa duplicate trước mọi callback; cleanup không phụ thuộc feedback.
            try { gate.SetState(PlayerGameplayState.Dying); }
            finally
            {
                lives.LoseLife();
                try { weapon.ApplyDeathDowngrade(); }
                finally { enemyFireGate.Block(preserveActiveAttacks: true); }
            }
            return true;
        }

        private void FixedUpdate()
        {
            if (!gate.isActiveAndEnabled || Time.timeScale <= 0) return;
            if (gate.State == PlayerGameplayState.Respawning)
            {
                if (!entrance.IsEntering && entrance.isActiveAndEnabled) BeginEntrance();
                return;
            }
            if (gate.State != PlayerGameplayState.Dying && gate.State != PlayerGameplayState.PostEntryInvincibility) return;
            remaining = Mathf.Max(0, remaining - Time.fixedDeltaTime);
            if (remaining > .000001f) return;
            if (gate.State == PlayerGameplayState.PostEntryInvincibility)
            {
                gate.SetState(PlayerGameplayState.Active);
                return;
            }
            // Hết pha chết: còn mạng thì bay vào lại, hết mạng thì báo kết thúc game.
            if (lives.CurrentLives > 0)
            {
                BeginEntrance();
                gate.SetState(PlayerGameplayState.Respawning);
            }
            else
            {
                try { gate.SetState(PlayerGameplayState.GameOver); }
                finally { GameOver?.Invoke(); }
            }
        }

        private void BeginEntrance()
        {
            entrance.Begin();
            movement.ApplyEntrancePosition();
        }

        // Chỉ mở lại điều khiển và bắt đầu bất tử sau khi đường bay hồi sinh hoàn tất.
        private void LateUpdate()
        {
            if (Time.timeScale <= 0 || !gate.isActiveAndEnabled || gate.State != PlayerGameplayState.Respawning || !entrance.IsComplete) return;
            // Movement đã ghi final pose trong LateUpdate trước khi mở permission.
            entrance.Cancel();
            remaining = invincibilityDuration;
            try { enemyFireGate.ResumeAfter(enemyFireGrace); }
            finally { gate.SetState(PlayerGameplayState.PostEntryInvincibility); }
        }

        private void OnDisable()
        {
            remaining = 0;
            if (entrance != null) entrance.Cancel();
            try { if (enemyFireGate != null) enemyFireGate.Block(); }
            finally
            {
                try { if (enemyFireGate != null) enemyFireGate.ClearProjectiles(); }
                finally { if (gate != null) gate.SetState(PlayerGameplayState.Entry); }
            }
        }

        private void OnValidate()
        {
            shieldBreakInvincibilityDuration = Mathf.Max(.02f, shieldBreakInvincibilityDuration);
            deathDuration = Mathf.Max(.02f, deathDuration);
            invincibilityDuration = Mathf.Max(.02f, invincibilityDuration);
            enemyFireGrace = Mathf.Max(0, enemyFireGrace);
        }
    }
}
