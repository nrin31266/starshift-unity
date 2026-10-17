using UnityEngine;

namespace StarShift.Combat
{
    public interface IEnemyProjectileTarget
    {
        void ReceiveEnemyHit(GameObject owner);
    }
}
