using StarShift.Combat;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    public sealed class SupportDrone : MonoBehaviour
    {
        [SerializeField] private PlayerTemporaryEffects effects;
        [SerializeField] private PlayerGameplayGate gate;
        [SerializeField] private PulseLaserDefinition weapon;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField, Min(.05f)] private float fireInterval = .4f;
        private ObjectPool<PlayerProjectile> pool;
        private Transform projectileRoot;
        private float remaining;
        private bool shuttingDown;
        public int ShotsFired { get; private set; }
        public bool IsVisible => visual.enabled;
        private void Awake()
        {
            projectileRoot = new GameObject("SupportProjectiles").transform;
            SceneManager.MoveGameObjectToScene(projectileRoot.gameObject, gameObject.scene);
            pool = new ObjectPool<PlayerProjectile>(() => Instantiate(weapon.ProjectilePrefab, projectileRoot), null,
                p => p.ResetForPool(), p => { if (p != null) Destroy(p.gameObject); }, true, 8, 32);
        }
        private void LateUpdate() => visual.enabled = effects.DroneRemaining > 0 && gate.CanInput;
        private void FixedUpdate()
        {
            if (effects.DroneRemaining <= 0) { remaining = 0; return; }
            if (!gate.CanFire) return;
            remaining -= Time.fixedDeltaTime;
            if (remaining > 0) return;
            pool.Get().Launch(weapon, transform.position, transform.up, weapon.Damage, 0, gate.gameObject, Release);
            remaining = fireInterval;
            ShotsFired++;
        }
        private void Release(PlayerProjectile shot) { if (shuttingDown) Destroy(shot.gameObject); else pool.Release(shot); }
        public void ResetForSession() { remaining = 0; ShotsFired = 0; }
        private void OnDisable() { remaining = 0; if (visual != null) visual.enabled = false; }
        private void OnDestroy() { shuttingDown = true; pool?.Dispose(); if (projectileRoot != null) Destroy(projectileRoot.gameObject); }
    }
}
