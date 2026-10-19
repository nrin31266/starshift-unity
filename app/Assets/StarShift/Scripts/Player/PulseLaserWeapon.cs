using StarShift.Combat;
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
        private readonly WeaponHeatState[] heatStates = { new WeaponHeatState(), new WeaponHeatState(), new WeaponHeatState() };
        private WeaponLoadout loadout;
        private PlayerTemporaryEffects effects;
        private PulseLaserDefinition ActiveDefinition => loadout != null && loadout.ActiveDefinition != null ? loadout.ActiveDefinition : definition;
        private WeaponHeatState heat => heatStates[(int)CurrentBranch];
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
            foreach (var state in heatStates) state.Reset();
            ResetCadence();
        }

        public void ResetForSession(bool preserveThermalState = false)
        {
            loadout?.ResetForSession();
            if (!preserveThermalState) foreach (var state in heatStates) state.Reset();
            ShotsFired = 0;
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
            projectileRoot = new GameObject("PlayerProjectiles").transform;
            SceneManager.MoveGameObjectToScene(projectileRoot.gameObject, gameObject.scene);
            projectiles = new ObjectPool<PlayerProjectile>(CreateProjectile, null,
                projectile => projectile.ResetForPool(), projectile =>
                {
                    // Scene teardown có thể hủy projectile root trước weapon.
                    if (projectile != null) Destroy(projectile.gameObject);
                },
                true, 8, 64);
        }

        private void OnEnable()
        {
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
            // Nhiệt có clock riêng: bị khóa bắn và nhánh không active vẫn tiếp tục nguội.
            for (int i = 0; i < heatStates.Length; i++)
            {
                var thermalDefinition = loadout != null ? loadout.Definition((WeaponBranch)i) : i == 0 ? definition : null;
                if (thermalDefinition != null && thermalDefinition.ThermalProfile != null)
                    heatStates[i].Tick(thermalDefinition.ThermalProfile, i == (int)CurrentBranch && gameplayGate.CanFire && pointerInput.FireHeld, Time.fixedDeltaTime);
            }
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
            var config = ActiveDefinition;
            var pattern = config.Pattern(CurrentLevel);
            var count = pattern != null ? pattern.ProjectileCount : 1;
            for (int i = 0; i < count; i++)
            {
                var slot = i - (count - 1) * .5f;
                var angle = count > 1 && pattern.SpreadDegrees > 0 ? slot * pattern.SpreadDegrees / (count - 1) : 0;
                var position = muzzle.position + muzzle.right * (pattern != null ? slot * pattern.LateralSpacing : 0);
                var damage = pattern != null ? pattern.Damage : config.Damage;
                if (pattern != null && Mathf.Abs(slot) < .01f) damage += pattern.CenterBonus;
                var projectile = projectiles.Get();
                projectile.Launch(config, position, Quaternion.AngleAxis(angle, Vector3.forward) * muzzle.up,
                    damage, pattern != null ? pattern.SplashRadius : 0, gameObject, ReleaseProjectile);
                ShotsFired++;
            }
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
