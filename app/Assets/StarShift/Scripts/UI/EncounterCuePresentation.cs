using StarShift.Encounters;
using StarShift.Gameplay;
using UnityEngine;
namespace StarShift.UI
{
    [RequireComponent(typeof(EncounterRunner))]
    public sealed class EncounterCuePresentation : MonoBehaviour
    {
        private EncounterRunner runner;
        private LevelFlow flow;
        private void Awake() { runner = GetComponent<EncounterRunner>(); flow = GetComponent<LevelFlow>(); }
        private void OnEnable() { runner.OptionalEventTriggered += OnOptional; runner.EncounterStarted += OnEncounter; }
        private void OnDisable() { runner.OptionalEventTriggered -= OnOptional; runner.EncounterStarted -= OnEncounter; }
        private void OnOptional(OptionalEvent e, int token)
        { if (e.Kind == OptionalEventKind.SupplyDrone) flow.Announce("TIẾP TẾ", "Hạ mục tiêu trước khi rời khu vực", 1.6f); }
        private void OnEncounter(EncounterDefinition e, int token)
        { if (e.Completion == EncounterCompletion.Boss) flow.Announce("CẢNH BÁO", "", 1.4f); }
    }
}
