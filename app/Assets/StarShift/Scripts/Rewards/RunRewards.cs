using UnityEngine;
using StarShift.Persistence;
using System;

namespace StarShift.Rewards
{
    [DisallowMultipleComponent]
    public sealed class RunRewards : MonoBehaviour
    {
        public int LivesLost { get; private set; }
        public float PlaySeconds { get; private set; }
        public void RecordLifeLost() => LivesLost++;
        public void AddPlayTime(float seconds) => PlaySeconds += seconds;
        public int Score { get; private set; }
        public int Shards { get; private set; }
        public int EnemiesDestroyed { get; private set; }
        public int CoresCollected { get; private set; }
        public int SupplyCaptured { get; private set; }
        public void AddKill(int score) { Score += Mathf.Max(0, score); EnemiesDestroyed++; }
        public event Action ShardsChanged;
        public void AddShards(int amount) { Shards = checked(Shards + Mathf.Max(0, amount)); ShardsChanged?.Invoke(); }
        public RunStatsData Capture() => new RunStatsData { Score = Score, Shards = Shards,
            EnemiesDestroyed = EnemiesDestroyed, CoresCollected = CoresCollected, SupplyCaptured = SupplyCaptured, LivesLost = LivesLost, PlaySeconds = PlaySeconds };
        public void Restore(RunStatsData stats)
        {
            Score = stats.Score; Shards = stats.Shards; EnemiesDestroyed = stats.EnemiesDestroyed;
            CoresCollected = stats.CoresCollected; SupplyCaptured = stats.SupplyCaptured;
            LivesLost = stats.LivesLost; PlaySeconds = stats.PlaySeconds;
        }
        public void RecordCore() => CoresCollected++;
        public void RecordSupply() => SupplyCaptured++;
        public void ResetForSession() { Score = Shards = EnemiesDestroyed = CoresCollected = SupplyCaptured = LivesLost = 0; PlaySeconds = 0; }
    }
}
