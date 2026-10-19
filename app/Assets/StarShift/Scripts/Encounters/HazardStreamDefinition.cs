using UnityEngine;
namespace StarShift.Encounters
{
    public enum AsteroidSize { Small, Medium, Large, Huge }
    [System.Serializable]
    public sealed class AsteroidVariant
    {
        public AsteroidSize Size;
        [Min(.1f)] public float Scale = 1;
        [Min(1)] public int Hp = 4;
        [Min(.1f)] public float TransitMultiplier = 1;
    }
    [CreateAssetMenu(menuName = "StarShift/Encounters/Hazard Stream")]
    public sealed class HazardStreamDefinition : ScriptableObject
    {
        public GameObject Prefab;
        public int Seed = 521;
        public Vector2 DominantDirection = new Vector2(.42f,-1);
        [Range(0,8)] public float AngularVariance = 3;
        [Min(1)] public int Count = 22;
        public Vector2 Interval = new Vector2(.5f,.9f);
        public Vector2 Scale = new Vector2(.65f,1.4f);
        public Vector2 TransitSeconds = new Vector2(2.8f,4.2f);
        public AsteroidVariant[] Variants = System.Array.Empty<AsteroidVariant>();
        private bool VariantsValid => Variants != null && System.Array.TrueForAll(Variants, v => v != null &&
            System.Enum.IsDefined(typeof(AsteroidSize), v.Size) && ConfigValue.Positive(v.Scale) && v.Hp > 0 && ConfigValue.Positive(v.TransitMultiplier));
        public bool IsValid => Prefab != null && Prefab.GetComponent<StarShift.Enemies.EnemyHealth>() != null &&
            Prefab.GetComponent<StarShift.Enemies.EntryHoldFlight>() != null && Prefab.GetComponent<StarShift.Enemies.SingleFireAttack>() != null &&
            VariantsValid && ConfigValue.Finite(Interval) && ConfigValue.Finite(Scale) && ConfigValue.Finite(TransitSeconds) &&
            ConfigValue.Finite(DominantDirection) && DominantDirection.y < -.1f && float.IsFinite(AngularVariance) && AngularVariance >= 0 && AngularVariance <= 8 && Count > 0 && Interval.x > 0 && Interval.y >= Interval.x && Scale.x > 0 && Scale.y >= Scale.x && TransitSeconds.x > 0 && TransitSeconds.y >= TransitSeconds.x;
    }
}
