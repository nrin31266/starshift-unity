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
        private float previousScale;
        private bool previousAudioPause;
        private bool ownsClock;
        public StarShift.Persistence.CampaignLifecycle Persistence => runner.GetComponent<StarShift.Persistence.CampaignLifecycle>();
        public bool CanRetry => Persistence != null && Persistence.CanRetry;
        public void RetryEncounter() => Persistence?.RetryEncounter();
        public LevelFlow Flow => runner.Flow;
        public bool HasActiveRun => runner.HasActiveRun;
        public bool CanPause => (region == null || !region.IsTravelling) && !IsTerminal && runner.HasActiveRun && (Flow == null || !Flow.IsCinematic);
        public bool IsPaused { get; private set; }
        public bool IsTerminal => playerGate.State == PlayerGameplayState.Result || playerGate.State == PlayerGameplayState.GameOver;
        public bool IsClear => playerGate.State == PlayerGameplayState.Result;
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
            if (IsPaused || !CanPause) return;
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

        public void ResetRuntime()
        {
            region?.Cancel();
            lifecycle.GetComponent<PlayerTemporaryEffects>()?.ResetForSession();
            lifecycle.GetComponent<EmergencyMissileReserve>()?.CancelPending();
            lifecycle.GetComponent<EmergencyMissilePresentation>()?.ResetForSession();
            lifecycle.GetComponentInChildren<SupportDrone>()?.ResetForSession();
            weapon.ResetForSession();
            ReleaseFriendlyProjectiles();
            RestoreClock();
            IsPaused = false;
            pointer.CancelGesture();
            Changed?.Invoke();
        }

        private void OnRunStarted()
        {
            ResetRuntime();
            MissionStarted?.Invoke();
        }

        private void OnMissionCompleted()
        {
            if (lifecycle.GetComponent<PlayerLives>().CurrentLives <= 0 || IsTerminal) return;
            if (region != null && region.QueueAdvance()) return;
            ShowRegionComplete();
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
            if (!ownsClock) return;
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
            RestoreClock();
            IsPaused = false;
        }
    }
}
