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
        private readonly EncounterRewardQuota quota = new EncounterRewardQuota();
        private int rewardSeed = 410;
        private bool hasKilledCandidate;
        private Vector3 lastKilledPosition;
        public int GuaranteedMajorsRemaining => quota.Remaining;
        public int ActivePickups { get; private set; }
        public int PickupsSpawned { get; private set; }
        public int PickupsCollected { get; private set; }
        public int PickupsExpired { get; private set; }
        public event Action<PickupDefinition, RewardDropTable> DropSpawned;
        public event Action<PickupDefinition> ItemCollected;
        private void Awake()
        {
            runner = GetComponent<EncounterRunner>(); cleanup = GetComponent<EncounterCleanup>();
            pool = GetComponent<ScenePrefabPool>(); rewards = GetComponent<RunRewards>();
            lives = receiver.GetComponent<PlayerLives>(); loadout = receiver.GetComponent<WeaponLoadout>();
            ResetForSession();
        }
        private void OnEnable() { runner.EnemyDefeated += OnEnemyDefeated; runner.RunStarted += ResetForSession; runner.EncounterStarted += BeginEncounter; runner.EnemyEscaped += OnEnemyEscaped; runner.Completed += CompleteQuota; }
        private void OnDisable() { runner.EnemyDefeated -= OnEnemyDefeated; runner.RunStarted -= ResetForSession; runner.EncounterStarted -= BeginEncounter; runner.EnemyEscaped -= OnEnemyEscaped; runner.Completed -= CompleteQuota; }
        private void ResetForSession()
        {
            rewards.ResetForSession(); PickupsSpawned = PickupsCollected = PickupsExpired = 0;
            roller.Reset(rewardSeed);
            quota.Reset(rewardSeed, 0, 0);
        }
        private static bool Eligible(EnemyHealth enemy) => enemy.Definition.BodyDamagesPlayer &&
            enemy.Definition.MissileTier != MissileDamageTier.Boss && enemy.GetComponent<AsteroidVisualMotion>() == null;
        private void BeginEncounter(EncounterDefinition encounter, int token)
        {
            int seed = EncounterRewardQuota.Seed(runner.Flow.Level.Id, encounter.Id, rewardSeed);
            int enemies = 0;
            foreach (var group in encounter.Groups)
            {
                var prefab = group.EnemyPrefab != null ? group.EnemyPrefab : encounter.ScoutPrefab;
                if (group.GrantsRewards && Eligible(prefab.GetComponent<EnemyHealth>())) enemies += group.Count;
            }
            roller.Reseed(seed);
            quota.Reset(seed ^ 0x51f15e, enemies, encounter.GuaranteedMajorCount);
            hasKilledCandidate = false;
        }
        // Floor không bị mất khi normal pickup cap đã đầy; chỉ quota đã author được dùng slot này.
        private void DeliverFloor(PickupDefinition item, Vector3 position) => Spawn(item, null, position, true);
        private PickupDefinition SelectGuarantee(RewardDropTable table) => table.GuaranteedPickup != null ? table.GuaranteedPickup : roller.SelectMajor(table, receiver, lives, loadout, false);
        private void CompleteQuota(EncounterDefinition encounter)
        {
            // Timeline có thể hết giờ trước kill cuối; chỉ dùng candidate đã bị hạ, không thưởng cho escape.
            if (!hasKilledCandidate) return;
            while (quota.ResolveFallback()) DeliverFloor(SelectGuarantee(encounter.GuaranteeTable), lastKilledPosition);
        }
        private void OnEnemyEscaped(EnemyHealth enemy) { if (Eligible(enemy)) quota.ResolveEscape(); }
        public void SetTestSeed(int seed)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            rewardSeed = seed;
            roller.Reset(seed);
            if (runner.HasActiveRun) BeginEncounter(runner.Current, runner.Generation);
