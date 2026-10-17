using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Dev;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[DefaultExecutionOrder(1000)]
public sealed class EncounterObserver : MonoBehaviour
{
    public int MaximumTelegraphs;
    public int Shots;
    public int ExitShots;
    public int OffscreenShots;
    public float MinimumGap = float.PositiveInfinity;
    public float LastShot = float.NegativeInfinity;
    readonly Dictionary<EnemyProjectile, float> lifetimes = new Dictionary<EnemyProjectile, float>();
    static readonly FieldInfo Lifetime = typeof(EnemyProjectile).GetField("remainingLifetime", BindingFlags.NonPublic | BindingFlags.Instance);
    void FixedUpdate()
    {
        MaximumTelegraphs = Mathf.Max(MaximumTelegraphs, FindObjectsByType<SingleFireAttack>().Count(attack => attack.IsTelegraphing));
        var active = FindObjectsByType<EnemyProjectile>();
        foreach (var shot in active)
        {
            var lifetime = (float)Lifetime.GetValue(shot);
            if (!lifetimes.TryGetValue(shot, out var previous) || lifetime > previous + .01f)
            {
                Shots++;
                var viewport = Camera.main.WorldToViewportPoint(shot.transform.position);
                if (viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) OffscreenShots++;
                MinimumGap = Mathf.Min(MinimumGap, Time.fixedTime - LastShot);
                LastShot = Time.fixedTime;
                var flight = shot.Owner != null ? shot.Owner.GetComponent<EntryHoldFlight>() : null;
                if (flight != null && flight.CanShoot && !flight.IsHolding) ExitShots++;
            }
            lifetimes[shot] = lifetime;
        }
        foreach (var old in lifetimes.Keys.Where(shot => shot == null || !shot.gameObject.activeInHierarchy).ToArray()) lifetimes.Remove(old);
    }
}

public static class VerifyL1Encounters
{
    static EncounterRunner Runner => UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static EnemyHealth[] Enemies => UnityEngine.Object.FindObjectsByType<EnemyHealth>().Where(enemy => enemy.IsAlive).ToArray();
    static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Isolate()
    {
        Runner.StopRun();
    }

    public static async Task<object> Run(int width, float size, bool lower70)
    {
        using (var rig = new Rig(width, size, lower70))
        {
            var frame = Time.frameCount;
            var completed = new List<string>();
            Action<EncounterDefinition> onComplete = definition =>
            {
                Require(Enemies.Length == 0 && UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length == 0,
                    "Completion published before hostile cleanup.");
                completed.Add(definition.Id);
            };
            Runner.Completed += onComplete;
            try
            {
                Runner.StartRun("LEVEL_START", false);
                await Until(() => Runner.SpawnedCount == 3 && Enemies.All(e => e.GetComponent<EntryHoldFlight>().IsHolding), 3500, "A did not settle.");
                Require(Runner.KilledCount == 0 && Runner.CompletionCount == 0, "A completed with live required enemies.");
                var aIds = Enemies.Select(e => e.GetEntityId()).ToArray();
                var x = Enemies.Select(e => rig.Camera.WorldToViewportPoint(e.transform.position).x).OrderBy(v => v).ToArray();
                Require(Mathf.Abs(x[1] - .5f) < .001f && Mathf.Abs((x[1]-x[0])-.18f) < .001f, "A formation projection failed.");
                await Until(() => rig.Observer.Shots >= 2, 6500, "A coordinated fire missing.");
                Require(rig.Observer.MaximumTelegraphs == 1 && rig.Observer.MinimumGap >= 1.35f, "A fire overlapped or ignored spacing.");
                foreach (var enemy in Enemies.OrderBy(e => e.transform.position.x).Take(2).ToArray()) await rig.KillWithPulse(enemy);
                var last = Enemies.Single();
                last.gameObject.SetActive(false);
                await Task.Delay(160);
                Require(Runner.IsRunning && Runner.CompletionCount == 0 && Runner.KilledCount == 2, "Disable counted as required death.");
                last.gameObject.SetActive(true);
                Require(last.CurrentHp == last.MaxHp, "Re-enable did not reset Scout HP.");
                await Until(() => last.GetComponent<EntryHoldFlight>().IsHolding, 2000, "Re-enabled Scout stuck in entry.");
                await rig.KillWithPulse(last);
                Require(completed.SequenceEqual(new[] { "ENCOUNTER_1" }), "A completion count wrong.");
                await Until(() => Runner.CurrentIndex == 1 && Runner.SpawnedCount == Runner.Current.EnemyCount, 4000, "A/B handoff missing.");
                var b = Runner.Current;
                Require(Enemies.Any(e => aIds.Contains(e.GetEntityId())), "Scout pool not reused in B.");
                Require(Enemies.All(e => e.CurrentHp == e.MaxHp), "Pooled Scout retained damage.");
                Require(b.Groups[0].EntryAnchor.x < 0 && b.Groups[1].EntryAnchor.x > 1 &&
                    b.Groups[1].StartTime < 1f, "B entries are not coordinated from opposite sides.");
                var moving = Enemies.Last();
                var before = rig.Camera.WorldToViewportPoint(moving.transform.position);
                await Task.Delay(250);
                var after = rig.Camera.WorldToViewportPoint(moving.transform.position);
                Require(Vector2.Distance(before, after) > .005f, "B entry not moving.");
                await Until(() => Enemies.Length == b.EnemyCount && Enemies.All(e => e.GetComponent<EntryHoldFlight>().IsHolding),
                    7000, "B did not assemble the full formation.");
                var formation = Enemies.Select(e => rig.Camera.WorldToViewportPoint(e.transform.position)).OrderBy(p => p.x).ToArray();
                Require(formation.Length == 6 && Mathf.Abs(formation[0].x-.19f) < .001f &&
                    Mathf.Abs(formation[5].x-.81f) < .001f && formation[2].y > formation[0].y + .1f &&
                    formation[3].y > formation[5].y + .1f, "B combined formation missing.");
                await SaveCapture("B-formation");
                await Until(() => !Runner.IsRunning && Runner.CompletionCount == 2,
                    (int)((b.Duration - Runner.Elapsed + 12) * 1000), "B timeline did not complete.");
                Require(Runner.Elapsed >= b.Duration && Runner.KilledCount == 0 && Runner.SpawnedCount == b.EnemyCount,
                    "B used kills/exit as completion or spawn sequence wrong.");
                Require(completed.SequenceEqual(new[] { "ENCOUNTER_1", "ENCOUNTER_2" }), "Completion double signal or order wrong.");
                Require(rig.Observer.MaximumTelegraphs == 1 && rig.Observer.ExitShots > 0 && rig.Observer.MinimumGap >= .85f &&
                    rig.Observer.OffscreenShots == 0,
                    "B firing missing during exit, overlapped or ignored spacing.");
                var shots = rig.Observer.Shots;
                await Task.Delay(500);
                Require(rig.Observer.Shots == shots && Enemies.Length == 0, "Stale fire after completion.");
                Require(Time.frameCount > frame + 100, "Player loop did not advance.");
                return new { passed = true, width, size, lower70, pulseKillsA = 3, timelineB = true,
                    pooledReuse = true, completions = completed, rig.Observer.Shots, rig.Observer.ExitShots,
                    rig.Observer.MaximumTelegraphs, rig.Observer.MinimumGap, rig.Observer.OffscreenShots,
                    frames = Time.frameCount - frame };
            }
            finally { Runner.Completed -= onComplete; }
        }
    }

