// WeaponHeatState — Nhiệt vũ khí trong lượt chơi; tách trạng thái nhiệt khỏi cấu hình WeaponThermalProfile.
using UnityEngine;

namespace StarShift.Combat
{
    public sealed class WeaponHeatState
    {
        private float coolingDelayRemaining;
        // Nhiệt hiện tại của lượt chơi; cấu hình sức chứa và tốc độ nằm trong profile.
        public float Heat { get; private set; }
        // Cờ khóa bắn do quá nhiệt; chỉ được xóa khi nhiệt nguội về 0.
        public bool IsOverheated { get; private set; }

        public void Reset()
        {
            Heat = coolingDelayRemaining = 0;
            IsOverheated = false;
        }

        // Tính nóng/nguội theo số giây, không cộng một lượng cố định mỗi khung hình.
        public void Tick(WeaponThermalProfile profile, bool held, float seconds)
        {
            if (seconds <= 0) return;
            // Khi quá nhiệt, phải nguội hết mới mở bắn lại; giữ tay không ngăn bước làm nguội này.
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
