using System;
using StarShift.Combat;
using UnityEngine;

namespace StarShift.Gameplay
{
    [DisallowMultipleComponent]
    [AddComponentMenu("StarShift/Gameplay/Enemy Fire Gate")]
    public sealed class EnemyFireGate : MonoBehaviour
    {
        private bool blocked;
        private float graceRemaining;
        public bool CanFire => isActiveAndEnabled && !blocked && graceRemaining <= 0 && Time.timeScale > 0;
        public event Action PermissionChanged;
        public float VisibleProjectileTimeLimit
        {
            get
            {
                float maximum = 0;
                foreach (var projectile in FindObjectsByType<EnemyProjectile>())
                    if (projectile.gameObject.scene == gameObject.scene)
                        maximum = Mathf.Max(maximum, projectile.VisibleFlightTimeLimit);
                return maximum;
            }
        }

        public void Block()
        {
            blocked = true;
            graceRemaining = 0;
            PermissionChanged?.Invoke();
        }

        public void ResumeAfter(float seconds)
        {
            blocked = false;
            graceRemaining = Mathf.Max(0, seconds);
            PermissionChanged?.Invoke();
        }

        public void ClearProjectiles()
        {
            foreach (var projectile in FindObjectsByType<EnemyProjectile>())
                if (projectile != null && projectile.gameObject.scene == gameObject.scene) projectile.Release();
        }

        public void BeginProjectileCleanup(float graceSeconds, float fadeSeconds)
        {
            foreach (var projectile in FindObjectsByType<EnemyProjectile>())
                if (projectile.gameObject.scene == gameObject.scene) projectile.BeginCleanup(graceSeconds, fadeSeconds);
        }

        private void FixedUpdate()
        {
            if (graceRemaining <= 0) return;
            graceRemaining = Mathf.Max(0, graceRemaining - Time.fixedDeltaTime);
        }

        private void OnDisable() => PermissionChanged?.Invoke();
    }
}
