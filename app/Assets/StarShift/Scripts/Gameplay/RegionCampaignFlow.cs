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
        [SerializeField, Min(.1f)] private float missionClearSeconds = 1.5f;
        [SerializeField, Min(1)] private float regionTravelSeconds = 8;
        private EncounterRunner runner;
        private CampaignLifecycle persistence;
        private GameplaySession session;
        private PlayerGameplayGate player;
        private EnemyFireGate enemyGate;
        private float elapsed;
        private Vector3 departure;
        public bool IsTravelling { get; private set; }
        public bool DestinationReady { get; private set; }
        public LevelDefinition[] Missions => (LevelDefinition[])missions.Clone();
        public LevelDefinition FindMission(string id) => System.Array.Find(missions, m => m != null && m.Id == id);
        public LevelDefinition NextMission
        {
            get { var index = System.Array.IndexOf(missions, runner.Flow.Level); return index >= 0 && index + 1 < missions.Length ? missions[index + 1] : null; }
        }
        private void Awake()
        {
            runner = GetComponent<EncounterRunner>(); persistence = GetComponent<CampaignLifecycle>(); session = GetComponent<GameplaySession>();
            player = FindAnyObjectByType<PlayerGameplayGate>(); enemyGate = FindAnyObjectByType<EnemyFireGate>();
        }
        private void OnEnable() => runner.RunStarted += Cancel;
        private void OnDisable() { runner.RunStarted -= Cancel; Cancel(); }
        public bool QueueAdvance()
        {
            if (persistence.Mode == SessionMode.Replay || System.Array.IndexOf(missions, runner.Flow.Level) < 0) return false;
            IsTravelling = true; DestinationReady = false; elapsed = 0; departure = player.transform.position;
            player.SetCinematicLock(true); player.SetState(PlayerGameplayState.Transition); enemyGate.Block();
            player.GetComponent<InputSystemPointerInput>().CancelGesture(); return true;
        }
        private void FixedUpdate()
        {
            if (!IsTravelling) return;
            elapsed += Time.fixedDeltaTime;
            var next = NextMission;
            if (next != null)
            {
                if (elapsed < missionClearSeconds) return;
                IsTravelling = false; persistence.AdvanceMission(next); return;
            }
            runner.Flow.PresentRegionTravel(elapsed / regionTravelSeconds, departure);
            if (elapsed < regionTravelSeconds) return;
            IsTravelling = false; DestinationReady = true;
            runner.Flow.FinishRegionTravel(); persistence.CompleteRegionTravel(); session.ShowRegionComplete();
        }
        public void RestoreDestination()
        {
            IsTravelling = false; DestinationReady = true;
            runner.Flow.FinishRegionTravel(); session.ShowRegionComplete();
        }
        public void Cancel() { IsTravelling = false; DestinationReady = false; }
    }
}
