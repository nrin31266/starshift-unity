using UnityEngine;

namespace StarShift.Enemies
{
    [CreateAssetMenu(menuName = "StarShift/Enemies/Modifier")]
    public sealed class EnemyModifierDefinition : ScriptableObject
    {
        [Min(1)] public float HpMultiplier = 1.75f;
        [Range(.5f, 1)] public float FireIntervalMultiplier = .8f;
        [Range(1, 1.3f)] public float Scale = 1.12f;
        public Color Accent = new Color(1, .65f, .15f);
    }
}
