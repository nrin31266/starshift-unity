using StarShift.Combat;
using StarShift.Player;
using StarShift.Gameplay;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth), typeof(EntryHoldFlight))]
    [AddComponentMenu("StarShift/Enemies/Single Fire Attack")]
    public sealed class SingleFireAttack : MonoBehaviour
    {
        [SerializeField] private SingleFireDefinition definition;
        [SerializeField] private Transform muzzle;
        [SerializeField, Tooltip("Scene bind Player; prefab fallback tìm Player gate, không dùng tọa độ cố định.")]
        private Transform target;
        [SerializeField] private EnemyFireGate fireGate;

        private EnemyHealth health;
        private EntryHoldFlight flight;
        private ObjectPool<EnemyProjectile> projectiles;
        private Transform projectileRoot;
        private float remaining;
        private bool shuttingDown;
        private bool encounterControlled;

        public bool IsTelegraphing { get; private set; }
        private bool CanRequestAttack => isActiveAndEnabled && Eligible && !IsTelegraphing && remaining <= .000001f;
        private bool Eligible => fireGate != null && fireGate.CanFire && health.IsAlive &&
            flight.isActiveAndEnabled && flight.CanShoot && flight.IsPointVisible(muzzle.position) &&
            target != null && target.gameObject.activeInHierarchy;

        public void SetEncounterControlled()
        {
            encounterControlled = true;
            ResetSequence();
        }

        public bool TryRequestAttack()
        {
            if (!CanRequestAttack) return false;
            IsTelegraphing = true;
            remaining = definition.TelegraphDuration;
            return true;
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            flight = GetComponent<EntryHoldFlight>();
            if (fireGate == null) fireGate = FindAnyObjectByType<EnemyFireGate>();
            if (definition == null || definition.ProjectilePrefab == null || muzzle == null || fireGate == null)
            {
                Debug.LogError("SingleFireAttack requires a definition, projectile prefab, muzzle and enemy fire gate.", this);
                enabled = false;
                return;
            }
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerGameplayGate>();
                if (player != null) target = player.transform;
            }
            projectileRoot = new GameObject("EnemyProjectiles").transform;
            SceneManager.MoveGameObjectToScene(projectileRoot.gameObject, gameObject.scene);
            projectiles = new ObjectPool<EnemyProjectile>(CreateProjectile, null,
                projectile => projectile.ResetForPool(), projectile =>
                {
                    if (projectile != null) Destroy(projectile.gameObject);
                }, true, 4, 16);
        }

        private void OnEnable()
        {
            if (fireGate != null) fireGate.PermissionChanged += ResetSequence;
            ResetSequence();
        }
        private void OnDisable()
        {
            if (fireGate != null) fireGate.PermissionChanged -= ResetSequence;
            ResetSequence();
        }

        private void FixedUpdate()
        {
            if (!Eligible)
            {
                ResetSequence();
                return;
            }
            if (Time.timeScale <= 0) return;
            remaining -= Time.fixedDeltaTime;
            if (remaining > .000001f) return;
            if (!IsTelegraphing)
            {
                if (!encounterControlled) TryRequestAttack();
                return;
            }
            var projectile = projectiles.Get();
            projectile.Launch(definition, muzzle, gameObject, target.gameObject, ReleaseProjectile);
            IsTelegraphing = false;
            // Không giữ backlog để resume hoặc frame chậm tạo burst.
            remaining = definition.Cooldown;
        }

        private void ResetSequence()
        {
            IsTelegraphing = false;
            remaining = definition != null ? definition.InitialDelay : 0;
        }

        private EnemyProjectile CreateProjectile()
        {
            var projectile = Instantiate(definition.ProjectilePrefab, projectileRoot);
            projectile.ResetForPool();
            return projectile;
        }

        private void ReleaseProjectile(EnemyProjectile projectile)
        {
            if (shuttingDown) Destroy(projectile.gameObject);
            else projectiles.Release(projectile);
        }

        private void OnDestroy()
        {
            shuttingDown = true;
            projectiles?.Dispose();
            if (projectileRoot != null) Destroy(projectileRoot.gameObject);
        }
    }
}
