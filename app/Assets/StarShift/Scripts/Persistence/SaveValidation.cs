using System;
using System.Collections.Generic;
using StarShift.Combat;
using StarShift.Player;

namespace StarShift.Persistence
{
    public static class SaveValidation
    {
        public const int CurrentVersion = 2;
        public static bool IsValid(SaveEnvelope data, Func<string, string, bool> checkpointExists, int maximumLives)
        {
            if (data == null || data.SchemaVersion != CurrentVersion || !IsValid(data.Settings)) return false;
            var p = data.Profile;
            if (!IsValid(p)) return false;
            var c = data.Campaign;
            if (!data.HasCampaign) return true;
            if (c == null) return false;
            var s = c.Checkpoint;
            if (!Enum.IsDefined(typeof(CampaignStatus), c.Status) || s == null ||
                !checkpointExists(c.MissionId, s.EncounterId) || Array.IndexOf(p.UnlockedMissions, c.MissionId) < 0 ||
                s.Lives < 1 || s.Lives > maximumLives || s.Missiles < 0 || s.Missiles > 3 || !Enum.IsDefined(typeof(WeaponBranch), s.ActiveWeapon) ||
                c.ScoreBeforeMission < 0 || c.ScoreBeforeMission > long.MaxValue - int.MaxValue ||
                s.WeaponLevels == null || s.WeaponLevels.Length != 3 || c.EncounterShardHighWater < 0) return false;
            for (int i = 0; i < s.WeaponLevels.Length; i++)
                if (s.WeaponLevels[i] < (i == 0 ? 1 : 0) || s.WeaponLevels[i] > WeaponLoadout.MaximumLevel) return false;
            var stats = s.Stats;
            return s.WeaponLevels[(int)s.ActiveWeapon] > 0 && stats != null && stats.Score >= 0 && stats.Shards >= 0 &&
                stats.EnemiesDestroyed >= 0 && stats.CoresCollected >= 0 && stats.SupplyCaptured >= 0 && stats.LivesLost >= 0 && NonNegative(stats.PlaySeconds);
        }
        public static bool IsValid(PlayerProfile p)
        {
            if (p == null || p.StellarShardBalance < 0 || p.HighScore < 0 || p.UnlockedMissions == null || p.Records == null) return false;
            var ids = new HashSet<string>();
            foreach (var id in p.UnlockedMissions) if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) return false;
            ids.Clear();
            foreach (var r in p.Records)
                if (r == null || string.IsNullOrWhiteSpace(r.MissionId) || r.BestScore < 0 || !NonNegative(r.BestTimeSeconds) || !ids.Add(r.MissionId)) return false;
            ids.Clear();
            if (p.OwnedCosmetics != null)
                foreach (var id in p.OwnedCosmetics) if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) return false;
            if (p.EquippedCosmetics != null && p.EquippedCosmetics.Length > 0)
            {
                if (p.EquippedCosmetics.Length != 3) return false;
                foreach (var id in p.EquippedCosmetics) if (string.IsNullOrWhiteSpace(id)) return false;
            }
            return true;
        }
        public static bool IsValid(SettingsData s) => s != null && Unit(s.MusicVolume) && Unit(s.SfxVolume);
        private static bool NonNegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;
        private static bool Unit(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0 && value <= 1;
    }
}
