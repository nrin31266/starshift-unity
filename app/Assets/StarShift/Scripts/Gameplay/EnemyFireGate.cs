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
        private bool preserveActiveAttacks;
        private float graceRemaining;
        public bool CanFire => isActiveAndEnabled && !blocked && graceRemaining <= 0 && Time.timeScale > 0;
        public bool CanContinueAttack => isActiveAndEnabled && (!blocked || preserveActiveAttacks);
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

        public void Block(bool preserveActiveAttacks = false)
        {
            blocked = true;
            this.preserveActiveAttacks = preserveActiveAttacks;
            graceRemaining = 0;
            if (!preserveActiveAttacks) PermissionChanged?.Invoke();
        }

        public void ResumeAfter(float seconds)
        {
            bool preserved = preserveActiveAttacks;
            blocked = false;
            preserveActiveAttacks = false;
            graceRemaining = Mathf.Max(0, seconds);
            if (!preserved) PermissionChanged?.Invoke();
        }

        public void ClearProjectiles()
        {
            foreach (var projectile in FindObjectsByType<EnemyProjectile>())
                if (projectile != null && projectile.gameObject.scene == gameObject.scene) projectile.Release();
        }

        private void FixedUpdate()
        {
            if (graceRemaining <= 0) return;
            graceRemaining = Mathf.Max(0, graceRemaining - Time.fixedDeltaTime);
        }

        private void OnDisable() => PermissionChanged?.Invoke();
    }
}
