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
        [SerializeField, Min(.02f)] private float deathDuration = .35f;
        [SerializeField, Min(.02f)] private float invincibilityDuration = 2.5f;
        [SerializeField, Min(0)] private float enemyFireGrace = 1f;
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

        public bool TryReceiveHit(GameObject owner)
        {
            if (!isActiveAndEnabled || !gate.CanTakeDamage || lives.CurrentLives <= 0) return false;
            if (effects != null && effects.TryAbsorb()) return true;
            remaining = deathDuration;
            // State khóa duplicate trước mọi callback; cleanup không phụ thuộc feedback.
            try { gate.SetState(PlayerGameplayState.Dying); }
            finally
            {
                lives.LoseLife();
                try { weapon.ApplyDeathDowngrade(); }
                finally
                {
                    try { enemyFireGate.Block(); }
                    finally { enemyFireGate.ClearProjectiles(); }
                }
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
            if (lives.CurrentLives > 0)
            {
                BeginEntrance();
                gate.SetState(PlayerGameplayState.Respawning);
            }
            else
            {
                try { enemyFireGate.Block(); }
                finally
                {
                    try { enemyFireGate.ClearProjectiles(); }
                    finally
                    {
                        try { gate.SetState(PlayerGameplayState.GameOver); }
                        finally { GameOver?.Invoke(); }
                    }
                }
            }
        }

        private void BeginEntrance()
        {
            entrance.Begin();
            movement.ApplyEntrancePosition();
        }

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
            deathDuration = Mathf.Max(.02f, deathDuration);
            invincibilityDuration = Mathf.Max(.02f, invincibilityDuration);
            enemyFireGrace = Mathf.Max(0, enemyFireGrace);
        }
    }
}
