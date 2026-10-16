using UnityEngine;

namespace StarShift.Combat
{
    // Chỉ enemy/destructible target opt-in; không dùng interface này cho friendly fire.
    public interface IPlayerProjectileTarget
    {
        void ReceivePlayerHit(int damage, GameObject owner);
    }
}
