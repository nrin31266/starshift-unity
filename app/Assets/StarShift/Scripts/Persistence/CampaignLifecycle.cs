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
        public SessionMode Mode { get; private set; } = SessionMode.DevTest;
        public bool CanRetry => checkpoint != null && !session.IsClear && (session.IsPaused || session.IsTerminal);
        public SaveEnvelope PersistentSnapshot => progress.Snapshot;
        public SaveLoadStatus LoadStatus => progress.LoadStatus;
        public string Diagnostic => progress.Diagnostic;
        public bool Dirty => progress.Dirty;
        public string SavePath { get; private set; }

        private void Awake()
        {
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
                d => SaveValidation.IsValid(d, CheckpointExists, lives.MaximumLives));
        }
        private bool CheckpointExists(string mission, string encounter) => runner.Flow.Level.Id == mission &&
            Array.Exists(runner.Flow.Level.Encounters, e => e != null && e.Id == encounter);

        public void UseTestStorage(string path)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Mode == SessionMode.Campaign && !Flush()) throw new IOException(Diagnostic);
            ExitIsolated();
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
            if (launching) return;
            if (!Flush()) throw new IOException(Diagnostic);
            Mode = SessionMode.DevTest;
            checkpoint = null;
        }
        private CheckpointData Capture() => new CheckpointData { EncounterId = runner.Current.Id,
            Lives = lives.CurrentLives, Missiles = missiles.Current, ActiveWeapon = loadout.ActiveBranch,
            WeaponLevels = loadout.CaptureLevels(), Stats = rewards.Capture() };
        public bool NewGame()
        {
            if (progress.Snapshot == null) return false;
            var level = runner.Flow.Level;
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
            Mode = SessionMode.Campaign;
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
            checkpoint = null;
            Launch("LEVEL_START", null, true);
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
            checkpoint = null;
        }
        public bool RetryEncounter()
        {
            if (!CanRetry) return false;
            if (!Flush()) return false;
            Launch(checkpoint.EncounterId, checkpoint, true);
            return true;
        }
        private void Launch(string start, CheckpointData state, bool skipIntro)
        {
            launching = true;
            restoring = state != null;
            try
            {
                runner.StopRun();
                player.RestoreCheckpoint(state?.Lives ?? 3);
                // RunStarted reset owners trước; restore phải xảy ra trước BeginEncounter/capture.
                runner.StartRun(start, false, skipIntro, null, null, () =>
                {
                    if (state == null) return;
                    loadout.Restore(state.ActiveWeapon, state.WeaponLevels);
                    lives.RestoreCheckpoint(state.Lives);
                    missiles.RestoreCheckpoint(state.Missiles);
                    rewards.Restore(state.Stats);
                });
                player.GetComponent<RelativeDragMovement>().ResetForCheckpoint(runner.Flow.Direction);
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
        private void OnShardsChanged()
        {
            if (Mode != SessionMode.Campaign || checkpoint == null) return;
            progress.CreditEncounterShards(rewards.Shards - checkpoint.Stats.Shards);
        }
        private void OnMissionCompleted()
        {
            if (Mode != SessionMode.Campaign || lives.CurrentLives <= 0) return;
            progress.CommitClear(rewards.Score, rewards.PlaySeconds);
            Flush();
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
