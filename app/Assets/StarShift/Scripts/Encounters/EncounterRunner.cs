using System;
using System.Collections.Generic;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.Rewards;
using UnityEngine;

namespace StarShift.Encounters
{
    public enum EncounterStage { Idle, Intro, Running, ProjectileDrain, BossPostDeath, RewardGrace, Handoff, ExitDrain }

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
        private float clearPauseRemaining;
        private float travelRemaining;
        private bool travelStarted;
        private int generation;
        private bool bossDefeated;
        private bool completionSent;
        private EnemySlot dyingBoss;
        private float[] beatStarts;
        private float entryLeadRemaining;
        private float cleanupOverlap;
        private float optionalResolvedAt;
        public bool PreservePlayerInput { get; private set; }
        public int ActiveEnemyCount => LiveCount(-1);
        public float BeatStartTime(int beat) => beatStarts != null && beat >= 0 && beat < beatStarts.Length ? beatStarts[beat] : -1;
        public EncounterDefinition Current
        {
            get
            {
                var defs = Definitions;
                if (defs == null || defs.Length == 0) return null;
                return defs[Mathf.Clamp(CurrentIndex, 0, defs.Length - 1)];
            }
        }
        private EncounterDefinition[] Definitions => levelFlow != null ? levelFlow.Level.Encounters : encounters;
        public LevelFlow Flow => levelFlow;
        public int CurrentIndex { get; private set; }
        public EncounterStage Stage { get; private set; }
        public bool IsRunning => Stage == EncounterStage.Running || Stage == EncounterStage.ExitDrain || Stage == EncounterStage.ProjectileDrain || Stage == EncounterStage.BossPostDeath;
        public bool HasActiveRun => Stage != EncounterStage.Idle;
        public bool IsClearPause => Stage == EncounterStage.Handoff && !travelStarted;
        public bool IsHandoffTravel => Stage == EncounterStage.Handoff && travelStarted;
        public float ClearPauseRemaining => clearPauseRemaining;
        public float TravelRemaining => travelRemaining;
        public float Elapsed { get; private set; }
        public int SpawnedCount => spawned.Count;
        public int KilledCount => killed;
        public int CompletionCount { get; private set; }
        public int Generation => generation;
        public event Action<EncounterDefinition> Completed;
        public event Action<EncounterDefinition, int> EncounterStarted;
        public event Action<OptionalEvent, int> OptionalEventTriggered;
        public event Action<EnemyHealth, RewardDropTable, Vector3> EnemyDefeated;
        public event Action<EnemyHealth, RewardDropTable, Vector3> EnemyDestroyed;
        public event Action<EnemyHealth> EnemyEscaped;
        public event Action RunStarted;
        public event Action MissionCompleted;

        private sealed class EnemySlot
        {
            public EnemyHealth Health;
            public EntryHoldFlight Flight;
            public SingleFireAttack Attack;
            public bool Required;
            public bool GrantsRewards;
            public int Beat;
            public bool Optional;
            public bool Resolved;
            public Action DeathHandler;
        }
        private sealed class ScheduledGroup
        {
            public SpawnAction Action;
            public float Start;
            public int Spawned;
            public bool Optional;
            public bool Started;
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
            if (GetComponent<StarShift.Persistence.CampaignLifecycle>()?.TryBoot() == true) return;
            StartRun("LEVEL_START", false);
        }
        public void StartRun(string startPoint, bool singleEncounter)
            => StartRun(startPoint, singleEncounter, false, null, null);

