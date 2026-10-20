using System;
using System.IO;
using StarShift.Encounters;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.Rewards;
using UnityEngine;

namespace StarShift.Persistence
{
    [DisallowMultipleComponent]
    public sealed class CampaignLifecycle : MonoBehaviour
    {
        [SerializeField] private EncounterRunner runner;
        [SerializeField] private GameplaySession session;
        [SerializeField] private PlayerDamageLifecycle player;
        private RunRewards rewards;
        private WeaponLoadout loadout;
        private PlayerLives lives;
        private EmergencyMissileReserve missiles;
        private CampaignProgress progress;
        private CheckpointData checkpoint;
        private bool launching;
        private bool restoring;
        private RegionCampaignFlow region;
        private LevelDefinition testFixture;
        private int isolatedShardBalance;
        public int ShardBalance => Mode == SessionMode.Campaign ? progress.ShardBalance : isolatedShardBalance;
        public bool IsAdvancingMission { get; private set; }
        public SessionMode Mode { get; private set; } = SessionMode.DevTest;
        public bool CanRetry => checkpoint != null && !session.IsClear && (session.IsPaused || session.IsTerminal) &&
            (region == null || !region.IsTravelling || !region.IsRegionTravel);
        public SaveEnvelope PersistentSnapshot => progress.Snapshot;
        public SaveLoadStatus LoadStatus => progress.LoadStatus;
        public string Diagnostic => progress.Diagnostic;
        public bool Dirty => progress.Dirty;
        public string SavePath { get; private set; }

        private void Awake()
        {
            region = GetComponent<RegionCampaignFlow>();
            rewards = runner.GetComponent<RunRewards>();
            loadout = player.GetComponent<WeaponLoadout>();
            lives = player.GetComponent<PlayerLives>();
            missiles = player.GetComponent<EmergencyMissileReserve>();
            OpenStorage(Path.Combine(Application.persistentDataPath, "starshift-save-v1.json"));
        }
        private void OnEnable()
        {
            runner.EncounterStarted += OnEncounterStarted;
            runner.MissionCompleted += OnMissionCompleted;
            rewards.ShardsChanged += OnShardsChanged;
            lives.LifeLost += rewards.RecordLifeLost;
            session.Changed += OnSessionChanged;
        }
        private void OpenStorage(string path)
        {
            SavePath = path;
            progress = new CampaignProgress(new JsonSaveStore(path),
                d => SaveValidation.IsValid(d, CheckpointExists, lives.MaximumLives) && RegionStatusValid(d));
        }
        private bool CheckpointExists(string mission, string encounter)
        {
            var level = testFixture != null && testFixture.Id == mission ? testFixture : region != null ? region.FindMission(mission) : null;
            if (level == null && runner.Flow.Level.Id == mission) level = runner.Flow.Level;
            return level != null && Array.Exists(level.Encounters, e => e != null && e.Id == encounter);
        }

        private bool RegionStatusValid(SaveEnvelope data)
        {
            if (!data.HasCampaign) return true;
            var status = data.Campaign.Status;
            if (status != CampaignStatus.RegionTransition && status != CampaignStatus.RegionReady && status != CampaignStatus.CampaignCompleted) return true;
            if (region == null) return false;
            var level = region.FindMission(data.Campaign.MissionId);
            if (level == null || !Array.Exists(level.Encounters, e => e.Id == data.Campaign.Checkpoint.EncounterId && e.Completion == EncounterCompletion.Boss)) return false;
            var missions = region.Missions;
            int index = Array.IndexOf(missions, level);
            return status == CampaignStatus.CampaignCompleted ? index == missions.Length - 1 :
                index >= 0 && index + 1 < missions.Length && missions[index + 1].RegionId != level.RegionId;
        }

