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

        private void Awake()
        {
            gameplayGate = GetComponent<PlayerGameplayGate>();
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
                true, 8, 32);
        }

        private void OnEnable()
        {
            if (gameplayGate != null) gameplayGate.StateChanged += OnGameplayStateChanged;
            if (pointerInput != null) pointerInput.FireIntentChanged += ResetCadence;
            ResetCadence();
        }

        private void OnDisable()
        {
            if (gameplayGate != null) gameplayGate.StateChanged -= OnGameplayStateChanged;
            if (pointerInput != null) pointerInput.FireIntentChanged -= ResetCadence;
            ResetCadence();
        }

        private void FixedUpdate()
        {
            if (!gameplayGate.CanFire || !pointerInput.FireHeld)
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
            var projectile = projectiles.Get();
            projectile.Launch(definition, muzzle, gameObject, ReleaseProjectile);
            nextShotTime += definition.FireInterval;
            // Không bắn bù thành burst nếu clock/config đổi qua nhiều interval.
            if (nextShotTime <= now) nextShotTime = now + definition.FireInterval;
        }

        private PlayerProjectile CreateProjectile()
        {
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