    public static async Task<object> Death()
    {
        using (var rig = new Rig(1600, 6, false, false))
        {
            var player = rig.Player;
            var life = player.GetComponent<PlayerDamageLifecycle>();
            var gate = player.GetComponent<PlayerGameplayGate>();
            var lives = player.GetComponent<PlayerLives>();
            var fire = UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>();
            life.enabled = false; life.enabled = true;
            Runner.StartRun("ENCOUNTER_1", false);
            await Until(() => Enemies.Length == 3 && Enemies.All(e => e.GetComponent<EntryHoldFlight>().IsHolding), 3500, "Death test A not ready.");
            await rig.KillWithPulse(Enemies.OrderBy(e => e.transform.position.x).First());
            foreach (var p in UnityEngine.Object.FindObjectsByType<PlayerProjectile>()) p.Release();
            var survivors = Enemies.ToArray();
            await Until(() => lives.CurrentLives == 2, 10000, "Real encounter fire did not hit Player.");
            Require(gate.State == PlayerGameplayState.Dying && !fire.CanFire &&
                UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length == 0, "Death did not block/clear encounter shots.");
            Require(Enemies.Length == survivors.Length && survivors.All(e => Enemies.Contains(e)) &&
                Runner.SpawnedCount == 3 && Runner.KilledCount == 1 && Runner.IsRunning, "Death reset encounter/survivors.");
            await Until(() => gate.State == PlayerGameplayState.PostEntryInvincibility, 2200, "Encounter respawn failed.");
            Require(!fire.CanFire && gate.CanFire && !gate.CanTakeDamage, "Grace/protection permission wrong.");
            var elapsed = Runner.Elapsed;
            var pose = player.transform.position;
            Time.timeScale = 0;
            await Task.Delay(150);
            Require(Runner.Elapsed == elapsed && player.transform.position == pose && !fire.CanFire, "Encounter advanced during pause.");
            Time.timeScale = 1;
            var shots = rig.Observer.Shots;
            await Task.Delay(650);
            Require(!fire.CanFire && rig.Observer.Shots == shots, "Shot escaped enemy fire grace.");
            await Until(() => fire.CanFire, 700, "Grace did not end.");
            Require(gate.State == PlayerGameplayState.PostEntryInvincibility, "Invincibility did not outlast fire grace.");
            await Until(() => rig.Observer.Shots > shots, 2600, "Surviving Scout did not resume fire.");
            foreach (var enemy in Enemies.OrderBy(e => e.transform.position.x).ToArray()) await rig.KillWithPulse(enemy);
            await Until(() => Runner.CurrentIndex == 1, 1800, "Encounter did not resume/handoff after death.");
            return new { passed = true, realProjectileDeath = true, lives = lives.CurrentLives,
                survivorsRetained = true, respawn = true, pause = true, protectionAndGrace = true, pulseFinish = true };
        }
    }

