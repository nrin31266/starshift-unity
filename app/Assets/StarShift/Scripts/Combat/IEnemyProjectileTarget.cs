// IEnemyProjectileTarget — Giao diện tàu nhận đạn địch.
using UnityEngine;

namespace StarShift.Combat
{
    public interface IEnemyProjectileTarget
    {
        // Đạn báo hit qua hợp đồng này; bên nhận tự xét khiên, bất tử và mất mạng.
        void ReceiveEnemyHit(GameObject owner);
    }
}
