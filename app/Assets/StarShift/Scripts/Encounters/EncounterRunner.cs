using System;
using System.Collections.Generic;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEngine;

namespace StarShift.Encounters
{
    public enum EncounterStage { Idle, Intro, Running, ProjectileDrain, BossPostDeath, RewardGrace, Handoff }

    [DisallowMultipleComponent, DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(EncounterFireCoordinator), typeof(ScenePrefabPool), typeof(EncounterCleanup))]
    public sealed class EncounterRunner : MonoBehaviour
    {
        [SerializeField, HideInInspector] private EncounterDefinition[] encounters;
        [SerializeField] private EnemyFireGate safetyGate;
        [SerializeField] private PlayerGameplayGate playerGate;
        [SerializeField] private LevelFlow levelFlow;
        private EncounterFireCoordinator coordinator;
        private PlayerLives playerLives;
        private ScenePrefabPool pool;
        private EncounterCleanup cleanup;
        private readonly List<EnemySlot> spawned = new List<EnemySlot>();
        private readonly List<ScheduledGroup> schedule = new List<ScheduledGroup>();
        private bool[] optionalFired;
        private int killed;
        private int requiredKilled;
        private bool standalone;
        private float remaining;
        private int generation;
        private bool bossDefeated;
        private bool completionSent;
        public EncounterDefinition Current => Definitions[CurrentIndex];
        private EncounterDefinition[] Definitions => levelFlow != null ? levelFlow.Level.Encounters : encounters;
        public LevelFlow Flow => levelFlow;
        public int CurrentIndex { get; private set; }
        public EncounterStage Stage { get; private set; }
        public bool IsRunning => Stage == EncounterStage.Running || Stage == EncounterStage.ProjectileDrain || Stage == EncounterStage.BossPostDeath;
        public bool HasActiveRun => Stage != EncounterStage.Idle;
        public float Elapsed { get; private set; }
        public int SpawnedCount => spawned.Count;
        public int KilledCount => killed;
        public int CompletionCount { get; private set; }
        public int Generation => generation;
        public event Action<EncounterDefinition> Completed;
        public event Action<EncounterDefinition, int> EncounterStarted;
        public event Action<OptionalEvent, int> OptionalEventTriggered;
        public event Action RunStarted;
        public event Action MissionCompleted;

        private sealed class EnemySlot
        {
            public EnemyHealth Health;
            public EntryHoldFlight Flight;
            public SingleFireAttack Attack;
            public bool Required;
            public bool Resolved;
        }
        private sealed class ScheduledGroup
        {
            public SpawnAction Action;
            public float Start;
            public int Spawned;
            public bool Optional;
        }

        private void Awake()
        {
            coordinator = GetComponent<EncounterFireCoordinator>();
            pool = GetComponent<ScenePrefabPool>();
            cleanup = GetComponent<EncounterCleanup>();
            if (levelFlow == null) levelFlow = GetComponent<LevelFlow>();
            if (playerGate != null) playerLives = playerGate.GetComponent<PlayerLives>();
        }
        private void Start()
        {
            var dev = GetComponent<StarShift.Dev.DevFastStart>();
            if (dev != null && dev.TryStart()) return;
            StartRun("LEVEL_START", false);
        }
        public void StartRun(string startPoint, bool singleEncounter)
            => StartRun(startPoint, singleEncounter, false, null, null);

        public void StartRun(string startPoint, bool singleEncounter, bool skipIntro,
            PlayDirection? directionOverride, CameraProfile cameraOverride)
        {
            StopRun();
            pool.ReclaimInactive();
            if (safetyGate == null || playerGate == null || Definitions == null || Definitions.Length == 0)
                throw new InvalidOperationException("EncounterRunner requires level, safety gate and player.");
            if (levelFlow != null && !levelFlow.Level.IsValid(out var levelError)) throw new InvalidOperationException(levelError);
            var ids = new HashSet<string>();
            foreach (var e in Definitions)
            {
                if (e == null) throw new InvalidOperationException("Missing encounter definition.");
                if (!e.IsValid(out var error)) throw new InvalidOperationException(error ?? "Invalid encounter: " + e.name);
                if (!ids.Add(e.Id)) throw new InvalidOperationException("Duplicate encounter ID: " + e.Id);
            }
            var index = levelFlow != null ? levelFlow.Level.ResolveStart(startPoint) :
                startPoint == "LEVEL_START" ? 0 : Array.FindIndex(Definitions, e => e.Id == startPoint);
            if (index < 0) throw new ArgumentException("Unavailable start point: " + startPoint);
            standalone = singleEncounter;
            CompletionCount = 0;
            CurrentIndex = index;
            var token = generation;
            RunStarted?.Invoke();
            if (token != generation) return;
            if (levelFlow != null)
            {
                levelFlow.Begin(startPoint == "LEVEL_START" && !skipIntro, directionOverride, cameraOverride, index);
                if (token != generation) return;
                if (levelFlow.IsIntroducing) { Stage = EncounterStage.Intro; return; }
            }
            BeginEncounter(index);
        }
        private void BeginEncounter(int index)
        {
            generation++;
            cleanup.BeginScope(generation);
            CurrentIndex = index;
            Elapsed = 0;
            killed = requiredKilled = 0;
            spawned.Clear(); schedule.Clear();
            foreach (var action in Current.Groups) schedule.Add(new ScheduledGroup { Action = action, Start = action.StartTime });
            optionalFired = new bool[Current.OptionalEvents.Length];
            bossDefeated = completionSent = false;
            Stage = EncounterStage.Running;
            coordinator.Begin(Current, safetyGate);
            if (levelFlow != null) levelFlow.ApplyEncounter(Current);
            EncounterStarted?.Invoke(Current, generation);
        }
        private void FixedUpdate()
        {
            if (!HasActiveRun) return;
            if (playerGate.State == PlayerGameplayState.GameOver || (playerLives != null && playerLives.CurrentLives == 0))
            { StopRun(); return; }
            if (Stage == EncounterStage.Intro)
            {
                var introToken = generation;
                levelFlow.TickIntro(Time.fixedDeltaTime);
                if (introToken != generation) return;
                if (!levelFlow.IsIntroducing) BeginEncounter(CurrentIndex);
                return;
            }
            if (Stage == EncounterStage.Handoff)
            {
                var handoffToken = generation;
                if (levelFlow != null) levelFlow.TickHandoff(Time.fixedDeltaTime);
                remaining -= Time.fixedDeltaTime;
                if (remaining <= 0)
                {
                    if (levelFlow != null) levelFlow.EndHandoff();
                    if (handoffToken == generation) BeginEncounter(CurrentIndex + 1);
                }
                return;
            }
            if (Stage == EncounterStage.RewardGrace)
            {
                remaining -= Time.fixedDeltaTime;
                if (remaining <= 0 || !cleanup.HasRewards) FinishHandoff();
                return;
            }
            if (Stage == EncounterStage.BossPostDeath)
            {
                remaining -= Time.fixedDeltaTime;
                if (remaining <= 0) CompleteEncounter();
                return;
            }
            Elapsed += Time.fixedDeltaTime;
            if (Stage == EncounterStage.ProjectileDrain)
            {
                remaining -= Time.fixedDeltaTime;
                if (safetyGate.VisibleProjectileTimeLimit <= 0 || remaining <= 0) CompleteEncounter();
                return;
            }
            if ((Current.Completion == EncounterCompletion.Timeline || Current.Completion == EncounterCompletion.Survive) && Elapsed >= Current.Duration)
            {
                CloseProducers();
                if (Current.Completion == EncounterCompletion.Timeline && safetyGate.VisibleProjectileTimeLimit > 0)
                {
                    Stage = EncounterStage.ProjectileDrain;
                    remaining = safetyGate.VisibleProjectileTimeLimit + Time.fixedDeltaTime;
                }
                else CompleteEncounter();
                return;
            }
            var token = generation;
            for (int i = 0; i < optionalFired.Length; i++)
            {
                var e = Current.OptionalEvents[i];
                if (optionalFired[i] || Elapsed < e.TimeOffset) continue;
                bool condition = e.Condition != OptionalEventCondition.NoActiveEnemies || !HasLiveEnemies();
                condition &= e.Condition != OptionalEventCondition.PlayerAlive || playerLives.CurrentLives > 0;
                if (!condition) continue;
                optionalFired[i] = true;
                if (e.Group != null) schedule.Add(new ScheduledGroup { Action = e.Group, Start = Elapsed + e.Group.StartTime, Optional = true });
                OptionalEventTriggered?.Invoke(e, token);
                if (generation != token || Stage != EncounterStage.Running) return;
            }
            // Min timestamp trước, stable group order nếu bằng nhau; không phụ thứ tự Inspector.
            while (true)
            {
                ScheduledGroup due = null; float nextTime = float.PositiveInfinity;
                foreach (var group in schedule)
                {
                    var time = group.Start + group.Spawned * group.Action.Stagger;
                    if (group.Spawned < group.Action.Count && time <= Elapsed && time < nextTime)
                    { due = group; nextTime = time; }
                }
                if (due == null) break;
                Spawn(due.Action, due.Spawned++, !due.Optional && due.Action.Required);
            }
            foreach (var slot in spawned)
            {
                if (slot.Resolved) continue;
                if (slot.Health != null && slot.Health.CurrentHp == 0)
                {
                    killed++;
                    if (slot.Required) requiredKilled++;
                    Release(slot);
                }
                else if (!slot.Required || (Current.Completion != EncounterCompletion.Eliminate && Current.Completion != EncounterCompletion.Boss))
                {
                    if (slot.Flight != null && (slot.Flight.HasExited || (slot.Flight.IsExiting && slot.Flight.IsOutsideView))) Release(slot);
                }
            }
            if (Current.Completion == EncounterCompletion.Eliminate && requiredKilled == Current.RequiredCount) CompleteEncounter();
            else if (Current.Completion == EncounterCompletion.Boss && Current.RequiredCount > 0 && requiredKilled == Current.RequiredCount)
                NotifyBossDefeated(token);
        }
        private bool HasLiveEnemies()
        {
            foreach (var e in spawned) if (!e.Resolved && e.Health != null && e.Health.IsAlive) return true;
            return false;
        }
        private void Spawn(SpawnAction group, int index, bool required)
        {
            var root = pool.Rent(group.EnemyPrefab != null ? group.EnemyPrefab : Current.ScoutPrefab);
            var slot = new EnemySlot { Health = root.GetComponent<EnemyHealth>(), Flight = root.GetComponent<EntryHoldFlight>(),
                Attack = root.GetComponent<SingleFireAttack>(), Required = required };
            var direction = levelFlow != null ? levelFlow.Direction : PlayDirection.Up;
            if (group.Sequence != null) slot.Flight.ConfigureSequence(group.Sequence, group.Anchor + group.SlotOffset(index), direction);
            else
            {
                var offset = group.SlotOffset(index);
                slot.Flight.Configure(PlayDirectionSpace.Point(group.EntryAnchor + offset, direction),
                    PlayDirectionSpace.Point(group.HoldAnchor + offset, direction), PlayDirectionSpace.Offset(group.EntryCurve,direction),
                    group.EntryDuration, PlayDirectionSpace.Point(group.ExitAnchor + offset,direction), group.HoldDuration,
                    Current.Completion == EncounterCompletion.Eliminate && required ? 0 : group.ExitDuration);
            }
            coordinator.Register(slot.Attack);
            root.transform.SetParent(transform, false);
            root.transform.rotation = Quaternion.Euler(0,0,PlayDirectionSpace.Rotation(direction));
            root.SetActive(true);
            spawned.Add(slot);
        }
        private void Release(EnemySlot slot)
        {
            slot.Resolved = true;
            if (slot.Health == null) return;
            coordinator.Unregister(slot.Attack);
            pool.Return(slot.Health.gameObject);
        }
        private void CloseProducers()
        {
            coordinator.Stop();
            foreach (var slot in spawned) if (!slot.Resolved) Release(slot);
            cleanup.CloseHostiles();
        }
        public bool NotifyBossDefeated(int token)
        {
            if (token != generation || Stage != EncounterStage.Running || Current.Completion != EncounterCompletion.Boss || bossDefeated) return false;
            bossDefeated = true;
            CloseProducers(); safetyGate.ClearProjectiles();
            if (token != generation) return false;
            Stage = EncounterStage.BossPostDeath;
            remaining = Current.BossPostDeathSeconds;
            return true;
        }
        public bool NotifyBossPresentationComplete(int token)
        {
            if (token != generation || Stage != EncounterStage.BossPostDeath) return false;
            CompleteEncounter(); return true;
        }
        private void CompleteEncounter()
        {
            if (completionSent) return;
            var token = generation;
            CloseProducers(); safetyGate.ClearProjectiles();
            if (token != generation) return;
            completionSent = true;
            CompletionCount++;
            Stage = EncounterStage.RewardGrace;
            remaining = Current.RewardGraceSeconds;
            Completed?.Invoke(Current);
            if (token != generation) return;
            // Completion consumer có thể vừa eject reward cuối; preserve sau notification.
            cleanup.BeginGrace(remaining);
            if (!cleanup.HasRewards) FinishHandoff();
        }
        private void FinishHandoff()
        {
            var token = generation;
            cleanup.ReleaseAll();
            if (token != generation) return;
            if (!standalone && CurrentIndex + 1 < Definitions.Length)
            {
                Stage = EncounterStage.Handoff;
                remaining = Current.HandoffDelay;
                if (levelFlow != null) levelFlow.BeginHandoff(remaining, Definitions[CurrentIndex + 1]);
            }
            else
            {
                Stage = EncounterStage.Idle;
                if (!standalone) MissionCompleted?.Invoke();
            }
        }
        public void StopRun()
        {
            generation++;
            Stage = EncounterStage.Idle;
            if (levelFlow != null) levelFlow.Cancel();
            if (coordinator != null) coordinator.Stop();
            foreach (var slot in spawned) if (!slot.Resolved) Release(slot);
            if (cleanup != null) cleanup.BeginScope(generation);
            if (safetyGate != null) safetyGate.ClearProjectiles();
        }
        private void OnDisable() => StopRun();
    }
}