    public static async Task<object> Lifecycle()
    {
        using (var rig = new Rig(2000, 6.8f, false))
        {
            Runner.StartRun("ENCOUNTER_1", true);
            await Until(() => Enemies.Any(e => e.GetComponent<SingleFireAttack>().IsTelegraphing), 4000, "No telegraph to kill.");
            var target = Enemies.First(e => e.GetComponent<SingleFireAttack>().IsTelegraphing);
            var targetRoot = target.gameObject;
            target.ReceivePlayerHit(target.MaxHp, rig.Player);
            await Task.Delay(600);
            Require(!UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Any(p => p.Owner == targetRoot), "Killed telegraph fired late.");
            var firstIds = Enemies.Select(e => e.GetEntityId()).ToArray();
            Runner.StopRun();
            Require(Enemies.Length == 0 && UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length == 0, "Stop left hostile objects.");
            Runner.StartRun("ENCOUNTER_1", true);
            await Until(() => Enemies.Length == 3, 1600, "Replay spawn failed.");
            Require(Enemies.Any(e => firstIds.Contains(e.GetEntityId())) && Enemies.All(e=>e.CurrentHp==e.MaxHp), "Replay did not reuse/reset Scouts.");
            var time = Runner.Elapsed;
            Time.timeScale = 0;
            var poses = Enemies.Select(e => e.transform.position).ToArray();
            await Task.Delay(160);
            Require(Runner.Elapsed == time && Enemies.Select(e=>e.transform.position).SequenceEqual(poses), "Spawn/entry advanced under pause.");
            Time.timeScale = 1;
            await Until(() => Enemies.Any(e=>e.GetComponent<SingleFireAttack>().IsTelegraphing), 3000, "Reset fire not ready.");
            Runner.enabled = false;
            await Task.Delay(600);
            Require(Enemies.Length == 0 && UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length == 0, "Runner disable left delayed fire.");
            Runner.enabled = true;
            Runner.StartRun("LEVEL_START", false);
            await Until(() => Enemies.Length == 3, 1600, "Runner re-enable replay failed.");
            foreach (var enemy in Enemies) enemy.ReceivePlayerHit(enemy.MaxHp, rig.Player);
            await Until(() => Runner.CompletionCount == 1 && !Runner.IsRunning, 300, "Handoff fixture did not complete A.");
            Runner.StartRun("ENCOUNTER_2", true);
            await Task.Delay(1200);
            Require(Runner.CurrentIndex == 1 && Runner.Elapsed > 1.1f && Runner.SpawnedCount == Runner.Current.EnemyCount,
                "Old handoff reset or duplicated the new run.");
            rig.Player.GetComponent<PlayerGameplayGate>().SetState(PlayerGameplayState.GameOver);
            await Task.Delay(100);
            Require(!Runner.IsRunning && Enemies.Length == 0, "GameOver left schedule active.");
            var dev = Runner.GetComponent<DevFastStart>();
            Set(dev, "devModeEnabled", true); Set(dev, "startPoint", "ENCOUNTER_2"); Set(dev, "standalone", true);
            dev.Replay();
            Require(Runner.CurrentIndex == 1 && Runner.IsRunning && rig.Player.GetComponent<PlayerLives>().CurrentLives == 3,
                "Dev B jump did not reset runtime session.");
            await Task.Delay(500);
            dev.Replay();
            Require(Runner.Elapsed == 0 && Runner.SpawnedCount == 0 && Enemies.Length == 0, "Dev replay retained schedule/enemies.");
            await Task.Delay(120);
            Require(Enemies.Length == 2 && Runner.SpawnedCount == 2, "Dev replay did not spawn one mirrored pair.");
            Set(dev, "startPoint", "ENCOUNTER_1"); dev.Replay();
            Require(Runner.CurrentIndex == 0 && Runner.IsRunning, "Dev A jump failed.");
            Set(dev, "startPoint", "NOT_A_REAL_START");
            bool rejected = false;
            try { dev.Replay(); } catch (ArgumentException) { rejected = true; }
            Require(rejected && !Runner.IsRunning && Enemies.Length == 0, "Invalid dev start silently fell back.");
            Set(dev, "devModeEnabled", false); Set(dev, "startPoint", "LEVEL_START"); Set(dev, "standalone", false);
            return new { passed = true, killTelegraph = true, pooling = true, pause = true,
                disableDuringTelegraph = true, resetDuringHandoff = true, gameOverStops = true,
                devJumpReplay = true, invalidStartRejected = true };
        }
    }

