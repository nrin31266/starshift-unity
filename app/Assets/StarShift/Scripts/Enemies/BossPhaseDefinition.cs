using UnityEngine;
namespace StarShift.Enemies
{
    [CreateAssetMenu(menuName = "StarShift/Enemies/Boss Phases")]
    public sealed class BossPhaseDefinition : ScriptableObject
    {
        public string DisplayName = "Vanguard Frigate";
        [Range(.1f,.9f)] public float PhaseBThreshold = .5f;
        [Min(.1f)] public float PhaseARestSeconds = 1.9f;
        [Min(.1f)] public float PhaseBRestSeconds = 1.55f;
        [Min(.1f)] public float TransitionSeconds = 1.2f;
        public bool IsValid(int moduleCount)
        {
            bool OrderValid(int[] order) => order != null && order.Length > 0 && System.Array.TrueForAll(order, i => i >= 0 && i < moduleCount);
            return PhaseBThreshold > 0 && PhaseBThreshold < 1 && PhaseARestSeconds > 0 && PhaseBRestSeconds > 0 && TransitionSeconds > 0 && OrderValid(PhaseAOrder) && OrderValid(PhaseBOrder);
        }
        public int[] PhaseAOrder = { 0, 0, 1 };
        public int[] PhaseBOrder = { 0, 1, 2, 0 };
    }
}
