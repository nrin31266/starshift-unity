// GameplayHud — Hiển thị mạng, súng, điểm và nút chơi.
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
        [SerializeField] private Image weaponIcon;
        [SerializeField] private Sprite[] weaponIcons;
        [SerializeField] private Image shieldIcon, overdriveIcon, supportIcon;
        [SerializeField] private Image heatFill;
        [SerializeField] private Image heatFrame;
        [SerializeField] private Image[] heatNotches;
        [SerializeField] private Text heatStatus;
        [SerializeField] private Text scoreLabel;
        [SerializeField] private Text shardLabel;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameplayAnnouncement announcement;
        [SerializeField] private CanvasGroup gameplayHud;
        [SerializeField] private Image establishingTapArea;
        [SerializeField] private Text continueHint;
        [SerializeField, Min(.1f)] private float maximumTapSeconds = .6f;
        [SerializeField, Min(.01f)] private float hintFadeSeconds = .4f;
        [SerializeField] private WeaponLoadout loadout;
        [SerializeField] private PlayerTemporaryEffects effects;
        [SerializeField] private PlayerPickupReceiver pickupReceiver;
        [SerializeField] private RunRewards rewards;
        [SerializeField] private Button weaponButton;
        [SerializeField] private Text pickupLabel;
        [SerializeField] private EmergencyMissileReserve missileReserve;
        [SerializeField] private Button missileButton;
        [SerializeField] private Text missileCount;
        [SerializeField] private Text overdriveTimer, supportTimer;
        private int previousMissiles = -1;
        private PlayerGameplayGate playerGate;
        private GameShell shell;
        private int previousOverdriveSeconds = -1;
        private int previousDroneSeconds = -1;
        private bool previousShield;
        private float cueRemaining;
        private void Awake()
        {
            playerGate = weapon.GetComponent<PlayerGameplayGate>();
            shell = session.GetComponent<GameShell>();
        }
        private int previousBranch = -1;
        private long previousScore = -1;
        private int previousShards = -1;
        private int previousLives = -1;
        private int previousLevel = -1;
        private float previousHeat = -1;
        [SerializeField] private Image heatVent;
        [SerializeField] private Sprite[] heatVentFrames;
        private bool previousOverheated;
        private readonly Dictionary<int, float> tapStarts = new Dictionary<int, float>();

        private void OnEnable()
        {
            session.Changed += RefreshOverlay;
            session.MissionStarted += OnMissionStarted;
            session.MissionTitleRequested += OnMissionTitle;
            pauseButton.onClick.AddListener(session.Pause);
            continueHint.text = GameplayCopy.TapToContinue;
            if (weaponButton != null) weaponButton.onClick.AddListener(CycleWeapon);
            if (missileButton != null) missileButton.onClick.AddListener(UseMissile);
            if (pickupReceiver != null) pickupReceiver.Collected += OnPickup;
            previousScore = previousShards = previousBranch = -1;
            RefreshOverlay();
        }

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

        // Đọc trạng thái từ gameplay để hiển thị mạng, cấp súng và điểm; HUD không tự trừ mạng.
        private void Update()
        {
            var visible = shell == null || shell.GameplayVisible;
            gameplayHud.alpha = visible ? (session.Flow != null ? session.Flow.HudOpacity : 1) : 0;
            gameplayHud.blocksRaycasts = gameplayHud.interactable = visible && (session.Flow == null || !session.Flow.IsCinematic);
            if (previousLives != lives.CurrentLives)
            {
                previousLives = lives.CurrentLives;
                livesLabel.text = GameplayCopy.Lives(previousLives);
            }
            if (previousLevel != weapon.CurrentLevel || previousBranch != (int)weapon.CurrentBranch)
            {
                previousLevel = weapon.CurrentLevel;
                previousBranch = (int)weapon.CurrentBranch;
                weaponLabel.text = GameplayCopy.WeaponLevel(previousLevel);
                if (weaponIcon != null && weaponIcons != null && previousBranch < weaponIcons.Length)
                    weaponIcon.sprite = weaponIcons[previousBranch];
            }
            var score = session.Persistence != null ? session.Persistence.DisplayScore : rewards != null ? rewards.Score : 0;
            if (previousScore != score) { previousScore = score; scoreLabel.text = previousScore.ToString(); }
            var shardBalance = session.Persistence != null ? session.Persistence.ShardBalance : rewards != null ? rewards.Shards : 0;
            if (previousShards != shardBalance) { previousShards = shardBalance; shardLabel.text = previousShards.ToString(); }
            var second = Mathf.CeilToInt(effects.OverdriveRemaining);
            var drone = Mathf.CeilToInt(effects.DroneRemaining);
            if (second != previousOverdriveSeconds || drone != previousDroneSeconds || effects.HasShield != previousShield)
            {
                previousOverdriveSeconds = second; previousDroneSeconds = drone; previousShield = effects.HasShield;
                if (shieldIcon != null) shieldIcon.enabled = previousShield;
                if (overdriveIcon != null) overdriveIcon.enabled = second > 0;
                if (supportIcon != null) supportIcon.enabled = drone > 0;
                if (overdriveTimer != null) overdriveTimer.text = second > 0 ? second + "s" : "";
                if (supportTimer != null) supportTimer.text = drone > 0 ? drone + "s" : "";
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
                    missileCount.text = $"{previousMissiles}/{missileReserve.Capacity}";
                }
            }
            RefreshHeat();
        }

        // Nút tên lửa gửi ý định; kho tên lửa tự kiểm tra số lượng và hồi chiêu.
        private void UseMissile() => missileReserve.TryUse();

        // Nút đổi súng chỉ gửi yêu cầu khi đang chơi; WeaponLoadout chọn nhánh đã mở tiếp theo.
        private void CycleWeapon()
        {
            if (!session.IsPaused && !session.IsTerminal && playerGate.CanInput) loadout.Cycle();
        }
        private void OnPickup(PickupDefinition item)
        {
            if (pickupLabel == null) return;
            pickupLabel.text = item.Kind switch {
                PickupKind.WeaponCore => $"{GameplayCopy.WeaponName(item.Branch)} · CẤP {loadout.Level(item.Branch)}",
                PickupKind.Shard => $"+{item.Amount} Coin", PickupKind.Shield => "KHIÊN",
                PickupKind.Overdrive => "CƯỜNG HÓA", PickupKind.SupportDrone => "TÀU HỖ TRỢ",
                PickupKind.MissileAmmo => "+1 TÊN LỬA", PickupKind.ExtraLife => "+1 MẠNG", _ => "" };
            cueRemaining = 1.2f;
        }

        // Vẽ chỉ báo từ nhiệt thực của súng; màu và thanh hiển thị không quyết định quyền bắn.
        private void RefreshHeat()
        {
            if (heatFill == null || heatFrame == null) return;
            var fraction = weapon.HeatFraction;
            var overheated = weapon.IsOverheated;
            if (Mathf.Approximately(previousHeat, fraction) && previousOverheated == overheated) return;
            previousHeat = fraction;
            previousOverheated = overheated;
            if (heatVent != null)
            {
                heatVent.enabled=overheated&&heatVentFrames!=null&&heatVentFrames.Length>0;
                if(heatVent.enabled)heatVent.sprite=heatVentFrames[(int)(Time.time/.06f)%heatVentFrames.Length];
            }
            heatFill.rectTransform.anchorMax = new Vector2(fraction, 1);
            bool hot = overheated || fraction >= .8f;
            var color = overheated ? new Color(1, .36f, .22f) : hot ? new Color(1, .74f, .2f) : new Color(.26f, .92f, 1);
            heatFill.color = color;
            // Housing sáng lên theo trạng thái; giữ notch đọc được cả khi khung đổi màu.
            heatFrame.color = overheated ? new Color(1, .62f, .45f) : hot ? new Color(1, .86f, .55f) : Color.white;
            if (heatNotches != null)
                foreach (var notch in heatNotches)
                    if (notch != null) notch.color = hot ? new Color(1, .9f, .7f, .9f) : new Color(.86f, .96f, 1, .8f);
            if (heatStatus == null) return;
            heatStatus.text = overheated ? GameplayCopy.HeatOverheated : hot ? GameplayCopy.HeatHot : "";
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
            tapStarts.Clear();
            establishingTapArea.raycastTarget = false;
        }
    }
}
