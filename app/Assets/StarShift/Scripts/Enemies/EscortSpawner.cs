using System.Collections.Generic;
using StarShift.Encounters;
using StarShift.Gameplay;
using UnityEngine;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EscortSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject escortPrefab;
        [SerializeField] private FlightSequence escortFlight;
        [SerializeField, Min(1)] private int capacity = 3;
        [SerializeField, Min(.1f)] private float intervalSeconds = 5;
        [SerializeField] private bool automatic = true;
        private EnemyHealth health;
        private EntryHoldFlight flight;
        private EncounterRunner runner;
        private EnemyFireGate gate;
        private int generation;
        private int ordinal;
        private float remaining;
        private readonly List<(EnemyHealth health, int version)> escorts = new List<(EnemyHealth, int)>();
        public int LiveCount
        {
            get { escorts.RemoveAll(e => e.health == null || !e.health.IsAlive || e.health.SpawnVersion != e.version); return escorts.Count; }
        }
        public int Capacity => capacity;
        public int SpawnCount { get; private set; }
        private void Awake()
        {
            health = GetComponentInParent<EnemyHealth>(); flight = GetComponentInParent<EntryHoldFlight>();
            runner = FindAnyObjectByType<EncounterRunner>(); gate = FindAnyObjectByType<EnemyFireGate>();
            if (escortPrefab == null || escortFlight == null || !escortFlight.IsValid(out _) || capacity < 1 || intervalSeconds <= 0 || health == null || flight == null || runner == null || gate == null)
                throw new System.InvalidOperationException("Invalid escort spawner: " + name);
        }
        private void OnEnable() { generation = runner.Generation; remaining = intervalSeconds; ordinal = 0; SpawnCount = 0; escorts.Clear(); }
        public bool SpawnEscort()
        {
            if (!isActiveAndEnabled || !health.IsAlive || !flight.IsHolding || !gate.CanFire || runner.Generation != generation || runner.Stage != EncounterStage.Running || LiveCount >= capacity) return false;
            // Quân tiếp viện dùng cùng registry/pool, không mở thêm reward rolls hoặc kill gate.
            var slot = ordinal++ % capacity;
            var group = new SpawnAction { EnemyPrefab = escortPrefab, Count = 1, Required = false, GrantsRewards = false,
                Sequence = escortFlight, Anchor = new Vector2((slot - (capacity - 1) * .5f) * .15f, 0), Stagger = 0 };
            var enemy = runner.SpawnReinforcement(group, generation);
            if (enemy == null) return false;
            enemy.GetComponent<EntryHoldFlight>().SetEntryOrigin(Camera.main.WorldToViewportPoint(transform.position));
            escorts.Add((enemy, enemy.SpawnVersion)); SpawnCount++;
            return true;
        }
        private void FixedUpdate()
        {
            if (!automatic || !health.IsAlive || !flight.IsHolding || !gate.CanFire) return;
            remaining -= Time.fixedDeltaTime;
            if (remaining > 0) return;
            remaining = intervalSeconds; SpawnEscort();
        }
        private void OnDisable()
        {
            foreach (var e in escorts)
                if (e.health != null && e.health.IsAlive && e.health.SpawnVersion == e.version) e.health.GetComponent<EntryHoldFlight>()?.BeginRetreat();
            escorts.Clear();
        }
    }
}
