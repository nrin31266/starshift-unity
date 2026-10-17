using System.Collections.Generic;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace StarShift.UI
{
    [DisallowMultipleComponent]
    public sealed class GameplayHud : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, IDragHandler
    {
        [SerializeField] private GameplaySession session;
        [SerializeField] private PlayerLives lives;
        [SerializeField] private PulseLaserWeapon weapon;
        [SerializeField] private Text livesLabel;
        [SerializeField] private Text weaponLabel;
        [SerializeField] private Image heatFill;
        [SerializeField] private Image heatFrame;
        [SerializeField] private Text heatStatus;
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
        [SerializeField] private CanvasGroup gameplayHud;
        [SerializeField] private Image establishingTapArea;
        [SerializeField] private Text continueHint;
        [SerializeField, Min(.1f)] private float maximumTapSeconds = .6f;
        [SerializeField, Min(.01f)] private float hintFadeSeconds = .4f;
        [SerializeField, Min(0)] private float clearDelay = .45f;
        private float clearRemaining;
        private int previousLives = -1;
        private int previousLevel = -1;
        private float previousHeat = -1;
        private bool previousOverheated;
        private readonly Dictionary<int, float> tapStarts = new Dictionary<int, float>();

        private void OnEnable()
        {
            session.Changed += RefreshOverlay;
            session.MissionStarted += OnMissionStarted;
            session.MissionTitleRequested += OnMissionTitle;
            pauseButton.onClick.AddListener(session.Pause);
            resumeButton.onClick.AddListener(session.Resume);
            continueHint.text = GameplayCopy.TapToContinue;
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

        private void OnMissionTitle(string copy, string description, float seconds)
        {
            if (session.Flow != null && session.Flow.IsAwaitingStart) announcement.ShowPersistent(copy, description);
            else announcement.Show(copy, description, seconds: seconds);
        }

        private void RefreshOverlay()
        {
            clearRemaining = session.IsClear ? clearDelay : 0;
            overlay.SetActive(session.IsPaused || (session.IsTerminal && !session.IsClear));
            title.text = session.IsClear ? GameplayCopy.MissionClear : session.IsTerminal ? GameplayCopy.GameOver : GameplayCopy.Pause;
            resumeButton.gameObject.SetActive(session.IsPaused);
            restartButton.gameObject.SetActive(!session.IsClear);
            settingsButton.gameObject.SetActive(session.IsPaused);
            continueButton.gameObject.SetActive(session.IsClear);
            var flow = session.Flow;
            var cinematic = flow != null && flow.IsCinematic;
            var awaitingStart = flow != null && flow.IsAwaitingStart;
            establishingTapArea.raycastTarget = cinematic;
            var showHint = awaitingStart && flow.CanContinue;
            var hintWasVisible = continueHint.gameObject.activeSelf;
            continueHint.gameObject.SetActive(showHint);
            if (showHint && !hintWasVisible)
            {
                continueHint.canvasRenderer.SetAlpha(0);
                continueHint.CrossFadeAlpha(1, hintFadeSeconds, true);
            }
            if (!awaitingStart) tapStarts.Clear();
            pauseButton.gameObject.SetActive(!cinematic);
            pauseButton.interactable = !session.IsPaused && session.CanPause;
            gameplayHud.blocksRaycasts = gameplayHud.interactable = !cinematic;
            if (announcement.IsPersistent && !awaitingStart) announcement.Hide();
            if (!session.HasActiveRun || session.IsPaused || session.IsTerminal) announcement.Hide();
        }

        private void Update()
        {
            gameplayHud.alpha = session.Flow != null ? session.Flow.HudOpacity : 1;
            if (previousLives != lives.CurrentLives)
            {
                previousLives = lives.CurrentLives;
                livesLabel.text = GameplayCopy.Lives(previousLives);
            }
            if (previousLevel != weapon.CurrentLevel)
            {
                previousLevel = weapon.CurrentLevel;
                weaponLabel.text = GameplayCopy.WeaponStatus(previousLevel);
            }
            RefreshHeat();
            if (!session.IsClear || overlay.activeSelf) return;
            clearRemaining -= Time.unscaledDeltaTime;
            if (clearRemaining <= 0) overlay.SetActive(true);
        }

        private void RefreshHeat()
        {
            if (heatFill == null || heatFrame == null || heatStatus == null) return;
            var fraction = weapon.HeatFraction;
            var overheated = weapon.IsOverheated;
            if (Mathf.Approximately(previousHeat, fraction) && previousOverheated == overheated) return;
            previousHeat = fraction;
            previousOverheated = overheated;
            heatFill.rectTransform.anchorMax = new Vector2(fraction, 1);
            var color = overheated ? new Color(1, .34f, .2f) : fraction >= .8f ? new Color(1, .72f, .25f) : new Color(.3f, .9f, 1);
            heatFill.color = color;
            heatFrame.color = overheated ? color : new Color(.3f, .9f, 1, .65f);
            heatStatus.text = overheated ? GameplayCopy.HeatOverheated : "";
            heatStatus.color = color;
        }

        public void OnPointerDown(PointerEventData data)
        {
            tapStarts.Remove(data.pointerId);
            if (session.Flow != null && session.Flow.CanContinue && data.button == PointerEventData.InputButton.Left && OnScreen(data.position))
                tapStarts[data.pointerId] = Time.unscaledTime;
        }

        public void OnDrag(PointerEventData data) => tapStarts.Remove(data.pointerId);

        public void OnPointerClick(PointerEventData data)
        {
            if (!tapStarts.TryGetValue(data.pointerId, out var started)) return;
            tapStarts.Remove(data.pointerId);
            if (data.button == PointerEventData.InputButton.Left && !data.dragging && ReleasedNormally(data) && OnScreen(data.position) && Time.unscaledTime - started <= maximumTapSeconds)
                session.BeginMission();
        }

        private static bool ReleasedNormally(PointerEventData data)
        {
            if (!(data is ExtendedPointerEventData extended) || extended.pointerType != UIPointerType.Touch) return true;
            // uGUI cũng gửi click khi touch bị Canceled; chỉ nhận Ended thật.
            var touch = extended.control?.parent as TouchControl ?? (extended.device as Touchscreen)?.primaryTouch;
            return touch != null && touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended;
        }

        private static bool OnScreen(Vector2 point) => new Rect(0, 0, Screen.width, Screen.height).Contains(point);
        private void OnApplicationFocus(bool focused) { if (!focused) tapStarts.Clear(); }

        private void OnDisable()
        {
            session.Changed -= RefreshOverlay;
            session.MissionStarted -= OnMissionStarted;
            session.MissionTitleRequested -= OnMissionTitle;
            pauseButton.onClick.RemoveListener(session.Pause);
            resumeButton.onClick.RemoveListener(session.Resume);
            tapStarts.Clear();
            establishingTapArea.raycastTarget = false;
        }
    }
}
