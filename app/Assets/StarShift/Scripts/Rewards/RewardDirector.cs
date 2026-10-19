using System;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEngine;

namespace StarShift.Rewards
{
    [DisallowMultipleComponent, RequireComponent(typeof(EncounterRunner), typeof(RunRewards))]
    public sealed class RewardDirector : MonoBehaviour
    {
        [SerializeField] private RewardDropTable defaultDrops;
        [SerializeField] private RewardPickup pickupPrefab;
        [SerializeField] private PlayerPickupReceiver receiver;
        [SerializeField, Min(1)] private int activePickupLimit = 6;
        private EncounterRunner runner;
        private EncounterCleanup cleanup;
        private ScenePrefabPool pool;
        private RunRewards rewards;
        private PlayerLives lives;
        private WeaponLoadout loadout;
        private readonly RewardRoller roller = new RewardRoller();
        public int ActivePickups { get; private set; }
        public int PickupsSpawned { get; private set; }
        public int PickupsCollected { get; private set; }
        public int PickupsExpired { get; private set; }
        private void Awake()
        {
            runner = GetComponent<EncounterRunner>(); cleanup = GetComponent<EncounterCleanup>();
            pool = GetComponent<ScenePrefabPool>(); rewards = GetComponent<RunRewards>();
            lives = receiver.GetComponent<PlayerLives>(); loadout = receiver.GetComponent<WeaponLoadout>();
            ResetForSession();
        }
        private void OnEnable() { runner.EnemyDefeated += OnEnemyDefeated; runner.RunStarted += ResetForSession; }
        private void OnDisable() { runner.EnemyDefeated -= OnEnemyDefeated; runner.RunStarted -= ResetForSession; }
        private void ResetForSession()
        {
            rewards.ResetForSession(); PickupsSpawned = PickupsCollected = PickupsExpired = 0;
            roller.Reset(Environment.TickCount);
        }
        public void SetTestSeed(int seed)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            roller.Reset(seed);
#endif
        }
        private void OnEnemyDefeated(EnemyHealth enemy, RewardDropTable table, Vector3 position)
        {
            rewards.AddKill(enemy.Definition.Score * (enemy.Modifier != null ? 2 : 1));
            table = table != null ? table : defaultDrops;
            if (table.Source == RewardSource.MajorCrate) Spawn(null, table, position);
            else
            {
                var item = roller.RollDirect(table, receiver, lives, loadout);
                if (item != null) Spawn(item, null, position);
            }
        }
        public RewardPickup Spawn(PickupDefinition item, RewardDropTable crate, Vector3 position)
        {
            if ((item == null && crate == null) || ActivePickups >= activePickupLimit || (runner.Stage != EncounterStage.Running && runner.Stage != EncounterStage.BossPostDeath)) return null;
            var pickup = pool.Rent(pickupPrefab.gameObject).GetComponent<RewardPickup>();
            pickup.transform.SetParent(transform, false);
            // Giữ reward trong viewport; drop ngoài edge vẫn có cơ hội collection.
            var camera = Camera.main;
            var viewport = camera.WorldToViewportPoint(position);
            viewport.x = Mathf.Clamp(viewport.x, .05f, .95f); viewport.y = Mathf.Clamp(viewport.y, .15f, .88f);
            ActivePickups++; PickupsSpawned++;
            pickup.Launch(item, crate, camera.ViewportToWorldPoint(viewport), cleanup, receiver, Collect, Release);
            return pickup;
        }
        private bool Collect(RewardPickup pickup)
        {
            var item = pickup.Item;
            if (pickup.Crate != null) item = roller.SelectMajor(pickup.Crate, receiver, lives, loadout, true);
            if (item == null) return false;
            var livesBefore = lives.CurrentLives;
            if (!receiver.TryCollect(item)) return false;
            if (pickup.Crate != null) { roller.RecordCrate(item, livesBefore); rewards.RecordSupply(); }
            PickupsCollected++;
            return true;
        }
        private void Release(RewardPickup pickup)
        {
            if (pickup.Remaining <= 0) PickupsExpired++;
            ActivePickups = Mathf.Max(0, ActivePickups - 1);
            pool.Return(pickup.gameObject);
        }
    }
}
