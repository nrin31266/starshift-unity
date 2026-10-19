using StarShift.Combat;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEngine;
using UnityEngine.Pool;

namespace StarShift.Enemies
{
    public sealed class BossAttackModule : MonoBehaviour
    {
        [SerializeField] private SingleFireDefinition definition;
        [SerializeField] private Transform[] muzzles;
        [SerializeField] private SpriteRenderer telegraph;
        private EnemyHealth health;
        private EnemyFireGate gate;
        private PlayerGameplayGate player;
        private ObjectPool<EnemyProjectile> pool;
        private Transform projectileRoot;
        private Color idleColor;
        private float remaining;
        private bool shuttingDown;
        public bool IsBusy { get; private set; }
        public int ShotsFired { get; private set; }
        private void Awake()
        {
            health = GetComponentInParent<EnemyHealth>();
            gate = FindAnyObjectByType<EnemyFireGate>();
            player = FindAnyObjectByType<PlayerGameplayGate>();
            idleColor = telegraph.color;
            projectileRoot = new GameObject("BossProjectilePool").transform;
            projectileRoot.SetParent(health.transform.parent, false);
            pool = new ObjectPool<EnemyProjectile>(() =>
            {
                var shot = Instantiate(definition.ProjectilePrefab, projectileRoot);
                shot.ResetForPool(); return shot;
            }, null, shot => shot.ResetForPool(), shot => { if (shot != null) Destroy(shot.gameObject); }, true, 8, 32);
        }
        public bool BeginAttack()
        {
            if (IsBusy || !health.IsAlive || !gate.CanFire) return false;
            IsBusy = true; remaining = definition.TelegraphDuration; return true;
        }
        public void CancelAttack() { IsBusy = false; if (telegraph != null) telegraph.color = idleColor; }
        private void FixedUpdate()
        {
            if (!IsBusy) return;
            if (!health.IsAlive || !gate.CanFire) { CancelAttack(); return; }
            remaining -= Time.fixedDeltaTime;
            telegraph.color = Color.Lerp(idleColor, Color.white, .5f + .5f * Mathf.Sin(remaining * 24));
            if (remaining > 0) return;
            foreach (var muzzle in muzzles)
            for (int i = 0; i < definition.ProjectileCount; i++)
            {
                var angle = definition.ProjectileCount == 1 ? 0 : definition.SpreadDegrees * (i / (float)(definition.ProjectileCount - 1) - .5f);
                pool.Get().Launch(definition, muzzle, health.gameObject, player.gameObject, Release, angle);
                ShotsFired++;
            }
            CancelAttack();
        }
        private void Release(EnemyProjectile shot) { if (shuttingDown) Destroy(shot.gameObject); else pool.Release(shot); }
        private void OnDisable() => CancelAttack();
        private void OnDestroy() { shuttingDown = true; pool?.Dispose(); if (projectileRoot != null) Destroy(projectileRoot.gameObject); }
    }
}
