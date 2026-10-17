using System.Collections.Generic;
using StarShift.Enemies;
using StarShift.Gameplay;
using UnityEngine;

namespace StarShift.Encounters
{
    [DisallowMultipleComponent, DefaultExecutionOrder(20)]
    public sealed class EncounterFireCoordinator : MonoBehaviour
    {
        private readonly List<SingleFireAttack> shooters = new List<SingleFireAttack>();
        private EnemyFireGate safetyGate;
        private EncounterDefinition definition;
        private readonly List<SingleFireAttack> selected = new List<SingleFireAttack>();
        private int next;
        private int volleyShots;
        private float remaining;

        public void Begin(EncounterDefinition config, EnemyFireGate gate)
        {
            Stop();
            definition = config;
            safetyGate = gate;
        }

        public void Register(SingleFireAttack attack)
        {
            attack.SetEncounterControlled();
            shooters.Add(attack);
        }

        public void Unregister(SingleFireAttack attack)
        {
            attack.SetEncounterControlled();
            shooters.Remove(attack);
            if (selected.Remove(attack))
            {
                remaining = definition != null ? definition.ShotSpacing : 0;
            }
            next = shooters.Count == 0 ? 0 : next % shooters.Count;
        }

        public void Stop()
        {
            foreach (var attack in shooters) if (attack != null) attack.SetEncounterControlled();
            shooters.Clear();
            selected.Clear();
            definition = null;
            next = volleyShots = 0;
            remaining = 0;
        }

        private void FixedUpdate()
        {
            if (definition == null) return;
            if (!safetyGate.CanFire)
            {
                selected.Clear();
                volleyShots = 0;
                remaining = definition.ShotSpacing;
                return;
            }
            for (int i = selected.Count - 1; i >= 0; i--)
            {
                var attack = selected[i];
                if (attack != null && attack.isActiveAndEnabled && attack.IsTelegraphing) continue;
                selected.RemoveAt(i);
                volleyShots++;
                remaining = volleyShots >= definition.ShotsPerVolley ? definition.VolleyPause : definition.ShotSpacing;
                if (volleyShots >= definition.ShotsPerVolley) volleyShots = 0;
            }
            if (selected.Count >= definition.ActiveShooterLimit) return;
            remaining -= Time.fixedDeltaTime;
            if (remaining > 0 || shooters.Count == 0) return;
            for (int i = 0; i < shooters.Count; i++)
            {
                var index = (next + i) % shooters.Count;
                if (shooters[index] == null || selected.Contains(shooters[index]) || !shooters[index].TryRequestAttack()) continue;
                selected.Add(shooters[index]);
                remaining = definition.ShotSpacing;
                next = (index + 1) % shooters.Count;
                return;
            }
        }

        private void OnDisable() => Stop();
    }
}
