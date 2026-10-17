using System;
using StarShift.Enemies;
using UnityEngine;

namespace StarShift.Encounters
{
    public enum EncounterCompletion { Eliminate, Timeline }

    [Serializable]
    public sealed class ScoutSpawnGroup
    {
        [Min(1)] public int Count = 3;
        [Min(0)] public float StartTime;
        [Min(0)] public float Stagger = .35f;
        public Vector2 EntryAnchor = new Vector2(.5f, 1.12f);
        public Vector2 HoldAnchor = new Vector2(.5f, .78f);
        public Vector2 SlotStep = new Vector2(.18f, 0);
        [Min(0)] public float FormationRise;
        public Vector2 EntryCurve;
        [Min(.01f)] public float EntryDuration = 1.2f;
        [Min(0)] public float HoldDuration = 3f;
        public Vector2 ExitAnchor = new Vector2(1.18f, .3f);
        [Min(.01f)] public float ExitDuration = 3f;

        public Vector2 SlotOffset(int index)
        {
            var slot = index - (Count - 1) * .5f;
            return SlotStep * slot + Vector2.up * (Mathf.Abs(slot) * FormationRise);
        }
    }

    [CreateAssetMenu(menuName = "StarShift/Encounters/Encounter Definition")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        public string Id = "ENCOUNTER_1";
        public EncounterCompletion Completion;
        public GameObject ScoutPrefab;
        [Min(.01f)] public float Duration = 16f;
        public ScoutSpawnGroup[] Groups = { new ScoutSpawnGroup() };
        [Min(.02f)] public float ShotSpacing = 1f;
        [Min(1)] public int ShotsPerVolley = 1;
        [Min(.02f)] public float VolleyPause = 1f;
        [Min(0)] public float HandoffDelay = 1f;

        public int EnemyCount
        {
            get { int count = 0; foreach (var group in Groups) count += group.Count; return count; }
        }

        public bool IsValid(out string error)
        {
            error = "Invalid encounter config: " + name;
            if (string.IsNullOrWhiteSpace(Id) || ScoutPrefab == null ||
                ScoutPrefab.GetComponent<EnemyHealth>() == null || ScoutPrefab.GetComponent<EntryHoldFlight>() == null ||
                ScoutPrefab.GetComponent<SingleFireAttack>() == null || Groups == null || Groups.Length == 0 ||
                !Positive(Duration) || !Positive(ShotSpacing) || !Positive(VolleyPause) ||
                ShotsPerVolley < 1 || !Nonnegative(HandoffDelay) || !Enum.IsDefined(typeof(EncounterCompletion), Completion)) return false;
            foreach (var group in Groups)
            {
                if (group == null || group.Count < 1 || !Nonnegative(group.StartTime) || !Nonnegative(group.Stagger) ||
                    !Positive(group.EntryDuration) || !Nonnegative(group.HoldDuration) || !Positive(group.ExitDuration) ||
                    !Finite(group.EntryAnchor) || !Finite(group.HoldAnchor) || !Finite(group.ExitAnchor) ||
                    !Finite(group.SlotStep) || !Finite(group.EntryCurve) || !Nonnegative(group.FormationRise)) return false;
                if (Completion == EncounterCompletion.Timeline && group.StartTime + (group.Count - 1) * group.Stagger >= Duration)
                    return false;
            }
            error = null;
            return true;
        }

        static bool Positive(float value) => float.IsFinite(value) && value > 0;
        static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0;
        static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }
}
