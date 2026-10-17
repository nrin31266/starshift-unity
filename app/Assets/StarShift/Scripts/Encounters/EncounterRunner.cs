using System;
using System.Collections.Generic;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEngine;

namespace StarShift.Encounters
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(EncounterFireCoordinator))]
    public sealed class EncounterRunner : MonoBehaviour
    {
        [SerializeField] private EncounterDefinition[] encounters;
        [SerializeField] private EnemyFireGate safetyGate;
        [SerializeField] private PlayerGameplayGate playerGate;
        private EncounterFireCoordinator coordinator;
        private Transform poolRoot;
        private readonly List<(GameObject Root, GameObject Prefab)> pool = new List<(GameObject, GameObject)>();
        private readonly List<ScoutSlot> spawned = new List<ScoutSlot>();
        private int[] groupSpawned;
        private int killed;
        private bool standalone;
        private bool handoff;
        private float handoffRemaining;
        private bool drainingProjectiles;
        private float drainRemaining;
        public EncounterDefinition Current => encounters[CurrentIndex];
        public int CurrentIndex { get; private set; }
        public bool IsRunning { get; private set; }
        public float Elapsed { get; private set; }
        public int SpawnedCount => spawned.Count;
        public int KilledCount => killed;
        public int CompletionCount { get; private set; }
        public event Action<EncounterDefinition> Completed;

        private sealed class ScoutSlot
        {
            public EnemyHealth Health;
            public EntryHoldFlight Flight;
            public SingleFireAttack Attack;
            public bool Resolved;
        }

        private void Awake()
        {
            coordinator = GetComponent<EncounterFireCoordinator>();
            poolRoot = new GameObject("ScoutPool").transform;
            poolRoot.SetParent(transform, false);
            poolRoot.gameObject.SetActive(false);
        }

        private void Start()
        {
            var dev = GetComponent<StarShift.Dev.DevFastStart>();
            if (dev != null && dev.TryStart()) return;
            StartRun("LEVEL_START", false);
        }

        public void StartRun(string startPoint, bool singleEncounter)
        {
            StopRun();
            // Unity cấm reparent lúc parent deactivate; gom các return bị hoãn trước lượt mới.
            foreach (var candidate in pool)
                if (candidate.Root != null && !candidate.Root.activeSelf)
                    candidate.Root.transform.SetParent(poolRoot, false);
            if (safetyGate == null || playerGate == null || encounters == null || encounters.Length == 0)
                throw new InvalidOperationException("EncounterRunner requires encounters, safety gate and player.");
            var ids = new HashSet<string>();
            foreach (var encounter in encounters)
            {
                if (encounter == null) throw new InvalidOperationException("Missing encounter definition.");
                if (!encounter.IsValid(out var error)) throw new InvalidOperationException(error);
                if (!ids.Add(encounter.Id)) throw new InvalidOperationException("Duplicate encounter ID: " + encounter.Id);
            }
            var index = startPoint == "LEVEL_START" ? 0 : Array.FindIndex(encounters, item => item.Id == startPoint);
            if (index < 0) throw new ArgumentException("Unavailable encounter start point: " + startPoint);
            standalone = singleEncounter;
            CompletionCount = 0;
            BeginEncounter(index);
        }

        private void BeginEncounter(int index)
        {
            CurrentIndex = index;
            Elapsed = 0;
            killed = 0;
            spawned.Clear();
            groupSpawned = new int[Current.Groups.Length];
            handoff = false;
            IsRunning = true;
            coordinator.Begin(Current, safetyGate);
        }

        private void FixedUpdate()
        {
            if (!IsRunning && !handoff) return;
            if (playerGate.State == PlayerGameplayState.GameOver)
            {
                if (IsRunning || handoff) StopRun();
                return;
            }
            if (handoff)
            {
                handoffRemaining -= Time.fixedDeltaTime;
                if (handoffRemaining <= 0) BeginEncounter(CurrentIndex + 1);
                return;
            }
            Elapsed += Time.fixedDeltaTime;
            if (drainingProjectiles)
            {
                drainRemaining -= Time.fixedDeltaTime;
                if (safetyGate.VisibleProjectileTimeLimit <= 0 || drainRemaining <= 0) CompleteEncounter();
                return;
            }
            if (Current.Completion == EncounterCompletion.Timeline && Elapsed >= Current.Duration)
            {
                coordinator.Stop();
                foreach (var slot in spawned) if (!slot.Resolved) Release(slot);
                var visibleTime = safetyGate.VisibleProjectileTimeLimit;
                if (visibleTime <= 0) CompleteEncounter();
                else
                {
                    drainingProjectiles = true;
                    drainRemaining = visibleTime + Time.fixedDeltaTime;
                }
                return;
            }
            for (int g = 0; g < Current.Groups.Length; g++)
            {
                var group = Current.Groups[g];
                while (groupSpawned[g] < group.Count && Elapsed >= group.StartTime + groupSpawned[g] * group.Stagger)
                    Spawn(group, groupSpawned[g]++);
            }
            foreach (var slot in spawned)
            {
                if (slot.Resolved) continue;
                // Disable/despawn không phải death: Eliminate chỉ đếm HP về zero.
                if (slot.Health != null && slot.Health.CurrentHp == 0)
                {
                    killed++;
                    Release(slot);
                }
                else if (Current.Completion == EncounterCompletion.Timeline && slot.Flight != null &&
                    (slot.Flight.HasExited || (slot.Flight.IsExiting && slot.Flight.IsOutsideView)))
                    Release(slot);
            }
            if (Current.Completion == EncounterCompletion.Eliminate && killed == Current.EnemyCount)
                CompleteEncounter();
        }

        private void Spawn(ScoutSpawnGroup group, int index)
        {
            GameObject root = null;
            foreach (var candidate in pool)
                if (candidate.Root != null && candidate.Prefab == Current.ScoutPrefab && candidate.Root.transform.parent == poolRoot)
                { root = candidate.Root; break; }
            if (root == null)
            {
                root = Instantiate(Current.ScoutPrefab, poolRoot);
                root.SetActive(false);
                pool.Add((root, Current.ScoutPrefab));
            }
            var slot = new ScoutSlot { Health = root.GetComponent<EnemyHealth>(),
                Flight = root.GetComponent<EntryHoldFlight>(), Attack = root.GetComponent<SingleFireAttack>() };
            var offset = group.SlotOffset(index);
            slot.Flight.Configure(group.EntryAnchor + offset, group.HoldAnchor + offset, group.EntryCurve,
                group.EntryDuration, group.ExitAnchor + offset, group.HoldDuration,
                Current.Completion == EncounterCompletion.Timeline ? group.ExitDuration : 0);
            coordinator.Register(slot.Attack);
            root.transform.SetParent(transform, false);
            root.SetActive(true);
            spawned.Add(slot);
        }

        private void Release(ScoutSlot slot)
        {
            slot.Resolved = true;
            if (slot.Health == null) return;
            coordinator.Unregister(slot.Attack);
            slot.Health.gameObject.SetActive(false);
            if (gameObject.activeInHierarchy) slot.Health.transform.SetParent(poolRoot, false);
        }

        private void CompleteEncounter()
        {
            var completed = Current;
            StopRun();
            CompletionCount++;
            handoff = !standalone && CurrentIndex + 1 < encounters.Length;
            handoffRemaining = completed.HandoffDelay;
            Completed?.Invoke(completed);
        }

        public void StopRun()
        {
            IsRunning = handoff = drainingProjectiles = false;
            if (coordinator != null) coordinator.Stop();
            foreach (var slot in spawned) if (!slot.Resolved) Release(slot);
            if (safetyGate != null) safetyGate.ClearProjectiles();
        }

        private void OnDisable() => StopRun();
    }
}
