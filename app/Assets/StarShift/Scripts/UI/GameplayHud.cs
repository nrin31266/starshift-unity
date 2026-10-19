using System.Collections.Generic;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.Rewards;
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
        [SerializeField] private Text weaponInventory;
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
        [SerializeField] private WeaponLoadout loadout;
        [SerializeField] private PlayerTemporaryEffects effects;
        [SerializeField] private PlayerPickupReceiver pickupReceiver;
        [SerializeField] private RunRewards rewards;
        [SerializeField] private Button weaponButton;
        [SerializeField] private Text effectLabel;
        [SerializeField] private Text pickupLabel;
        [SerializeField] private EmergencyMissileReserve missileReserve;
        [SerializeField] private Button missileButton;
        [SerializeField] private Text missileCount;
        private int previousMissiles = -1;
        private PlayerGameplayGate playerGate;
        private int previousOverdriveSeconds = -1;
        private int previousDroneSeconds = -1;
        private bool previousShield;
        private float cueRemaining;
        private void Awake() => playerGate = weapon.GetComponent<PlayerGameplayGate>();
        private int previousBranch = -1;
        private int previousScore = -1;
        private int previousShards = -1;
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
            restartButton.onClick.AddListener(session.RetryEncounter);
            continueHint.text = GameplayCopy.TapToContinue;
            Label(resumeButton, GameplayCopy.Resume);
            Label(restartButton, GameplayCopy.RestartEncounter);
            Label(settingsButton, GameplayCopy.Settings);
            Label(menuButton, GameplayCopy.MainMenu);
            Label(continueButton, GameplayCopy.Continue);
            settingsButton.interactable = menuButton.interactable = continueButton.interactable = false;
            if (weaponButton != null) weaponButton.onClick.AddListener(CycleWeapon);
            if (missileButton != null) missileButton.onClick.AddListener(UseMissile);
            if (pickupReceiver != null) pickupReceiver.Collected += OnPickup;
            previousScore = previousShards = previousBranch = -1;
            unavailableLabel.text = GameplayCopy.Unavailable;
            RefreshOverlay();
        }

        private static void Label(Button button, string copy) => button.GetComponentInChildren<Text>().text = copy;

        private void OnMissionStarted()
        {
            previousLives = previousLevel = previousBranch = -1;
            cueRemaining = 0;
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
            restartButton.interactable = session.CanRetry;
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
            if (!session.HasActiveRun || session.IsTerminal) announcement.Hide();
        }

        private void Update()
        {
            gameplayHud.alpha = session.Flow != null ? session.Flow.HudOpacity : 1;
            if (previousLives != lives.CurrentLives)
            {
                previousLives = lives.CurrentLives;
                livesLabel.text = GameplayCopy.Lives(previousLives);
            }
            if (previousLevel != weapon.CurrentLevel || previousBranch != (int)weapon.CurrentBranch)
            {
                previousLevel = weapon.CurrentLevel;
                previousBranch = (int)weapon.CurrentBranch;
                weaponLabel.text = $"T{previousBranch + 1} L{previousLevel}";
            }
            if (weaponInventory != null)
            {
                var text = ""; var others = 0;
                for (int i = 0; i < 3; i++)
                {
                    var level = loadout.Level((StarShift.Combat.WeaponBranch)i);
                    if (level > 0 && i != (int)loadout.ActiveBranch) others++;
                    text += (i > 0 ? "  " : "") + (i == (int)loadout.ActiveBranch ? "[" : "") + (level > 0 ? $"T{i+1}" : "-") + (i == (int)loadout.ActiveBranch ? "]" : "");
                }
                weaponInventory.text = text + $"  +{others}";
            }
            if (rewards != null && previousScore != rewards.Score) { previousScore = rewards.Score; scoreLabel.text = previousScore.ToString(); }
            if (rewards != null && previousShards != rewards.Shards) { previousShards = rewards.Shards; shardLabel.text = previousShards.ToString(); }
            if (effectLabel != null)
            {
                var second = Mathf.CeilToInt(effects.OverdriveRemaining);
                var drone = Mathf.CeilToInt(effects.DroneRemaining);
                if (second != previousOverdriveSeconds || drone != previousDroneSeconds || effects.HasShield != previousShield)
                {
                    previousOverdriveSeconds = second; previousDroneSeconds = drone; previousShield = effects.HasShield;
                    var text = previousShield ? "SH" : "";
                    if (second > 0) text += $"  OD {second}";
                    if (drone > 0) text += $"  DR {drone}";
                    effectLabel.text = text.Trim();
                }
            }
            if (pickupLabel != null)
            {
                if (!session.IsPaused) cueRemaining -= Time.deltaTime;
                if (session.IsTerminal || effects == null || cueRemaining <= 0 || playerGate.State == PlayerGameplayState.Dying) pickupLabel.text = "";
            }
            if (weaponButton != null) weaponButton.interactable = !session.IsPaused && !session.IsTerminal && playerGate.CanInput;
            if (missileButton != null)
            {
                missileButton.interactable = missileReserve.CanUse;
                if (previousMissiles != missileReserve.Current)
                {
                    previousMissiles = missileReserve.Current;
                    missileCount.text = $"M {previousMissiles}/{missileReserve.Capacity}";
                }
            }
            RefreshHeat();
            if (!session.IsClear || overlay.activeSelf) return;
            clearRemaining -= Time.unscaledDeltaTime;
            if (clearRemaining <= 0) overlay.SetActive(true);
        }

        private void UseMissile() => missileReserve.TryUse();

        private void CycleWeapon()
        {
            if (!session.IsPaused && !session.IsTerminal && playerGate.CanInput) loadout.Cycle();
        }
        private void OnPickup(string code) { if (pickupLabel == null) return; pickupLabel.text = code; cueRemaining = 1.2f; }

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
            if (weaponButton != null) weaponButton.onClick.RemoveListener(CycleWeapon);
            if (missileButton != null) missileButton.onClick.RemoveListener(UseMissile);
            if (pickupReceiver != null) pickupReceiver.Collected -= OnPickup;
            session.Changed -= RefreshOverlay;
            session.MissionStarted -= OnMissionStarted;
            session.MissionTitleRequested -= OnMissionTitle;
            pauseButton.onClick.RemoveListener(session.Pause);
            resumeButton.onClick.RemoveListener(session.Resume);
            restartButton.onClick.RemoveListener(session.RetryEncounter);
            tapStarts.Clear();
            establishingTapArea.raycastTarget = false;
        }
    }
}
