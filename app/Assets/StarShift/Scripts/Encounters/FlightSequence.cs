using System;
using UnityEngine;

namespace StarShift.Encounters
{
    public enum FlightPhaseKind { Entry, Move, Hold, Combat, Cross, Exit }
    public enum FlightPath { Straight, DiagonalPass, ArcLeft, ArcRight, Sine, Dive, Loop, Cross, Pincer, Orbit, ExitCurve }

    [Serializable]
    public sealed class FlightPhase
    {
        public FlightPhaseKind Kind;
        public FlightPath Path;
        public Vector2 From = new Vector2(.5f, 1.12f);
        public Vector2 To = new Vector2(.5f, .78f);
        public Vector2 Curve;
        [Min(.01f)] public float Duration = 1;
        public bool CanShoot;
        public bool ClampToView;
        public bool HoldUntilResolved;
        public bool RepeatMotion;
        public bool Smooth = true;
        [Min(.01f)] public float Cycles = 1;

        public Vector2 Evaluate(float progress, Vector2 from, Vector2 to)
        {
            var t = Smooth ? Mathf.SmoothStep(0, 1, progress) : Mathf.Clamp01(progress);
            var position = Vector2.Lerp(from, to, t);
            switch (Path)
            {
                case FlightPath.ArcLeft: return position + new Vector2(-Mathf.Abs(Curve.x), Curve.y) * (4 * t * (1 - t));
                case FlightPath.ArcRight: return position + new Vector2(Mathf.Abs(Curve.x), Curve.y) * (4 * t * (1 - t));
                case FlightPath.Sine: return position + Curve * Mathf.Sin(t * Mathf.PI * 2 * Cycles) * Mathf.Sin(t * Mathf.PI);
                case FlightPath.Dive: return position + Curve * Mathf.Sin(t * Mathf.PI);
                case FlightPath.Loop:
                case FlightPath.Orbit:
                    var angle = t * Mathf.PI * 2 * Cycles;
                    return position + new Vector2(Curve.x * Mathf.Sin(angle), Curve.y * (1 - Mathf.Cos(angle)));
                case FlightPath.Pincer:
                case FlightPath.ExitCurve: return position + Curve * (4 * t * (1 - t));
                default: return position;
            }
        }
    }

    [CreateAssetMenu(menuName = "StarShift/Encounters/Flight Sequence")]
    public sealed class FlightSequence : ScriptableObject
    {
        public FlightPhase[] Phases = { new FlightPhase(), new FlightPhase {
            Kind = FlightPhaseKind.Hold, From = new Vector2(.5f,.78f), To = new Vector2(.5f,.78f),
            CanShoot = true, ClampToView = true, HoldUntilResolved = true } };

        public bool IsValid(out string error)
        {
            error = "Invalid flight sequence: " + name;
            if (Phases == null || Phases.Length == 0) return false;
            for (int i = 0; i < Phases.Length; i++)
            {
                var p = Phases[i];
                if (p == null || !ConfigValue.Positive(p.Duration) || !ConfigValue.Positive(p.Cycles) ||
                    !ConfigValue.Finite(p.From) || !ConfigValue.Finite(p.To) || !ConfigValue.Finite(p.Curve) ||
                    !Enum.IsDefined(typeof(FlightPhaseKind), p.Kind) || !Enum.IsDefined(typeof(FlightPath), p.Path) ||
                    (p.HoldUntilResolved && (i != Phases.Length - 1 || (p.Kind != FlightPhaseKind.Hold && p.Kind != FlightPhaseKind.Combat)))) return false;
                if ((p.Path == FlightPath.Loop || p.Path == FlightPath.Orbit) && !Mathf.Approximately(p.Cycles, Mathf.Round(p.Cycles))) return false;
                if (i > 0 && Vector2.Distance(Phases[i-1].To, p.From) > .0001f) return false;
            }
            error = null;
            return true;
        }
    }

    public static class ConfigValue
    {
        public static bool Positive(float value) => float.IsFinite(value) && value > 0;
        public static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0;
        public static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
