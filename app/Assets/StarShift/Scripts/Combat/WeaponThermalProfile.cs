using UnityEngine;

namespace StarShift.Combat
{
    [CreateAssetMenu(menuName = "StarShift/Weapons/Thermal Profile")]
    public sealed class WeaponThermalProfile : ScriptableObject
    {
        [SerializeField, Min(1)] private float capacity = 100f;
        [SerializeField, Min(.01f)] private float heatPerSecond = 18f;
        [SerializeField, Min(0)] private float coolingDelay = .2f;
        [SerializeField, Min(.01f)] private float coolRate = 30f;
        [SerializeField, Min(.01f)] private float overheatCoolRate = 50f;
        [SerializeField, Range(0, .99f)] private float recoveryFraction = .4f;

        public float Capacity => capacity;
        public float HeatPerSecond => heatPerSecond;
        public float CoolingDelay => coolingDelay;
        public float CoolRate => coolRate;
        public float OverheatCoolRate => overheatCoolRate;
        public float RecoveryFraction => recoveryFraction;

        private void OnValidate()
        {
            capacity = Mathf.Max(1, capacity);
            heatPerSecond = Mathf.Max(.01f, heatPerSecond);
            coolingDelay = Mathf.Max(0, coolingDelay);
            coolRate = Mathf.Max(.01f, coolRate);
            overheatCoolRate = Mathf.Max(.01f, overheatCoolRate);
            recoveryFraction = Mathf.Clamp(recoveryFraction, 0, .99f);
        }
    }
}
