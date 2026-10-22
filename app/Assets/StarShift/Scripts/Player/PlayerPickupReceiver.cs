// PlayerPickupReceiver — Nhận vật phẩm; chuyển từng loại cho hệ thống mạng, súng, tên lửa hoặc hiệu ứng sở hữu nó.
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
        public event Action<PickupDefinition> Collected;
        private void Awake()
        {
            gate = GetComponent<PlayerGameplayGate>(); effects = GetComponent<PlayerTemporaryEffects>();
            loadout = GetComponent<WeaponLoadout>(); lives = GetComponent<PlayerLives>();
            missiles = GetComponent<EmergencyMissileReserve>();
        }
        public bool CanCollect => isActiveAndEnabled && gate.CanInput;
        // Xét vật phẩm có thể dùng không, như súng chưa tối đa hoặc kho tên lửa chưa đầy.
        public bool CanUse(PickupDefinition item)
        {
            if (item == null) return false;
            if (item.Kind == PickupKind.WeaponCore) return loadout.CanUpgrade(item.Branch);
            if (item.Kind == PickupKind.MissileAmmo) return missiles != null && missiles.CanCollect;
            if (item.Kind == PickupKind.ExtraLife) return lives.CurrentLives < lives.MaximumLives;
            return true;
        }
        // Phân loại vật phẩm rồi giao đúng owner: mạng, lõi súng, tên lửa, Shard hoặc hiệu ứng.
        public bool TryCollect(PickupDefinition item)
        {
            // Từ chối khi tàu chưa được điều khiển hoặc vật phẩm không thể dùng, trước khi cấp thưởng.
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
            Collected?.Invoke(item);
            return true;
        }
    }
}
