using System;
using StarShift.Rewards;
using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerGameplayGate), typeof(PlayerTemporaryEffects))]
    public sealed class PlayerPickupReceiver : MonoBehaviour
    {
        [SerializeField] private RunRewards rewards;
        private PlayerGameplayGate gate;
        private PlayerTemporaryEffects effects;
        private WeaponLoadout loadout;
        private PlayerLives lives;
        private EmergencyMissileReserve missiles;
        public event Action<string> Collected;
        private void Awake()
        {
            gate = GetComponent<PlayerGameplayGate>(); effects = GetComponent<PlayerTemporaryEffects>();
            loadout = GetComponent<WeaponLoadout>(); lives = GetComponent<PlayerLives>();
            missiles = GetComponent<EmergencyMissileReserve>();
        }
        public bool CanCollect => isActiveAndEnabled && gate.CanInput;
        public bool CanUse(PickupDefinition item)
        {
            if (item == null) return false;
            if (item.Kind == PickupKind.WeaponCore) return loadout.CanUpgrade(item.Branch);
            if (item.Kind == PickupKind.MissileAmmo) return missiles != null && missiles.CanCollect;
            if (item.Kind == PickupKind.ExtraLife) return lives.CurrentLives < lives.MaximumLives;
            return true;
        }
        public bool TryCollect(PickupDefinition item)
        {
            if (!CanCollect || !CanUse(item)) return false;
            switch (item.Kind)
            {
                case PickupKind.Shard: rewards.AddShards(item.Amount); break;
                case PickupKind.WeaponCore:
                    if (!loadout.ApplyCore(item.Branch, true)) return false;
                    rewards.RecordCore(); break;
                case PickupKind.ExtraLife: if (!lives.TryAddLife()) return false; break;
                case PickupKind.MissileAmmo: if (!missiles.TryAdd()) return false; break;
                default: if (!effects.Apply(item)) return false; break;
            }
            Collected?.Invoke(item.Kind == PickupKind.WeaponCore ? $"T{(int)item.Branch + 1} L{loadout.Level(item.Branch)}" : item.ShortCode);
            return true;
        }
    }
}
