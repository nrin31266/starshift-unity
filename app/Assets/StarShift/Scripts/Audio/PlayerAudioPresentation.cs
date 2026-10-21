using StarShift.Combat;
using StarShift.Player;
using StarShift.Rewards;
using UnityEngine;

namespace StarShift.Audio
{
    [DisallowMultipleComponent, RequireComponent(typeof(PulseLaserWeapon))]
    public sealed class PlayerAudioPresentation : MonoBehaviour
    {
        private AudioDirector audio;
        private PulseLaserWeapon weapon;
        private PlayerGameplayGate gate;
        private PlayerPickupReceiver pickups;
        private PlayerTemporaryEffects effects;
        private EmergencyMissileReserve reserve;
        private int shots, shieldBreaks;
        private bool overheated;
        private float ventAt = -1;
        private void Awake()
        {
            audio = FindAnyObjectByType<AudioDirector>();
            weapon = GetComponent<PulseLaserWeapon>(); gate = GetComponent<PlayerGameplayGate>();
            pickups = GetComponent<PlayerPickupReceiver>(); effects = GetComponent<PlayerTemporaryEffects>();
            reserve = GetComponent<EmergencyMissileReserve>();
        }
        private void OnEnable()
        {
            shots = weapon.ShotsFired; shieldBreaks = effects.ShieldBreaks; overheated = weapon.IsOverheated; ventAt = -1;
            pickups.Collected += OnCollected; gate.StateChanged += OnState;
            reserve.Fired += OnMissile; reserve.Detonated += OnImpact;
        }
        private void OnCollected(PickupDefinition item) => audio.Play(item.Kind == PickupKind.Shard ? AudioCue.Coin : item.Kind == PickupKind.ExtraLife ? AudioCue.ExtraLife : AudioCue.PowerUp);
        private void OnMissile() => audio.Play(AudioCue.Homing);
        private void OnImpact() => audio.Play(AudioCue.HomingImpact);
        private void OnState()
        {
            if (gate.State == PlayerGameplayState.Dying) { audio.Play(AudioCue.PlayerHit); audio.Play(AudioCue.PlayerDeath); ventAt = -1; }
        }
        private void LateUpdate()
        {
            if (weapon.ShotsFired > shots) audio.Play(weapon.CurrentBranch == WeaponBranch.Pulse ? AudioCue.Pulse : weapon.CurrentBranch == WeaponBranch.Plasma ? AudioCue.Plasma : AudioCue.Homing);
            shots = weapon.ShotsFired;
            if (effects.ShieldBreaks > shieldBreaks) { audio.Play(AudioCue.ShieldAbsorb); audio.Play(AudioCue.ShieldBreak); }
            shieldBreaks = effects.ShieldBreaks;
            if (weapon.IsOverheated && !overheated) { audio.Play(AudioCue.Overheat); ventAt = Time.time + .22f; }
            overheated = weapon.IsOverheated;
            if (ventAt >= 0 && Time.time >= ventAt && Time.timeScale > 0) { audio.Play(AudioCue.Vent); ventAt = -1; }
        }
        private void OnDisable()
        {
            pickups.Collected -= OnCollected; gate.StateChanged -= OnState;
            reserve.Fired -= OnMissile; reserve.Detonated -= OnImpact; ventAt = -1;
        }
    }
}
