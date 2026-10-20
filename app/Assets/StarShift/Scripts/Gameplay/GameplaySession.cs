using System;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Player;
using UnityEngine;

namespace StarShift.Gameplay
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-5)]
    public sealed class GameplaySession : MonoBehaviour
    {
        [SerializeField] private EncounterRunner runner;
        [SerializeField] private PlayerDamageLifecycle lifecycle;
        [SerializeField] private EnemyFireGate enemyFireGate;
        private PlayerGameplayGate playerGate;
        private InputSystemPointerInput pointer;
        private PulseLaserWeapon weapon;
        private RegionCampaignFlow region;
        public bool RequireMissionResults { get; set; }
        private float previousScale;
        private bool previousAudioPause;
        private bool ownsClock;
        private bool transitionHeld;
        private bool transitionBackgrounded;
        public StarShift.Persistence.CampaignLifecycle Persistence => runner.GetComponent<StarShift.Persistence.CampaignLifecycle>();
        public bool CanRetry => Persistence != null && Persistence.CanRetry;
        public void RetryEncounter() => Persistence?.RetryEncounter();
        public LevelFlow Flow => runner.Flow;
        public bool HasActiveRun => runner.HasActiveRun || (Persistence != null && (Persistence.IsAdvancingMission || Persistence.HasPendingRegionTransition)) || (region != null && (region.IsTravelling || region.DestinationReady));
        public bool CanPause => !IsTerminal && HasActiveRun && (Flow == null || !Flow.IsCinematic);
        public bool IsPaused { get; private set; }
        public bool IsTerminal => playerGate.State == PlayerGameplayState.Result || playerGate.State == PlayerGameplayState.GameOver;
        public bool IsClear => playerGate.State == PlayerGameplayState.Result;
        public bool IsCampaignVictory => IsClear && region != null && region.FindMission(Flow.Level.Id) == Flow.Level &&
            region.NextMission == null && Persistence.Mode != StarShift.Persistence.SessionMode.Replay;
        public event Action Changed;
        public event Action MissionStarted;
        public event Action<string, string, float> MissionTitleRequested;

        private void Awake()
        {
            region = GetComponent<RegionCampaignFlow>();
            playerGate = lifecycle.GetComponent<PlayerGameplayGate>();
            pointer = lifecycle.GetComponent<InputSystemPointerInput>();
            weapon = lifecycle.GetComponent<PulseLaserWeapon>();
        }

        private void OnEnable()
        {
            runner.RunStarted += OnRunStarted;
            runner.EncounterStarted += OnEncounterStarted;
            if (runner.Flow != null)
            {
                runner.Flow.TitleRequested += OnMissionTitle;
                runner.Flow.PresentationChanged += OnPresentationChanged;
            }
            runner.MissionCompleted += OnMissionCompleted;
            lifecycle.GameOver += OnGameOver;
        }

        private void OnMissionTitle(string title, string description, float seconds) => MissionTitleRequested?.Invoke(title, description, seconds);

        private void OnEncounterStarted(EncounterDefinition definition, int token) => Changed?.Invoke();
        private void OnPresentationChanged() => Changed?.Invoke();
        public void BeginMission() { if (!IsPaused && Flow != null) Flow.BeginApproach(); }

        public void Pause()
        {
            if (IsPaused || IsTerminal) return;
            // Cho phép pause khi run đang chạy hoặc khi save failure trong chuyển vùng cần giữ màn hình tạm dừng.
            if (!CanPause && (Persistence == null || !Persistence.HasPendingRegionTransition)) return;
            IsPaused = true;
            Freeze();
            Changed?.Invoke();
        }

        public void Resume()
        {
            if (!IsPaused || IsTerminal) return;
            pointer.CancelGesture();
            RestoreClock();
            IsPaused = false;
            Changed?.Invoke();
            if (Flow != null) Flow.RefreshMissionTitle();
        }

        public void ResetRuntime(bool preserveInput = false)
        {
            region?.Cancel();
            lifecycle.GetComponent<ShipAppearance>()?.ResetTrail();
            lifecycle.GetComponent<PlayerTemporaryEffects>()?.ResetForSession();
            lifecycle.GetComponent<EmergencyMissileReserve>()?.CancelPending();
            lifecycle.GetComponent<EmergencyMissilePresentation>()?.ResetForSession();
            lifecycle.GetComponentInChildren<SupportDrone>()?.ResetForSession();
            weapon.ResetForSession(preserveThermalState: preserveInput);
            ReleaseFriendlyProjectiles();
            RestoreClock();
            IsPaused = false;
            if(!preserveInput)pointer.CancelGesture();
            Changed?.Invoke();
        }

        private void OnRunStarted()
        {
            ResetRuntime(runner.PreservePlayerInput);
            if(!runner.PreservePlayerInput)MissionStarted?.Invoke();
        }

        private void OnMissionCompleted()
        {
            if (lifecycle.GetComponent<PlayerLives>().CurrentLives <= 0 || IsTerminal) return;
            if (RequireMissionResults && Persistence.Mode != StarShift.Persistence.SessionMode.DevTest)
            { ShowRegionComplete(); return; }
            if (region != null && region.QueueAdvance()) return;
            ShowRegionComplete();
        }

        public bool ContinueAfterResult()
        {
            if (!IsClear || IsCampaignVictory || Persistence.Mode != StarShift.Persistence.SessionMode.Campaign ||
                !Persistence.Flush()) return false;
            RestoreClock();
            playerGate.SetState(PlayerGameplayState.Active);
            return region.QueueAdvance();
        }

        public void HoldForShell()
        {
            runner.StopRun();
            ResetRuntime();
            playerGate.SetState(PlayerGameplayState.Entry);
            playerGate.SetCinematicLock(true);
            Freeze();
        }

        public void HoldForTransition() { transitionHeld = true; transitionBackgrounded = false; Freeze(); }
        public void ReleaseTransition()
        {
            if (!transitionHeld) return;
            transitionHeld = false;
            if (transitionBackgrounded && HasActiveRun && !IsTerminal)
            {
                IsPaused = true;
                Freeze();
                Changed?.Invoke();
            }
            transitionBackgrounded = false;
            if (playerGate != null && HasActiveRun && !IsPaused && !IsTerminal) RestoreClock();
        }

        private void OnApplicationFocus(bool focused) { if (!focused) PauseForBackground(); }
        private void OnApplicationPause(bool paused) { if (paused) PauseForBackground(); }
        private void PauseForBackground()
        {
            if (transitionHeld) transitionBackgrounded = true;
            pointer.CancelGesture();
            if (HasActiveRun && !IsPaused && !IsTerminal)
            {
                IsPaused = true;
                Freeze();
                Changed?.Invoke();
            }
        }

        public void PrepareMajorTravel()
        {
            pointer.CancelGesture();enemyFireGate.Block();ReleaseFriendlyProjectiles();
            lifecycle.GetComponent<EmergencyMissileReserve>()?.CancelPending();
            lifecycle.GetComponent<EmergencyMissilePresentation>()?.ResetForSession();
            lifecycle.GetComponent<PlayerTemporaryEffects>()?.ResetForSession();
        }
        public void ShowRegionReady(bool preserveInput = false)
        {
            RestoreClock();IsPaused=false;if(!preserveInput)pointer.CancelGesture();playerGate.SetState(PlayerGameplayState.Active);playerGate.SetCinematicLock(false);enemyFireGate.Block();Changed?.Invoke();
        }
        public void ShowRegionComplete() { playerGate.SetState(PlayerGameplayState.Result); EndCombat(); }

        private void OnGameOver() => EndCombat();

        private void EndCombat()
        {
            runner.StopRun();
            lifecycle.GetComponent<EmergencyMissileReserve>()?.CancelPending();
            lifecycle.GetComponent<EmergencyMissilePresentation>()?.ResetForSession();
            enemyFireGate.Block();
            ReleaseFriendlyProjectiles();
            IsPaused = false;
            Freeze();
            Changed?.Invoke();
        }

        private void ReleaseFriendlyProjectiles()
        {
            foreach (var shot in FindObjectsByType<PlayerProjectile>())
                if (shot.gameObject.scene == gameObject.scene) shot.Release();
        }

        private void Freeze()
        {
            pointer.CancelGesture();
            if (!ownsClock)
            {
                previousScale = Time.timeScale;
                previousAudioPause = AudioListener.pause;
                ownsClock = true;
            }
            Time.timeScale = 0;
            AudioListener.pause = true;
        }

        private void RestoreClock()
        {
            // RunStarted không được nhả clock khi loading vẫn chặn điều khiển.
            if (!ownsClock || transitionHeld) return;
            ownsClock = false;
            Time.timeScale = previousScale;
            AudioListener.pause = previousAudioPause;
        }

        private void OnDisable()
        {
            runner.RunStarted -= OnRunStarted;
            runner.EncounterStarted -= OnEncounterStarted;
            if (runner.Flow != null)
            {
                runner.Flow.TitleRequested -= OnMissionTitle;
                runner.Flow.PresentationChanged -= OnPresentationChanged;
            }
            runner.MissionCompleted -= OnMissionCompleted;
            lifecycle.GameOver -= OnGameOver;
            transitionHeld = false;
            RestoreClock();
            IsPaused = false;
        }
    }
}
