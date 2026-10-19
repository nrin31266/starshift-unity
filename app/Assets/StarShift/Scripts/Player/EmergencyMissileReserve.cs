using System;
using System.Collections.Generic;
using StarShift.Combat;
using StarShift.Enemies;
using StarShift.Encounters;
using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    public sealed class EmergencyMissileReserve : MonoBehaviour
    {
        [SerializeField] private EncounterRunner runner;
        [SerializeField] private PlayerDamageLifecycle lifecycle;
        [SerializeField, Min(1)] private int capacity = 3;
        [SerializeField, Min(0)] private int startingCount = 1;
        [SerializeField, Min(.05f)] private float activationCooldown = .8f;
        [SerializeField, Range(0, 1)] private float largeDamageFraction = .6f;
        [SerializeField, Range(0, 1)] private float bossDamageFraction = .1f;
        [SerializeField, Min(.02f)] private float protectionSeconds = .7f;
        [SerializeField, Min(.05f)] private float flightSeconds = .32f;
        private PlayerGameplayGate gate;
        private float cooldown;
        private float flightRemaining;
        private float flightDuration;
        private int flightGeneration;
        private readonly List<EnemyHealth> targets = new List<EnemyHealth>(16);
        private readonly List<Collider2D> targetColliders = new List<Collider2D>(4);
        private readonly Plane[] viewPlanes = new Plane[6];
        public int Current { get; private set; }
        public int Capacity => capacity;
        public bool CanCollect => Current < capacity;
        public bool IsInFlight => flightRemaining > 0;
        public float FlightProgress => IsInFlight ? 1 - flightRemaining / flightDuration : 1;
        public Vector2 LaunchViewport { get; private set; }
        public bool CanUse => isActiveAndEnabled && gate.CanInput && (runner.Stage == EncounterStage.Running || runner.Stage == EncounterStage.ExitDrain) && Current > 0 && cooldown <= 0 && !IsInFlight;
        public event Action Fired;
        public event Action Detonated;
        private void Awake() { gate = GetComponent<PlayerGameplayGate>(); ResetForSession(); }
        private void OnEnable() => runner.RunStarted += ResetForSession;
        private void OnDisable() { runner.RunStarted -= ResetForSession; cooldown = 0; CancelPending(); }
        private void FixedUpdate()
        {
            cooldown = Mathf.Max(0, cooldown - Time.fixedDeltaTime);
            if (!IsInFlight) return;
            if (runner.Generation != flightGeneration || !runner.HasActiveRun ||
                gate.State == PlayerGameplayState.GameOver || gate.State == PlayerGameplayState.Result)
            { CancelPending(); return; }
            flightRemaining = Mathf.Max(0, flightRemaining - Time.fixedDeltaTime);
            if (!IsInFlight) Detonate();
        }
        public void CancelPending() => flightRemaining = 0;
        public void ResetForSession() { Current = Mathf.Clamp(startingCount, 0, capacity); cooldown = 0; CancelPending(); }
        public void RestoreCheckpoint(int count)
        {
            if (count < 0 || count > capacity) throw new ArgumentOutOfRangeException(nameof(count));
            Current = count; cooldown = 0; CancelPending();
        }
        public bool TryAdd()
        {
            if (!CanCollect) return false;
            Current++; return true;
        }
        public bool TryUse()
        {
            if (!CanUse) return false;
            Current--; cooldown = activationCooldown;
            lifecycle.GrantBriefProtection(protectionSeconds);
            LaunchViewport = Camera.main.WorldToViewportPoint(transform.position);
            flightDuration = flightRemaining = flightSeconds;
            flightGeneration = runner.Generation;
            Fired?.Invoke();
            return true;
        }
        private void Detonate()
        {
            foreach (var shot in FindObjectsByType<EnemyProjectile>())
                if (shot.gameObject.scene == gameObject.scene) shot.Release();
            targets.Clear();
            var camera = Camera.main;
            GeometryUtility.CalculateFrustumPlanes(camera, viewPlanes);
            foreach (var enemy in EnemyHealth.Active)
            {
                if (!enemy.IsAlive || enemy.gameObject.scene != gameObject.scene || !enemy.Definition.BodyDamagesPlayer) continue;
                if (IsVisible(enemy, camera)) targets.Add(enemy);
            }
            // Snapshot tránh death callback/pool reuse sửa registry trong lúc resolve.
            foreach (var enemy in targets)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                var tier = enemy.Definition.MissileTier;
                var damage = tier == MissileDamageTier.Normal ? enemy.CurrentHp :
                    Mathf.RoundToInt(enemy.MaxHp * (tier == MissileDamageTier.Boss ? bossDamageFraction : largeDamageFraction));
                enemy.ReceivePlayerHit(damage, gameObject);
            }
            targets.Clear();
            Detonated?.Invoke();
        }
        private bool IsVisible(EnemyHealth enemy, Camera camera)
        {
            enemy.GetComponentsInChildren(false, targetColliders);
            foreach (var collider in targetColliders)
                if (collider.enabled && GeometryUtility.TestPlanesAABB(viewPlanes, collider.bounds)) return true;
            if (targetColliders.Count > 0) return false;
            // Target không có shape vẫn dùng anchor; target lớn có shape kiểm cả phần ló vào view.
            var point = camera.WorldToViewportPoint(enemy.transform.position);
            return point.z >= camera.nearClipPlane && point.z <= camera.farClipPlane && point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1;
        }
    }
}