    public static async Task<object> Offscreen()
    {
        var results = new List<object>();
        foreach (var width in new[] { 1600, 2000 })
        using (var rig = new Rig(width, width == 1600 ? 6 : 6.8f, false))
        {
            var original = (EncounterDefinition[])Runner.GetType().GetField("encounters", Fields).GetValue(Runner);
            var definition = UnityEngine.Object.Instantiate(original[1]);
            var fixture = new GameObject("OffscreenFlightFixture");
            fixture.SetActive(false);
            var template = UnityEngine.Object.Instantiate(definition.ScoutPrefab, fixture.transform);
            var sourceAttack = template.GetComponent<SingleFireAttack>();
            var profile = UnityEngine.Object.Instantiate((SingleFireDefinition)typeof(SingleFireAttack)
                .GetField("definition", Fields).GetValue(sourceAttack));
            Set(profile, "initialDelay", .04f); Set(profile, "telegraphDuration", .25f); Set(profile, "cooldown", .02f);
            Set(sourceAttack, "definition", profile);
            definition.ScoutPrefab = template;
            definition.Duration = 2f;
            definition.ShotSpacing = definition.VolleyPause = .02f;
            definition.ShotsPerVolley = 1;
            var ids = new HashSet<UnityEngine.EntityId>();
            int cancelledAtEdge = 0;
            try
            {
                Set(Runner, "encounters", new[] { definition });
                foreach (var exit in new[] { new Vector2(-2, .78f), new Vector2(3, .78f), new Vector2(.5f, 3), new Vector2(.5f, -2) })
                {
                    definition.Groups = new[] { new ScoutSpawnGroup { Count = 1, EntryAnchor = new Vector2(.5f, .78f),
                        HoldAnchor = new Vector2(.5f, .78f), EntryDuration = .02f, HoldDuration = .12f,
                        ExitAnchor = exit, ExitDuration = 1.5f } };
                    Runner.StartRun("ENCOUNTER_2", true);
                    await Until(() => Enemies.Length == 1, 500, "Offscreen fixture did not spawn.");
                    var scout = Enemies.Single(); ids.Add(scout.GetEntityId());
                    var flight = scout.GetComponent<EntryHoldFlight>();
                    var attack = scout.GetComponent<SingleFireAttack>();
                    bool telegraphAtEdge = false;
                    await Until(() => !scout.gameObject.activeInHierarchy, 1600, "Offscreen exit not returned to pool.", () =>
                    {
                        var p = rig.Camera.WorldToViewportPoint(scout.transform.position);
                        if (attack.IsTelegraphing && (p.x < .15f || p.x > .85f || p.y < .15f || p.y > .85f)) telegraphAtEdge = true;
                    });
                    if (telegraphAtEdge) cancelledAtEdge++;
                    Require(flight.IsOutsideView && !attack.IsTelegraphing && scout.CurrentHp == scout.MaxHp &&
                        Runner.KilledCount == 0 && Runner.IsRunning, "Offscreen release killed enemy or completed timeline.");
                    var shots = rig.Observer.Shots;
                    await Task.Delay(300);
                    Require(rig.Observer.Shots == shots && rig.Observer.OffscreenShots == 0,
                        "Pending offscreen attack launched back into gameplay.");
                }
                Runner.StopRun();
                definition.Completion = EncounterCompletion.Eliminate;
                definition.Groups = new[] { new ScoutSpawnGroup { Count = 1, EntryAnchor = new Vector2(.5f, 0),
                    HoldAnchor = new Vector2(.5f, 0), EntryDuration = .02f } };
                var before = rig.Observer.Shots;
                Runner.StartRun("ENCOUNTER_2", true);
                await Task.Delay(650);
                var edge = Enemies.Single();
                var edgeFlight = edge.GetComponent<EntryHoldFlight>();
                Require(edgeFlight.CanShoot && !edgeFlight.IsPointVisible(edge.transform.Find("Muzzle").position) &&
                    !edge.GetComponent<SingleFireAttack>().IsTelegraphing && rig.Observer.Shots == before,
                    "Visible body with offscreen muzzle still fired.");
                Require(ids.Count == 1 && cancelledAtEdge > 0 && rig.Observer.OffscreenShots == 0,
                    "Offscreen fixture did not cover pending telegraph cancellation and pooled reuse.");
                results.Add(new { width, edges = 4, cancelledAtEdge, pooledReuse = true, offscreenMuzzleBlocked = true,
                    rig.Observer.OffscreenShots });
            }
            finally
            {
                Runner.StopRun();
                Set(Runner, "encounters", original);
                foreach (var attack in Runner.GetComponentsInChildren<SingleFireAttack>(true))
                    if ((SingleFireDefinition)typeof(SingleFireAttack).GetField("definition", Fields).GetValue(attack) == profile)
                        UnityEngine.Object.DestroyImmediate(attack.gameObject);
                UnityEngine.Object.DestroyImmediate(fixture);
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(profile);
            }
        }
        return new { passed = true, results };
    }

