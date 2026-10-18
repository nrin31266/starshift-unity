using UnityEngine;

namespace StarShift.Rewards
{
    [DisallowMultipleComponent]
    public sealed class RunRewards : MonoBehaviour
    {
        public int Score { get; private set; }
        public int Shards { get; private set; }
        public int EnemiesDestroyed { get; private set; }
        public int CoresCollected { get; private set; }
        public int SupplyCaptured { get; private set; }
        public void AddKill(int score) { Score += Mathf.Max(0, score); EnemiesDestroyed++; }
        public void AddShards(int amount) => Shards += Mathf.Max(0, amount);
        public void RecordCore() => CoresCollected++;
        public void RecordSupply() => SupplyCaptured++;
        public void ResetForSession() { Score = Shards = EnemiesDestroyed = CoresCollected = SupplyCaptured = 0; }
    }
}
