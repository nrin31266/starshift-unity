// WeaponThermalProfile — Cấu hình sức chứa nhiệt và tốc độ nóng/nguội; nhiệt hiện tại nằm ở WeaponHeatState.
using UnityEngine;

namespace StarShift.Combat
{
    [CreateAssetMenu(menuName = "StarShift/Weapons/Thermal Profile")]
    public sealed class WeaponThermalProfile : ScriptableObject
    {
        // Ngưỡng nhiệt khóa bắn; tăng giá trị thì giữ bắn được lâu hơn nếu tốc độ nóng giữ nguyên.
        [SerializeField, Min(1)] private float capacity = 100f;
        // Nhiệt tăng mỗi giây giữ bắn; tăng giá trị làm quá nhiệt nhanh hơn.
        [SerializeField, Min(.01f)] private float heatPerSecond = 18f;
        // Thời gian chờ sau ngừng bắn trước khi bắt đầu nguội bình thường.
        [SerializeField, Min(0)] private float coolingDelay = .2f;
        [SerializeField, Min(.01f)] private float coolRate = 18f;
        // Tốc độ nguội khi đã quá nhiệt; khác tốc độ nguội khi chỉ thả tay.
        [SerializeField, Min(.01f)] private float overheatCoolRate = 65f;

        public float Capacity => capacity;
        public float HeatPerSecond => heatPerSecond;
        public float CoolingDelay => coolingDelay;
        public float CoolRate => coolRate;
        public float OverheatCoolRate => overheatCoolRate;

        private void OnValidate()
        {
            capacity = Mathf.Max(1, capacity);
            heatPerSecond = Mathf.Max(.01f, heatPerSecond);
            coolingDelay = Mathf.Max(0, coolingDelay);
            coolRate = Mathf.Max(.01f, coolRate);
            overheatCoolRate = Mathf.Max(.01f, overheatCoolRate);
        }
    }
}
