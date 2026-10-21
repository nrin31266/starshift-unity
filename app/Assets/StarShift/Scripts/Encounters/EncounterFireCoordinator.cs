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
        private BossPhaseRunner boss;
        public int BusyCount => selected.Count + (boss != null && boss.IsAttacking ? 1 : 0);
        public bool CanStartBossAttack => definition != null && safetyGate.CanFire &&
            remaining <= 0 && volleyShots < definition.ShotsPerVolley && BusyCount < Mathf.Min(4, definition.ActiveShooterLimit);
        public void RecordBossAttack() => RecordAttack();
        private void RecordAttack()
        {
            volleyShots++;
            remaining = volleyShots >= definition.ShotsPerVolley ? definition.VolleyPause : definition.ShotSpacing;
        }

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
            var candidate = attack.GetComponent<BossPhaseRunner>();
            if (candidate != null) boss = candidate;
        }

        public void Unregister(SingleFireAttack attack)
        {
            attack.SetEncounterControlled();
            shooters.Remove(attack);
            selected.Remove(attack);
            if (boss != null && boss.gameObject == attack.gameObject) boss = null;
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
            boss = null;
        }

        private void FixedUpdate()
        {
            if (Time.timeScale <= 0) return;
            if (definition == null) return;
            if (!safetyGate.CanFire)
            {
                if (!safetyGate.CanContinueAttack) selected.Clear();
                volleyShots = 0;
                remaining = definition.ShotSpacing;
                return;
            }
            for (int i = selected.Count - 1; i >= 0; i--)
            {
                var attack = selected[i];
                if (attack != null && attack.isActiveAndEnabled && attack.IsTelegraphing) continue;
                selected.RemoveAt(i);
            }
            // Đợi cả đợt kết thúc rồi mới đếm nghỉ; kill/slot trống không mở đợt mới sớm.
            if (volleyShots >= definition.ShotsPerVolley)
            {
                if (BusyCount > 0) return;
                remaining -= Time.fixedDeltaTime;
                if (remaining > 0) return;
                volleyShots = 0;
            }
            else remaining -= Time.fixedDeltaTime;
            if (BusyCount >= Mathf.Min(4, definition.ActiveShooterLimit)) return;
            if (remaining > 0 || shooters.Count == 0) return;
            // Cho đặc nhiệm thể hiện đòn đầu trước khi hàng lính thường dùng hết budget.
            for (int priority = 0; priority < 2; priority++)
            for (int i = 0; i < shooters.Count; i++)
            {
                var index = (next + i) % shooters.Count;
                if (shooters[index] == null || (priority == 0 && !shooters[index].AwaitingFirstDirectedAttack) || selected.Contains(shooters[index]) || !shooters[index].TryRequestAttack()) continue;
                selected.Add(shooters[index]);
                RecordAttack();
                next = (index + 1) % shooters.Count;
                return;
            }
        }

        private void OnDisable() => Stop();
    }
}