        public void StartRun(string startPoint, bool singleEncounter, bool skipIntro,
            PlayDirection? directionOverride, CameraProfile cameraOverride, Action restoreState = null, bool missionArrival = false, bool preservePlayerInput = false)
        {
            GetComponent<StarShift.Persistence.CampaignLifecycle>()?.OnRunnerStarting();
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
            PreservePlayerInput = preservePlayerInput;
            var token = generation;
            RunStarted?.Invoke();
            if (token != generation) return;
            restoreState?.Invoke();
            if (token != generation) return;
            if (levelFlow != null)
            {
                levelFlow.Begin(startPoint == "LEVEL_START" && !skipIntro, directionOverride, cameraOverride, index, missionArrival, preservePlayerInput);
                if (token != generation) return;
                if (levelFlow.IsIntroducing) { Stage = EncounterStage.Intro; return; }
            }
            BeginEncounter(index);
        }
        private void BeginEncounter(int index, bool fromMissionTitle = false)
        {
            generation++;
            cleanup.BeginScope(generation, preserveRewards: true);
            CurrentIndex = index;
            Elapsed = 0;
            killed = requiredKilled = 0;
            clearPauseRemaining = travelRemaining = 0;
            travelStarted = false;
            spawned.Clear(); schedule.Clear();
            beatStarts = new float[Current.EntryBeats.Length + 1];
            for (int i = 0; i < beatStarts.Length; i++) beatStarts[i] = -1;
            beatStarts[0] = 0; entryLeadRemaining = fromMissionTitle ? 0 : Current.EntryLeadSeconds; cleanupOverlap = 0; optionalResolvedAt = -1;
            foreach (var action in Current.Groups) schedule.Add(new ScheduledGroup { Action = action, Start = action.StartTime, Started = action.EntryBeat == 0 });
            if (Current.HazardStream != null)
                foreach (var action in HazardStreamSchedule.Build(Current.HazardStream, Current.Duration)) schedule.Add(new ScheduledGroup { Action = action, Start = action.StartTime, Started = true });
            optionalFired = new bool[Current.OptionalEvents.Length];
            bossDefeated = completionSent = false;
            dyingBoss = null;
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
                if (!levelFlow.IsIntroducing) BeginEncounter(CurrentIndex, true);
                return;
            }
            if (Stage == EncounterStage.Handoff)
            {
                var handoffToken = generation;
                remaining -= Time.fixedDeltaTime;
                if (clearPauseRemaining > 0)
                {
                    clearPauseRemaining -= Time.fixedDeltaTime;
                    return;
                }
                if (!travelStarted)
                {
                    travelStarted = true;
                    if (levelFlow != null) levelFlow.BeginHandoff(travelRemaining, Definitions[CurrentIndex + 1]);
                }
                if (levelFlow != null) levelFlow.TickHandoff(Time.fixedDeltaTime);
                travelRemaining -= Time.fixedDeltaTime;
                if (travelRemaining <= 0)
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
                if (remaining <= 0 && dyingBoss == null) CompleteEncounter();
                return;
            }
            if (Stage == EncounterStage.ExitDrain)
            {
                ResolveDepartedSlots();
                if (!HasLiveEnemies()) { BeginProjectileDrain(); return; }
                remaining -= Time.fixedDeltaTime;
                if (remaining <= 0)
                {
                    foreach (var slot in spawned) if (!slot.Resolved && slot.Health.IsAlive && !slot.Flight.IsExiting) slot.Flight.BeginRetreat();
                }
                return;
            }
            if (Stage == EncounterStage.Running && entryLeadRemaining > 0) { entryLeadRemaining -= Time.fixedDeltaTime; return; }
            Elapsed += Time.fixedDeltaTime;
            if (Stage == EncounterStage.ProjectileDrain)
            {
                cleanupOverlap += Time.fixedDeltaTime;
                remaining -= Time.fixedDeltaTime;
                if (safetyGate.VisibleProjectileTimeLimit <= 0 || remaining <= 0) CompleteEncounter();
                return;
            }
            if ((Current.Completion == EncounterCompletion.Timeline || Current.Completion == EncounterCompletion.Survive) && Elapsed >= Current.Duration)
            {
                ResolveEncounter();
                return;
            }
            var token = generation;
            for (int i = 0; i < optionalFired.Length; i++)
            {
                var e = Current.OptionalEvents[i];
                if (optionalFired[i] || Elapsed < e.TimeOffset) continue;
                if (e.UsePressureWindow && LiveCount(-1) > e.MaximumActiveEnemies && Elapsed < e.FallbackTime) continue;
                bool condition = e.Condition != OptionalEventCondition.NoActiveEnemies || !HasLiveEnemies();
                condition &= e.Condition != OptionalEventCondition.PlayerAlive || playerLives.CurrentLives > 0;
                if (!condition) continue;
                optionalFired[i] = true;
                if (e.Group != null) schedule.Add(new ScheduledGroup { Action = e.Group, Start = Elapsed + e.Group.StartTime, Optional = true, Started = true });
                OptionalEventTriggered?.Invoke(e, token);
                if (generation != token || Stage != EncounterStage.Running) return;
            }
            ActivateEntryBeats();
            // Min timestamp trước, stable group order nếu bằng nhau; không phụ thứ tự Inspector.
            while (true)
            {
                ScheduledGroup due = null; float nextTime = float.PositiveInfinity;
                foreach (var group in schedule)
                {
                    var time = group.Start + group.Spawned * group.Action.Stagger;
                    if (group.Started && group.Spawned < group.Action.Count && time <= Elapsed && time < nextTime)
                    { due = group; nextTime = time; }
                }
                if (due == null) break;
                Spawn(due.Action, due.Spawned++, !due.Optional && due.Action.Required, due.Optional);
            }
            ResolveDepartedSlots();
            if (Current.Completion == EncounterCompletion.Timeline && Current.CompleteWhenScheduleExhausted &&
                ScheduleExhausted() && !HasLiveEnemies()) ResolveEncounter();
            else if (Current.Completion == EncounterCompletion.Eliminate && requiredKilled == Current.RequiredCount) ResolveEncounter();
            else if (Current.Completion == EncounterCompletion.Boss && Current.RequiredCount > 0 && requiredKilled == Current.RequiredCount)
                NotifyBossDefeated(token);
        }
        private void ResolveDepartedSlots()
        {
            foreach (var slot in spawned)
            {
                if (slot.Resolved) continue;
                if (slot.Health != null && slot.Health.CurrentHp == 0)
                {
                    killed++;
                    if (slot.Required) requiredKilled++;
                    Release(slot);
                }
                else if (Stage == EncounterStage.ExitDrain || !slot.Required || (Current.Completion != EncounterCompletion.Eliminate && Current.Completion != EncounterCompletion.Boss))
                {
                    if (slot.Flight != null && (slot.Flight.HasExited || (slot.Flight.IsExiting && slot.Flight.IsOutsideView)))
                    { if (slot.GrantsRewards) EnemyEscaped?.Invoke(slot.Health); Release(slot); }
                }
            }
        }
        private int LiveCount(int beat)
        {
            int count = 0;
            foreach (var slot in spawned)
                if (!slot.Resolved && !slot.Optional && (beat < 0 || slot.Beat == beat) && slot.Health != null && slot.Health.IsAlive && !slot.Flight.HasExited) count++;
            return count;
        }
        private void ActivateEntryBeats()
        {
            for (int beat = 1; beat < beatStarts.Length; beat++)
            {
                if (beatStarts[beat] >= 0 || beatStarts[beat - 1] < 0) continue;
                var rule = Current.EntryBeats[beat - 1];
                bool exhausted = true;
                foreach (var group in schedule) if (!group.Optional && group.Action.EntryBeat == beat - 1 && group.Spawned < group.Action.Count) exhausted = false;
                if (!exhausted) continue;
                if (rule.WaitForOptionalResolution)
                {
                    bool pending = Array.Exists(optionalFired, fired => !fired);
                    foreach (var slot in spawned) if (slot.Optional && !slot.Resolved && slot.Health != null && slot.Health.IsAlive && !slot.Flight.HasExited) pending = true;
                    if (pending) { optionalResolvedAt = -1; continue; }
                    if (optionalResolvedAt < 0) optionalResolvedAt = Elapsed;
                }
                var gap = Elapsed - (rule.WaitForOptionalResolution ? optionalResolvedAt : beatStarts[beat - 1]);
                if (gap < rule.MinimumGap || (LiveCount(beat - 1) > rule.RemainingThreshold && gap < rule.FallbackDelay)) continue;
                beatStarts[beat] = Elapsed;
                foreach (var group in schedule) if (!group.Optional && group.Action.EntryBeat == beat) { group.Started = true; group.Start = Elapsed + group.Action.StartTime; }
            }
        }
        private bool ScheduleExhausted()
        {
            foreach (var group in schedule) if (group.Spawned < group.Action.Count) return false;
            // Event chưa tới giờ vẫn là nội dung cần chạy; không cắt mất reward/scripted moment.
            foreach (var fired in optionalFired) if (!fired) return false;
            return true;
        }
        private bool HasLiveEnemies()
        {
            foreach (var e in spawned) if (!e.Resolved && e.Health != null && e.Health.IsAlive) return true;
            return false;
        }
        public EnemyHealth SpawnReinforcement(SpawnAction group, int token)
        {
            if (token != generation || Stage != EncounterStage.Running || group == null ||
                !group.IsValid(null, EncounterCompletion.Timeline, float.MaxValue, out _)) return null;
            return Spawn(group, 0, false, true);
        }
        private EnemyHealth Spawn(SpawnAction group, int index, bool required, bool optional = false)
        {
            var root = pool.Rent(group.EnemyPrefab != null ? group.EnemyPrefab : Current.ScoutPrefab);
            var slot = new EnemySlot { Health = root.GetComponent<EnemyHealth>(), Flight = root.GetComponent<EntryHoldFlight>(),
                Attack = root.GetComponent<SingleFireAttack>(), Required = required, GrantsRewards = group.GrantsRewards, Beat = group.EntryBeat, Optional = optional };
            var hp = group.HpOverride;
            if (levelFlow != null && slot.Health.Definition.BodyDamagesPlayer && slot.Health.Definition.MissileTier != MissileDamageTier.Boss && root.GetComponent<AsteroidVisualMotion>() == null)
                hp = Mathf.CeilToInt((hp > 0 ? hp : slot.Health.Definition.MaxHp) * levelFlow.Level.EnemyHealthMultiplier * levelFlow.Level.RegionEnemyHealthMultiplier);
            slot.Health.ConfigureModifier(group.Elite, hp);
            var direction = levelFlow != null ? levelFlow.Direction : PlayDirection.Up;
            if (group.Sequence != null) slot.Flight.ConfigureSequence(group.Sequence, group.Anchor + group.SlotOffset(index), direction, Elapsed);
            else
            {
                var offset = group.SlotOffset(index);
                slot.Flight.Configure(PlayDirectionSpace.Point(group.EntryAnchor + offset, direction),
                    PlayDirectionSpace.Point(group.HoldAnchor + offset, direction), PlayDirectionSpace.Offset(group.EntryCurve,direction),
                    group.EntryDuration, PlayDirectionSpace.Point(group.ExitAnchor + offset,direction), group.HoldDuration,
                    required && (Current.Completion == EncounterCompletion.Eliminate || Current.Completion == EncounterCompletion.Boss) ? 0 : group.ExitDuration, group.ContinuousTransit);
            }
            coordinator.Register(slot.Attack);
            root.transform.SetParent(transform, false);
            root.transform.rotation = Quaternion.Euler(0,0,PlayDirectionSpace.Rotation(direction));
            var token = generation;
            var rewarded = false;
            slot.DeathHandler = () =>
            {
                if (rewarded || slot.Resolved || token != generation || (Stage != EncounterStage.Running && Stage != EncounterStage.ExitDrain)) return;
                rewarded = true;
                var death = slot.Health.GetComponent<BossDeathSequence>();
                if (death != null && Current.Completion == EncounterCompletion.Boss)
                {
                    dyingBoss = slot;
                    NotifyBossDefeated(token);
                    death.Begin(() =>
                    {
                        if (token != generation || Stage != EncounterStage.BossPostDeath) return;
                        EnemyDefeated?.Invoke(slot.Health, group.RewardOverride, slot.Health.transform.position);
                        dyingBoss = null;
                        NotifyBossPresentationComplete(token);
                    });
                }
                else
                {
                    EnemyDestroyed?.Invoke(slot.Health, group.RewardOverride, slot.Health.transform.position);
                    if (group.GrantsRewards) EnemyDefeated?.Invoke(slot.Health, group.RewardOverride, slot.Health.transform.position);
                }
            };
            slot.Health.Died += slot.DeathHandler;
            root.SetActive(true);
            var prefab = group.EnemyPrefab != null ? group.EnemyPrefab : Current.ScoutPrefab;
            if (group.Elite == null) root.transform.localScale = prefab.transform.localScale * group.SpawnScale;
            root.GetComponent<AsteroidVisualMotion>()?.Configure(group.VisualSpin);
            spawned.Add(slot);
            return slot.Health;
        }
        private void Release(EnemySlot slot)
        {
            slot.Resolved = true;
            if (slot.Health == null) return;
            slot.Health.Died -= slot.DeathHandler;
            slot.DeathHandler = null;
            coordinator.Unregister(slot.Attack);
            pool.Return(slot.Health.gameObject);
        }
        private void CloseProducers()
        {
            coordinator.Stop();
            foreach (var slot in spawned) if (!slot.Resolved && slot != dyingBoss) Release(slot);
            cleanup.CloseHostiles();
        }
        private void ResolveEncounter()
        {
            coordinator.Stop();
            if (HasLiveEnemies())
            {
                Stage = EncounterStage.ExitDrain; remaining = 8;
                foreach (var slot in spawned)
                    if (!slot.Resolved && slot.Health.IsAlive && slot.Health.GetComponent<AsteroidVisualMotion>() == null) slot.Flight.BeginRetreat();
                return;
            }
            BeginProjectileDrain();
        }
        private void BeginProjectileDrain()
        {
            var token = generation;
            CloseProducers();
            if (token != generation) return;
            var visibleTime = safetyGate.VisibleProjectileTimeLimit;
            cleanupOverlap = 0;
            if (Current.Completion != EncounterCompletion.Boss && visibleTime > 0)
            {
                cleanupOverlap = 0;
                Stage = EncounterStage.ProjectileDrain;
                remaining = visibleTime + Time.fixedDeltaTime * 2;
            }
            else CompleteEncounter();
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
            // Pickup tiếp tục rơi qua encounter; chỉ chờ nhặt trước khi kết thúc world.
            if (!standalone && CurrentIndex + 1 < Definitions.Length) { FinishHandoff(); return; }
            // Completion consumer có thể vừa eject reward cuối; preserve sau notification.
            cleanup.BeginGrace(remaining);
            if (Current.Completion == EncounterCompletion.Boss && cleanup.HasRewards) levelFlow?.Announce("CHIẾN LỢI PHẨM", "Thu nhặt phần thưởng trước khi khởi hành.", remaining);
            if (!cleanup.HasRewards) FinishHandoff();
        }
        private void FinishHandoff()
        {
            if (!standalone && CurrentIndex + 1 < Definitions.Length)
            {
                Stage = EncounterStage.Handoff;
                clearPauseRemaining = Mathf.Max(0, Current.EffectiveClearPauseSeconds - cleanupOverlap);
                travelRemaining = Current.EffectiveHandoffTravelSeconds;
                travelStarted = false;
                remaining = clearPauseRemaining + travelRemaining;
                if (clearPauseRemaining <= 0)
                {
                    travelStarted = true;
                    if (levelFlow != null) levelFlow.BeginHandoff(travelRemaining, Definitions[CurrentIndex + 1]);
                }
            }
            else
            {
                var token = generation;
                cleanup.ReleaseAll();
                if (token != generation) return;
                Stage = EncounterStage.Idle;
                if (!standalone) MissionCompleted?.Invoke();
            }
        }
        public void StopRun()
        {
            generation++;
            Stage = EncounterStage.Idle;
            CurrentIndex = 0;
            dyingBoss = null;
            clearPauseRemaining = travelRemaining = 0;
            travelStarted = false;
            if (levelFlow != null) levelFlow.Cancel();
            if (coordinator != null) coordinator.Stop();
            foreach (var slot in spawned) if (!slot.Resolved) Release(slot);
            if (cleanup != null) cleanup.BeginScope(generation);
            if (safetyGate != null) safetyGate.ClearProjectiles();
        }
        private void OnDisable() => StopRun();
    }
}
