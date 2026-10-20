using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarShift.Persistence
{
    // Authority của persistent data; gameplay chỉ gửi state đã resolve.
    public sealed class CampaignProgress
    {
        private SaveEnvelope data;
        private readonly JsonSaveStore store;
        private readonly Func<SaveEnvelope, bool> validate;
        public SaveLoadStatus LoadStatus { get; private set; }
        public bool Dirty { get; private set; }
        public string Diagnostic => store.Diagnostic;
        public SaveEnvelope Snapshot => Copy(data);
        public int ShardBalance => data?.Profile.StellarShardBalance ?? 0;
        public CampaignProgress(JsonSaveStore store, Func<SaveEnvelope, bool> validate)
        {
            this.store = store; this.validate = validate;
            Reload();
        }
        public bool Reload()
        {
            if (Dirty) return false;
            LoadStatus = store.Load(validate, out var loaded);
            data = loaded;
            if (LoadStatus == SaveLoadStatus.Missing)
                data = new SaveEnvelope { SchemaVersion = SaveValidation.CurrentVersion,
                    Profile = new PlayerProfile { UnlockedMissions = Array.Empty<string>(), Records = Array.Empty<MissionRecord>() },
                    Settings = SettingsData.Defaults() };
            return data != null;
        }
        public bool NewGame(string mission, CheckpointData initial)
        {
            if (data == null) return false;
            var previous = Copy(data);
            var wasDirty = Dirty;
            var unlocked = new List<string>(data.Profile.UnlockedMissions);
            if (!unlocked.Contains(mission)) unlocked.Add(mission);
            data.Profile.UnlockedMissions = unlocked.ToArray();
            data.HasCampaign = true;
            data.Campaign = new CampaignSave { MissionId = mission, Checkpoint = Copy(initial) };
            Dirty = true;
            return CommitOrRestore(previous, wasDirty);
        }
        public bool RecoverNewGame(string mission, CheckpointData initial)
        {
            if (data != null || LoadStatus != SaveLoadStatus.Invalid) return false;
            var candidate = store.ReadForRecovery();
            // Chỉ explicit recovery mới salvage metadata; Continue không partial-load campaign lỗi.
            data = new SaveEnvelope { SchemaVersion = SaveValidation.CurrentVersion,
                Profile = SaveValidation.IsValid(candidate?.Profile) ? Copy(candidate.Profile) :
                    new PlayerProfile { UnlockedMissions = Array.Empty<string>(), Records = Array.Empty<MissionRecord>() },
                Settings = SaveValidation.IsValid(candidate?.Settings) ? Copy(candidate.Settings) : SettingsData.Defaults() };
            if (NewGame(mission, initial)) { LoadStatus = SaveLoadStatus.Loaded; return true; }
            data = null;
            return false;
        }
        public void Capture(CheckpointData checkpoint)
        {
            data.Campaign.Checkpoint = Copy(checkpoint);
            data.Campaign.EncounterShardHighWater = 0;
            Dirty = true;
        }
        public void CreditEncounterShards(int cumulative)
        {
            var campaign = data.Campaign;
            int delta = cumulative - campaign.EncounterShardHighWater;
            if (delta <= 0) return;
            data.Profile.StellarShardBalance = checked(data.Profile.StellarShardBalance + delta);
            campaign.EncounterShardHighWater = cumulative;
            Dirty = true;
        }
        public void CommitClear(int score, float playSeconds = 0)
        {
            if (data.Campaign.Status != CampaignStatus.Active) return;
            data.Campaign.Status = CampaignStatus.MissionCleared;
            data.Profile.HighScore = Math.Max(data.Profile.HighScore, score);
            var records = new List<MissionRecord>(data.Profile.Records);
            var record = records.Find(r => r.MissionId == data.Campaign.MissionId);
            if (record == null) { record = new MissionRecord { MissionId = data.Campaign.MissionId }; records.Add(record); }
            record.BestScore = Math.Max(record.BestScore, score);
            if (playSeconds > 0 && (record.BestTimeSeconds <= 0 || playSeconds < record.BestTimeSeconds))
                record.BestTimeSeconds = playSeconds;
            data.Profile.Records = records.ToArray();
            Dirty = true;
        }
        public void AdvanceMission(string mission, CheckpointData checkpoint)
        {
            var unlocked = new List<string>(data.Profile.UnlockedMissions);
            if (!unlocked.Contains(mission)) unlocked.Add(mission);
            data.Profile.UnlockedMissions = unlocked.ToArray();
            data.Campaign = new CampaignSave { MissionId = mission, Checkpoint = Copy(checkpoint) };
            Dirty = true;
        }
        public bool TryCommitClear(int score, float seconds, CheckpointData resolved,
            string nextMission, CheckpointData nextCheckpoint, CampaignStatus status)
        {
            var previous = Copy(data);
            var wasDirty = Dirty;
            CommitClear(score, seconds);
            if (nextMission != null) AdvanceMission(nextMission, nextCheckpoint);
            else { Capture(resolved); SetRegionStatus(status); }
            return CommitOrRestore(previous, wasDirty);
        }

        private bool CommitOrRestore(SaveEnvelope previous, bool wasDirty)
        {
            if (Flush()) return true;
            data = previous;
            Dirty = wasDirty;
            return false;
        }
        public bool TryAdvanceMission(string mission, CheckpointData checkpoint)
        {
            var backup = Copy(data);
            var wasDirty = Dirty;
            var unlocked = new List<string>(data.Profile.UnlockedMissions);
            if (!unlocked.Contains(mission)) unlocked.Add(mission);
            data.Profile.UnlockedMissions = unlocked.ToArray();
            data.Campaign = new CampaignSave { MissionId = mission, Checkpoint = Copy(checkpoint) };
            Dirty = true;
            if (!Flush())
            {
                data = backup;
                Dirty = wasDirty;
                return false;
            }
            return true;
        }
        public void SetRegionStatus(CampaignStatus status) { data.Campaign.Status = status; Dirty = true; }
        public bool SetSettings(SettingsData settings)
        {
            if (data == null || !SaveValidation.IsValid(settings)) return false;
            var previous = Copy(data);
            var wasDirty = Dirty;
            data.Settings = Copy(settings);
            Dirty = true;
            return CommitOrRestore(previous, wasDirty);
        }
        public bool EquipCosmetic(string id, int cost, int slot, string[] defaults)
        {
            if (data == null || string.IsNullOrWhiteSpace(id) || cost < 0 || slot < 0 || slot >= 3 || defaults == null || defaults.Length != 3) return false;
            var profile = data.Profile;
            var owned = new List<string>(profile.OwnedCosmetics ?? Array.Empty<string>());
            foreach (var item in defaults) if (!owned.Contains(item)) owned.Add(item);
            bool purchased = owned.Contains(id);
            if (!purchased && profile.StellarShardBalance < cost) return false;
            var previous = Copy(data);
            var wasDirty = Dirty;
            if (!purchased) { profile.StellarShardBalance -= cost; owned.Add(id); }
            profile.OwnedCosmetics = owned.ToArray();
            if (profile.EquippedCosmetics == null || profile.EquippedCosmetics.Length == 0) profile.EquippedCosmetics = (string[])defaults.Clone();
            profile.EquippedCosmetics[slot] = id;
            Dirty = true;
            return CommitOrRestore(previous, wasDirty);
        }
        public bool Flush()
        {
            if (!Dirty) return true;
            if (!store.Write(data, validate)) return false;
            Dirty = false;
            return true;
        }
        public static T Copy<T>(T value) where T : class => value == null ? null : JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    }
}
