using UnityEngine;

namespace StarShift.Combat
{
    [CreateAssetMenu(menuName = "StarShift/Weapons/Pulse Laser Definition")]
    public sealed class PulseLaserDefinition : ScriptableObject
    {
        [SerializeField] private PlayerProjectile projectilePrefab;
        [SerializeField, Min(.02f)] private float fireInterval = .2f;
        [SerializeField, Min(.01f)] private float projectileSpeed = 12f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(.02f)] private float projectileLifetime = 1.5f;

        public PlayerProjectile ProjectilePrefab => projectilePrefab;
        public float FireInterval => fireInterval;
        public float ProjectileSpeed => projectileSpeed;
        public int Damage => damage;
        public float ProjectileLifetime => projectileLifetime;

        private void OnValidate()
        {
            fireInterval = Mathf.Max(.02f, fireInterval);
            projectileSpeed = Mathf.Max(.01f, projectileSpeed);
            damage = Mathf.Max(1, damage);
            projectileLifetime = Mathf.Max(.02f, projectileLifetime);
        }
    }
}