    public static async Task<object> QuickTuning()
    {
        using (var rig = new Rig(1600, 6, false))
        {
            Runner.StartRun("ENCOUNTER_1", true);
            await Until(() => Enemies.Length == 3 && Enemies.All(e => e.GetComponent<EntryHoldFlight>().IsHolding),
                3000, "Quick tuning fixture did not settle.");
            var scout = Enemies.OrderBy(e => e.transform.position.x).First();
            var times = new List<float>();
            scout.Damaged += () => times.Add(Time.fixedTime);
            Require(scout.MaxHp == 4 && scout.CurrentHp == 4, "Scout HP was not doubled.");
            await rig.KillWithPulse(scout);
            Require(times.Count == 4 && scout.CurrentHp == 0, "Scout did not require four real Pulse hits.");
            var gaps = times.Skip(1).Select((time, index) => time - times[index]).ToArray();
            Require(gaps.All(gap => Mathf.Abs(gap - .17f) <= .025f), "Pulse interval tuning not reflected in real hits.");
            var pulse = AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>("Assets/StarShift/Data/Weapons/PulseLaserLv1.asset");
            var projectile = pulse.ProjectilePrefab;
            Require(Mathf.Abs(pulse.FireInterval - .17f) < .0001f &&
                Mathf.Abs(projectile.transform.Find("Visual").localScale.x - .8f) < .0001f &&
                Mathf.Abs(projectile.GetComponent<BoxCollider2D>().size.x - .08f) < .0001f,
                "Pulse prefab width/timing was not saved.");
            Runner.StartRun("ENCOUNTER_2", true);
            Require(Runner.Current.Duration == 14 && Runner.Current.Groups.All(g => g.HoldDuration <= 3 && g.ExitDuration == 7.5f),
                "Quick combat or unchanged slow exit tuning missing.");
            await Until(() => !Runner.IsRunning, 24500, "Shortened B did not complete.");
            Require(Runner.CompletionCount == 1 && Enemies.Length == 0 && rig.Observer.OffscreenShots == 0 &&
                UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length == 0, "Shortened B left stale/offscreen threats.");
            return new { passed = true, scoutHp = 4, realPulseHits = times.Count, hitIntervals = gaps,
                visualWidthScale = .8f, colliderWidth = .08f, combatHold = "3 / 3 s", exitDuration = 7.5f,
                timeline = Runner.Elapsed, rig.Observer.OffscreenShots };
        }
    }

