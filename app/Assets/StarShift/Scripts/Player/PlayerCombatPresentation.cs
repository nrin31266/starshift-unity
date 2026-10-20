using StarShift.Combat;
using StarShift.Rewards;
using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerGameplayGate), typeof(PulseLaserWeapon))]
    [AddComponentMenu("StarShift/Player/Player Combat Presentation")]
    public sealed class PlayerCombatPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer muzzle;
        [SerializeField] private SpriteRenderer reward;
        [SerializeField] private SpriteRenderer survival;
        [SerializeField] private Sprite[] muzzleFrames;
        [SerializeField] private Sprite[] rewardFrames;
        [SerializeField] private Sprite[] survivalFrames;
        [Header("Muzzle theo branch")]
        [SerializeField] private Sprite[] pulseMuzzleFrames;
        [SerializeField] private Sprite[] plasmaMuzzleFrames;
        [SerializeField] private Sprite[] missileMuzzleFrames;
        [Header("Hull breakup")]
        [SerializeField, Tooltip("Renderer debris riêng, nằm ngoài hull để không bị ẩn theo hull.")]
        private SpriteRenderer debris;
        [SerializeField] private Sprite[] debrisFrames;
        [SerializeField, Min(.05f)] private float breakupSeconds = .6f;
        private PulseLaserWeapon weapon;
        private PlayerGameplayGate gate;
        private PlayerPickupReceiver receiver;
        private PlayerGameplayState previousState;
        private int previousShots;
        private float muzzleRemaining, rewardRemaining, survivalRemaining, breakupRemaining;

        private void Awake()
        {
            weapon = GetComponent<PulseLaserWeapon>();
            gate = GetComponent<PlayerGameplayGate>();
            receiver = GetComponent<PlayerPickupReceiver>();
        }

        private void OnEnable()
        {
            receiver.Collected += OnCollected;
            previousShots = 0;
            previousState = gate.State;
            breakupRemaining = 0;
        }

        private void OnCollected(PickupDefinition item)
        {
            rewardRemaining = .36f;
            reward.color = item.Kind == PickupKind.ExtraLife ? new Color(.75f, 1, .8f) : item.Kind == PickupKind.Shield ? new Color(.4f, 1, .9f) : Color.white;
        }

        private void LateUpdate()
        {
            if (weapon.ShotsFired != previousShots) { previousShots = weapon.ShotsFired; muzzleRemaining = .1f; }
            if (gate.State != previousState) OnStateChanged(previousState, gate.State);
            Present(muzzle, MuzzleFrames, ref muzzleRemaining, .1f);
            Present(reward, rewardFrames, ref rewardRemaining, .36f);
            Present(survival, survivalFrames, ref survivalRemaining, .6f);
            PresentBreakup();
        }

        private Sprite[] MuzzleFrames
        {
            get
            {
                var branch = weapon.CurrentBranch;
                var frames = branch == WeaponBranch.Plasma ? plasmaMuzzleFrames : branch == WeaponBranch.Homing ? missileMuzzleFrames : pulseMuzzleFrames;
                return frames != null && frames.Length > 0 ? frames : muzzleFrames;
            }
        }

        private void OnStateChanged(PlayerGameplayState previous, PlayerGameplayState next)
        {
            previousState = next;
            if (next == PlayerGameplayState.PostEntryInvincibility) survivalRemaining = .6f;
            if (next == PlayerGameplayState.Dying && previous != PlayerGameplayState.Dying)
            {
                survivalRemaining = .6f;
                // Death: flash, hull breakup và debris chạy độc lập; hull ẩn sau flash.
                breakupRemaining = breakupSeconds;
            }
            if (next == PlayerGameplayState.Respawning) breakupRemaining = 0;
        }

        private void PresentBreakup()
        {
            if (debris != null)
            {
                bool showDebris = breakupRemaining > 0 && debrisFrames != null && debrisFrames.Length > 0;
                debris.enabled = showDebris;
                if (showDebris)
                {
                    float t = 1 - breakupRemaining / breakupSeconds;
                    debris.sprite = debrisFrames[Mathf.Min(debrisFrames.Length - 1, (int)(t * debrisFrames.Length))];
                    debris.transform.localRotation = Quaternion.Euler(0, 0, t * 140f);
                }
            }
            // Hull hide/flash thuộc PlayerDamagePresentation; ở đây chỉ debris độc lập.
            breakupRemaining = Mathf.Max(0, breakupRemaining - Time.deltaTime);
        }

        private static void Present(SpriteRenderer visual, Sprite[] frames, ref float remaining, float duration)
        {
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            visual.enabled = remaining > 0 && frames != null && frames.Length > 0;
            if (visual.enabled) visual.sprite = frames[Mathf.Min(frames.Length - 1, (int)((1 - remaining / duration) * frames.Length))];
        }

        private void OnDisable()
        {
            if (receiver != null) receiver.Collected -= OnCollected;
            muzzle.enabled = reward.enabled = survival.enabled = false;
            if (debris != null) debris.enabled = false;
            muzzleRemaining = rewardRemaining = survivalRemaining = breakupRemaining = 0;
        }
    }
}
