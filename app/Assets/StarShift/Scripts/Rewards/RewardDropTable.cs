using System;
using UnityEngine;

namespace StarShift.Rewards
{
    public enum RewardSource { Normal, Elite, MajorCrate, Resource }
    [Serializable]
    public sealed class WeightedPickup
    {
        public PickupDefinition Item;
        [Min(0)] public float Weight = 1;
    }
    [CreateAssetMenu(menuName = "StarShift/Rewards/Drop Table")]
    public sealed class RewardDropTable : ScriptableObject
    {
        public RewardSource Source;
        [Range(0, 1)] public float MajorChance = .0075f;
        [Range(0, 1)] public float ShardChance = .035f;
        public PickupDefinition Shard;
        [Range(0,48), Tooltip("Chỉ boss defeat mới rải vàng toàn màn hình.")] public int BossShardCount;
        [Tooltip("Chỉ dùng cho authored system-test reward moments, không đổi global drop rate.")]
        public PickupDefinition GuaranteedPickup;
        public WeightedPickup[] MajorItems = Array.Empty<WeightedPickup>();
    }
}
