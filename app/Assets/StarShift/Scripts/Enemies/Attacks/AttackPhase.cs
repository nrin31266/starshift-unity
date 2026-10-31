// AttackPhase — Các bước chuẩn bị, khóa mục tiêu, ra đòn và hồi để hiệu ứng đọc trạng thái tấn công.
namespace StarShift.Enemies
{
    // Chuỗi chuẩn cho mọi skill có telegraph: chuẩn bị, khóa mục tiêu, ra đòn, hồi.
    public enum AttackPhase { Idle, Prepare, Lock, Attack, Recovery }
}
