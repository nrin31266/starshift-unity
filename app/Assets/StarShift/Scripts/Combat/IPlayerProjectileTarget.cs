// IPlayerProjectileTarget — Giao diện mục tiêu nhận đạn người chơi.
using UnityEngine;

namespace StarShift.Combat
{
    // Chỉ enemy/destructible target opt-in; không dùng interface này cho friendly fire.
    public interface IPlayerProjectileTarget
    {
        // Hợp đồng nhận sát thương chung để đạn không phải viết riêng cho từng loại mục tiêu.
        void ReceivePlayerHit(int damage, GameObject owner);
    }
}
