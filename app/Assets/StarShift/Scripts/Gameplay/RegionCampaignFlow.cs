using StarShift.Encounters;
using StarShift.Persistence;
using StarShift.Player;
using UnityEngine;
namespace StarShift.Gameplay
{
    [RequireComponent(typeof(EncounterRunner))]
    public sealed class RegionCampaignFlow : MonoBehaviour
    {
        [SerializeField] private LevelDefinition[] missions;
        [SerializeField, Min(.1f)] private float missionClearSeconds = 2.3f;
        private EncounterRunner runner;
        private CampaignLifecycle persistence;
        private GameplaySession session;
        private PlanetJourney journey;
        private EnemyFireGate enemyGate;
        private float elapsed;
        private bool titleShown;
        public bool IsTravelling { get; private set; }
        public bool IsMissionTravel => IsTravelling && NextMission != null;
        public bool DestinationReady { get; private set; }
        public LevelDefinition[] Missions => (LevelDefinition[])missions.Clone();
        public LevelDefinition FindMission(string id) => System.Array.Find(missions, m => m != null && m.Id == id);
        public LevelDefinition NextMission
        {
            get { var index = System.Array.IndexOf(missions, runner.Flow.Level); return index >= 0 && index + 1 < missions.Length ? missions[index + 1] : null; }
        }
        private void Awake()
        {
            runner=GetComponent<EncounterRunner>();persistence=GetComponent<CampaignLifecycle>();session=GetComponent<GameplaySession>();journey=GetComponent<PlanetJourney>();
            enemyGate=FindAnyObjectByType<EnemyFireGate>();
        }
        private void OnEnable() => runner.RunStarted += Cancel;
        private void OnDisable() { runner.RunStarted -= Cancel; Cancel(); }
        public bool QueueAdvance()
        {
            if(persistence.Mode==SessionMode.Replay || System.Array.IndexOf(missions,runner.Flow.Level)<0)return false;
            IsTravelling=true;DestinationReady=false;elapsed=0;titleShown=false;enemyGate.Block();
            if(NextMission==null)
            {
                session.PrepareMajorTravel();journey.Begin(()=>{
                    IsTravelling=false;DestinationReady=true;persistence.CompleteRegionTravel();session.ShowRegionReady();
                });
            }
            else runner.Flow.NotifyPresentationChanged();
            return true;
        }
        private void FixedUpdate()
        {
            if(!IsMissionTravel)return;
            elapsed+=Time.fixedDeltaTime;
            var next=NextMission;
            runner.Flow.PresentMissionTravel(Mathf.Clamp01((elapsed-.3f)/(missionClearSeconds-.3f)));
            if(!titleShown && elapsed>=1.95f){titleShown=true;runner.Flow.Announce(next.Intro.ArrivalTitle,next.MissionBriefing,next.Intro.MissionTitleSeconds);}
            if(elapsed<missionClearSeconds)return;
            IsTravelling=false;runner.Flow.EndMissionTravel();persistence.AdvanceMission(next,true);
        }
        public void RestoreDestination()
        {
            IsTravelling=false;DestinationReady=true;runner.Flow.FinishRegionTravel();session.ShowRegionReady();
        }
        public void Cancel()
        {
            IsTravelling=false;DestinationReady=false;
            if(runner!=null)runner.Flow.EndMissionTravel();if(journey!=null)journey.Cancel();
        }
    }
}