#endif
        }
        private void OnEnemyDefeated(EnemyHealth enemy, RewardDropTable table, Vector3 position)
        {
            rewards.AddKill(enemy.Definition.Score * (enemy.Modifier != null ? 2 : 1));
            table = table != null ? table : defaultDrops;
            if (table.Source == RewardSource.MajorCrate)
            {
                SpawnSupply(table, position);
                if (enemy.Definition.MissileTier == MissileDamageTier.Boss && table.Shard != null)
                {
                    var camera = Camera.main;
                    for (int i = 0; i < table.BossShardCount; i++)
                    {
                        var point = PlayDirectionSpace.Point(new Vector2(.07f + .86f * Mathf.Repeat(i * .618034f, 1), .58f + .27f * Mathf.Repeat(i * .414214f, 1)), runner.Flow.Direction);
                        var gold = Spawn(table.Shard, null, camera.ViewportToWorldPoint(new Vector3(point.x, point.y, -camera.transform.position.z)), true);
                        gold?.Eject(new Vector2(Mathf.Sin(i * 2.4f) * .8f, .6f + Mathf.Repeat(i * .37f, 1)));
                    }
                }
            }
            else
            {
                var item = roller.RollDirect(table, receiver, lives, loadout);
                bool deliverFloor = false;
                if (Eligible(enemy))
                {
                    hasKilledCandidate = true; lastKilledPosition = position;
                    var guarantee = runner.Current.GuaranteeTable;
                    bool satisfies = item != null && item.Kind != PickupKind.Shard &&
                        (guarantee == null || guarantee.GuaranteedPickup == null || item == guarantee.GuaranteedPickup);
                    deliverFloor = satisfies && quota.Remaining > 0;
                    if (quota.ResolveKill(satisfies))
                    {
                        deliverFloor = true;
                        item = SelectGuarantee(guarantee);
                    }
                }
                if (deliverFloor) DeliverFloor(item, position);
                else if (item != null) Spawn(item, null, position);
            }
        }
        private void SpawnSupply(RewardDropTable table, Vector3 position)
        {
            var core = roller.SelectMajor(table, receiver, lives, loadout, false, true);
            var support = roller.SelectMajor(table, receiver, lives, loadout, true, false);
            // Khi cả ba nhánh đã max, dùng thêm support hữu dụng thay Core không thể nhặt.
            if (core == null) core = roller.SelectMajor(table, receiver, lives, loadout, false, false);
            var camera = Camera.main;
            var center = camera.WorldToViewportPoint(position);
            if (runner.Flow.Direction == PlayDirection.Right) center.y = Mathf.Clamp(center.y, .12f, .88f);
            else center.x = Mathf.Clamp(center.x, .12f, .88f);
            position = camera.ViewportToWorldPoint(center);
            bool captured = false;
            Action capture = () => { if (captured) return; captured = true; rewards.RecordSupply(); };
            var side = (Vector3)PlayDirectionSpace.Offset(Vector2.right, runner.Flow.Direction);
            var first = Spawn(core, null, position - side * .35f, true);
            var second = Spawn(support, null, position + side * .35f, true);
            if (first != null) { first.Eject(new Vector2(-1.5f, 1)); first.Collected += capture; }
            if (second != null) { second.Eject(new Vector2(1.5f, 1)); second.Collected += capture; }
            if (support != null) roller.RecordCrate(support, lives.CurrentLives);
        }
        public RewardPickup Spawn(PickupDefinition item, RewardDropTable crate, Vector3 position) => Spawn(item, crate, position, false);
        private RewardPickup Spawn(PickupDefinition item, RewardDropTable crate, Vector3 position, bool guaranteed)
        {
            if ((item == null && crate == null) || !runner.HasActiveRun || runner.Stage == EncounterStage.Intro ||
                (!guaranteed && (ActivePickups >= activePickupLimit || (runner.Stage != EncounterStage.Running && runner.Stage != EncounterStage.ExitDrain && runner.Stage != EncounterStage.BossPostDeath)))) return null;
            var pickup = pool.Rent(pickupPrefab.gameObject).GetComponent<RewardPickup>();
            pickup.transform.SetParent(transform, false);
            // Giữ reward trong viewport; drop ngoài edge vẫn có cơ hội collection.
            var camera = Camera.main;
            var viewport = camera.WorldToViewportPoint(position);
            var local = PlayDirectionSpace.LocalPoint(viewport, runner.Flow.Direction);
            local.x = Mathf.Clamp(local.x, .05f, .95f); local.y = Mathf.Clamp(local.y, .15f, .88f);
            var point = PlayDirectionSpace.Point(local, runner.Flow.Direction);
            viewport.x = point.x; viewport.y = point.y;
            ActivePickups++; PickupsSpawned++;
            pickup.Launch(item, crate, camera.ViewportToWorldPoint(viewport), cleanup, receiver, Collect, Release, runner.Flow.Direction);
            DropSpawned?.Invoke(item, crate);
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
            ItemCollected?.Invoke(item);
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
