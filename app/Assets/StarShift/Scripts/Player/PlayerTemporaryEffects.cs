// PlayerTemporaryEffects — Khiên và hiệu ứng tạm thời; giữ trạng thái trong lượt chơi thay vì sửa asset vật phẩm.
using StarShift.Rewards;
using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerGameplayGate))]
    public sealed class PlayerTemporaryEffects : MonoBehaviour
    {
        private PlayerGameplayGate gate;
        private float fireRateMultiplier = 1;
        // Khiên đang có chỉ hấp thụ một hit; không thay thế số mạng của tàu.
        public bool HasShield { get; private set; }
        public float OverdriveRemaining { get; private set; }
        public float DroneRemaining { get; private set; }
        // Hệ số tăng nhịp bắn chỉ có hiệu lực khi thời gian Overdrive còn lớn hơn 0.
        public float FireRateMultiplier => OverdriveRemaining > 0 ? fireRateMultiplier : 1;
        public int ShieldBreaks { get; private set; }
        private void Awake() => gate = GetComponent<PlayerGameplayGate>();
        private void OnEnable() => gate.StateChanged += OnStateChanged;
        private void OnDisable() { gate.StateChanged -= OnStateChanged; Clear(); }
        private void OnStateChanged()
        {
            if (gate.State == PlayerGameplayState.Dying || gate.State == PlayerGameplayState.GameOver || gate.State == PlayerGameplayState.Result) Clear();
        }
        // Áp hiệu ứng từ dữ liệu vật phẩm; thời gian và hệ số lấy từ asset cấu hình.
        public bool Apply(PickupDefinition item)
        {
            switch (item.Kind)
            {
                case PickupKind.Shield: HasShield = true; return true;
                case PickupKind.Overdrive:
                    OverdriveRemaining = item.Duration; fireRateMultiplier = item.FireRateMultiplier; return true;
                case PickupKind.SupportDrone: DroneRemaining = item.Duration; return true;
                default: return false;
            }
        }
        // Khiên chỉ hấp thụ một hit; PlayerDamageLifecycle xử lý khoảng bảo vệ sau khi khiên vỡ.
        public bool TryAbsorb()
        {
            if (!HasShield) return false;
            HasShield = false; ShieldBreaks++; return true;
        }
        private void FixedUpdate()
        {
            if (!gate.CanFire) return;
            OverdriveRemaining = Mathf.Max(0, OverdriveRemaining - Time.fixedDeltaTime);
            DroneRemaining = Mathf.Max(0, DroneRemaining - Time.fixedDeltaTime);
        }
        public void Clear() { HasShield = false; OverdriveRemaining = DroneRemaining = 0; fireRateMultiplier = 1; }
        public void ResetForSession() { Clear(); ShieldBreaks = 0; }
    }
}
