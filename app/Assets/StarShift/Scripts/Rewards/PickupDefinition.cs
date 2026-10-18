using StarShift.Combat;
using UnityEngine;

namespace StarShift.Rewards
{
    public enum PickupKind { Shard, WeaponCore, Shield, Overdrive, SupportDrone, Emp, ExtraLife }

    [CreateAssetMenu(menuName = "StarShift/Rewards/Pickup Definition")]
    public sealed class PickupDefinition : ScriptableObject
    {
        public PickupKind Kind;
        public WeaponBranch Branch;
        public string ShortCode;
        public Color Color = Color.white;
        [Min(1)] public int Amount = 1;
        [Min(.1f)] public float Lifetime = 12;
        [Min(0)] public float DriftSpeed = .65f;
        [Min(.1f)] public float Duration = 8;
        [Min(1)] public float FireRateMultiplier = 1.6f;
        [Min(.1f)] public float EmpRadius = 8;
        [Min(1)] public int EmpDamage = 2;
    }
}