    public static async Task<object> DepartureFeedback()
    {
        using (var rig = new Rig(2000, 6.8f, false))
        {
            var original = (EncounterDefinition[])Runner.GetType().GetField("encounters", Fields).GetValue(Runner);
            var definition = UnityEngine.Object.Instantiate(original[1]);
            EnemyProjectile shot = null;
            try
            {
                Time.timeScale = 3;
                Runner.StartRun("ENCOUNTER_2", true);
                await Until(() => Enemies.Length == 6, 600, "Mirrored entry did not spawn three pairs.");
                float symmetryError = 0;
                await Until(() => Enemies.All(e => e.GetComponent<EntryHoldFlight>().IsHolding), 1800,
                    "Faster entry did not finish.", () =>
                    {
                        var points = Enemies.Select(e => rig.Camera.WorldToViewportPoint(e.transform.position)).OrderBy(p => p.x).ToArray();
                        for (int i = 0; i < 3; i++)
                        {
                            symmetryError = Mathf.Max(symmetryError, Mathf.Abs(points[i].x + points[5-i].x - 1));
                            symmetryError = Mathf.Max(symmetryError, Mathf.Abs(points[i].y - points[5-i].y));
                        }
                    });
                Require(symmetryError < .001f && Runner.Current.Groups.All(g => g.StartTime == 0 && g.EntryDuration == 3.2f),
                    "Two wings entered with different timing/slot order: " + symmetryError);
                var damaged = Enemies.First();
                var hull = damaged.transform.Find("Visual/HullPlaceholder").GetComponent<SpriteRenderer>();
                var normal = hull.color;
                damaged.ReceivePlayerHit(1, rig.Player);
                await Task.Delay(60);
                Require(damaged.CurrentHp == 3 && hull.color == normal, "3/4 HP already showed persistent damage.");
                damaged.ReceivePlayerHit(1, rig.Player);
                await Task.Delay(60);
                var damagedTint = hull.color;
                Require(damaged.CurrentHp == 2 && damagedTint != normal && damagedTint != Color.white,
                    "2/4 HP did not enter Damaged after flash.");
                damaged.ReceivePlayerHit(1, rig.Player);
                await Task.Delay(60);
                Require(damaged.CurrentHp == 1 && hull.color == damagedTint, "1/4 HP lost Damaged state.");
                damaged.gameObject.SetActive(false); damaged.gameObject.SetActive(true);
                Require(damaged.CurrentHp == 4 && hull.color == normal, "Reuse retained damaged presentation.");
                definition.Duration = .5f;
                definition.Groups = new[] { new ScoutSpawnGroup { Count = 1,
                    EntryAnchor = new Vector2(.05f, .72f), HoldAnchor = new Vector2(.05f, .72f),
                    EntryDuration = .02f, HoldDuration = .1f, ExitAnchor = new Vector2(-.2f, .72f), ExitDuration = .2f } };
                Set(Runner, "encounters", new[] { definition });
                Runner.StartRun("ENCOUNTER_2", true);
                await Until(() => Enemies.Length == 1, 400, "Late-shot source missing.");
                var scout = Enemies.Single();
                var position = rig.Camera.ViewportToWorldPoint(new Vector3(.95f, .18f, 10));
                rig.Player.transform.position = position;
                rig.Player.GetComponent<Rigidbody2D>().position = position;
                var profile = AssetDatabase.LoadAssetAtPath<SingleFireDefinition>("Assets/StarShift/Data/Enemies/ScoutSingleFire.asset");
                shot = UnityEngine.Object.Instantiate(profile.ProjectilePrefab);
                Vector3 releasedPosition = default;
                shot.Launch(profile, scout.transform.Find("Muzzle"), scout.gameObject, rig.Player, p =>
                {
                    releasedPosition = p.GetComponent<Rigidbody2D>().position;
                    p.ResetForPool();
                });
                await Until(() => Runner.Elapsed >= .6f, 500, "Timeline boundary did not arrive.");
                Require(!scout.gameObject.activeInHierarchy && shot.gameObject.activeInHierarchy &&
                    Runner.CompletionCount == 0 && shot.VisibleFlightTimeLimit > 0,
                    "Source exit/timeline cutoff removed its visible last shot.");
                position = rig.Camera.ViewportToWorldPoint(new Vector3(.5f, .1f, 10));
                rig.Player.transform.position = position;
                rig.Player.GetComponent<Rigidbody2D>().position = position;
                await Until(() => Runner.Elapsed >= 6.5f, 2600, "Visible lifetime test stalled.");
                Require(shot.gameObject.activeInHierarchy && shot.VisibleFlightTimeLimit > 0,
                    "Configured lifetime removed the missed shot while still in view.");
                await Until(() => Runner.CompletionCount == 1, 1800, "Timeline did not complete after last shot left view.");
                var exitPoint = rig.Camera.WorldToViewportPoint(releasedPosition);
                Require(exitPoint.x <= 0 || exitPoint.x >= 1 || exitPoint.y <= 0 || exitPoint.y >= 1,
                    "Timeline timeout still cut the shot inside the view: " + exitPoint + " at " + Runner.Elapsed);
                Require(!shot.gameObject.activeInHierarchy && Enemies.Length == 0 &&
                    UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length == 0, "Handoff retained a hostile shot.");
                Runner.StartRun("ENCOUNTER_2", true);
                await Until(() => Enemies.Length == 1, 400, "Cancel fixture did not spawn.");
                shot.Launch(profile, Enemies.Single().transform.Find("Muzzle"), Enemies.Single().gameObject, rig.Player, p => p.ResetForPool());
                await Until(() => Runner.Elapsed >= .6f, 500, "Reset fixture did not enter projectile drain.");
                Require(Runner.IsRunning && shot.VisibleFlightTimeLimit > 0, "Reset fixture has no visible draining shot.");
                Runner.StopRun();
                Require(!shot.gameObject.activeInHierarchy && shot.Owner == null &&
                    typeof(EnemyProjectile).GetField("gameplayCamera", Fields).GetValue(shot) == null,
                    "Explicit reset did not clear/fully reset the shot.");
                Runner.StartRun("ENCOUNTER_2", true);
                await Task.Delay(50);
                Require(Runner.IsRunning && Runner.CompletionCount == 0, "New run retained stale projectile-drain completion.");
                return new { passed = true, symmetryError, hpStates = "4/3 Normal; 2/1 Damaged; reuse Normal",
                    sourceExitPreservesShot = true, visibleShotSurvivesSixSeconds = true,
                    timelineWaitsForViewExit = true, explicitResetClearsShot = true, noStaleDrainAfterReplay = true };
            }
            finally
            {
                Runner.StopRun(); Set(Runner, "encounters", original);
                if (shot != null) UnityEngine.Object.DestroyImmediate(shot.gameObject);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }
    }

    public static async Task<object> RootToggle()
    {
        using (var rig = new Rig(1600, 6, false))
        {
            var runner = Runner;
            var root = runner.gameObject;
            var errors = new List<string>();
            Application.LogCallback onLog = (message, stack, type) =>
            { if (type == LogType.Error || type == LogType.Exception) errors.Add(message); };
            Application.logMessageReceived += onLog;
            try
            {
                runner.StartRun("ENCOUNTER_2", true);
                await Until(() => Enemies.Length == runner.Current.EnemyCount, 1500, "Root toggle fixture did not spawn.");
                var old = Enemies.ToArray();
                root.SetActive(false);
                Require(Enemies.Length == 0 && !runner.IsRunning, "Root disable retained live enemies.");
                root.SetActive(true);
                runner.StartRun("ENCOUNTER_2", true);
                await Until(() => Enemies.Length == runner.Current.EnemyCount, 1500, "Root replay failed.");
                Require(old.All(enemy => Enemies.Contains(enemy)) && errors.Count == 0,
                    "Root deactivate/replay lost pool reuse or emitted error: " + string.Join("; ", errors));
                return new { passed = true, rootDeactivate = true, replayReusesAllScouts = true, errors = errors.Count };
            }
            finally
            {
                if (!root.activeSelf) root.SetActive(true);
                Application.logMessageReceived -= onLog;
            }
        }
    }

    public static async Task<object> Capture(string startPoint, float elapsed)
    {
        using (var rig = new Rig(1600, 6, false))
        {
            Runner.StartRun(startPoint, true);
            await Until(() => Runner.Elapsed >= elapsed, (int)(elapsed * 1000) + 2000, "Capture timeline stalled.");
            var path = await SaveCapture(startPoint);
            return new { path, activeScouts = Enemies.Length, Runner.Elapsed };
        }
    }

    public static async Task<object> Boundary()
    {
        using (var rig = new Rig(1600, 6, false))
        {
            Time.timeScale = 3;
            var original = (EncounterDefinition[])Runner.GetType().GetField("encounters", Fields).GetValue(Runner);
            var clone = UnityEngine.Object.Instantiate(original[1]);
            int deathSignals = 0;
            Action died = () => deathSignals++;
            try
            {
                clone.Groups[1].StartTime = 2f;
                foreach (var group in clone.Groups) { group.EntryDuration = .2f; group.HoldDuration = 6f; }
                clone.Duration = 4.6f;
                Set(Runner, "encounters", new[] { clone });
                Runner.StartRun("ENCOUNTER_2", true);
                await Until(() => Runner.SpawnedCount == clone.EnemyCount, 3200, "Boundary group not spawned.");
                foreach (var enemy in Enemies) enemy.Died += died;
                await Until(() => Runner.Elapsed >= 4.3f, 2200, "Boundary timeline stalled.");
                Require(Enemies.Length == clone.EnemyCount && Runner.IsRunning, "Boundary fixture has no surviving threats.");
                await Until(() => !Runner.IsRunning, 10000, "Boundary did not finish draining visible shots.");
                Require(Enemies.Length == 0 && deathSignals == 0 && Runner.KilledCount == 0 && Runner.CompletionCount == 1 &&
                    UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length == 0, "Cleanup killed survivors or left hostile threat.");
                await Task.Delay(650);
                Require(Enemies.Length == 0 && UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length == 0 &&
                    Runner.CompletionCount == 1, "Stale boundary callback or delayed shot.");
                return new { passed = true, liveEnemiesAtCutoff = clone.EnemyCount, deathSignals, completionCount = Runner.CompletionCount,
                    hostileCleanup = true, noDelayedFire = true };
            }
            finally
            {
                Runner.StopRun();
                foreach (var enemy in UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include)) enemy.Died -= died;
                Set(Runner, "encounters", original);
                UnityEngine.Object.DestroyImmediate(clone);
            }
        }
    }

