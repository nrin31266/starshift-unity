using UnityEngine;

namespace StarShift.Enemies
{
    // Loại trúng chỉ phục vụ presentation; damage/splash vẫn do owner combat quyết.
    public enum EnemyHitKind { Hull, Module, ShieldBlock }

    // Vị trí/normal được truyền từ projectile; fallback về tâm enemy khi thiếu.
    public readonly struct EnemyHitInfo
    {
        public readonly EnemyHitKind Kind;
        public readonly Vector2 Point;
        public readonly Vector2 Normal;
        public readonly int Damage;

        public EnemyHitInfo(EnemyHitKind kind, Vector2 point, Vector2 normal, int damage)
        {
            Kind = kind;
            Point = point;
            Normal = normal;
            Damage = damage;
        }

        public static EnemyHitInfo AtCenter(Transform target, EnemyHitKind kind, int damage)
            => new EnemyHitInfo(kind, target != null ? (Vector2)target.position : Vector2.zero, Vector2.up, damage);
    }
}
