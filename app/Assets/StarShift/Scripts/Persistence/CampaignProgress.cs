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
            var unlocked = new List<string>(data.Profile.UnlockedMissions);
            if (!unlocked.Contains(mission)) unlocked.Add(mission);
            data.Profile.UnlockedMissions = unlocked.ToArray();
            data.HasCampaign = true;
            data.Campaign = new CampaignSave { MissionId = mission, Checkpoint = Copy(initial) };
            Dirty = true;
            return Flush();
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
            if (data.Campaign.Status == CampaignStatus.MissionCleared) return;
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
        public bool SetSettings(SettingsData settings)
        {
            if (data == null || !SaveValidation.IsValid(settings)) return false;
            data.Settings = Copy(settings);
            Dirty = true;
            return Flush();
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
