using System;
using UnityEngine;
using StarShift.Gameplay;
using StarShift.Rewards;

namespace StarShift.Encounters
{
    public enum EncounterCompletion { Eliminate, Timeline, Survive, Boss }
    public enum OptionalEventKind { SupplyDrone, GoldenMeteor, EliteFlyBy, BonusFormation }
    public enum OptionalEventCondition { Always, PlayerAlive, NoActiveEnemies }

    [Serializable]
    public sealed class EntryBeatRule
    {
        [Min(0)] public float MinimumGap = 2;
        [Min(0)] public int RemainingThreshold = 2;
        [Min(0)] public float FallbackDelay = 7;
        public bool WaitForOptionalResolution;
    }

    [Serializable]
    public sealed class OptionalEvent
    {
        public string Id;
        public OptionalEventKind Kind;
        [Min(0)] public float TimeOffset;
        public OptionalEventCondition Condition;
        public bool UsePressureWindow;
        [Min(0)] public int MaximumActiveEnemies = 2;
        [Min(0)] public float FallbackTime = 12;
        [Tooltip("Không có group: chỉ gửi hook cho consumer tương lai.")]
        public SpawnAction Group;
    }

    [CreateAssetMenu(menuName = "StarShift/Encounters/Encounter Definition")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        public string Id = "ENCOUNTER_1";
        public EncounterCompletion Completion;
        [Tooltip("Timeline kết thúc sớm khi đã chạy hết lịch spawn/events và không còn địch.")]
        public bool CompleteWhenScheduleExhausted;
        public EntryBeatRule[] EntryBeats = Array.Empty<EntryBeatRule>();
        [Min(0)] public float EntryLeadSeconds;
        [Min(.1f)] public float TitleSeconds = 1.3f;
        public HazardStreamDefinition HazardStream;
        [Tooltip("Fallback prefab cho các spawn action dùng cùng archetype.")]
        public GameObject ScoutPrefab;
        [Min(.01f)] public float Duration = 16f;
        public SpawnAction[] Groups = { new ScoutSpawnGroup() };
        public OptionalEvent[] OptionalEvents = Array.Empty<OptionalEvent>();
        public CameraProfile CameraOverride;
        [Min(0)] public int GuaranteedMajorCount;
        [Min(0), Tooltip("Mốc kill eligible muộn nhất cho floor đầu tiên; 0 giữ lựa chọn seeded thông thường.")]
        public int GuaranteedMajorByKill;
        public RewardDropTable GuaranteeTable;
        [Range(1, 4)] public int ActiveShooterLimit = 1;
        [Min(.02f)] public float ShotSpacing = 1f;
        [Min(1)] public int ShotsPerVolley = 1;
        [Min(.02f)] public float VolleyPause = 1f;
        [Min(0)] public float ClearPauseSeconds = 1f;
        [Min(0)] public float HandoffTravelSeconds = 1.5f;
        [Min(0)] public float HandoffDelay = 2f;
        public float EffectiveClearPauseSeconds => ClearPauseSeconds > 0 ? ClearPauseSeconds : (HandoffDelay > 0 ? HandoffDelay : 1f);
        public float EffectiveHandoffTravelSeconds => HandoffTravelSeconds > 0 ? HandoffTravelSeconds : 1.5f;
        [Min(0)] public float ProjectileGraceSeconds = .65f;
        [Min(.01f)] public float ProjectileFadeSeconds = .3f;
        public string TransitionTitle;
        [TextArea] public string TransitionDescription;
        [Min(.01f)] public float RewardGraceSeconds = 2f;
        [Min(0)] public float BossPostDeathSeconds = 1f;

        public int EnemyCount
        {
            get { int count = 0; if (Groups != null) foreach (var g in Groups) if (g != null) count += g.Count; return count; }
        }
        public int RequiredCount
        {
            get { int count = 0; if (Groups != null) foreach (var g in Groups) if (g != null && g.Required) count += g.Count; return count; }
        }
        public bool IsValid(out string error)
        {
            error = "Invalid encounter config: " + name;
            if (string.IsNullOrWhiteSpace(Id) || Groups == null || OptionalEvents == null ||
                !ConfigValue.Positive(Duration) || !ConfigValue.Positive(ShotSpacing) || !ConfigValue.Positive(VolleyPause) ||
                ActiveShooterLimit < 1 || ActiveShooterLimit > 4 || ShotsPerVolley < 1 || !ConfigValue.Nonnegative(HandoffDelay) ||
                !ConfigValue.Nonnegative(ClearPauseSeconds) || !ConfigValue.Nonnegative(HandoffTravelSeconds) ||
                !ConfigValue.Nonnegative(ProjectileGraceSeconds) || !ConfigValue.Positive(ProjectileFadeSeconds) ||
                !ConfigValue.Positive(RewardGraceSeconds) || !ConfigValue.Nonnegative(BossPostDeathSeconds) ||
                (CameraOverride != null && !CameraOverride.IsValid) || !Enum.IsDefined(typeof(EncounterCompletion), Completion)) return false;
            if (EntryBeats == null || !ConfigValue.Nonnegative(EntryLeadSeconds) || !ConfigValue.Positive(TitleSeconds)) return false;
            foreach (var beat in EntryBeats) if (beat == null || !ConfigValue.Nonnegative(beat.MinimumGap) || beat.RemainingThreshold < 0 || !ConfigValue.Nonnegative(beat.FallbackDelay) || beat.FallbackDelay < beat.MinimumGap) return false;
            if (HazardStream != null && !HazardStream.IsValid) return false;
            if (Completion == EncounterCompletion.Eliminate && RequiredCount == 0) return false;
            int rewardEligibleCount = 0;
            foreach (var g in Groups)
            {
                if (g == null || g.EntryBeat > EntryBeats.Length) { error = "Missing spawn action in " + name; return false; }
                if (!g.IsValid(ScoutPrefab, Completion, Duration, out error)) return false;
                var prefab = g.EnemyPrefab != null ? g.EnemyPrefab : ScoutPrefab;
                var health = prefab.GetComponent<StarShift.Enemies.EnemyHealth>();
                if (g.GrantsRewards && health.Definition.BodyDamagesPlayer && health.Definition.MissileTier != StarShift.Enemies.MissileDamageTier.Boss &&
                    prefab.GetComponent<StarShift.Enemies.AsteroidVisualMotion>() == null) rewardEligibleCount += g.Count;
            }
            if (GuaranteedMajorByKill < 0 || GuaranteedMajorByKill > rewardEligibleCount ||
                (GuaranteedMajorByKill > 0 && GuaranteedMajorCount == 0) || GuaranteedMajorCount < 0 || (GuaranteedMajorCount > 0 &&
                (GuaranteeTable == null || GuaranteedMajorCount > rewardEligibleCount || Completion == EncounterCompletion.Boss ||
                (GuaranteeTable.Source != RewardSource.Normal && GuaranteeTable.Source != RewardSource.Elite) ||
                (GuaranteeTable.GuaranteedPickup != null ? GuaranteeTable.GuaranteedPickup.Kind == PickupKind.Shard :
                    GuaranteeTable.MajorItems == null || !Array.Exists(GuaranteeTable.MajorItems, p => p != null && p.Item != null && p.Item.Kind != PickupKind.Shard && ConfigValue.Positive(p.Weight))))))
            { error = "Invalid encounter reward guarantee: " + name; return false; }
            for (int i = 0; i < Groups.Length; i++)
            for (int j = i + 1; j < Groups.Length; j++)
            {
                var a = Groups[i]; var b = Groups[j];
                if (a.Plan != null && a.Plan == b.Plan &&
                    a.FirstSlot < b.FirstSlot + b.Count && b.FirstSlot < a.FirstSlot + a.Count)
                { error = "Subgroups share occupied formation slots: " + name; return false; }
            }
            error = "Invalid optional event in " + name;
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var e in OptionalEvents)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.Id) || !ids.Add(e.Id) || !ConfigValue.Nonnegative(e.TimeOffset) ||
                    (e.UsePressureWindow && (e.MaximumActiveEnemies < 0 || !ConfigValue.Nonnegative(e.FallbackTime) || e.FallbackTime < e.TimeOffset)) ||
                    !Enum.IsDefined(typeof(OptionalEventKind), e.Kind) || !Enum.IsDefined(typeof(OptionalEventCondition), e.Condition) ||
                    ((Completion == EncounterCompletion.Timeline || Completion == EncounterCompletion.Survive) && e.TimeOffset >= Duration)) return false;
                if (e.Group != null && !e.Group.IsValid(ScoutPrefab, EncounterCompletion.Timeline, float.MaxValue, out error)) return false;
            }
            error = null;
            return true;
        }
    }
}
