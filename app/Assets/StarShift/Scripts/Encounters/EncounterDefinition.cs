using System;
using UnityEngine;
using StarShift.Gameplay;

namespace StarShift.Encounters
{
    public enum EncounterCompletion { Eliminate, Timeline, Survive, Boss }
    public enum OptionalEventKind { SupplyDrone, GoldenMeteor, EliteFlyBy, BonusFormation }
    public enum OptionalEventCondition { Always, PlayerAlive, NoActiveEnemies }

    [Serializable]
    public sealed class OptionalEvent
    {
        public string Id;
        public OptionalEventKind Kind;
        [Min(0)] public float TimeOffset;
        public OptionalEventCondition Condition;
        [Tooltip("Không có group: chỉ gửi hook cho consumer tương lai.")]
        public SpawnAction Group;
    }

    [CreateAssetMenu(menuName = "StarShift/Encounters/Encounter Definition")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        public string Id = "ENCOUNTER_1";
        public EncounterCompletion Completion;
        [Tooltip("Fallback prefab cho các spawn action dùng cùng archetype.")]
        public GameObject ScoutPrefab;
        [Min(.01f)] public float Duration = 16f;
        public SpawnAction[] Groups = { new ScoutSpawnGroup() };
        public OptionalEvent[] OptionalEvents = Array.Empty<OptionalEvent>();
        public CameraProfile CameraOverride;
        [Min(1)] public int ActiveShooterLimit = 1;
        [Min(.02f)] public float ShotSpacing = 1f;
        [Min(1)] public int ShotsPerVolley = 1;
        [Min(.02f)] public float VolleyPause = 1f;
        [Min(0)] public float HandoffDelay = 1f;
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
                ActiveShooterLimit < 1 || ShotsPerVolley < 1 || !ConfigValue.Nonnegative(HandoffDelay) ||
                !ConfigValue.Nonnegative(ProjectileGraceSeconds) || !ConfigValue.Positive(ProjectileFadeSeconds) ||
                !ConfigValue.Positive(RewardGraceSeconds) || !ConfigValue.Nonnegative(BossPostDeathSeconds) ||
                (CameraOverride != null && !CameraOverride.IsValid) || !Enum.IsDefined(typeof(EncounterCompletion), Completion)) return false;
            if (Completion == EncounterCompletion.Eliminate && RequiredCount == 0) return false;
            foreach (var g in Groups)
            {
                if (g == null) { error = "Missing spawn action in " + name; return false; }
                if (!g.IsValid(ScoutPrefab, Completion, Duration, out error)) return false;
            }
            error = "Invalid optional event in " + name;
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var e in OptionalEvents)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.Id) || !ids.Add(e.Id) || !ConfigValue.Nonnegative(e.TimeOffset) ||
                    !Enum.IsDefined(typeof(OptionalEventKind), e.Kind) || !Enum.IsDefined(typeof(OptionalEventCondition), e.Condition) ||
                    ((Completion == EncounterCompletion.Timeline || Completion == EncounterCompletion.Survive) && e.TimeOffset >= Duration)) return false;
                if (e.Group != null && !e.Group.IsValid(ScoutPrefab, EncounterCompletion.Timeline, float.MaxValue, out error)) return false;
            }
            error = null;
            return true;
        }
    }
}
