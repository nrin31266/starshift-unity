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
        private float previousScale;
        private bool previousAudioPause;
        private bool ownsClock;
        public bool IsPaused { get; private set; }
        public bool IsTerminal => playerGate.State == PlayerGameplayState.Result || playerGate.State == PlayerGameplayState.GameOver;
        public bool IsClear => playerGate.State == PlayerGameplayState.Result;
        public event Action Changed;
        public event Action MissionStarted;
        public event Action<string> MissionTitleRequested;

        private void Awake()
        {
            playerGate = lifecycle.GetComponent<PlayerGameplayGate>();
            pointer = lifecycle.GetComponent<InputSystemPointerInput>();
        }

        private void OnEnable()
        {
            runner.RunStarted += OnRunStarted;
            if (runner.Flow != null) runner.Flow.TitleRequested += OnMissionTitle;
            runner.MissionCompleted += OnMissionCompleted;
            lifecycle.GameOver += OnGameOver;
        }

        private void OnMissionTitle(string title) => MissionTitleRequested?.Invoke(title);

        public void Pause()
        {
            if (IsPaused || IsTerminal || !runner.HasActiveRun) return;
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
            if (runner.Flow != null) runner.Flow.RefreshIntroTitle();
        }

        private void OnRunStarted()
        {
            RestoreClock();
            IsPaused = false;
            pointer.CancelGesture();
            Changed?.Invoke();
            MissionStarted?.Invoke();
        }

        private void OnMissionCompleted()
        {
            if (lifecycle.GetComponent<PlayerLives>().CurrentLives <= 0 || IsTerminal) return;
            playerGate.SetState(PlayerGameplayState.Result);
            EndCombat();
        }

        private void OnGameOver() => EndCombat();

        private void EndCombat()
        {
            runner.StopRun();
            enemyFireGate.Block();
            foreach (var shot in FindObjectsByType<PlayerProjectile>())
                if (shot.gameObject.scene == gameObject.scene) shot.Release();
            IsPaused = false;
            Freeze();
            Changed?.Invoke();
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
            if (runner.Flow != null) runner.Flow.TitleRequested -= OnMissionTitle;
            runner.MissionCompleted -= OnMissionCompleted;
            lifecycle.GameOver -= OnGameOver;
            RestoreClock();
            IsPaused = false;
        }
    }
}
