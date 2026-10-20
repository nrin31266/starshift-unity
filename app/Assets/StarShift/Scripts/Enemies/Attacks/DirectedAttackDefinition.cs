using StarShift.Encounters;
using UnityEngine;

namespace StarShift.Enemies
{
    public enum DirectedAttackKind { Beam, Strike }

    [CreateAssetMenu(menuName = "StarShift/Enemies/Directed Attack")]
    public sealed class DirectedAttackDefinition : ScriptableObject
    {
        public DirectedAttackKind Kind;
        [Min(.1f)] public float ChargeSeconds = 1.25f;
        [Min(0)] public float TrackingSeconds = .35f;
        [Min(.05f)] public float ActiveSeconds = .55f;
        [Min(.1f)] public float CooldownSeconds = 3.5f;
        [Min(.03f)] public float BeamWidth = .22f;
        public bool Sweep;
        public bool AlternateSweep;
        public Vector2 SweepLanes = new Vector2(.22f, .78f);
        [Min(.1f)] public float ReturnSeconds = 1.1f;
        public bool IsValid => System.Enum.IsDefined(typeof(DirectedAttackKind), Kind) &&
            ConfigValue.Positive(ChargeSeconds) && ConfigValue.Nonnegative(TrackingSeconds) && TrackingSeconds < ChargeSeconds &&
            ConfigValue.Positive(ActiveSeconds) && ConfigValue.Positive(CooldownSeconds) && ConfigValue.Positive(BeamWidth) &&
            ConfigValue.Positive(ReturnSeconds) && ConfigValue.Finite(SweepLanes) && SweepLanes.x > 0 && SweepLanes.y < 1 && SweepLanes.y > SweepLanes.x;
    }
}
