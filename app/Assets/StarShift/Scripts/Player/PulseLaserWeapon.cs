using StarShift.Combat;
using StarShift.Enemies;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerGameplayGate))]
    [AddComponentMenu("StarShift/Player/Pulse Laser Weapon")]
    public sealed class PulseLaserWeapon : MonoBehaviour
    {
        [SerializeField] private PulseLaserDefinition definition;
        [SerializeField, Tooltip("Cùng input abstraction với movement; weapon chỉ đọc held intent.")]
        private PlayerPointerInput pointerInput;
        [SerializeField, Tooltip("Up của muzzle là hướng bắn gameplay hiện tại.")]
        private Transform muzzle;

        private PlayerGameplayGate gameplayGate;
        private ObjectPool<PlayerProjectile> projectiles;
        private Transform projectileRoot;
        private double nextShotTime;
        private bool wasAllowed;
        private bool shuttingDown;
        private readonly WeaponHeatState heat = new WeaponHeatState();
        private WeaponLoadout loadout;
        private PlayerTemporaryEffects effects;
        private readonly List<EnemyHealth> volleyTargets = new List<EnemyHealth>(5);
        private bool alternateLaunch;
        private PulseLaserDefinition ActiveDefinition => loadout != null && loadout.ActiveDefinition != null ? loadout.ActiveDefinition : definition;
        public WeaponBranch CurrentBranch => loadout != null ? loadout.ActiveBranch : WeaponBranch.Pulse;
        public int CurrentLevel => loadout != null ? loadout.ActiveLevel : 1;
        public int ShotsFired { get; private set; }
        public int CreatedProjectileCount { get; private set; }
        public float Heat => heat.Heat;
        public float HeatFraction => ActiveDefinition != null && ActiveDefinition.ThermalProfile != null ? heat.Heat / ActiveDefinition.ThermalProfile.Capacity : 0;
        public bool IsOverheated => heat.IsOverheated;
        public bool CanFire => gameplayGate != null && gameplayGate.CanFire && !heat.IsOverheated;

        public void ApplyDeathDowngrade()
        {
            loadout?.ApplyDeathDowngrade();
            heat.Reset();
            ResetCadence();
        }

        public void ResetForSession(bool preserveThermalState = false)
        {
            loadout?.ResetForSession();
            if (!preserveThermalState) heat.Reset();
            ShotsFired = 0;
            alternateLaunch = false;
            ResetCadence();
        }

        private void Awake()
        {
            gameplayGate = GetComponent<PlayerGameplayGate>();
            loadout = GetComponent<WeaponLoadout>();
            effects = GetComponent<PlayerTemporaryEffects>();
            if (definition == null || definition.ProjectilePrefab == null || muzzle == null || pointerInput == null)
            {
                Debug.LogError("PulseLaserWeapon requires a definition, projectile prefab, muzzle and pointer input.", this);
                enabled = false;
                return;
            }
            EnsurePool();
        }

        private void EnsurePool()
        {
            if (projectiles != null && projectileRoot != null) return;
            if (projectileRoot == null)
            {
                projectileRoot = new GameObject("PlayerProjectiles").transform;
                SceneManager.MoveGameObjectToScene(projectileRoot.gameObject, gameObject.scene);
            }
            if (projectiles == null)
            {
                projectiles = new ObjectPool<PlayerProjectile>(CreateProjectile, null,
                    projectile => projectile.ResetForPool(), projectile =>
                    {
                        // Scene teardown có thể hủy projectile root trước weapon.
                        if (projectile != null) Destroy(projectile.gameObject);
                    },
                    true, 8, 64);
            }
        }

        private void OnEnable()
        {
            EnsurePool();
            if (loadout != null) loadout.Changed += ResetCadence;
            if (gameplayGate != null) gameplayGate.StateChanged += OnGameplayStateChanged;
            if (pointerInput != null) pointerInput.FireIntentChanged += ResetCadence;
            ResetCadence();
        }

        private void OnDisable()
        {
            if (loadout != null) loadout.Changed -= ResetCadence;
            if (gameplayGate != null) gameplayGate.StateChanged -= OnGameplayStateChanged;
            if (pointerInput != null) pointerInput.FireIntentChanged -= ResetCadence;
            ResetCadence();
        }

        private void FixedUpdate()
        {
            if (Time.timeScale <= 0) return;
            // Các loại đạn dùng chung nhiệt; khóa bắn vẫn cho clock nguội chạy.
            var thermalProfile = ActiveDefinition.ThermalProfile;
            if (thermalProfile != null)
                heat.Tick(thermalProfile, gameplayGate.CanFire && pointerInput.FireHeld, Time.fixedDeltaTime);
            if (!CanFire || !pointerInput.FireHeld)
            {
                ResetCadence();
                return;
            }
            var now = Time.fixedTimeAsDouble;
            if (!wasAllowed)
            {
                wasAllowed = true;
                nextShotTime = now;
            }
            if (now + .000001 < nextShotTime) return;
            EnsurePool();
            var config = ActiveDefinition;
            var pattern = config.Pattern(CurrentLevel);
            var count = pattern != null ? pattern.ProjectileCount : 1;
            volleyTargets.Clear();
            for (int i = 0; i < count; i++)
            {
                var slot = i - (count - 1) * .5f;
                var angle = count > 1 && pattern.SpreadDegrees > 0 ? slot * pattern.SpreadDegrees / (count - 1) : 0;
                var position = muzzle.position + muzzle.right * (pattern != null ? slot * pattern.LateralSpacing : 0);
                var damage = pattern != null ? pattern.Damage : config.Damage;
                if (pattern != null && Mathf.Abs(slot) < .01f) damage += pattern.CenterBonus;
                EnemyHealth target = null;
                float launchLane = 0;
                if (config.TrackingDegreesPerSecond > 0)
                {
                    angle = -angle;
                    target = PlayerProjectile.SelectTrackingTarget(position, muzzle.up, config.TrackingRange, gameObject.scene, volleyTargets);
                    if (target == null)
                    {
                        volleyTargets.Clear();
                        target = PlayerProjectile.SelectTrackingTarget(position, muzzle.up, config.TrackingRange, gameObject.scene);
                    }
                    if (target != null) volleyTargets.Add(target);
                    // Volley lẻ giữ quả giữa đúng trục; volley một quả chỉ cong khi đã có đích.
                    launchLane = Mathf.Abs(slot) < .01f
                        ? (count == 1 && target != null ? (alternateLaunch ? -.65f : .65f) : 0)
                        : -slot / ((count - 1) * .5f);
                }
                var projectile = projectiles.Get();
                projectile.Launch(config, position, Quaternion.AngleAxis(angle, Vector3.forward) * muzzle.up,
                    damage, pattern != null ? pattern.SplashRadius : 0, gameObject, ReleaseProjectile, target, launchLane);
                ShotsFired++;
            }
            if (config.TrackingDegreesPerSecond > 0) alternateLaunch = !alternateLaunch;
            var interval = (pattern != null ? pattern.FireInterval : config.FireInterval) / (effects != null ? effects.FireRateMultiplier : 1);
            nextShotTime += interval;
            // Không bắn bù thành burst nếu clock/config đổi qua nhiều interval.
            if (nextShotTime <= now) nextShotTime = now + interval;
        }

        private PlayerProjectile CreateProjectile()
        {
            CreatedProjectileCount++;
            var projectile = Instantiate(definition.ProjectilePrefab, projectileRoot);
            projectile.ResetForPool();
            return projectile;
        }

        private void ReleaseProjectile(PlayerProjectile projectile)
        {
            if (shuttingDown) Destroy(projectile.gameObject);
            else projectiles.Release(projectile);
        }

        private void OnGameplayStateChanged()
        {
            if (!gameplayGate.CanFire) ResetCadence();
        }

        private void ResetCadence()
        {
            wasAllowed = false;
            nextShotTime = 0;
        }

        private void OnDestroy()
        {
            shuttingDown = true;
            projectiles?.Dispose();
            if (projectileRoot != null) Destroy(projectileRoot.gameObject);
        }
    }
}
