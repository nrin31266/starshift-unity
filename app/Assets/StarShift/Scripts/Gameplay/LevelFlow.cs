using System;
using StarShift.Encounters;
using StarShift.Player;
using StarShift.UI;
using UnityEngine;

namespace StarShift.Gameplay
{
    public enum MissionIntroPhase { None, Briefing, Approach, Travel, Brake, MissionTitle }

    [DisallowMultipleComponent]
    public sealed class LevelFlow : MonoBehaviour
    {
        [SerializeField] private LevelDefinition level;
        [SerializeField] private GameplayViewController view;
        [SerializeField] private PlayerGameplayGate player;
        [SerializeField] private EnemyFireGate enemyFireGate;
        [SerializeField] private MissionIntroPresentation presentation;
        private float elapsed;
        private float phaseElapsed;
        private bool handoff;
        private float handoffDuration;
        private int startingEncounterIndex;
        public LevelDefinition Level => level;
        public MissionIntroPhase IntroPhase { get; private set; }
        public bool IsIntroducing => IntroPhase != MissionIntroPhase.None;
        public bool IsAwaitingStart => IntroPhase == MissionIntroPhase.Briefing;
        public bool CanContinue => IsAwaitingStart && phaseElapsed >= level.Intro.ContinueDelaySeconds;
        public bool IsCinematic => IsIntroducing && IntroPhase != MissionIntroPhase.MissionTitle;
        public float IntroElapsed => elapsed;
        public float HudOpacity => IntroPhase == MissionIntroPhase.Brake ? Mathf.Clamp01(phaseElapsed / level.Intro.BrakeSeconds) : IsCinematic ? 0 : 1;
        public PlayDirection Direction { get; private set; }
        public CameraProfile Baseline { get; private set; }
        public string EstablishingTitle => string.IsNullOrWhiteSpace(level.LocationName) ? GameplayCopy.SpaceTitle : level.LocationName;
        public event Action<string, string, float> TitleRequested;
        public event Action PresentationChanged;

        public void LoadLevel(LevelDefinition definition)
        {
            if (IsIntroducing || handoff) throw new InvalidOperationException("Cancel active presentation before loading.");
            if (definition == null || !definition.IsValid(out _)) throw new ArgumentException("Invalid level.");
            level = definition;
        }

        public void Begin(bool withIntro, PlayDirection? directionOverride = null, CameraProfile cameraOverride = null, int encounterIndex = 0, bool missionArrival = false)
        {
            startingEncounterIndex = encounterIndex;
            Direction = directionOverride ?? level.Direction;
            Baseline = cameraOverride != null ? cameraOverride : level.Camera;
            view.SetBaseline(Baseline.View);
            player.GetComponent<PlayerRespawnEntrance>().Direction = Direction;
            player.transform.rotation = Quaternion.Euler(0,0,PlayDirectionSpace.Rotation(Direction));
            elapsed = phaseElapsed = 0;
            if (missionArrival)
            {
                presentation.Hide(); view.RequestView(Baseline.View, 0);
                EnterPhase(MissionIntroPhase.MissionTitle);
            }
            else if (withIntro && level.Intro.Enabled)
            {
                player.SetState(PlayerGameplayState.Entry);
                player.SetCinematicLock(true);
                player.GetComponent<InputSystemPointerInput>().CancelGesture();
                enemyFireGate.Block();
                view.RequestView(level.Intro.ApproachView, 0);
                EnterPhase(MissionIntroPhase.Briefing);
            }
            else
            {
                presentation.Hide();
                view.RequestView(Baseline.View, 0);
                PresentationChanged?.Invoke();
            }
            if (IsAwaitingStart)
                TitleRequested?.Invoke(EstablishingTitle, string.IsNullOrWhiteSpace(level.MissionBriefing) ? GameplayCopy.EstablishingDescription : level.MissionBriefing, 0);
            else RequestMissionTitle();
        }

        public bool BeginApproach()
        {
            if (!CanContinue) return false;
            player.GetComponent<InputSystemPointerInput>().CancelGesture();
            EnterPhase(MissionIntroPhase.Approach);
            return true;
        }

        public void TickIntro(float delta)
        {
            if (!IsIntroducing) return;
            if (IsAwaitingStart)
            {
                bool wasReady = CanContinue;
                phaseElapsed += delta;
                if (!wasReady && CanContinue) PresentationChanged?.Invoke();
                return;
            }
            elapsed += delta;
            phaseElapsed += delta;
            var duration = PhaseDuration;
            if (IsCinematic) presentation.Present(IntroPhase, Mathf.Clamp01(phaseElapsed / duration), Direction);
            if (phaseElapsed < duration) return;
            if (IntroPhase != MissionIntroPhase.MissionTitle)
            {
                EnterPhase(IntroPhase + 1);
                return;
            }
            IntroPhase = MissionIntroPhase.None;
            PresentationChanged?.Invoke();
        }