        public void UseTestStorage(string path, LevelDefinition fixture = null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Mode == SessionMode.Campaign && !Flush()) throw new IOException(Diagnostic);
            ExitIsolated();
            testFixture = fixture;
            if (fixture != null) runner.Flow.LoadLevel(fixture);
            OpenStorage(path);
#else
            throw new InvalidOperationException("Test storage requires a development build.");
#endif
        }
        public bool TryBoot()
        {
#if UNITY_EDITOR
            return false;
#else
            if (Continue()) return true;
            if (LoadStatus == SaveLoadStatus.Missing) return NewGame();
            runner.StopRun();
            return true;
#endif
        }
        // Mọi raw/dev start thu hồi quyền ghi trước RunStarted hoặc reward callback.
        public void OnRunnerStarting()
        {
            region?.Cancel();
            if (launching) return;
            if (!Flush()) throw new IOException(Diagnostic);
            Mode = SessionMode.DevTest;
            isolatedShardBalance = 0;
            checkpoint = null;
        }
        private CheckpointData Capture() => new CheckpointData { EncounterId = runner.Current.Id,
            Lives = lives.CurrentLives, Missiles = missiles.Current, ActiveWeapon = loadout.ActiveBranch,
            WeaponLevels = loadout.CaptureLevels(), Stats = rewards.Capture() };
        public bool NewGame()
        {
            if (progress.Snapshot == null) return false;
            var level = testFixture != null ? testFixture : region != null ? region.Missions[0] : runner.Flow.Level;
            runner.StopRun(); runner.Flow.LoadLevel(level);
            var initial = new CheckpointData { EncounterId = level.Encounters[0].Id, Lives = 3, Missiles = 1,
                WeaponLevels = new[] { 1, 0, 0 }, Stats = new RunStatsData() };
            if (!progress.NewGame(level.Id, initial)) return false;
            Mode = SessionMode.Campaign;
            checkpoint = initial;
            Launch("LEVEL_START", initial, false);
            return true;
        }
        public bool Continue()
        {
            if (Mode == SessionMode.Campaign && !Flush()) return false;
            if (!progress.Reload()) return false;
            var snapshot = progress.Snapshot;
            var c = snapshot != null && snapshot.HasCampaign ? snapshot.Campaign : null;
            if (c == null || c.Status == CampaignStatus.MissionCleared) return false;
            runner.StopRun();
            var level = testFixture != null && testFixture.Id == c.MissionId ? testFixture : region != null ? region.FindMission(c.MissionId) : runner.Flow.Level;
            if (level == null) return false;
            runner.Flow.LoadLevel(level);
            Mode = SessionMode.Campaign;
            if (c.Status == CampaignStatus.RegionReady || c.Status == CampaignStatus.RegionTransition)
            {
                session.ResetRuntime(); player.RestoreCheckpoint(c.Checkpoint.Lives);
                loadout.Restore(c.Checkpoint.ActiveWeapon, c.Checkpoint.WeaponLevels);
                missiles.RestoreCheckpoint(c.Checkpoint.Missiles); rewards.Restore(c.Checkpoint.Stats);
                if (c.Status == CampaignStatus.RegionReady) region.RestoreDestination(); else { runner.Flow.ResetRegionDeparture(); region.QueueAdvance(); }
                return true;
            }
            if (c.Status == CampaignStatus.CampaignCompleted)
            {
                session.ResetRuntime(); player.RestoreCheckpoint(c.Checkpoint.Lives);
                loadout.Restore(c.Checkpoint.ActiveWeapon, c.Checkpoint.WeaponLevels);
                missiles.RestoreCheckpoint(c.Checkpoint.Missiles); rewards.Restore(c.Checkpoint.Stats);
                session.ShowRegionComplete(); return true;
            }
            checkpoint = c.Checkpoint;
            Launch(checkpoint.EncounterId, checkpoint, true);
            return true;
        }
        public bool BeginReplay()
        {
            var profile = progress.Snapshot?.Profile;
            if (profile == null || Array.IndexOf(profile.UnlockedMissions, runner.Flow.Level.Id) < 0) return false;
            if (!Flush()) return false;
            Mode = SessionMode.Replay;
            isolatedShardBalance = 0;
            checkpoint = null;
            var preset = runner.Flow.Level.ReplayPreset;
            Launch("LEVEL_START", preset != null ? new CheckpointData { EncounterId = runner.Flow.Level.Encounters[0].Id, Lives = preset.Lives, Missiles = preset.Missiles, ActiveWeapon = preset.ActiveWeapon, WeaponLevels = (int[])preset.WeaponLevels.Clone(), Stats = new RunStatsData() } : null, true);
            return true;
        }
        public void ExitIsolated()
        {
            if (!Flush()) throw new IOException(Diagnostic);
            runner.StopRun();
            session.ResetRuntime();
            player.RestoreCheckpoint(3);
            player.GetComponent<PlayerGameplayGate>().SetState(PlayerGameplayState.Entry);
            Mode = SessionMode.DevTest;
            isolatedShardBalance = 0;
            checkpoint = null;
        }
        public bool RetryEncounter()
        {
            if (!CanRetry) return false;
            if (!Flush()) return false;
            if (Mode == SessionMode.Campaign && region != null && region.IsMissionTravel)
            {
                // Clear đã commit entry kế tiếp; Retry không được quay lại mission vừa resolve.
                var next = region.NextMission;
                checkpoint = progress.Snapshot.Campaign.Checkpoint;
                runner.StopRun(); runner.Flow.LoadLevel(next);
            }
            Launch(checkpoint.EncounterId, checkpoint, true);
            return true;
        }
        private void Launch(string start, CheckpointData state, bool skipIntro, bool missionArrival = false, bool preservePoseAndInput = false)
        {
            launching = true;
            restoring = state != null;
            try
            {
                runner.StopRun();
                if(!preservePoseAndInput)player.RestoreCheckpoint(state?.Lives ?? 3);
                // RunStarted reset owners trước; restore phải xảy ra trước BeginEncounter/capture.
                runner.StartRun(start, false, skipIntro, null, null, () =>
                {
                    if (state == null) return;
                    loadout.Restore(state.ActiveWeapon, state.WeaponLevels);
                    lives.RestoreCheckpoint(state.Lives);
                    missiles.RestoreCheckpoint(state.Missiles);
                    rewards.Restore(state.Stats);
                }, missionArrival, preservePoseAndInput);
                if(!preservePoseAndInput)player.GetComponent<RelativeDragMovement>().ResetForCheckpoint(runner.Flow.Direction);
                else FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0);
            }
            finally { launching = restoring = false; }
        }
        private void OnEncounterStarted(EncounterDefinition definition, int token)
        {
            if (restoring && checkpoint != null && definition.Id == checkpoint.EncounterId) return;
            // Không ghi checkpoint vô hiệu nếu handoff trùng frame death/GameOver.
            if (lives.CurrentLives <= 0) return;
            checkpoint = Capture();
            if (Mode != SessionMode.Campaign) return;
            progress.Capture(checkpoint);
            Flush();
        }
        private void OnShardsChanged(int amount)
        {
            if (Mode != SessionMode.Campaign)
            {
                isolatedShardBalance = checked(isolatedShardBalance + amount);
                return;
            }
            if (checkpoint == null) return;
            progress.CreditEncounterShards(rewards.Shards - checkpoint.Stats.Shards);
        }
        private void OnMissionCompleted()
        {
            if (Mode != SessionMode.Campaign || lives.CurrentLives <= 0) return;
            progress.CommitClear(rewards.Score, rewards.PlaySeconds);
            if (region != null && testFixture == null)
            {
                var next = region.NextMission;
                if (next != null && next.RegionId == runner.Flow.Level.RegionId)
                {
                    var entry = Capture(); entry.EncounterId = next.Encounters[0].Id; entry.Stats = new RunStatsData();
                    progress.AdvanceMission(next.Id, entry);
                }
                else { progress.Capture(Capture()); progress.SetRegionStatus(next != null ? CampaignStatus.RegionTransition : CampaignStatus.CampaignCompleted); }
            }
            Flush();
        }
        public void AdvanceMission(LevelDefinition next, bool preservePoseAndInput = false)
        {
            IsAdvancingMission = true;
            try
            {
                var state = Capture(); state.EncounterId = next.Encounters[0].Id; state.Stats = new RunStatsData();
                if (Mode == SessionMode.Campaign) state = progress.Snapshot.Campaign.Checkpoint;
                runner.StopRun(); runner.Flow.LoadLevel(next); checkpoint = state; Launch("LEVEL_START", state, true, !preservePoseAndInput, preservePoseAndInput);
            }
            finally { IsAdvancingMission = false; }
        }
        public void CompleteRegionTravel()
        {
            var next = region.NextMission;
            if (next == null) return;
            if (Mode == SessionMode.Campaign)
            {
                var entry = Capture(); entry.EncounterId = next.Encounters[0].Id; entry.Stats = new RunStatsData();
                progress.AdvanceMission(next.Id, entry);
                if (!Flush()) { session.ShowRegionComplete(); return; }
            }
            AdvanceMission(next, true);
        }
        private void FixedUpdate()
        {
            if (runner.HasActiveRun && !runner.Flow.IsCinematic && !session.IsTerminal) rewards.AddPlayTime(Time.fixedDeltaTime);
        }
        private void OnSessionChanged() { if (session.IsPaused || session.IsTerminal) Flush(); }
        public bool Flush() => Mode != SessionMode.Campaign || progress.Flush();
        public bool SetSettings(SettingsData value) => Mode == SessionMode.Campaign && progress.SetSettings(value);
        private void OnApplicationPause(bool paused) { if (paused) Flush(); }
        private void OnApplicationQuit() => Flush();
        private void OnDisable()
        {
            Flush();
            runner.EncounterStarted -= OnEncounterStarted;
            runner.MissionCompleted -= OnMissionCompleted;
            rewards.ShardsChanged -= OnShardsChanged;
            lives.LifeLost -= rewards.RecordLifeLost;
            session.Changed -= OnSessionChanged;
        }
        [ContextMenu("Inspect Save State")]
        private void Inspect() => Debug.Log($"Session={Mode}, load={LoadStatus}, dirty={Dirty}, save={SavePath}\n{JsonUtility.ToJson(PersistentSnapshot, true)}\n{Diagnostic}", this);
    }
}
