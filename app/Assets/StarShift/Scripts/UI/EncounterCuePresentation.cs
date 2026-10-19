using StarShift.Encounters;
using StarShift.Gameplay;
using UnityEngine;
namespace StarShift.UI
{
    [RequireComponent(typeof(EncounterRunner))]
    public sealed class EncounterCuePresentation : MonoBehaviour
    {
        [SerializeField] private GameplayAnnouncement encounterAnnouncement;
        private EncounterRunner runner;
        private LevelFlow flow;
        private void Awake() { runner = GetComponent<EncounterRunner>(); flow = GetComponent<LevelFlow>(); }
        private void OnEnable() { runner.RunStarted += OnRunStarted; runner.OptionalEventTriggered += OnOptional; runner.EncounterStarted += OnEncounter; }
        private void OnDisable() { runner.RunStarted -= OnRunStarted; runner.OptionalEventTriggered -= OnOptional; runner.EncounterStarted -= OnEncounter; }
        private void OnRunStarted() => encounterAnnouncement?.Hide();
        private void OnOptional(OptionalEvent e, int token)
        { if (e.Kind == OptionalEventKind.SupplyDrone) flow.Announce("TIẾP TẾ", "Hạ mục tiêu trước khi rời khu vực", 1.6f); }
        private void OnEncounter(EncounterDefinition e, int token)
        {
            var title = e.Completion == EncounterCompletion.Boss ? "CẢNH BÁO" : e.TransitionTitle;
            var detail = e.Completion == EncounterCompletion.Boss ? "VANGUARD FRIGATE" : e.TransitionDescription;
            if (encounterAnnouncement != null) encounterAnnouncement.Show(title, detail, e.TitleSeconds);
            else if (!string.IsNullOrWhiteSpace(title)) flow.Announce(title, detail, e.TitleSeconds);
        }
    }
}
