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

        private DirectedAttack directed;
        private EnemyHealth health;
        private EntryHoldFlight flight;
        private ObjectPool<EnemyProjectile> projectiles;
        private Transform projectileRoot;
        private float remaining;
        private bool shuttingDown;
        private bool encounterControlled;
        private int shotsRemaining;

        public event System.Action VolleyFired;
        private bool telegraphing;
        public bool AwaitingFirstDirectedAttack => directed != null && directed.AttacksFired == 0;
        public bool IsTelegraphing => directed != null ? directed.IsBusy : telegraphing;
        private bool CanRequestAttack => isActiveAndEnabled && fireGate != null && fireGate.CanFire && Eligible && !IsTelegraphing && remaining <= .000001f;
        // Chỉ tấn công khi địch còn sống, đường bay cho phép và nòng súng đang trong vùng nhìn.
        private bool Eligible => fireGate != null && fireGate.CanContinueAttack && health.IsAlive &&
            flight.isActiveAndEnabled && flight.CanShoot && flight.IsPointVisible(muzzle.position) &&
            target != null && target.gameObject.activeInHierarchy;

        public void SetEncounterControlled()
        {
            encounterControlled = true;
            ResetSequence();
        }

        // Xin bắt đầu đòn: chạy thời gian báo trước rồi mới phóng các loạt đạn.
        public bool TryRequestAttack()
        {
            if (directed != null) return isActiveAndEnabled && directed.TryRequestAttack();
            if (!CanRequestAttack) return false;
            telegraphing = true;
            shotsRemaining = definition.BurstCount;
            remaining = definition.TelegraphDuration;
            return true;
        }

        private void Awake()
        {
            directed = GetComponent<DirectedAttack>();
            health = GetComponent<EnemyHealth>();
            flight = GetComponent<EntryHoldFlight>();
            if (fireGate == null) fireGate = FindAnyObjectByType<EnemyFireGate>();
            if (directed != null) return;
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

        // Đếm thời gian báo trước, bắn theo loạt và chờ hồi chiêu; dùng pool cho đạn địch.
        private void FixedUpdate()
        {
            if (Time.timeScale <= 0) return;
            if (directed != null) return;
            if (!Eligible || !telegraphing && !fireGate.CanFire)
            {
                ResetSequence();
                return;
            }
            remaining -= Time.fixedDeltaTime;
            if (remaining > .000001f) return;
            if (!IsTelegraphing)
            {
                if (!encounterControlled) TryRequestAttack();
                return;
            }
            for (int i = 0; i < definition.ProjectileCount; i++)
            {
                float offset = definition.ProjectileCount == 1 ? 0 : definition.SpreadDegrees * (i / (float)(definition.ProjectileCount - 1) - .5f);
                projectiles.Get().Launch(definition, muzzle, gameObject, target.gameObject, ReleaseProjectile, offset);
            }
            VolleyFired?.Invoke();
            if (--shotsRemaining > 0) { remaining = definition.BurstInterval; return; }
            telegraphing = false;
            // Không giữ backlog để resume hoặc frame chậm tạo burst.
            remaining = definition.Cooldown * (health.Modifier != null ? health.Modifier.FireIntervalMultiplier : 1);
        }

        private void ResetSequence()
        {
            directed?.CancelAttack();
            shotsRemaining = 0;
            telegraphing = false;
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
