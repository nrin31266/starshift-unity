using System;
using StarShift.Encounters;
using StarShift.Player;
using StarShift.UI;
using UnityEngine;
namespace StarShift.Gameplay
{
    public enum PlanetJourneyPhase { None, Glide, Accelerate, Turn, Decelerate, Reveal, AwaitingTap, Approach, Travel, Brake }
    [RequireComponent(typeof(LevelFlow),typeof(EncounterRunner))]
    public sealed class PlanetJourney : MonoBehaviour
    {
        [SerializeField] private MissionIntroPresentation presentation;
        [SerializeField] private PlayerGameplayGate player;
        [SerializeField] private GameplayViewController view;
        [SerializeField, Min(.1f)] private float glideSeconds = .9f;
        [SerializeField, Min(.1f)] private float accelerationSeconds = 1.5f;
        [SerializeField, Min(.1f)] private float turnSeconds = 2;
        [SerializeField, Min(.1f)] private float decelerationSeconds = 1.5f;
        [SerializeField, Min(.1f)] private float revealSeconds = 1.3f;
        private LevelFlow flow;
        private EncounterRunner runner;
        private RelativeDragMovement movement;
        private float elapsed;
        private int generation;
        private Vector3 departure;
        private Action completed;
        private LevelDefinition destination;
        public PlanetJourneyPhase Phase { get; private set; }
        public bool IsCinematic => IsActive && Phase < PlanetJourneyPhase.Approach;
        public bool IsActive => Phase != PlanetJourneyPhase.None;
        public bool IsAwaitingTap => Phase == PlanetJourneyPhase.AwaitingTap;
        public float HudOpacity => Phase == PlanetJourneyPhase.Brake ? Mathf.Clamp01(elapsed/.8f) : IsActive ? 0 : 1;
        public float TravelSeconds => glideSeconds+accelerationSeconds+turnSeconds+decelerationSeconds+revealSeconds;
        private void Awake(){flow=GetComponent<LevelFlow>();runner=GetComponent<EncounterRunner>();movement=player.GetComponent<RelativeDragMovement>();}
        public void Begin(LevelDefinition next, Action onCompleted)
        {
            Cancel(); destination=next; completed=onCompleted;generation=runner.Generation;departure=player.transform.position;
            player.SetCinematicLock(true);player.SetState(PlayerGameplayState.Transition);player.GetComponent<InputSystemPointerInput>().CancelGesture();
            Enter(PlanetJourneyPhase.Glide);
        }
        public bool AcceptTap()
        {
            if(!IsAwaitingTap)return false;
            player.GetComponent<InputSystemPointerInput>().CancelGesture();
            Enter(PlanetJourneyPhase.Approach);
            presentation.Present(MissionIntroPhase.Approach,0,PlayDirection.Right);
            flow.ReleaseApproach(PlayDirection.Right); return true;
        }
        private float Duration => Phase switch{
            PlanetJourneyPhase.Glide=>glideSeconds,PlanetJourneyPhase.Accelerate=>accelerationSeconds,PlanetJourneyPhase.Turn=>turnSeconds,
            PlanetJourneyPhase.Decelerate=>decelerationSeconds,PlanetJourneyPhase.Reveal=>revealSeconds,
            PlanetJourneyPhase.Approach=>.7f,PlanetJourneyPhase.Travel=>1.8f,PlanetJourneyPhase.Brake=>.8f,_=>float.PositiveInfinity};
        private void Enter(PlanetJourneyPhase phase)
        {
            Phase=phase;elapsed=0;
            if(phase==PlanetJourneyPhase.Accelerate)view.RequestView(flow.Level.Intro.TravelView,accelerationSeconds);
            if(phase==PlanetJourneyPhase.Decelerate)view.RequestView(flow.Level.Intro.ApproachView,decelerationSeconds);
            if(phase==PlanetJourneyPhase.Approach)view.RequestView(flow.Level.Intro.TravelView,.7f);
            if(phase==PlanetJourneyPhase.Brake)view.RequestView((destination.Encounters[0].CameraOverride != null ? destination.Encounters[0].CameraOverride : destination.Camera).View,.8f);
            flow.NotifyPresentationChanged();
            if(phase==PlanetJourneyPhase.AwaitingTap)flow.Announce("HÀNH TINH TIẾP THEO","Hành trình tiếp tục về phía trước.",0);
        }
        private void FixedUpdate()
        {
            if(!IsActive)return;
            if(generation!=runner.Generation){Cancel();return;}
            elapsed+=Time.fixedDeltaTime;
            float t=Mathf.Clamp01(elapsed/Duration);
            float heading=Phase==PlanetJourneyPhase.Glide?0:Phase==PlanetJourneyPhase.Accelerate?Mathf.Lerp(0,-20,t):Phase==PlanetJourneyPhase.Turn?Mathf.Lerp(-20,-90,Mathf.SmoothStep(0,1,t)):-90;
            float speed=Phase switch{PlanetJourneyPhase.Glide=>.045f,PlanetJourneyPhase.Accelerate=>Mathf.Lerp(.045f,2.8f,t),PlanetJourneyPhase.Turn=>Mathf.Lerp(2.8f,3.6f,t),PlanetJourneyPhase.Decelerate=>Mathf.Lerp(3.6f,.04f,Mathf.SmoothStep(0,1,t)),_=>.015f};
            float palette=Phase==PlanetJourneyPhase.Turn?t:Phase>=PlanetJourneyPhase.Decelerate?1:0;
            if(Phase<PlanetJourneyPhase.Approach)
            {
                float travelProgress=Phase==PlanetJourneyPhase.Glide?.025f*t:Phase==PlanetJourneyPhase.Accelerate?.025f+.2f*t:Phase==PlanetJourneyPhase.Turn?.225f+.55f*t:Phase==PlanetJourneyPhase.Decelerate?.775f+.225f*t:1;
                var destination=Camera.main.ViewportToWorldPoint(new Vector3(.18f,.5f,-Camera.main.transform.position.z));
                var pos=Vector3.Lerp(departure,destination,Mathf.SmoothStep(0,1,travelProgress))+Vector3.up*Mathf.Sin(travelProgress*Mathf.PI)*1.3f;
                movement.SetCinematicPosition(pos);player.transform.rotation=Quaternion.Euler(0,0,heading);
                float reveal=Phase==PlanetJourneyPhase.Reveal?.08f+.92f*t:Phase==PlanetJourneyPhase.AwaitingTap?1:Phase==PlanetJourneyPhase.Decelerate?.08f*t:0;
                presentation.PresentPlanetJourney(heading,speed,palette,reveal);
            }
            else presentation.Present(Phase==PlanetJourneyPhase.Approach?MissionIntroPhase.Approach:Phase==PlanetJourneyPhase.Travel?MissionIntroPhase.Travel:MissionIntroPhase.Brake,t,PlayDirection.Right);
            if(IsAwaitingTap||elapsed<Duration)return;
            if(Phase!=PlanetJourneyPhase.Brake){Enter(Phase+1);return;}
            Phase=PlanetJourneyPhase.None;presentation.Hide();flow.FinishRegionTravel(true, destination);
            var callback=completed;completed=null;flow.NotifyPresentationChanged();callback?.Invoke();
        }
        public void Cancel(){Phase=PlanetJourneyPhase.None;completed=null;if(presentation!=null)presentation.Hide();}
        private void OnDisable()=>Cancel();
    }
}
