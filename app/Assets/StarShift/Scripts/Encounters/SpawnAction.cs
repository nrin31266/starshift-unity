// SpawnAction — Cấu hình một nhóm sinh địch: số lượng, lịch xuất hiện, đội hình và đường bay.
using System;
using StarShift.Enemies;
using StarShift.Rewards;
using UnityEngine;

namespace StarShift.Encounters
{
    public enum FormationLayout { Custom, Line, Column, V, Wedge, Staggered, TwinFlank, Cross, Escort, Ring }

    [Serializable]
    public class SpawnAction
    {
        [Min(0)] public int EntryBeat;
        [Min(.1f)] public float SpawnScale = 1;
        public float VisualSpin;
        public bool ContinuousTransit;
        // Số địch của nhóm này; không phải tổng số địch của cả màn.
        [Min(1)] public int Count = 3;
        // Độ trễ nhóm tính từ mốc beat được gán; runner kết hợp với lịch beat để sinh địch.
        [Min(0)] public float StartTime;
        // Khoảng thời gian giữa hai địch liên tiếp trong cùng nhóm.
        [Min(0)] public float Stagger = .35f;
        public GameObject EnemyPrefab;
        public EnemyDefinition EnemyDefinition;
        public RewardDropTable RewardOverride;
        public EnemyModifierDefinition Elite;
        // Đánh dấu mục tiêu bắt buộc cho điều kiện tiêu diệt hoặc boss của đợt.
        public bool Required = true;
        public bool GrantsRewards = true;
        public FormationLayout Formation;
        public Vector2 Anchor;
        public Vector2 SlotStep = new Vector2(.18f, 0);
        [Min(0)] public float FormationRise;
        public FlightSequence Sequence;
        public FormationPlan Plan;
        [Min(0)] public int FirstSlot;
        // HP gốc ghi đè cho nhóm; 0 nghĩa là dùng máu từ EnemyDefinition.
        [Min(0)] public int HpOverride;

        // Transit trực tiếp cho hazard stream; formation phức tạp dùng Sequence.
        [HideInInspector] public Vector2 EntryAnchor = new Vector2(.5f, 1.12f);
        [HideInInspector] public Vector2 HoldAnchor = new Vector2(.5f, .78f);
        [HideInInspector] public Vector2 EntryCurve;
        [HideInInspector] public float EntryDuration = 1.2f;
        [HideInInspector] public float HoldDuration = 3f;
        [HideInInspector] public Vector2 ExitAnchor = new Vector2(1.18f, .3f);
        [HideInInspector] public float ExitDuration = 3f;

        // Ưu tiên vị trí do FormationPlan định sẵn; nếu không có thì tính từ kiểu đội hình.
        public Vector2 SlotOffset(int index)
        {
            if (Plan != null) return Plan.Slots[FirstSlot + index];
            var slot = index - (Count - 1) * .5f;
            var spacing = new Vector2(Mathf.Abs(SlotStep.x), Mathf.Abs(SlotStep.y));
            if (spacing.x == 0) spacing.x = spacing.y;
            if (spacing.y == 0) spacing.y = spacing.x;
            switch (Formation)
            {
                case FormationLayout.Line: return new Vector2(slot * spacing.x, 0);
                case FormationLayout.Column: return new Vector2(0, slot * spacing.y);
                case FormationLayout.V: return new Vector2(slot * spacing.x, Mathf.Abs(slot) * FormationRise);
                case FormationLayout.Wedge: return new Vector2(slot * spacing.x, -Mathf.Abs(slot) * FormationRise);
                case FormationLayout.Staggered: return new Vector2(slot * spacing.x, ((index % 2) - .5f) * spacing.y);
                case FormationLayout.TwinFlank:
                    return new Vector2((index % 2 == 0 ? -1 : 1) * spacing.x * (1 + index / 2), index / 2 * spacing.y);
                case FormationLayout.Cross:
                    if (index == 0) return Vector2.zero;
                    var arm = (index - 1) % 4; var distance = 1 + (index - 1) / 4;
                    return (arm == 0 ? Vector2.right * spacing.x : arm == 1 ? Vector2.left * spacing.x :
                        arm == 2 ? Vector2.up * spacing.y : Vector2.down * spacing.y) * distance;
                case FormationLayout.Escort:
                    return index == 0 ? Vector2.zero : new Vector2(((index % 2) == 0 ? 1 : -1) * spacing.x * ((index+1)/2), -FormationRise * ((index+1)/2));
                case FormationLayout.Ring:
                    var angle = index * Mathf.PI * 2 / Count;
                    return new Vector2(Mathf.Cos(angle) * spacing.x, Mathf.Sin(angle) * spacing.y);
                default: return SlotStep * slot + Vector2.up * (Mathf.Abs(slot) * FormationRise);
            }
        }

        public bool IsValid(GameObject fallback, EncounterCompletion mode, float duration, out string error)
        {
            error = "Invalid spawn action.";
            var prefab = EnemyPrefab != null ? EnemyPrefab : fallback;
            if (HpOverride < 0 || FirstSlot < 0 || (Plan != null &&
                (!Plan.IsValid || FirstSlot > Plan.Slots.Length - Count || Sequence == null))) return false;
            if (EntryBeat < 0 || !ConfigValue.Positive(SpawnScale) || !float.IsFinite(VisualSpin) || Count < 1 || !ConfigValue.Nonnegative(StartTime) || !ConfigValue.Nonnegative(Stagger) ||
                !ConfigValue.Finite(Anchor) || !ConfigValue.Finite(SlotStep) || !ConfigValue.Nonnegative(FormationRise) ||
                !Enum.IsDefined(typeof(FormationLayout), Formation) || prefab == null ||
                prefab.GetComponent<EnemyHealth>() == null || prefab.GetComponent<EntryHoldFlight>() == null ||
                prefab.GetComponent<SingleFireAttack>() == null || prefab.GetComponent<EnemyHealth>().Definition == null) return false;
            if (EnemyDefinition != null && prefab.GetComponent<EnemyHealth>().Definition != EnemyDefinition) return false;
            if (mode != EncounterCompletion.Eliminate && mode != EncounterCompletion.Boss && StartTime + (Count - 1) * Stagger >= duration) return false;
            if (Sequence != null)
            {
                if (!Sequence.IsValid(out error)) return false;
                var end = Sequence.Phases[Sequence.Phases.Length - 1];
                if (Required && (mode == EncounterCompletion.Eliminate || mode == EncounterCompletion.Boss) &&
                    (!end.HoldUntilResolved || end.Kind == FlightPhaseKind.Exit)) { error = "Required targets need a terminal hold."; return false; }
            }
            else if (!ConfigValue.Positive(EntryDuration) || !ConfigValue.Nonnegative(HoldDuration) ||
                !ConfigValue.Positive(ExitDuration) || !ConfigValue.Finite(EntryAnchor) || !ConfigValue.Finite(HoldAnchor) ||
                !ConfigValue.Finite(ExitAnchor) || !ConfigValue.Finite(EntryCurve)) return false;
            if (Count > 1)
                for (int i = 0; i < Count; i++) for (int j = i + 1; j < Count; j++)
                    if ((SlotOffset(i) - SlotOffset(j)).sqrMagnitude < .000001f) { error = "Duplicate formation slots."; return false; }
            error = null;
            return true;
        }
    }

    [Serializable]
    public sealed class ScoutSpawnGroup : SpawnAction { }
}