    public static object Validate()
    {
        var source = AssetDatabase.LoadAssetAtPath<EncounterDefinition>("Assets/StarShift/Data/Encounters/L1EncounterB.asset");
        var clone = UnityEngine.Object.Instantiate(source);
        try
        {
            Require(clone.IsValid(out _), "Valid B rejected.");
            clone.Groups[0].Count = 0; Require(!clone.IsValid(out _), "Zero group count accepted.");
            clone.Groups[0].Count = 3; clone.ShotSpacing = float.NaN; Require(!clone.IsValid(out _), "NaN spacing accepted.");
            clone.ShotSpacing = .55f; clone.Duration = .1f; Require(!clone.IsValid(out _), "Timeline ended before scheduled group accepted.");
            clone.Duration = 16; clone.Groups[0].EntryAnchor = new Vector2(float.PositiveInfinity, 1);
            Require(!clone.IsValid(out _), "Infinite path accepted.");
            return new { passed = true, invalidConfigsRejected = 4 };
        }
        finally { UnityEngine.Object.DestroyImmediate(clone); }
    }

    sealed class Rig : IDisposable
    {
        public readonly GameObject Player = GameObject.Find("Player");
        public readonly Camera Camera = Camera.main;
        public readonly EncounterObserver Observer;
        readonly GameplayViewController view;
        readonly GameplayViewSettings oldView;
        readonly Vector3 oldPosition;
        readonly PlayerGameplayState oldState;
        readonly bool oldLife;
        readonly float oldScale;
        readonly InputSettings.EditorInputBehaviorInPlayMode oldEditorPolicy;
        readonly InputSettings.BackgroundBehavior oldBackgroundPolicy;
        readonly Mouse oldMouse;
        readonly Mouse mouse;
        readonly List<InputDevice> disabled = new List<InputDevice>();
        readonly object gameView;
        readonly PropertyInfo selectedSize;
        readonly object oldSize;
        readonly string[] configBefore;
        Vector2 pointer = new Vector2(300, 180);

