using UnityEngine;

namespace StarShift.Combat
{
    public sealed class WeaponHeatState
    {
        private float coolingDelayRemaining;
        public float Heat { get; private set; }
        public bool IsOverheated { get; private set; }

        public void Reset()
        {
            Heat = coolingDelayRemaining = 0;
            IsOverheated = false;
        }

        public void Tick(WeaponThermalProfile profile, bool held, float seconds)
        {
            if (seconds <= 0) return;
            if (IsOverheated)
            {
                coolingDelayRemaining = 0;
                Heat = Mathf.Max(0, Heat - profile.OverheatCoolRate * seconds);
                if (Heat <= 0) IsOverheated = false;
                return;
            }
            if (held)
            {
                coolingDelayRemaining = profile.CoolingDelay;
                Heat = Mathf.Min(profile.Capacity, Heat + profile.HeatPerSecond * seconds);
                if (Heat >= profile.Capacity) IsOverheated = true;
                return;
            }
            // Chỉ phần thời gian sau cooling delay mới được giảm heat.
            var coolingSeconds = Mathf.Max(0, seconds - coolingDelayRemaining);
            coolingDelayRemaining = Mathf.Max(0, coolingDelayRemaining - seconds);
            Heat = Mathf.Max(0, Heat - profile.CoolRate * coolingSeconds);
        }
    }
}
