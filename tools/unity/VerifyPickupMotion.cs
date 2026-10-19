using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Encounters;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.Rewards;
using UnityEditor;
using UnityEngine;

public static class VerifyPickupMotion
{
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying, "Play Mode required");
        var runner = UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
        runner.StartRun("LEVEL_START", true, true, null, null);
        var gate = GameObject.Find("Player").GetComponent<PlayerGameplayGate>();
        gate.SetState(PlayerGameplayState.Transition);
        var director = runner.GetComponent<RewardDirector>();
        var camera = Camera.main;
        var tick = typeof(RewardPickup).GetMethod("FixedUpdate", Fields);
        var count = typeof(RewardPickup).GetField("bounceCount", Fields);
        var velocity = typeof(RewardPickup).GetField("fallVelocity", Fields);
        var session = runner.GetComponent<GameplaySession>();
        PickupDefinition Item(string path) => AssetDatabase.LoadAssetAtPath<PickupDefinition>(path);
        Vector3 At(float y) => camera.ViewportToWorldPoint(new Vector3(.8f, y, -camera.transform.position.z));
        RewardPickup Spawn(PickupDefinition item, float y) => director.Spawn(item, null, At(y));
        int Bounces(RewardPickup pickup) => (int)count.GetValue(pickup);
        void Step(RewardPickup pickup) { if (pickup.IsActive) tick.Invoke(pickup, null); }
        object result;
        try
        {
            var shard = Item("Assets/StarShift/Data/Rewards/StellarShard.asset");
            var normal = Spawn(shard, .3f);
            var grace = Spawn(shard, .3f); grace.PreserveForGrace(12);
            var late = Spawn(shard, .3f);
            bool lateGrace = false;
            for (int i = 0; i < 500 && (normal.IsActive || grace.IsActive || late.IsActive); i++)
            {
                Step(normal); Step(grace);
                Check(Vector3.Distance(normal.transform.position, grace.transform.position) < .0001f && Bounces(normal) == Bounces(grace), "Grace changed shard trajectory");
                if (late.IsActive && Bounces(late) == 2 && camera.WorldToViewportPoint(late.transform.position).y < .05f && !lateGrace)
                {
                    late.PreserveForGrace(12); lateGrace = true;
                    var y = late.transform.position.y; Step(late);
                    Check(late.transform.position.y < y, "Late grace pushed shard upward");
                }
                else Step(late);
            }
            Check(lateGrace && !normal.IsActive && !grace.IsActive && !late.IsActive, "Shards failed to leave viewport");
            Check(Bounces(normal) == 2 && Bounces(grace) == 2 && Bounces(late) == 2, "Shards did not bounce exactly twice");
            var reused = Spawn(shard, .3f);
            Check(Bounces(reused) == 0 && (float)velocity.GetValue(reused) < 0, "Pool retained bounce/velocity state");
            reused.Release();
            var gold = Item("Assets/StarShift/Data/RegionA/Boss/BossGold.asset");
            for (int i = 0; i < 28; i++)
            {
                var pickup = Spawn(gold, .58f + .27f * Mathf.Repeat(i * .414214f, 1));
                pickup.Eject(new Vector2(Mathf.Sin(i * 2.4f) * .8f, .6f + Mathf.Repeat(i * .37f, 1)));
                pickup.PreserveForGrace(12);
                for (int step = 0; step < 600 && pickup.IsActive; step++) Step(pickup);
                Check(!pickup.IsActive && Bounces(pickup) == 2 && pickup.Remaining > 0 && camera.WorldToViewportPoint(pickup.transform.position).y < -.06f, "Boss gold failed two-bounce exit: " + i);
            }
            var support = Spawn(Item("Assets/StarShift/Data/Rewards/Overdrive.asset"), .15f);
            support.PreserveForGrace(12);
            for (int i = 0; i < 300; i++) Step(support);
            Check(support.IsActive && camera.WorldToViewportPoint(support.transform.position).y >= .089f, "Support grace retention regressed");
            support.Release();
            var live = Spawn(gold, .5f); live.PreserveForGrace(12);
            session.Pause(); var paused = live.transform.position;
            await Task.Delay(250);
            Check(session.IsPaused && live.transform.position == paused, "Pause moved gold");
            session.Resume(); gate.SetState(PlayerGameplayState.Transition);
            await Task.Delay(250);
            Check(live.transform.position.y < paused.y, "Gold did not resume falling");
            gate.SetState(PlayerGameplayState.PostEntryInvincibility);
            Check(live.TryCollect() && !live.TryCollect(), "Gold collection was lost or duplicated");
            Check(director.ActivePickups == 0, "Pickup pool count leak");
            result = new { status = "PASS", normalBounces = 2, graceBounces = 2, lateGraceNoJump = true, pooledStateReset = true, bossGoldTwoBounceExits = 28, supportRetained = true, pauseResume = true, collectionOnce = true };
        }
        finally { if (session.IsPaused) session.Resume(); runner.StopRun(); }
        Directory.CreateDirectory("../.design/m5-pickup-motion");
        File.WriteAllText("../.design/m5-pickup-motion/result.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
