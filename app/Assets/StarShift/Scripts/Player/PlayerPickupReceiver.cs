using System;
using System.Collections.Generic;
using StarShift.Combat;
using StarShift.Enemies;
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
        private readonly List<EnemyHealth> empTargets = new List<EnemyHealth>(16);
        public event Action<string> Collected;
        public int EmpUses { get; private set; }
        private void Awake()
        {
            gate = GetComponent<PlayerGameplayGate>(); effects = GetComponent<PlayerTemporaryEffects>();
            loadout = GetComponent<WeaponLoadout>(); lives = GetComponent<PlayerLives>();
        }
        public bool CanCollect => isActiveAndEnabled && gate.CanInput;
        public bool CanUse(PickupDefinition item)
        {
            if (!CanCollect || item == null) return false;
            if (item.Kind == PickupKind.WeaponCore) return loadout.CanUpgrade(item.Branch);
            if (item.Kind == PickupKind.ExtraLife) return lives.CurrentLives < lives.MaximumLives;
            return true;
        }
        public bool TryCollect(PickupDefinition item)
        {
            if (!CanUse(item)) return false;
            switch (item.Kind)
            {
                case PickupKind.Shard: rewards.AddShards(item.Amount); break;
                case PickupKind.WeaponCore:
                    if (!loadout.ApplyCore(item.Branch)) return false;
                    rewards.RecordCore(); break;
                case PickupKind.ExtraLife: if (!lives.TryAddLife()) return false; break;
                case PickupKind.Emp: ApplyEmp(item); break;
                default: if (!effects.Apply(item)) return false; break;
            }
            Collected?.Invoke(item.Kind == PickupKind.WeaponCore ? $"T{(int)item.Branch + 1} L{loadout.Level(item.Branch)}" : item.ShortCode);
            return true;
        }
        private void ApplyEmp(PickupDefinition item)
        {
            EmpUses++;
            // EMP không sửa fire gate hay khởi động death/handoff cleanup.
            foreach (var shot in FindObjectsByType<EnemyProjectile>()) if (shot.gameObject.scene == gameObject.scene) shot.Release();
            empTargets.Clear();
            foreach (var enemy in EnemyHealth.Active)
                if (enemy.IsAlive && enemy.gameObject.scene == gameObject.scene &&
                    (enemy.transform.position - transform.position).sqrMagnitude <= item.EmpRadius * item.EmpRadius) empTargets.Add(enemy);
            foreach (var enemy in empTargets) if (enemy != null && enemy.IsAlive) enemy.ReceivePlayerHit(item.EmpDamage, gameObject);
            empTargets.Clear();
        }
        public void ResetForSession() => EmpUses = 0;
    }
}
