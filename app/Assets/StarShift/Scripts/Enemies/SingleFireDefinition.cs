using StarShift.Combat;
using UnityEngine;

namespace StarShift.Enemies
{
    [CreateAssetMenu(menuName = "StarShift/Enemies/Single Fire Definition")]
    public sealed class SingleFireDefinition : ScriptableObject
    {
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField, Min(0)] private float initialDelay = .6f;
        [SerializeField, Min(.02f)] private float telegraphDuration = .4f;
        [SerializeField, Min(.02f)] private float cooldown = 1.8f;
        [SerializeField, Min(.01f)] private float projectileSpeed = 4f;
        [SerializeField, Min(.02f)] private float projectileLifetime = 6f;

        public EnemyProjectile ProjectilePrefab => projectilePrefab;
        public float InitialDelay => initialDelay;
        public float TelegraphDuration => telegraphDuration;
        public float Cooldown => cooldown;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;

        private void OnValidate()
        {
            initialDelay = Mathf.Max(0, initialDelay);
            telegraphDuration = Mathf.Max(.02f, telegraphDuration);
            cooldown = Mathf.Max(.02f, cooldown);
            projectileSpeed = Mathf.Max(.01f, projectileSpeed);
            projectileLifetime = Mathf.Max(.02f, projectileLifetime);
        }
    }
}
