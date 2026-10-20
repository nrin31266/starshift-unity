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
        private PulseLaserWeapon weapon;
        private PlayerGameplayGate gate;
        private PlayerPickupReceiver receiver;
        private int previousShots;
        private float muzzleRemaining, rewardRemaining, protectionAge;

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
        }

        private void OnCollected(PickupDefinition item)
        {
            rewardRemaining = .36f;
            reward.color = item.Kind == PickupKind.ExtraLife ? new Color(.75f, 1, .8f) : item.Kind == PickupKind.Shield ? new Color(.4f, 1, .9f) : Color.white;
        }

        private void LateUpdate()
        {
            if (weapon.ShotsFired != previousShots) { previousShots = weapon.ShotsFired; muzzleRemaining = .1f; }
            Present(muzzle, MuzzleFrames, ref muzzleRemaining, .1f);
            Present(reward, rewardFrames, ref rewardRemaining, .36f);
            if (gate.State == PlayerGameplayState.PostEntryInvincibility)
            {
                protectionAge += Time.deltaTime;
                survival.enabled = survivalFrames.Length > 0 && Mathf.Repeat(protectionAge, .3f) < .2f;
                if (survival.enabled) survival.sprite = survivalFrames[(int)(protectionAge / .1f) % survivalFrames.Length];
            }
            else { survival.enabled = false; protectionAge = 0; }
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
            muzzleRemaining = rewardRemaining = protectionAge = 0;
        }
    }
}
