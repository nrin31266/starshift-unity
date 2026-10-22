// PulseLaserWeapon — Bắn vũ khí chính; dùng cấu hình của súng đang chọn, nhịp bắn và kho đạn tái sử dụng.
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
        // Mốc được bắn loạt tiếp theo; dùng thời gian thay vì đếm khung hình.
        private double nextShotTime;
        private bool wasAllowed;
        private bool shuttingDown;
        // Một trạng thái nhiệt dùng chung giữa các nhánh; đổi súng không tự xóa nhiệt.
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
        // Quyền bắn tổng hợp: trạng thái tàu cho phép và súng chưa quá nhiệt.
        public bool CanFire => gameplayGate != null && gameplayGate.CanFire && !heat.IsOverheated;

        // Khi mất mạng, hạ cấp súng đang dùng rồi xóa nhiệt và nhịp bắn cũ.
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

        // Tạo kho đạn khi cần; đạn trả về được reset để lần bắn mới không mang trạng thái cũ.
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

        // Cập nhật nhiệt, xét giữ bắn và nhịp; sau đó tạo loạt theo cấu hình cấp súng.
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
            // Chờ đúng thời điểm phát tiếp theo để nhịp bắn không phụ thuộc số khung hình.
            if (now + .000001 < nextShotTime) return;
            EnsurePool();
            var config = ActiveDefinition;
            // Ưu tiên thông số của cấp súng hiện tại; chỉ dùng giá trị dự phòng khi không có cấu hình cấp.
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
                // Lấy đạn từ pool để tái sử dụng; Launch gán dữ liệu cho lần bắn mới.
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

        // Bỏ lịch bắn cũ khi ngừng bắn hoặc đổi súng, tránh bắn bù các phát đã trễ.
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