        private float PhaseDuration => IntroPhase switch
        {
            MissionIntroPhase.Approach => level.Intro.ApproachSeconds,
            MissionIntroPhase.Travel => level.Intro.TravelSeconds,
            MissionIntroPhase.Brake => level.Intro.BrakeSeconds,
            MissionIntroPhase.MissionTitle => level.Intro.MissionTitleSeconds,
            _ => 1
        };

        private void EnterPhase(MissionIntroPhase phase)
        {
            IntroPhase = phase;
            phaseElapsed = 0;
            if (phase == MissionIntroPhase.Approach) view.RequestView(level.Intro.TravelView, PhaseDuration);
            if (phase == MissionIntroPhase.Brake) view.RequestView(Baseline.View, PhaseDuration);
            if (phase == MissionIntroPhase.MissionTitle)
            {
                presentation.Hide();
                player.GetComponent<InputSystemPointerInput>().CancelGesture();
                player.SetState(PlayerGameplayState.Active);
                player.SetCinematicLock(false);
                enemyFireGate.ResumeAfter(0);
            }
            else presentation.Present(phase, 0, Direction);
            PresentationChanged?.Invoke();
            if (IntroPhase == phase) RefreshMissionTitle();
        }

        private void RequestMissionTitle(float seconds = 0) => TitleRequested?.Invoke(level.Intro.ArrivalTitle,
            GameplayCopy.Encounter(startingEncounterIndex + 1), seconds > 0 ? seconds : level.Intro.MissionTitleSeconds);

        public void RefreshMissionTitle()
        {
            if (IntroPhase == MissionIntroPhase.MissionTitle) RequestMissionTitle(level.Intro.MissionTitleSeconds - phaseElapsed);
        }

        public void BeginHandoff(float seconds, EncounterDefinition next)
        {
            handoff = true;
            handoffDuration = Mathf.Max(.01f, seconds);
            phaseElapsed = 0;
            presentation.PresentHandoff(0, Direction);
            PresentationChanged?.Invoke();
            if (handoff) TitleRequested?.Invoke(next.TransitionTitle, next.TransitionDescription, handoffDuration);
        }
        public void TickHandoff(float delta)
        {
            phaseElapsed += delta;
            presentation.PresentHandoff(Mathf.Clamp01(phaseElapsed / handoffDuration), Direction);
        }
        public void EndHandoff()
        {
            handoff = false;
            presentation.Hide();
            PresentationChanged?.Invoke();
        }
        public void ApplyEncounter(EncounterDefinition encounter)
        {
            var profile = encounter.CameraOverride != null ? encounter.CameraOverride : Baseline;
            view.RequestView(profile.View, profile.TransitionSeconds);
        }
        public void Announce(string title, string description, float seconds) => TitleRequested?.Invoke(title, description, seconds);

        public void PresentRegionTravel(float progress, Vector3 departure)
        {
            player.SetCinematicLock(true);
            presentation.PresentHandoff(Mathf.Clamp01(progress), Direction);
            var t = Mathf.SmoothStep(0, 1, progress);
            var destination = Camera.main.ViewportToWorldPoint(new Vector3(.18f,.5f, -Camera.main.transform.position.z));
            player.GetComponent<RelativeDragMovement>().SetCinematicPosition(Vector3.Lerp(departure, destination, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 2);
            player.transform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, -90, t));
            if (progress < .04f) view.RequestView(level.Intro.TravelView, 1);
            if (progress > .65f) view.RequestView(Baseline.View, .5f);
        }
        public void FinishRegionTravel()
        {
            Direction = PlayDirection.Right;
            view.SetBaseline(level.Camera.View); view.RequestView(level.Camera.View, 0);
            player.GetComponent<PlayerRespawnEntrance>().Direction = Direction;
            player.GetComponent<RelativeDragMovement>().ResetForCheckpoint(Direction);
            player.transform.rotation = Quaternion.Euler(0,0,PlayDirectionSpace.Rotation(Direction));
            presentation.Hide();
            PresentationChanged?.Invoke();
        }
        public void Cancel()
        {
            IntroPhase = MissionIntroPhase.None;
            handoff = false;
            if (presentation != null) presentation.Hide();
            if (player != null) player.SetCinematicLock(false);
            PresentationChanged?.Invoke();
        }
    }
}
