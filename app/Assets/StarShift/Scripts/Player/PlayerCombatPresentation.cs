using StarShift.Rewards;
using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerGameplayGate), typeof(PulseLaserWeapon))]
    public sealed class PlayerCombatPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer muzzle;
        [SerializeField] private SpriteRenderer reward;
        [SerializeField] private SpriteRenderer survival;
        [SerializeField] private Sprite[] muzzleFrames;
        [SerializeField] private Sprite[] rewardFrames;
        [SerializeField] private Sprite[] survivalFrames;
        private PulseLaserWeapon weapon;
        private PlayerGameplayGate gate;
        private PlayerPickupReceiver receiver;
        private PlayerGameplayState previousState;
        private int previousShots;
        private float muzzleRemaining, rewardRemaining, survivalRemaining;
        private void Awake() { weapon = GetComponent<PulseLaserWeapon>(); gate = GetComponent<PlayerGameplayGate>(); receiver = GetComponent<PlayerPickupReceiver>(); }
        private void OnEnable() { receiver.Collected += OnCollected; previousShots = 0; previousState = gate.State; }
        private void OnCollected(PickupDefinition item)
        {
            rewardRemaining = .36f;
            reward.color = item.Kind == PickupKind.ExtraLife ? new Color(.75f, 1, .8f) : item.Kind == PickupKind.Shield ? new Color(.4f, 1, .9f) : Color.white;
        }
        private void LateUpdate()
        {
            if (weapon.ShotsFired != previousShots) { previousShots = weapon.ShotsFired; muzzleRemaining = .1f; }
            if (gate.State != previousState)
            {
                previousState = gate.State;
                if (gate.State == PlayerGameplayState.Dying || gate.State == PlayerGameplayState.PostEntryInvincibility) survivalRemaining = .6f;
            }
            Present(muzzle, muzzleFrames, ref muzzleRemaining, .1f);
            Present(reward, rewardFrames, ref rewardRemaining, .36f);
            Present(survival, survivalFrames, ref survivalRemaining, .6f);
        }
        private static void Present(SpriteRenderer visual, Sprite[] frames, ref float remaining, float duration)
        {
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            visual.enabled = remaining > 0 && frames.Length > 0;
            if (visual.enabled) visual.sprite = frames[Mathf.Min(frames.Length - 1, (int)((1 - remaining / duration) * frames.Length))];
        }
        private void OnDisable()
        {
            if (receiver != null) receiver.Collected -= OnCollected;
            muzzle.enabled = reward.enabled = survival.enabled = false;
            muzzleRemaining = rewardRemaining = survivalRemaining = 0;
        }
    }
}
