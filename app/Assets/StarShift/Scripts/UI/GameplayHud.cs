using StarShift.Gameplay;
using StarShift.Player;
using UnityEngine;
using UnityEngine.UI;

namespace StarShift.UI
{
    [DisallowMultipleComponent]
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private GameplaySession session;
        [SerializeField] private PlayerLives lives;
        [SerializeField] private PulseLaserWeapon weapon;
        [SerializeField] private Text livesLabel;
        [SerializeField] private Text weaponLabel;
        [SerializeField] private Text scoreLabel;
        [SerializeField] private Text shardLabel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject overlay;
        [SerializeField] private Text title;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text unavailableLabel;
        [SerializeField] private GameplayAnnouncement announcement;
        [SerializeField, Min(0)] private float clearDelay = .45f;
        private float clearRemaining;
        private int previousLives = -1;
        private int previousLevel = -1;

        private void OnEnable()
        {
            session.Changed += RefreshOverlay;
            session.MissionStarted += OnMissionStarted;
            session.MissionTitleRequested += OnMissionTitle;
            pauseButton.onClick.AddListener(session.Pause);
            resumeButton.onClick.AddListener(session.Resume);
            Label(resumeButton, GameplayCopy.Resume);
            Label(restartButton, GameplayCopy.RestartEncounter);
            Label(settingsButton, GameplayCopy.Settings);
            Label(menuButton, GameplayCopy.MainMenu);
            Label(continueButton, GameplayCopy.Continue);
            restartButton.interactable = settingsButton.interactable = menuButton.interactable = continueButton.interactable = false;
            scoreLabel.text = shardLabel.text = GameplayCopy.Placeholder;
            unavailableLabel.text = GameplayCopy.Unavailable;
            RefreshOverlay();
        }

        private static void Label(Button button, string copy) => button.GetComponentInChildren<Text>().text = copy;

        private void OnMissionStarted()
        {
            previousLives = previousLevel = -1;
            announcement.Hide();
        }

        private void OnMissionTitle(string copy) => announcement.Show(copy);

        private void RefreshOverlay()
        {
            clearRemaining = session.IsClear ? clearDelay : 0;
            overlay.SetActive(session.IsPaused || (session.IsTerminal && !session.IsClear));
            title.text = session.IsClear ? GameplayCopy.MissionClear : session.IsTerminal ? GameplayCopy.GameOver : GameplayCopy.Pause;
            resumeButton.gameObject.SetActive(session.IsPaused);
            restartButton.gameObject.SetActive(!session.IsClear);
            settingsButton.gameObject.SetActive(session.IsPaused);
            continueButton.gameObject.SetActive(session.IsClear);
            pauseButton.interactable = !session.IsPaused && !session.IsTerminal;
            if (session.IsPaused || session.IsTerminal) announcement.Hide();
        }

        private void Update()
        {
            if (previousLives != lives.CurrentLives)
            {
                previousLives = lives.CurrentLives;
                livesLabel.text = GameplayCopy.Lives(previousLives);
            }
            if (previousLevel != weapon.CurrentLevel)
            {
                previousLevel = weapon.CurrentLevel;
                weaponLabel.text = GameplayCopy.Weapon(previousLevel);
            }
            if (!session.IsClear || overlay.activeSelf) return;
            clearRemaining -= Time.unscaledDeltaTime;
            if (clearRemaining <= 0) overlay.SetActive(true);
        }

        private void OnDisable()
        {
            session.Changed -= RefreshOverlay;
            session.MissionStarted -= OnMissionStarted;
            session.MissionTitleRequested -= OnMissionTitle;
            pauseButton.onClick.RemoveListener(session.Pause);
            resumeButton.onClick.RemoveListener(session.Resume);
        }
    }
}
