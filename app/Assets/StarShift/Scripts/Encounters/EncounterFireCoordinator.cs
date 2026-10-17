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
        private SingleFireAttack selected;
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
            if (selected == attack)
            {
                selected = null;
                remaining = definition != null ? definition.ShotSpacing : 0;
            }
            next = shooters.Count == 0 ? 0 : next % shooters.Count;
        }

        public void Stop()
        {
            foreach (var attack in shooters) if (attack != null) attack.SetEncounterControlled();
            shooters.Clear();
            selected = null;
            definition = null;
            next = volleyShots = 0;
            remaining = 0;
        }

        private void FixedUpdate()
        {
            if (definition == null) return;
            if (!safetyGate.CanFire)
            {
                selected = null;
                volleyShots = 0;
                remaining = definition.ShotSpacing;
                return;
            }
            if (selected != null)
            {
                if (selected.isActiveAndEnabled && selected.IsTelegraphing) return;
                selected = null;
                volleyShots++;
                remaining = volleyShots >= definition.ShotsPerVolley ? definition.VolleyPause : definition.ShotSpacing;
                if (volleyShots >= definition.ShotsPerVolley) volleyShots = 0;
            }
            remaining -= Time.fixedDeltaTime;
            if (remaining > 0 || shooters.Count == 0) return;
            for (int i = 0; i < shooters.Count; i++)
            {
                var index = (next + i) % shooters.Count;
                if (shooters[index] == null || !shooters[index].TryRequestAttack()) continue;
                selected = shooters[index];
                next = (index + 1) % shooters.Count;
                return;
            }
        }

        private void OnDisable() => Stop();
    }
}
