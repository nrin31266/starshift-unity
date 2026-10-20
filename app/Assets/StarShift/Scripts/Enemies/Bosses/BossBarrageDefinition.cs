using StarShift.Encounters;
using UnityEngine;

namespace StarShift.Enemies
{
    public enum BossBarrageKind { ProjectileCurtain, IonShelter }

    [CreateAssetMenu(menuName = "StarShift/Enemies/Boss Barrage")]
    public sealed class BossBarrageDefinition : ScriptableObject
    {
        public BossBarrageKind Kind;
        [Range(.18f, .35f)] public float OpeningWidth = .24f;
        [Min(.8f)] public float ChargeSeconds = 1.15f;
        [Range(2, 4)] public int PulseCount = 3;
        [Min(.1f)] public float PulseInterval = .55f;
        [Min(.1f)] public float ActiveSeconds = .55f;
        [Range(9, 25)] public int ProjectilesPerRow = 17;
        public SingleFireDefinition Projectile;
        public bool IsValid => System.Enum.IsDefined(typeof(BossBarrageKind), Kind) &&
            ConfigValue.Positive(OpeningWidth) && OpeningWidth >= .18f && OpeningWidth <= .35f &&
            ConfigValue.Positive(ChargeSeconds) && ChargeSeconds >= .8f && PulseCount >= 2 && PulseCount <= 4 &&
            ConfigValue.Positive(PulseInterval) && ConfigValue.Positive(ActiveSeconds) &&
            ProjectilesPerRow >= 9 && ProjectilesPerRow <= 25 &&
            (Kind != BossBarrageKind.ProjectileCurtain || (Projectile != null && Projectile.ProjectilePrefab != null));
    }
}
