using StarShift.Combat;
using UnityEngine;

namespace StarShift.Rewards
{
    public enum PickupKind { Shard, WeaponCore, Shield, Overdrive, SupportDrone, MissileAmmo, ExtraLife }

    [CreateAssetMenu(menuName = "StarShift/Rewards/Pickup Definition")]
    public sealed class PickupDefinition : ScriptableObject
    {
        public PickupKind Kind;
        public WeaponBranch Branch;
        public string ShortCode;
        public Color Color = Color.white;
        public Sprite Sprite;
        public Sprite[] AnimationFrames;
        [Range(.2f,1)] public float VisualScale = 1;
        [Min(1)] public int Amount = 1;
        [Min(.1f)] public float Lifetime = 12;
        [Min(0)] public float DriftSpeed = .65f;
        [Min(.1f)] public float Duration = 8;
        [Min(1)] public float FireRateMultiplier = 1.6f;
        [Min(.1f)] public float FallAcceleration = 14;
        [Min(.1f)] public float MaximumFallSpeed = 8;
        [Range(.01f,.2f)] public float ShardBounceHeight = .06f;
    }
}
