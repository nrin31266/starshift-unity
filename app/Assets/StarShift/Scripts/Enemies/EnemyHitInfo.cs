using UnityEngine;
using StarShift.Combat;

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
        public readonly WeaponBranch Branch;

        public EnemyHitInfo(EnemyHitKind kind, Vector2 point, Vector2 normal, int damage, WeaponBranch branch = WeaponBranch.Pulse)
        {
            Kind = kind;
            Point = point;
            Normal = normal;
            Damage = damage;
            Branch = branch;
        }

        public static EnemyHitInfo AtCenter(Transform target, EnemyHitKind kind, int damage, WeaponBranch branch = WeaponBranch.Pulse)
            => new EnemyHitInfo(kind, target != null ? (Vector2)target.position : Vector2.zero, Vector2.up, damage, branch);
    }
}
