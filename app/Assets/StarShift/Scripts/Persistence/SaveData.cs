using System;
using StarShift.Combat;

namespace StarShift.Persistence
{
    public enum SessionMode { Campaign, Replay, DevTest }
    public enum CampaignStatus { Active, MissionCleared, RegionTransition, RegionReady, CampaignCompleted }
    public enum SaveLoadStatus { Missing, Loaded, RecoveredBackup, Invalid, UnsupportedVersion, IoFailure }

    [Serializable]
    public sealed class RunStatsData
    {
        public int Score, Shards, EnemiesDestroyed, CoresCollected, SupplyCaptured, LivesLost;
        public float PlaySeconds;
    }

    [Serializable]
    public sealed class CheckpointData
    {
        public string EncounterId;
        public int Lives;
        public int Missiles;
        public WeaponBranch ActiveWeapon;
        public int[] WeaponLevels;
        public RunStatsData Stats;
    }

    [Serializable]
    public sealed class CampaignSave
    {
        public string MissionId;
        public CampaignStatus Status;
        public CheckpointData Checkpoint;
        public int EncounterShardHighWater;
    }

    [Serializable]
    public sealed class MissionRecord
    {
        public string MissionId;
        public int BestScore;
        public float BestTimeSeconds;
    }

    [Serializable]
    public sealed class PlayerProfile
    {
        public int StellarShardBalance;
        public int HighScore;
        public string[] UnlockedMissions;
        public MissionRecord[] Records;
    }

    [Serializable]
    public sealed class SettingsData
    {
        public float MusicVolume, SfxVolume;
        public bool VibrationEnabled, ScreenShakeEnabled;
        public static SettingsData Defaults() => new SettingsData
        { MusicVolume = 1, SfxVolume = 1, VibrationEnabled = true, ScreenShakeEnabled = true };
    }

    [Serializable]
    public sealed class SaveEnvelope
    {
        public int SchemaVersion;
        public bool HasCampaign;
        public CampaignSave Campaign;
        public PlayerProfile Profile;
        public SettingsData Settings;
    }
}
