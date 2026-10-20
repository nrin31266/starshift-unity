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
        [SerializeField, Tooltip("Level đang chạy. Production dùng Data/RegionA/Missions/Mission01; Data/Fixtures/Levels/M4SystemTestbed chỉ là M4 fixture.")] private LevelDefinition level;
        [SerializeField] private GameplayViewController view;
        [SerializeField] private PlayerGameplayGate player;
        [SerializeField] private EnemyFireGate enemyFireGate;
        [SerializeField] private MissionIntroPresentation presentation;
        private float elapsed;
        private float phaseElapsed;
        private bool handoff;
        private float handoffDuration;
        private int startingEncounterIndex;
        private PlanetJourney journey;
        private bool missionTravel;
        private GameplayViewSettings entryView;
        private void Awake() => journey=GetComponent<PlanetJourney>();
        public LevelDefinition Level => level;
        public MissionIntroPhase IntroPhase { get; private set; }
        public bool IsIntroducing => IntroPhase != MissionIntroPhase.None;
        public bool IsAwaitingStart => IntroPhase == MissionIntroPhase.Briefing || (journey != null && journey.IsAwaitingTap);
        public bool CanContinue => (journey != null && journey.IsAwaitingTap) || (IntroPhase == MissionIntroPhase.Briefing && phaseElapsed >= level.Intro.ContinueDelaySeconds);
        public bool IsCinematic => (journey != null && journey.IsCinematic) || IntroPhase == MissionIntroPhase.Briefing;
        public float IntroElapsed => elapsed;
        public float HudOpacity => IsCinematic ? 0 : 1;
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

        public void Begin(bool withIntro, PlayDirection? directionOverride = null, CameraProfile cameraOverride = null, int encounterIndex = 0, bool missionArrival = false, bool suppressTitle = false)
        {
            startingEncounterIndex = encounterIndex;
            if(!suppressTitle)presentation.ResetEnvironment();
            Direction = directionOverride ?? level.Direction;
            Baseline = cameraOverride != null ? cameraOverride : level.Camera;
            view.SetBaseline(Baseline.View);
            var entryCamera = cameraOverride != null ? cameraOverride : level.Encounters[startingEncounterIndex].CameraOverride;
            entryView = entryCamera != null ? entryCamera.View : Baseline.View;
            player.GetComponent<PlayerRespawnEntrance>().Direction = Direction;
            player.transform.rotation = Quaternion.Euler(0,0,PlayDirectionSpace.Rotation(Direction));
            elapsed = phaseElapsed = 0;
            if (missionArrival)
            {
                presentation.Hide(); view.RequestView(entryView, Baseline.TransitionSeconds);
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
                view.RequestView(entryView, suppressTitle ? Baseline.TransitionSeconds : 0);
                PresentationChanged?.Invoke();
            }
            if (IsAwaitingStart)
                TitleRequested?.Invoke(EstablishingTitle, string.IsNullOrWhiteSpace(level.MissionBriefing) ? GameplayCopy.EstablishingDescription : level.MissionBriefing, 0);
            else if(!suppressTitle) RequestMissionTitle();
        }

        public bool BeginApproach()
        {
            if (journey != null && journey.IsAwaitingTap) return journey.AcceptTap();
            if (!CanContinue) return false;
            player.GetComponent<InputSystemPointerInput>().CancelGesture();
            EnterPhase(MissionIntroPhase.Approach);
            ReleaseApproach(Direction);
            return true;
        }

        public void ReleaseApproach(PlayDirection direction)
        {
            Direction = direction;
            player.GetComponent<PlayerRespawnEntrance>().Direction = direction;
            player.GetComponent<RelativeDragMovement>().SetCinematicPosition(presentation.ShipPosition);
            player.transform.rotation = Quaternion.Euler(0,0,PlayDirectionSpace.Rotation(direction));
            presentation.BeginGameplayApproach();
            player.SetState(PlayerGameplayState.Active); player.SetCinematicLock(false);
            PresentationChanged?.Invoke();
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
            if (IntroPhase != MissionIntroPhase.MissionTitle) presentation.Present(IntroPhase, Mathf.Clamp01(phaseElapsed / duration), Direction);
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
            MissionIntroPhase.MissionTitle => level.Intro.MissionEntryLeadSeconds,
            _ => 1
        };

        private void EnterPhase(MissionIntroPhase phase)
        {
            IntroPhase = phase;
            phaseElapsed = 0;
            if (phase == MissionIntroPhase.Approach) view.RequestView(level.Intro.TravelView, PhaseDuration);
            if (phase == MissionIntroPhase.Brake) view.RequestView(entryView, PhaseDuration);
            if (phase == MissionIntroPhase.MissionTitle)
            {
                presentation.Hide();
                player.SetState(PlayerGameplayState.Active);
                player.SetCinematicLock(false);
                enemyFireGate.ResumeAfter(0);
            }
            else presentation.Present(phase, 0, Direction);
            PresentationChanged?.Invoke();
            if (IntroPhase == phase) RefreshMissionTitle();
        }

        private void RequestMissionTitle(float seconds = 0) => TitleRequested?.Invoke(level.Intro.ArrivalTitle,
            level.MissionBriefing, seconds > 0 ? seconds : level.Intro.MissionTitleSeconds);

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
            // Đợt mới có overlay riêng khi EncounterStarted, không xếp timer chữ trước travel.
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
        public void NotifyPresentationChanged() => PresentationChanged?.Invoke();
        public void Announce(string title, string description, float seconds) => TitleRequested?.Invoke(title, description, seconds);

        public void PresentMissionTravel(float progress)
        {
            missionTravel=true;
            presentation.PresentMissionTravel(progress,Direction);
            var bias=PlayDirectionSpace.Offset(new Vector2(0,-.04f*Mathf.Sin(Mathf.Clamp01(progress)*Mathf.PI)),Direction);
            player.GetComponent<RelativeDragMovement>().SetTravelBias(bias);
        }
        public void PrepareMissionTravel(LevelDefinition next, float seconds)
        {
            var profile = next.Encounters[0].CameraOverride != null ? next.Encounters[0].CameraOverride : next.Camera;
            view.RequestView(profile.View, seconds);
        }
        public void EndMissionTravel()
        {
            if(!missionTravel)return;
            missionTravel=false;player.GetComponent<RelativeDragMovement>().SetTravelBias(Vector2.zero);presentation.Hide();
        }
        public void ResetRegionDeparture()
        {
            Direction=level.Direction;presentation.ResetEnvironment();
            view.SetBaseline(level.Camera.View);view.RequestView(level.Camera.View,0);
            player.GetComponent<PlayerRespawnEntrance>().Direction=Direction;
            player.GetComponent<RelativeDragMovement>().ResetForCheckpoint(Direction);
            player.transform.rotation=Quaternion.Euler(0,0,PlayDirectionSpace.Rotation(Direction));
        }
        public void FinishRegionTravel(bool preservePose = false, LevelDefinition destination = null)
        {
            Direction = PlayDirection.Right;
            presentation.SetDestinationPalette();
            var target = destination != null ? destination : level;
            var profile = target.Encounters[0].CameraOverride != null ? target.Encounters[0].CameraOverride : target.Camera;
            view.SetBaseline(target.Camera.View); view.RequestView(profile.View, 0);
            player.GetComponent<PlayerRespawnEntrance>().Direction = Direction;
            if (!preservePose) player.GetComponent<RelativeDragMovement>().ResetForCheckpoint(Direction);
            player.transform.rotation = Quaternion.Euler(0,0,PlayDirectionSpace.Rotation(Direction));
            presentation.Hide();
            PresentationChanged?.Invoke();
        }
        public void Cancel()
        {
            IntroPhase = MissionIntroPhase.None;
            EndMissionTravel();journey?.Cancel();
            handoff = false;
            if (presentation != null) presentation.Hide();
            if (player != null) player.SetCinematicLock(false);
            PresentationChanged?.Invoke();
        }
    }
}
