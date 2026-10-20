using System;
using StarShift.Persistence;
using UnityEngine;

namespace StarShift.Player
{
    public enum CosmeticSlot { Chassis, Skin, Trail }

    [Serializable]
    public sealed class CosmeticOption
    {
        public string Id, DisplayName;
        public CosmeticSlot Slot;
        [Min(0)] public int ShardCost;
        public bool IsDefault;
        public Sprite Sprite;
        public bool FlipY;
        public Color Tint = Color.white;
    }

    [CreateAssetMenu(menuName = "StarShift/Cosmetic Catalog")]
    public sealed class CosmeticCatalog : ScriptableObject
    {
        public CosmeticOption[] Options;
        public CosmeticOption Find(string id) => Array.Find(Options, o => o.Id == id);
        public CosmeticOption Default(CosmeticSlot slot) => Array.Find(Options, o => o.Slot == slot && o.IsDefault);
        public string[] Defaults => new[] { Default(CosmeticSlot.Chassis).Id, Default(CosmeticSlot.Skin).Id, Default(CosmeticSlot.Trail).Id };
        public bool Owns(PlayerProfile profile, CosmeticOption option) => option.IsDefault || Array.IndexOf(profile?.OwnedCosmetics ?? Array.Empty<string>(), option.Id) >= 0;
        public CosmeticOption Equipped(PlayerProfile profile, CosmeticSlot slot)
        {
            var ids = profile?.EquippedCosmetics;
            var option = ids != null && ids.Length == 3 ? Find(ids[(int)slot]) : null;
            return option != null && option.Slot == slot && Owns(profile, option) ? option : Default(slot);
        }
    }
}
