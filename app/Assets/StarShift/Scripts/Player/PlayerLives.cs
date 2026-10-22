// PlayerLives — Số mạng người chơi; giữ số mạng hiện tại riêng với giới hạn mạng.
using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("StarShift/Player/Player Lives")]
    public sealed class PlayerLives : MonoBehaviour
    {
        // Số mạng đang có của riêng lượt chơi; bắt đầu từ 3, không phải máu HP.
        public int CurrentLives { get; private set; } = 3;
        // Trần số mạng có thể tích lũy; chỉnh trong Inspector, không phải số mạng khởi đầu.
        [SerializeField, Min(3)] private int maximumLives = 5;
        public event System.Action LifeLost;
        public int MaximumLives => maximumLives;
        // Chỉ cộng mạng khi còn sống và chưa đạt trần; trả false nếu không thể nhận.
        public bool TryAddLife()
        {
            if (CurrentLives <= 0 || CurrentLives >= maximumLives) return false;
            CurrentLives++; return true;
        }
        // Khôi phục số mạng đã lưu; từ chối dữ liệu nằm ngoài giới hạn.
        public void RestoreCheckpoint(int value)
        {
            if (value < 1 || value > maximumLives) throw new System.ArgumentOutOfRangeException(nameof(value));
            CurrentLives = value;
        }
        // Lượt chơi mới trở về số mạng khởi đầu, không dùng số mạng của lượt trước.
        internal void ResetForSession() => CurrentLives = 3;
        // Mỗi lần gọi giảm một mạng, chặn số âm và báo sự kiện mất mạng.
        internal void LoseLife() { CurrentLives = Mathf.Max(0, CurrentLives - 1); LifeLost?.Invoke(); }
    }
}