        public Rig(int width, float size, bool lower70, bool protect = true)
        {
            Require(Application.isPlaying, "Play Mode required.");
            oldScale = Time.timeScale; Time.timeScale = 1;
            Runner.StopRun();
            oldPosition = Player.transform.position;
            var gate = Player.GetComponent<PlayerGameplayGate>();
            oldState = gate.State;
            oldLife = Player.GetComponent<PlayerDamageLifecycle>().enabled;
            if (protect)
            {
                Player.GetComponent<PlayerDamageLifecycle>().enabled = false;
                gate.SetState(PlayerGameplayState.PostEntryInvincibility);
                UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0);
            }
            view = Camera.GetComponent<GameplayViewController>(); oldView = view.TargetView;
            view.RequestView(new GameplayViewSettings(size, new Rect(0, 0, 1, lower70 ? .7f : 1)), 0);
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            gameView = EditorWindow.GetWindow(gameViewType);
            selectedSize = gameViewType.GetProperty("selectedSizeIndex"); oldSize = selectedSize.GetValue(gameView);
            gameViewType.GetMethod("SetCustomResolution", Fields).Invoke(gameView, new object[] { new Vector2(width, 900), "StarShift Encounter Verification" });
            oldEditorPolicy = InputSystem.settings.editorInputBehaviorInPlayMode;
            oldBackgroundPolicy = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            oldMouse = Mouse.current;
            foreach (var device in InputSystem.devices)
                if (device.native && device.enabled && (device is Mouse || device is Touchscreen))
                { disabled.Add(device); InputSystem.DisableDevice(device); }
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            Observer = new GameObject("EncounterVerificationObserver", typeof(EncounterObserver)).GetComponent<EncounterObserver>();
            Observer.gameObject.hideFlags = HideFlags.DontSave;
            configBefore = Definitions().Select(EditorJsonUtility.ToJson).ToArray();
        }

        public async Task KillWithPulse(EnemyHealth target)
        {
            Require(target != null && target.IsAlive, "Pulse target unavailable.");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
            await Task.Delay(50);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }.WithButton(MouseButton.Left));
            await Task.Delay(50);
            var current = Camera.WorldToScreenPoint(Player.transform.position);
            var wanted = Camera.WorldToScreenPoint(target.transform.position);
            pointer += new Vector2(wanted.x - current.x, 0);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }.WithButton(MouseButton.Left));
            await Until(() => target == null || !target.IsAlive, 2500, "Held Pulse did not kill required Scout.");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
            await Task.Delay(60);
            await Until(() => Runner.KilledCount > 0, 250, "Runner did not count real death.");
        }

        public void Dispose()
        {
            Time.timeScale = 1;
            Runner.StopRun();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
            InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled) InputSystem.EnableDevice(device);
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorPolicy;
            InputSystem.settings.backgroundBehavior = oldBackgroundPolicy;
            UnityEngine.Object.DestroyImmediate(Observer.gameObject);
            var life = Player.GetComponent<PlayerDamageLifecycle>(); life.enabled = false; life.enabled = oldLife;
            Player.GetComponent<PlayerGameplayGate>().SetState(oldState);
            Player.transform.position = oldPosition; Player.GetComponent<Rigidbody2D>().position = oldPosition;
            view.RequestView(oldView, 0);
            selectedSize.SetValue(gameView, oldSize);
            Time.timeScale = oldScale;
            Require(configBefore.SequenceEqual(Definitions().Select(EditorJsonUtility.ToJson)), "Verification mutated encounter assets.");
        }
    }

    static EncounterDefinition[] Definitions() => new[] { "A", "B" }.Select(id =>
        AssetDatabase.LoadAssetAtPath<EncounterDefinition>("Assets/StarShift/Data/Encounters/L1Encounter" + id + ".asset")).ToArray();
    static async Task<string> SaveCapture(string name)
    {
        var directory = System.IO.Path.GetFullPath("Temp/StarShiftL1Checkpoint");
        System.IO.Directory.CreateDirectory(directory);
        var path = System.IO.Path.Combine(directory, name + ".png");
        ScreenCapture.CaptureScreenshot(path);
        await Task.Delay(120);
        Require(System.IO.File.Exists(path), "Game View screenshot missing.");
        return path;
    }
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Fields).SetValue(target, value);
    static async Task Until(Func<bool> predicate, int milliseconds, string message, Action observe = null)
    {
        var deadline = Time.realtimeSinceStartup + milliseconds / 1000f;
        while (!predicate() && Time.realtimeSinceStartup < deadline) { observe?.Invoke(); await Task.Delay(10); }
        Require(predicate(), message);
    }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
