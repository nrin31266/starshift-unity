using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using StarShift.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public sealed class MinorCleanupHitProbe : MonoBehaviour, IEnemyProjectileTarget
{
    public int Hits;
    public void ReceiveEnemyHit(GameObject owner) => Hits++;
}

public static class VerifyVerticalSlice
{
    static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static EncounterRunner Runner => UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static GameplaySession Session => UnityEngine.Object.FindAnyObjectByType<GameplaySession>();
    static EnemyHealth[] Enemies => UnityEngine.Object.FindObjectsByType<EnemyHealth>().Where(e => e.IsAlive).ToArray();
    static T Ui<T>(string name) where T : Component => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include).Single(c => c.name == name && (name != "Title" || c.transform.parent.name == "Card") && c.transform.IsChildOf(GameObject.Find("GameplayUI").transform));
    static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static async Task Until(Func<bool> predicate, int milliseconds, string message)
    {
        var deadline = Time.realtimeSinceStartup + milliseconds / 1000f;
        while (!predicate() && Time.realtimeSinceStartup < deadline) await Task.Delay(10);
        Require(predicate(), message);
    }
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Fields).SetValue(target, value);
    static float Timer(object target, string field) => (float)target.GetType().GetField(field, Fields).GetValue(target);
    static Vector2 Center(Component c) => RectTransformUtility.WorldToScreenPoint(null, c.transform.position);
    static async Task<string> Capture(string name)
    {
        var path = System.IO.Path.GetFullPath("Temp/StarShiftClosure/" + name + ".png");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path);
        await Task.Delay(100);
        Require(System.IO.File.Exists(path), "Screenshot missing.");
        return path;
    }

    static async Task<string> CaptureThermal(string state, int width)
    {
        var path = System.IO.Path.GetFullPath("../.design/m3-5-thermal-hud/screenshots/" + state + "-" + width + "x900.png");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        ScreenCapture.CaptureScreenshot(path);
        await Task.Delay(150);
        Require(System.IO.File.Exists(path), "Thermal screenshot missing: " + path);
        return path;
    }

    public static async Task<object> NaturalProjectileDrain()
    {
        Require(Application.isPlaying&&!Runner.HasActiveRun,"Idle Menu required");
        var save=Runner.GetComponent<StarShift.Persistence.CampaignLifecycle>();var shell=Runner.GetComponent<GameShell>();
        var path=save.SavePath;var before=new[]{"",".bak",".invalid"}.ToDictionary(s=>s,s=>System.IO.File.Exists(path+s)?System.IO.File.ReadAllBytes(path+s):null);
        var temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"starshift-drain-"+Guid.NewGuid().ToString("N"),"save.json");
        var level=Runner.Flow.Level;
        try
        {
            save.UseTestStorage(temp);shell.Boot();Runner.Flow.LoadLevel(AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/StarShift/Data/Fixtures/Levels/M4SystemTestbed.asset"));
            var result=await MinorProjectileCleanup();
            System.IO.File.WriteAllText("../.design/presentation-polish/projectile-drain.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally
        {
            Runner.StopRun();save.ExitIsolated();Runner.Flow.LoadLevel(level);save.UseTestStorage(path);shell.Boot();
            foreach(var pair in before){var after=System.IO.File.Exists(path+pair.Key)?System.IO.File.ReadAllBytes(path+pair.Key):null;Require(pair.Value==null?after==null:after!=null&&pair.Value.SequenceEqual(after),"Production save changed");}
            if(System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(temp)))System.IO.Directory.Delete(System.IO.Path.GetDirectoryName(temp),true);
        }
    }

    public static async Task<object> MinorProjectileCleanup()
    {
        using (var rig = new Rig(1600, 6))
        {
            var profile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<SingleFireDefinition>(
                "Assets/StarShift/Data/Enemies/ScoutSingleFire.asset"));
            Set(profile, "projectileSpeed", 4f);
            Set(profile, "projectileLifetime", .1f);
            var target = new GameObject("MinorCleanupTarget", typeof(BoxCollider2D), typeof(MinorCleanupHitProbe));
            var hitbox = target.GetComponent<BoxCollider2D>(); hitbox.isTrigger = true; hitbox.size = Vector2.one * .2f;
            var probe = target.GetComponent<MinorCleanupHitProbe>();
            var muzzle = new GameObject("MinorCleanupMuzzle");
            int releases = 0;
            var pool = new UnityEngine.Pool.ObjectPool<EnemyProjectile>(
                () => UnityEngine.Object.Instantiate(profile.ProjectilePrefab), null,
                p => { releases++; p.ResetForPool(); }, p => UnityEngine.Object.DestroyImmediate(p.gameObject));
            EnemyProjectile shot = null;
            Action<EnemyProjectile> recycle = p => pool.Release(p);
            try
            {
                Runner.StartRun("ENCOUNTER_1", false, true, null, null);
                var expected=Runner.Current.Groups.Sum(g=>g.Count);
                await Until(() => Runner.SpawnedCount==expected&&Enemies.Length==expected,4500,"Minor cleanup fixture did not spawn");
                await Until(() => Enemies.Length==expected&&Enemies.All(e=>e.CanReceiveDamage),3500,"Fixture still entering");
                var fireGate=UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>();fireGate.Block();fireGate.ClearProjectiles();
                target.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(.5f, -.2f, 10));
                muzzle.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(.5f, .65f, 10));
                shot = pool.Get();
                shot.Launch(profile, muzzle.transform, Enemies[0].gameObject, target, recycle);
                var visual = shot.GetComponentInChildren<SpriteRenderer>(); var color = visual.color;
                var collider = shot.GetComponent<BoxCollider2D>();
                foreach (var enemy in Enemies) enemy.ReceivePlayerHit(enemy.CurrentHp, rig.Player);
                await Until(() => Runner.Stage == EncounterStage.ProjectileDrain, 500, "Minor completion hard-cleared the shot.");
                var drainStart = Time.fixedTime; var initialPosition = shot.transform.position;
                Require(shot.gameObject.activeSelf && collider.enabled && visual.color == color && Runner.CompletionCount == 0,
                    "Shot did not retain normal flight/damage during grace.");
                await rig.MouseEvent(new Vector2(400, 220), true);
                var playerPosition = rig.Player.transform.position;
                await rig.MouseEvent(new Vector2(455, 220), true);
                await rig.MouseEvent(new Vector2(455, 220), false);
                Require(rig.Player.transform.position.x > playerPosition.x && rig.Gate.CanInput &&
                    Runner.Flow.HudOpacity == 1 && Ui<Button>("Pause").interactable && Enemies.Length == 0 &&
                    !UnityEngine.Object.FindObjectsByType<SingleFireAttack>().Any(a => a.IsTelegraphing),
                    "Minor cleanup locked HUD/movement or retained hostile producers.");
                Require(shot.gameObject.activeSelf && collider.enabled && shot.transform.position != initialPosition,
                    "Grace did not preserve a moving visible shot.");
                await Task.Delay(100);
                Require(shot.gameObject.activeSelf&&collider.enabled&&visual.color==color,"Visible projectile faded or lost damage");
                Session.Pause(); var frozen = shot.transform.position; var frozenColor = visual.color;
                await Task.Delay(100);
                Require(shot.transform.position == frozen && visual.color == frozenColor, "Pause advanced cleanup flight/fade.");
                Session.Resume();
                while(shot.gameObject.activeSelf)
                {
                    Require(visual.color==color&&collider.enabled,"Projectile faded before viewport exit");
                    Require(Time.fixedTime-drainStart<3,"Natural exit exceeded flight time");await Task.Delay(10);
                }
                var flightElapsed = Time.fixedTime - drainStart;
                Require(probe.Hits == 0 && releases == 1 && pool.CountInactive == 1 && shot.Owner == null &&
                    visual.color == color && !collider.enabled,
                    "Natural exit or pool reset failed.");
                await Until(() => Runner.Stage == EncounterStage.Handoff, 500, "Cleanup did not finish before handoff.");
                var handoffStart = Time.fixedTime;
                await Until(() => Runner.IsHandoffTravel, 2500, "Travel did not start after clear pause.");
                var travelSeconds=Runner.Current.EffectiveHandoffTravelSeconds;
                Require(UnityEngine.Object.FindAnyObjectByType<MissionIntroPresentation>().IsCueVisible,"Minor travel cue missing");
                var reused = pool.Get(); Require(reused == shot, "Pool did not reuse the cleaned projectile.");
                target.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(.8f, .5f, 10));
                muzzle.transform.position = target.transform.position + Vector3.up * .05f;
                reused.Launch(profile, muzzle.transform, rig.Player, target, recycle);
                Require(collider.enabled && visual.color == color, "Reuse retained fade/damage state.");
                Physics2D.SyncTransforms();
                await Until(() => probe.Hits == 1 && releases == 2, 500, "Reused projectile could not damage/recycle normally.");
                var edgeShot = pool.Get();
                target.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(1.5f, .5f, 10));
                muzzle.transform.position = Camera.main.ViewportToWorldPoint(new Vector3(.999f, .5f, 10));
                edgeShot.Launch(profile, muzzle.transform, rig.Player, target, recycle);
                var edgeStart = Time.fixedTime;
                await Until(() => !edgeShot.gameObject.activeSelf, 400, "Offscreen shot waited for fade instead of recycling.");
                Require(Time.fixedTime - edgeStart < .3f && releases == 3 && probe.Hits == 1,
                    "Viewport exit did not recycle early and cleanly.");
                await Until(() => Time.fixedTime - handoffStart >= .9f, 1500, "Extended handoff stalled.");
                Require(Runner.CurrentIndex == 0 && Runner.Stage == EncounterStage.Handoff &&
                    UnityEngine.Object.FindAnyObjectByType<MissionIntroPresentation>().IsCueVisible, "Travel cue ended before handoff.");
                var screenshot = await Capture("minor-handoff-cleanup");
                await Until(() => Runner.CurrentIndex == 1, 1000, "Next encounter never started.");
                Require(!shot.gameObject.activeSelf && Runner.CompletionCount == 1,
                    "Old projectile or duplicate completion crossed the encounter boundary.");
                return new { passed = true, flightElapsed, handoffDuration = travelSeconds,
                    naturalExitNoFade = true, poolReuseRestoredDamage = true, pauseFrozen = true,
                    viewportExitRecyclesEarly = true, movementAndHudActive = true, noOldShotInNextEncounter = true, screenshot };
            }
            finally
            {
                Runner.StopRun();
                if (shot != null && shot.gameObject.activeSelf) shot.Release();
                pool.Dispose();
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(muzzle);
            }
        }
    }

    public static async Task<object> ThermalReviewFixes()
    {
        using (var rig = new Rig(1600, 6))
        {
            var failures = new List<string>();
            var hud = UnityEngine.Object.FindAnyObjectByType<GameplayHud>();
            var refresh = typeof(GameplayHud).GetMethod("RefreshHeat", Fields);
            foreach (var name in new[] { "heatFill", "heatFrame", "heatStatus" })
            {
                var field = typeof(GameplayHud).GetField(name, Fields);
                var original = field.GetValue(hud);
                try
                {
                    field.SetValue(hud, null); Set(hud, "previousHeat", -1f);
                    try { refresh.Invoke(hud, null); }
                    catch (TargetInvocationException error) { failures.Add(name + ": " + error.InnerException.GetType().Name); }
                }
                finally { field.SetValue(hud, original); }
            }
            Set(hud, "previousHeat", -1f); refresh.Invoke(hud, null);
            var weapon = rig.Player.GetComponent<PulseLaserWeapon>();
            await rig.MouseEvent(new Vector2(300, 200), false);
            await rig.MouseEvent(new Vector2(300, 200), true);
            await Until(() => weapon.Heat > 0 && UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length > 0,
                1500, "No real held shot for death-boundary cadence probe.");
            var cadence = typeof(PulseLaserWeapon).GetField("nextShotTime", Fields);
            var allowed = typeof(PulseLaserWeapon).GetField("wasAllowed", Fields);
            var shotTime = (double)cadence.GetValue(weapon); var wasAllowed = (bool)allowed.GetValue(weapon);
            var state = (WeaponHeatState)typeof(PulseLaserWeapon).GetField("heat", Fields).GetValue(weapon);
            var profile = AssetDatabase.LoadAssetAtPath<WeaponThermalProfile>("Assets/StarShift/Data/Weapons/PulseLaserThermal.asset");
            state.Tick(profile, true, profile.Capacity / profile.HeatPerSecond + 1);
            Require(weapon.IsOverheated, "Death-boundary fixture did not latch overheat.");
            weapon.ApplyDeathDowngrade();
            if (weapon.Heat != 0 || weapon.IsOverheated) failures.Add("Death retained heat/latch.");
            if ((double)cadence.GetValue(weapon) != shotTime || (bool)allowed.GetValue(weapon) != wasAllowed)
                failures.Add("Death downgrade reset cadence outside the gameplay gate.");
            await rig.MouseEvent(new Vector2(300, 200), false);
            rig.Unprotect();
            Require(rig.Player.GetComponent<PlayerDamageLifecycle>().TryReceiveHit(null), "Real death was rejected.");
            Require(weapon.Heat == 0 && !weapon.IsOverheated && (double)cadence.GetValue(weapon) == 0 &&
                !(bool)allowed.GetValue(weapon), "Dying gate did not reset thermal/cadence permissions.");
            await Until(() => rig.Gate.State == PlayerGameplayState.PostEntryInvincibility, 2500, "Respawn did not finish.");
            Require(weapon.Heat == 0 && weapon.CanFire, "Respawn did not return thermal-ready.");
            Runner.StartRun("LEVEL_START", false, true, null, null);
            Require(weapon.Heat == 0 && !weapon.IsOverheated && (double)cadence.GetValue(weapon) == 0 &&
                !(bool)allowed.GetValue(weapon), "Mission reset did not reset full weapon session state.");
            Require(failures.Count == 0, string.Join("; ", failures));
            return new { passed = true, missingHeatBindingsSafe = 3, deathOnlyResetsThermal = true,
                gateResetsCadence = true, deathRespawn = true, missionResetsCadence = true };
        }
    }

    public static async Task<object> PlainHud(int width)
    {
        using (var rig = new Rig(width, 6))
        {
            var weapon = rig.Player.GetComponent<PulseLaserWeapon>();
            rig.Place(new Vector2(-5.5f, -3.8f));
            await rig.MouseEvent(new Vector2(300, 200), false);
            await Until(() => UnityEngine.Object.FindAnyObjectByType<GameplayAnnouncement>().GetComponent<CanvasGroup>().alpha < .01f,
                5000, "Mission card did not fade.");
            await rig.MouseEvent(new Vector2(300, 200), true);
            await Until(() => weapon.HeatFraction >= .35f, 4000, "HUD smoke did not warm weapon.");
            Require(Mathf.Abs(Ui<Image>("HeatFill").rectTransform.anchorMax.x - weapon.HeatFraction) < .04f, "Plain meter does not match runtime.");
            var screenshot = await CaptureThermal("plain-hud", width);
            await rig.MouseEvent(new Vector2(300, 200), false);
            await rig.MouseEvent(Center(Ui<Button>("Pause")), true); await rig.MouseEvent(Center(Ui<Button>("Pause")), false);
            Require(Session.IsPaused && !rig.Input.FireHeld, "Plain Pause leaked fire intent.");
            var pausedHeat = weapon.Heat;
            await Task.Delay(100); Require(weapon.Heat == pausedHeat, "Pause did not freeze heat.");
            var pauseScreenshot = await CaptureThermal("plain-pause", width);
            await rig.MouseEvent(Center(Ui<Button>("Resume")), true); await rig.MouseEvent(Center(Ui<Button>("Resume")), false);
            Require(!Session.IsPaused && !rig.Input.FireHeld, "Resume retained UI press.");
            return new { passed = true, width, height = Screen.height, screenshot, pauseScreenshot,
                weaponCopy = Ui<Text>("WeaponLabel").text, heatCopy = Ui<Text>("Caption").text, pauseExclusion = true };
        }
    }

    public static async Task<object> ThermalHud(int width, bool lifecycleChecks)
    {
        using (var rig = new Rig(width, 6))
        {
            var weapon = rig.Player.GetComponent<PulseLaserWeapon>();
            var profile = AssetDatabase.LoadAssetAtPath<WeaponThermalProfile>("Assets/StarShift/Data/Weapons/PulseLaserThermal.asset");
            Require(profile != null, "Missing type-owned thermal profile.");
            var shots = new List<string>();
            rig.Place(new Vector2(-5.5f, -3.8f));
            await rig.MouseEvent(new Vector2(300, 200), false);
            weapon.ResetForSession();
            await Until(() => UnityEngine.Object.FindAnyObjectByType<GameplayAnnouncement>().GetComponent<CanvasGroup>().alpha < .01f,
                5000, "Mission announcement did not auto fade before normal HUD capture.");
            await Task.Delay(150);
            shots.Add(await CaptureThermal("normal", width));
            var start = Time.fixedTime;
            await rig.MouseEvent(new Vector2(300, 200), true);
            try { await Until(() => weapon.HeatFraction >= .5f, 4500, "Heat did not reach 50%."); }
            catch (InvalidOperationException error)
            {
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = new Vector2(300, 200) }, hits);
                throw new InvalidOperationException(error.Message + " held=" + rig.Input.FireHeld + " gate=" + rig.Gate.State +
                    " canFire=" + rig.Gate.CanFire + " heat=" + weapon.Heat + " scale=" + Time.timeScale +
                    " pointerHits=" + string.Join(",", hits.Select(hit => hit.gameObject.name)) +
                    " resolution=" + Screen.width + "x" + Screen.height + " focused=" + Application.isFocused);
            }
            Require(!weapon.IsOverheated && weapon.CanFire, "Warning threshold locked fire.");
            Require(UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length > 0, "Hold did not produce real shots.");
            shots.Add(await CaptureThermal("heat-50", width));
            try { await Until(() => weapon.HeatFraction >= .9f, 3500, "Heat did not approach MAX."); }
            catch (InvalidOperationException error)
            {
                throw new InvalidOperationException(error.Message + " held=" + rig.Input.FireHeld + " gate=" + rig.Gate.State +
                    " canFire=" + rig.Gate.CanFire + " heat=" + weapon.Heat + " scale=" + Time.timeScale +
                    " fixedTime=" + Time.fixedTime + " start=" + start + " focused=" + Application.isFocused);
            }
            Require(!weapon.IsOverheated && weapon.CanFire, "Near-MAX fire was locked.");
            shots.Add(await CaptureThermal("near-max", width));
            await Until(() => weapon.IsOverheated, 1500, "MAX did not latch Overheated.");
            var overheatSeconds = Time.fixedTime - start;
            Require(overheatSeconds >= 5.4f && overheatSeconds <= 5.8f, "Unexpected sustained fire timing: " + overheatSeconds);
            Require(!weapon.CanFire && rig.Gate.CanInput && rig.Gate.CanFire, "Overheat changed Player permissions.");
            var recoveryStart = Time.fixedTime;
            shots.Add(await CaptureThermal("overheated", width));
            Require(Ui<Text>("HeatStatus").text == GameplayCopy.HeatOverheated, "Overheat state cue missing.");
            foreach (var shot in UnityEngine.Object.FindObjectsByType<PlayerProjectile>()) shot.Release();
            var position = rig.Player.transform.position;
            await rig.MouseEvent(new Vector2(380, 220), true);
            Require(rig.Player.transform.position.x > position.x && rig.Player.transform.position.y > position.y,
                "Overheat blocked relative movement.");
            await Task.Delay(150);
            Require(weapon.IsOverheated && UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length == 0,
                "Overheated weapon spawned a new shot.");
            await Until(() => !weapon.IsOverheated, 1700, "Recovery never enabled fire.");
            var recoverySeconds = Time.fixedTime - recoveryStart;
            Require(recoverySeconds >= 1.45f && recoverySeconds <= 1.7f && weapon.HeatFraction <= .04f,
                "Unexpected recovery threshold/timing: " + recoverySeconds + "/" + weapon.HeatFraction);
            await Until(() => UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length > 0, 500, "Held fire did not resume automatically.");
            Require(rig.Input.FireHeld, "Hold-through fixture lost intent.");
            Require(Mathf.Abs(Ui<Image>("HeatFill").rectTransform.anchorMax.x - weapon.HeatFraction) < .04f,
                "HUD heat differs from runtime.");

            await rig.MouseEvent(new Vector2(380, 220), false);
            var beforePause = rig.Player.transform.position;
            await rig.MouseEvent(Center(Ui<Button>("Pause")), true);
            await rig.MouseEvent(Center(Ui<Button>("Pause")), false);
            Require(Session.IsPaused, "Pause rejected.");
            Require(!rig.Input.FireHeld && rig.Player.transform.position == beforePause, "Compact Pause hit area entered gameplay.");
            var frozen = weapon.Heat;
            shots.Add(await CaptureThermal("pause", width));
            await Task.Delay(200);
            Require(weapon.Heat == frozen, "Pause advanced thermal time.");
            await rig.MouseEvent(Center(Ui<Button>("Resume")), true);
            await rig.MouseEvent(Center(Ui<Button>("Resume")), false);
            Require(!Session.IsPaused && !rig.Input.FireHeld, "Resume retained UI fire intent.");

            if (lifecycleChecks)
            {
                weapon.ResetForSession();
                await rig.MouseEvent(new Vector2(300, 200), true);
                await Until(() => weapon.HeatFraction >= .8f, 5500, "Release fixture failed to warm.");
                await rig.MouseEvent(new Vector2(300, 200), false);
                var released = weapon.Heat;
                Require(!weapon.IsOverheated && weapon.CanFire, "Release before MAX locked weapon.");
                await Task.Delay(100);
                Require(Mathf.Abs(weapon.Heat - released) < .01f, "Cooling delay missing.");
                await Task.Delay(300);
                Require(weapon.Heat < released && !weapon.IsOverheated, "Release did not cool normally.");
                var cooled = weapon.Heat;
                await rig.MouseEvent(new Vector2(300, 200), true);
                Require(weapon.Heat > cooled && weapon.CanFire, "Fresh burst could not fire immediately.");
                await rig.MouseEvent(new Vector2(300, 200), false);
                weapon.ResetForSession();
                for (var burst = 0; burst < 4; burst++)
                {
                    await rig.MouseEvent(new Vector2(300, 200), true); await Task.Delay(150);
                    var peak = weapon.Heat;
                    await rig.MouseEvent(new Vector2(300, 200), false); await Task.Delay(400);
                    Require(weapon.Heat < peak && !weapon.IsOverheated && weapon.CanFire, "Tap/burst accidentally locked.");
                }
                await rig.MouseEvent(new Vector2(300, 200), true); await Task.Delay(400);
                await rig.MouseEvent(new Vector2(300, 200), false);
                rig.Unprotect();
                Require(weapon.Heat > 0 && rig.Player.GetComponent<PlayerDamageLifecycle>().TryReceiveHit(null), "Death fixture failed.");
                Require(weapon.Heat == 0 && !weapon.IsOverheated, "Death retained heat/latch.");
                await Until(() => rig.Gate.State == PlayerGameplayState.PostEntryInvincibility, 2500, "Respawn failed.");
                Require(weapon.Heat == 0 && weapon.CanFire, "Respawn not thermal-ready.");
                rig.Protect();
            }

            await rig.MouseEvent(new Vector2(300, 200), false);
            Runner.StartRun("LEVEL_START", false, true, null, null);
            rig.Protect(); rig.Place(new Vector2(-5.5f, -3.8f));
            Require(weapon.Heat == 0 && !weapon.IsOverheated, "Mission replay retained thermal state.");
            await rig.MouseEvent(new Vector2(300, 200), true);
            await Until(() => Runner.SpawnedCount == 3 && weapon.HeatFraction >= .25f, 4000, "Handoff fixture failed.");
            var beforeHandoff = weapon.Heat;
            float heatAtNextEncounter = -1;
            Action<EncounterDefinition, int> next = (definition, token) => { if (Runner.CurrentIndex == 1) heatAtNextEncounter = weapon.Heat; };
            Runner.EncounterStarted += next;
            try
            {
                foreach (var enemy in Enemies) enemy.ReceivePlayerHit(int.MaxValue, rig.Player);
                await Until(() => Runner.Stage == EncounterStage.Handoff, 2000, "No minor handoff.");
                await Task.Delay(150);
                Require(Ui<CanvasGroup>("CombatHud").alpha == 1 && Ui<Button>("Pause").gameObject.activeSelf,
                    "Minor transition hid compact HUD.");
                shots.Add(await CaptureThermal("encounter-transition", width));
                await Until(() => heatAtNextEncounter >= 0, 2500, "Next Encounter did not start.");
                Require(heatAtNextEncounter >= beforeHandoff && weapon.Heat > 0, "Encounter reset heat.");
            }
            finally { Runner.EncounterStarted -= next; }
            await rig.MouseEvent(new Vector2(300, 200), false);
            foreach (var state in new[] { PlayerGameplayState.GameOver, PlayerGameplayState.Result })
            {
                rig.Gate.SetState(state);
                var terminalHeat = weapon.Heat;
                await Task.Delay(150);
                Require(weapon.Heat == terminalHeat, "Terminal state advanced heat: " + state);
            }
            Runner.StartRun("LEVEL_START", false, true, null, null);
            Require(weapon.Heat == 0 && !weapon.IsOverheated, "Terminal replay retained heat.");
            var result = new { passed = true, width, height = Screen.height, lifecycleChecks, overheatSeconds, recoverySeconds,
                capacity = profile.Capacity, heatPerSecond = profile.HeatPerSecond, coolingDelay = profile.CoolingDelay,
                coolRate = profile.CoolRate, overheatCoolRate = profile.OverheatCoolRate, recoveryFraction = profile.RecoveryFraction,
                maxOnlyLock = true, movementIndependent = true, holdAutoResume = true, pauseFrozen = true, handoffRetainsHeat = true,
                deathRespawnReset = lifecycleChecks, releaseBeforeMax = lifecycleChecks, tapBurst = lifecycleChecks, missionReset = true,
                terminalFrozen = true, hudMatchesRuntime = true, screenshots = shots.ToArray() };
            var evidence = System.IO.Path.GetFullPath("../.design/m3-5-thermal-hud/verification-" + width + (lifecycleChecks ? "-targeted" : "-visual") + ".json");
            System.IO.File.WriteAllText(evidence, Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
    }

    public static async Task<object> InputAndPause()
    {
        using (var rig = new Rig(1600, 6))
        {
            await Task.Delay(250);
            await rig.MouseEvent(new Vector2(300, 180), false);
            var pause = Ui<Button>("Pause");
            var resume = Ui<Button>("Resume");
            var position = rig.Player.transform.position;
            await rig.MouseEvent(Center(pause), true);
            Require(!rig.Input.FireHeld && rig.Input.ReadDelta() == Vector2.zero && rig.Player.transform.position == position,
                "Mouse down on Pause entered gameplay.");
            await rig.MouseEvent(Center(pause), false);
            Require(Session.IsPaused && Time.timeScale == 0, "Mouse Pause did not activate: paused=" + Session.IsPaused + " scale=" + Time.timeScale + " interactable=" + pause.interactable + " pos=" + Center(pause) + " resolution=" + Screen.width + "x" + Screen.height + " gate=" + rig.Gate.State);
            await Capture("pause-16x9");
            await rig.MouseEvent(Center(resume), true); await rig.MouseEvent(Center(resume), false);
            Require(!Session.IsPaused && Time.timeScale == 1 && !rig.Input.FireHeld, "Resume retained UI press.");
            await rig.MouseEvent(new Vector2(400, 220), true);
            Require(rig.Input.FireHeld, "Same-frame re-press fixture has no gameplay pointer.");
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = new Vector2(400, 220) });
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = Center(pause) }.WithButton(MouseButton.Left));
            await Task.Delay(90);
            Require(!rig.Input.FireHeld && rig.Input.ReadDelta() == Vector2.zero && rig.Player.transform.position == position,
                "Same-frame UI re-press retained old gameplay ownership.");
            await rig.MouseEvent(Center(pause), false);
            if (Session.IsPaused)
            {
                await rig.MouseEvent(Center(resume), true); await rig.MouseEvent(Center(resume), false);
            }
            await rig.MouseEvent(new Vector2(400, 220), true);
            Require(rig.Input.FireHeld && rig.Player.transform.position == position, "Gameplay press teleported or failed fire.");
            await Task.Delay(350);
            Require(UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length > 0, "Stationary hold did not fire.");
            await rig.MouseEvent(new Vector2(460, 240), true);
            var moved = rig.Player.transform.position;
            Require(moved.x > position.x && moved.y > position.y, "Mouse relative drag missing.");
            await rig.MouseEvent(Center(pause), true); await rig.MouseEvent(Center(pause), false);
            Require(!Session.IsPaused, "Gameplay drag clicked Pause.");
            await Task.Delay(100);
            Require(!rig.Input.FireHeld, "Mouse release retained fire.");
            rig.Place(new Vector2(0, -3.8f));
            var beforeUiTouch = rig.Player.transform.position;
            await rig.TouchEvent(1, TouchPhase.Began, Center(pause));
            Require(!rig.Input.FireHeld && rig.Input.ReadDelta() == Vector2.zero, "Touch UI owned gameplay.");
            await rig.TouchEvent(1, TouchPhase.Moved, new Vector2(500, 300));
            Require(!rig.Input.FireHeld && rig.Player.transform.position == beforeUiTouch,
                "UI touch became gameplay when leaving button.");
            await rig.TouchEvent(1, TouchPhase.Ended, new Vector2(500, 300));
            Require(!Session.IsPaused, "UI drag released outside still clicked Pause.");
            await rig.TouchEvent(2, TouchPhase.Began, new Vector2(400, 220));
            var touchStart = rig.Player.transform.position;
            await rig.TouchEvent(2, TouchPhase.Moved, new Vector2(440, 235));
            Require(rig.Input.FireHeld && rig.Player.transform.position.x > touchStart.x, "Touch gameplay drag failed.");
            await rig.TouchEvent(3, TouchPhase.Began, Center(pause));
            await rig.TouchEvent(3, TouchPhase.Ended, Center(pause));
            Require(Session.IsPaused && !rig.Input.FireHeld, "Second UI finger failed pause/cancel gameplay.");
            await rig.TouchEvent(4, TouchPhase.Began, Center(resume)); await rig.TouchEvent(4, TouchPhase.Ended, Center(resume));
            Require(!Session.IsPaused && !rig.Input.FireHeld, "Touch Resume retained old held finger.");
            var afterResume = rig.Player.transform.position;
            await rig.TouchEvent(2, TouchPhase.Moved, new Vector2(600, 300));
            Require(!rig.Input.FireHeld && Vector3.Distance(afterResume, rig.Player.transform.position) < .001f, "Old finger resumed stale drag/fire.");
            await rig.TouchEvent(2, TouchPhase.Ended, new Vector2(600, 300));
            await Until(() => Enemies.Any(e => e.GetComponent<SingleFireAttack>().IsTelegraphing), 4500, "No telegraph to pause.");
            await FreezeCheck();
            await Until(() => UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length > 0, 4500, "No projectile to pause.");
            await FreezeCheck();
            var screenshot = await Capture("hud-16x9");
            return new { passed = true, mouseUiExclusion = true, gameplayDragCannotClickUi = true, syntheticMultiTouch = true,
                freshPressAfterResume = true, telegraphAndProjectilePause = true, screenshot, frames = Time.frameCount - rig.FirstFrame };
        }
    }

    static async Task FreezeCheck()
    {
        Session.Pause(); Require(Session.IsPaused, "Pause rejected current encounter phase.");
        await Task.Delay(45);
        var elapsed = Runner.Elapsed;
        var handoffRemaining = Timer(Runner, "remaining");
        var enemies = Enemies.ToDictionary(e => e, e => e.transform.position);
        var timers = Enemies.ToDictionary(e => e, e => Timer(e.GetComponent<SingleFireAttack>(), "remaining"));
        var projectiles = UnityEngine.Object.FindObjectsByType<EnemyProjectile>().ToDictionary(p => p, p => p.transform.position);
        var lifecycle = GameObject.Find("Player").GetComponent<PlayerDamageLifecycle>();
        var lifeTimer = Timer(lifecycle, "remaining");
        var grace = Timer(UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>(), "graceRemaining");
        await Task.Delay(220);
        Require(Runner.Elapsed == elapsed && Timer(Runner, "remaining") == handoffRemaining && enemies.All(e => e.Key != null && e.Key.transform.position == e.Value) &&
            projectiles.All(p => p.Key != null && p.Key.transform.position == p.Value) &&
            timers.All(e => Timer(e.Key.GetComponent<SingleFireAttack>(), "remaining") == e.Value) &&
            Timer(lifecycle, "remaining") == lifeTimer && Timer(UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>(), "graceRemaining") == grace,
            "Simulation/timer changed while paused.");
        Session.Resume(); Require(!Session.IsPaused && Time.timeScale == 1, "Pause did not resume prior clock.");
    }

    public static Task<object> FullRun(int width, float size, bool withDeath) => FullRunCore(width,size,withDeath,false);
    public static Task<object> FullMissionRun(int width, float size, bool withDeath) => FullRunCore(width,size,withDeath,true);
    static async Task<object> FullRunCore(int width, float size, bool withDeath, bool intro)
    {
        using (var rig = new Rig(width, size, intro))
        {
            var order = new List<string>();
            Action<EncounterDefinition> completed = d => order.Add(d.Id);
            var clearCount = 0;
            Action cleared = () => clearCount++;
            Runner.Completed += completed; Runner.MissionCompleted += cleared;
            try
            {
                string startCapture;
                if (intro)
                {
                    Require(Runner.Stage == EncounterStage.Intro && Runner.Flow.IsAwaitingStart && !rig.Gate.CanInput && !rig.Gate.CanFire && Enemies.Length == 0,
                        "Briefing did not gate first spawn.");
                    await Task.Delay(650);
                    var announcement = UnityEngine.Object.FindAnyObjectByType<GameplayAnnouncement>();
                    Require(announcement.GetComponent<CanvasGroup>().alpha == 1,"Briefing title not persistent.");
                    Require(Ui<CanvasGroup>("CombatHud").alpha==0 && !Ui<Button>("Pause").gameObject.activeSelf,"HUD visible during briefing.");
                    var before = Runner.Flow.IntroElapsed;
                    Session.Pause();await Task.Delay(220);
                    Require(!Session.IsPaused && Time.timeScale==1 && Runner.Flow.IntroElapsed==before,"Briefing accepted Pause/advanced without input.");
                    Require(!Ui<Text>("ContinueHint").gameObject.activeSelf,"Continue hint appeared before reading delay.");
                    await rig.MouseEvent(new Vector2(width*.86f,450),true);await rig.MouseEvent(new Vector2(width*.86f,450),false);
                    Require(Runner.Flow.IsAwaitingStart,"Early tap skipped reading delay.");
                    await Until(()=>Runner.Flow.CanContinue,2200,"Continue prompt never armed.");await Task.Delay(450);
                    Require(Ui<Text>("ContinueHint").gameObject.activeSelf,"Continue hint missing.");
                    var headline=announcement.transform.Find("Title").GetComponent<Text>().text;
                    Require(headline==Runner.Flow.EstablishingTitle && !headline.Contains("NHIỆM VỤ"),"Mission number used as establishing hero.");
                    Require(announcement.transform.Find("Title").GetComponent<Text>().alignment==TextAnchor.MiddleLeft,"Major establishing lost its distinct layout.");
                    Require(!UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include).Any(b=>b.name=="BeginMission"),"Menu button survived migration.");
                    startCapture=await Capture("establishing-"+width+"-"+size);
                    await rig.MouseEvent(new Vector2(width*.86f,450),true);
                    Require(!rig.Input.FireHeld && Runner.Flow.IsAwaitingStart,"Tap down launched or entered gameplay: phase="+Runner.Flow.IntroPhase+" held="+rig.Input.FireHeld+" screen="+Screen.width+"x"+Screen.height);
                    await rig.MouseEvent(new Vector2(width*.86f,450),false);
                    Require(!Runner.Flow.IsAwaitingStart && !rig.Input.FireHeld && !Runner.Flow.BeginApproach(),"Anywhere tap failed or launched twice.");
                    await Capture("approach-"+width+"-"+size);
                    await Until(()=>Runner.Flow.IntroPhase==MissionIntroPhase.Travel,1500,"Travel phase missing.");
                    var travelStarted=Runner.Flow.IntroElapsed;
                    await Task.Delay(450);await Capture("travel-"+width+"-"+size);
                    Require(Mathf.Abs(Camera.main.orthographicSize-9)<.01f,"Travel did not open framing to 9.");
                    await rig.MouseEvent(new Vector2(width*.2f,200),true);
                    Session.Pause();Require(!Session.IsPaused && Time.timeScale==1,"Travel accepted Pause.");
                    await Until(()=>Runner.Flow.IntroPhase==MissionIntroPhase.Brake,3000,"Brake phase missing.");
                    Require(Mathf.Abs(Runner.Flow.IntroElapsed-travelStarted-Runner.Flow.Level.Intro.TravelSeconds)<.08f,"Travel duration did not honor configured timing.");
                    Require(!announcement.IsPersistent && announcement.GetComponent<CanvasGroup>().alpha==0,
                        "Mission card appeared before gameplay framing/HUD/control returned.");
                    var brakingAt=Time.realtimeSinceStartup;
                    while(Runner.Flow.IsCinematic && Time.realtimeSinceStartup-brakingAt<2)
                    {
                        Require(Runner.Flow.IntroPhase==MissionIntroPhase.Brake,"A stationary phase delayed gameplay after braking.");
                        await Task.Delay(10);
                    }
                    Require(!Runner.Flow.IsCinematic,"Braking did not hand straight to gameplay.");
                    await Task.Delay(80);
                    Require(Runner.Stage==EncounterStage.Intro && Enemies.Length==0 && rig.Gate.CanInput && rig.Gate.CanFire && Session.CanPause &&
                        Ui<CanvasGroup>("CombatHud").alpha>.99f && Ui<Button>("Pause").gameObject.activeSelf && Mathf.Abs(Camera.main.orthographicSize-size)<.01f,
                        "Mission card did not wait on the controllable gameplay screen before first spawn.");
                    Require(announcement.transform.Find("Title").GetComponent<Text>().text=="Mission 01" && announcement.transform.Find("Title").GetComponent<Text>().alignment==TextAnchor.MiddleCenter,"Mission arrival label not structural/centered.");
                    Require(announcement.transform.Find("Description").GetComponent<Text>().text=="Encounter 01","Mission card did not include first Encounter or retained descriptive copy.");
                    await FreezeCheck();
                    await Task.Delay(1200);
                    Require(announcement.GetComponent<CanvasGroup>().alpha>.99f && Runner.Stage==EncounterStage.Intro && Enemies.Length==0,
                        "Mission card disappeared too soon or first combat started before reading.");
                    await Capture("mission-card-"+width+"-"+size);
                    Require(!rig.Input.FireHeld,"Held cinematic press survived into gameplay Mission card.");
                    var cardPoint=new Vector2(width*.2f,200);
                    await rig.MouseEvent(cardPoint,false);
                    var cardPosition=rig.Player.transform.position;
                    await rig.MouseEvent(cardPoint,true);await rig.MouseEvent(cardPoint+new Vector2(65,20),true);
                    Require(rig.Input.FireHeld && rig.Player.transform.position.x>cardPosition.x && UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length>0 && Enemies.Length==0,
                        "Mission card blocked fresh gameplay drag/fire or spawned enemies before reading completed.");
                    await rig.MouseEvent(cardPoint+new Vector2(65,20),false);
                    foreach(var shot in UnityEngine.Object.FindObjectsByType<PlayerProjectile>())shot.Release();
                    await Until(()=>Runner.Stage==EncounterStage.Running,3200,"Intro did not hand off.");
                    rig.Protect();
                    Require(!rig.Input.FireHeld && UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length==0 && Mathf.Abs(Camera.main.orthographicSize-size)<.01f,"Intro camera/input did not settle or held input fired.");
                    await rig.MouseEvent(new Vector2(width*.2f,200),false);
                    Require(Ui<CanvasGroup>("CombatHud").alpha>.99f && Ui<Button>("Pause").gameObject.activeSelf,"HUD did not return.");
                }
                else
                {
                    await Task.Delay(500);
                    startCapture=await Capture("mission-start-"+width);
                    Require(UnityEngine.Object.FindAnyObjectByType<GameplayAnnouncement>().GetComponent<CanvasGroup>().alpha>0,"Mission title missing.");
                }
                Require(Ui<Text>("Title").text == GameplayCopy.Pause,"Hidden overlay copy invalid.");
                await Until(() => Enemies.Length == 3 && Enemies.All(e => e.GetComponent<EntryHoldFlight>().IsHolding), 2500, "A formation not ready.");
                Require(Runner.CompletionCount == 0, "A resolved without kills.");
                if (withDeath)
                {
                    rig.Unprotect();
                    var life = rig.Player.GetComponent<PlayerDamageLifecycle>();
                    // Đưa tàu đúng đường đạn để kiểm tra collision và death thật.
                    var target = Enemies.OrderBy(e => Mathf.Abs(e.transform.position.x)).First();
                    rig.Place(new Vector2(target.transform.position.x, -3.8f));
                    await Until(() => rig.Lives.CurrentLives == 2, 12000, "Real hostile shot did not trigger death.");
                    Require(!rig.Gate.CanFire && !rig.Gate.CanTakeDamage, "Death permission remained open.");
                    await Until(() => rig.Gate.State == PlayerGameplayState.Respawning, 1200, "Respawn entrance missing.");
                    Require(!rig.Gate.CanInput && !rig.Gate.CanFire && !rig.Gate.CanTakeDamage, "Entrance permissions invalid.");
                    await Until(() => rig.Gate.State == PlayerGameplayState.PostEntryInvincibility, 1800, "Invincibility missing.");
                    Require(rig.Gate.CanInput && rig.Gate.CanFire && !rig.Gate.CanTakeDamage &&
                        !UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().CanFire, "Protection/grace boundary invalid.");
                    await FreezeCheck();
                    Require(!life.TryReceiveHit(null), "Invincibility accepted hit.");
                    await Until(() => rig.Gate.State == PlayerGameplayState.Active, 3300, "Invincibility clock stuck.");
                    rig.Protect();
                }
                await FreezeCheck();
                foreach (var enemy in Enemies.OrderBy(e => e.transform.position.x).ToArray()) await rig.KillWithPulse(enemy);
                await Until(() => Runner.CompletionCount == 1,
                    700 + Mathf.CeilToInt((Runner.Current.ProjectileGraceSeconds + Runner.Current.ProjectileFadeSeconds) * 1000),
                    "A did not complete after bounded projectile cleanup.");
                Require(Runner.HasActiveRun && !Runner.IsRunning, "A/B handoff boundary missing.");
                await Task.Delay(300);
                Require(Ui<CanvasGroup>("CombatHud").alpha==1 && Ui<Button>("Pause").gameObject.activeSelf && Ui<Button>("Pause").interactable && rig.Gate.CanInput,
                    "Minor handoff hid HUD/Pause or locked controls.");
                Require(!Ui<Text>("ContinueHint").gameObject.activeSelf,"Minor transition demanded confirmation.");
                var handoffAnnouncement=UnityEngine.Object.FindAnyObjectByType<GameplayAnnouncement>();
                Require(handoffAnnouncement.transform.Find("Title").GetComponent<Text>().text=="Encounter 02" && handoffAnnouncement.transform.Find("Title").GetComponent<Text>().alignment==TextAnchor.MiddleCenter && ((RectTransform)handoffAnnouncement.transform).anchorMin==new Vector2(.5f,.5f),"Minor transition title not centered/structural.");
                await Capture("handoff-"+width+"-"+size);
                await FreezeCheck();
                await Until(() => Runner.CurrentIndex == 1 && Runner.SpawnedCount == 6, 3200, "B mirrored spawn failed.");
                await Until(() => Enemies.Length == 6 && Enemies.All(e => e.GetComponent<EntryHoldFlight>().IsHolding), 4500, "B hold formation missing.");
                var bCapture = await Capture("formation-" + width + "-" + size);
                await FreezeCheck();
                await Until(() => Session.IsClear, 18000, "Mission Clear did not occur.");
                await Task.Delay(600);
                Require(rig.Gate.State == PlayerGameplayState.Result && !rig.Gate.CanInput && !rig.Gate.CanFire && !rig.Gate.CanTakeDamage &&
                    clearCount == 1 && Runner.CompletionCount == 2 && !Runner.HasActiveRun && Enemies.Length == 0 &&
                    UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length == 0 && UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length == 0,
                    "Clear authority/cleanup/count failed.");
                Require(order.SequenceEqual(new[] { "ENCOUNTER_1", "ENCOUNTER_2" }), "Encounter order/double-completion failed.");
                var overlay = Ui<RectTransform>("Overlay"); Require(overlay.gameObject.activeSelf, "Clear overlay missing.");
                var clearCapture = await Capture("clear-" + width + "-" + size);
                await Task.Delay(450);
                Require(clearCount == 1 && Enemies.Length == 0 && !rig.Input.FireHeld, "Stale clear/spawn/input.");
                return new { passed = true, width, height = Screen.height, size, withDeath, clearCount,
                    intro, anywhereTap = intro, gameplayCardControls = intro, directGameplayAfterBrake = intro, travelSeconds = Runner.Flow.Level.Intro.TravelSeconds,
                    cinematicPauseBlocked = true, minorTransitionHudPreserved = true, realPulseKills = 3, realHostileDeath = withDeath, pauseCombat = true, handoffBreathingRoom = true, startCapture, bCapture, clearCapture,
                    frames = Time.frameCount - rig.FirstFrame };
            }
            finally { Runner.Completed -= completed; Runner.MissionCompleted -= cleared; }
        }
    }

    public static async Task<object> EstablishingInput()
    {
        using(var rig=new Rig(2000,6,true))
        {
            var point=new Vector2(280,650);
            await rig.MouseEvent(point,true);
            await Until(()=>Runner.Flow.CanContinue,2400,"Prompt never armed.");
            await rig.MouseEvent(point,false);
            Require(Runner.Flow.IsAwaitingStart,"Press begun before prompt became a continue tap.");
            await rig.MouseEvent(point,true);await rig.MouseEvent(point+new Vector2(90,0),true);await rig.MouseEvent(point,false);
            Require(Runner.Flow.IsAwaitingStart,"Drag counted as tap.");
            await rig.MouseEvent(point,true);await Task.Delay(650);await rig.MouseEvent(point,false);
            Require(Runner.Flow.IsAwaitingStart,"Long hold counted as tap.");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState {position=point}.WithButton(MouseButton.Right));await Task.Delay(80);
            InputSystem.QueueStateEvent(Mouse.current,new MouseState {position=point});await Task.Delay(80);
            Require(Runner.Flow.IsAwaitingStart,"Right click counted as continue.");
            await rig.TouchEvent(1,TouchPhase.Began,point);await rig.TouchEvent(1,TouchPhase.Canceled,point);
            Require(Runner.Flow.IsAwaitingStart,"Canceled touch counted as continue.");
            await rig.TouchEvent(2,TouchPhase.Began,new Vector2(1700,300));await rig.TouchEvent(2,TouchPhase.Ended,new Vector2(1700,300));
            Require(!Runner.Flow.IsAwaitingStart && !rig.Input.FireHeld,"Valid touch failed or leaked fire.");
            await rig.TouchEvent(3,TouchPhase.Began,point);
            await Until(()=>Runner.Stage==EncounterStage.Running,9500,"Touch intro never completed.");rig.Protect();
            var position=rig.Player.transform.position;
            await rig.TouchEvent(3,TouchPhase.Moved,point+new Vector2(100,0));
            Require(!rig.Input.FireHeld && Vector3.Distance(position,rig.Player.transform.position)<.001f && UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length==0,
                "Held cinematic finger moved/fired after unlock.");
            await rig.TouchEvent(3,TouchPhase.Ended,point+new Vector2(100,0));
            await rig.TouchEvent(4,TouchPhase.Began,point);
            Require(rig.Input.FireHeld,"Fresh gameplay touch did not restore fire.");
            await rig.TouchEvent(4,TouchPhase.Ended,point);
            return new {passed=true,earlyHoldRejected=true,dragRejected=true,longHoldRejected=true,rightClickRejected=true,canceledTouchRejected=true,
                anywhereTouch=true,heldFingerConsumed=true,freshGameplayPress=true};
        }
    }

    public static async Task<object> CaptureIntroPreview()
    {
        using(var rig=new Rig(1600,6,true))
        {
            var folder=System.IO.Path.GetFullPath("Temp/StarShiftIntroReview/ux-preview-frames");System.IO.Directory.CreateDirectory(folder);
            int frame=0;
            float capturedAt=Time.realtimeSinceStartup;
            async Task Frame()
            {
                ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder,$"{frame++:0000}.png"));
                await Task.Delay(100);
            }
            while(!Runner.Flow.CanContinue)await Frame();
            for(int i=0;i<7;i++)await Frame();
            await rig.MouseEvent(new Vector2(1400,500),true);
            await rig.MouseEvent(new Vector2(1400,500),false);
            var deadline=Time.realtimeSinceStartup+12;
            while(Runner.Stage==EncounterStage.Intro && Time.realtimeSinceStartup<deadline)await Frame();
            Require(Runner.Stage==EncounterStage.Running,"Preview never reached gameplay.");rig.Protect();
            for(int i=0;i<10;i++)await Frame();
            float duration=Time.realtimeSinceStartup-capturedAt;
            return new {passed=true,folder,frames=frame,durationSeconds=duration,framesPerSecond=frame/duration};
        }
    }

    public static async Task<object> EditorViewRestoration()
    {
        Require(Application.isPlaying,"Play Mode required.");
        var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var window=EditorWindow.GetWindow(type);
        var selected=type.GetProperty("selectedSizeIndex");var originalIndex=selected.GetValue(window);
        var size=type.GetProperty("currentGameViewSize",Fields).GetValue(window);var originalSize=JsonUtility.ToJson(size);
        var zoom=type.GetField("m_ZoomArea",Fields).GetValue(window);
        var scaleProperty=zoom.GetType().GetProperty("scale");var panProperty=zoom.GetType().GetProperty("translation");
        var originalZoom=(Vector2)scaleProperty.GetValue(zoom);var originalPan=(Vector2)panProperty.GetValue(zoom);
        for(int i=0;i<2;i++)
        {
            try
            {
                using(var rig=new Rig(i==0?1600:2000,6,true))
                {
                    await Task.Delay(150);
                    if(i==1)throw new OperationCanceledException("Expected verification interruption.");
                }
            }
            catch(OperationCanceledException) { }
            await Task.Delay(200);
            Require(Equals(selected.GetValue(window),originalIndex) && JsonUtility.ToJson(size)==originalSize &&
                Vector2.Distance((Vector2)scaleProperty.GetValue(zoom),originalZoom)<.0001f &&
                Vector2.Distance((Vector2)panProperty.GetValue(zoom),originalPan)<.001f,
                "Verification changed the original Game View preset, resolution, zoom or pan.");
        }
        return new {passed=true,successAndExceptionCleanup=true,originalIndex,zoom=originalZoom.x,panX=originalPan.x,panY=originalPan.y};
    }

    public static async Task<object> GameOver()
    {
        using (var rig = new Rig(2000, 6))
        {
            rig.Unprotect();
            var lifecycle = rig.Player.GetComponent<PlayerDamageLifecycle>();
            var signals = 0; Action onOver = () => signals++;
            lifecycle.GameOver += onOver;
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    await Until(() => rig.Gate.State == PlayerGameplayState.Active, 6000, "Player never active before fatal hit.");
                    Require(lifecycle.TryReceiveHit(null), "Hit not accepted.");
                    Require(rig.Lives.CurrentLives == 2 - i && !lifecycle.TryReceiveHit(null), "Duplicate fatal damage accepted.");
                    if (i < 2) continue;
                    var spawned = Runner.SpawnedCount;
                    await Task.Delay(90);
                    Require(!Runner.HasActiveRun && Runner.SpawnedCount == spawned && !Session.IsTerminal,
                        "Lives zero did not stop schedule before death sequence finished.");
                }
                await Until(() => Session.IsTerminal, 1200, "Game Over boundary missing.");
                Require(!Session.IsClear && signals == 1 && rig.Gate.State == PlayerGameplayState.GameOver &&
                    !rig.Gate.CanInput && !rig.Gate.CanFire && Enemies.Length == 0 &&
                    UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length == 0, "Game Over cleanup/state invalid.");
                var capture = await Capture("game-over-20x9");
                await Task.Delay(400); Require(signals == 1 && !Runner.HasActiveRun, "Duplicate Game Over or stale schedule.");
                return new { passed = true, signals, capture, finalLives = rig.Lives.CurrentLives, noSpawnDuringFinalDeath = true };
            }
            finally { lifecycle.GameOver -= onOver; }
        }
    }

    public static async Task<object> Pooling()
    {
        var selection = Selection.objects;
        try
        {
            // Inspector preview có thể Destroy object Editor, gây nhiễu marker toàn cục.
            Selection.objects = Array.Empty<UnityEngine.Object>();
            using (var rig = new Rig(1600, 6))
            {
                await Task.Delay(1800);
                var edge = Camera.main.ViewportToWorldPoint(new Vector3(.06f, .16f, 10));
                rig.Place(edge);
                await rig.MouseEvent(new Vector2(280, 180), false);
                await rig.MouseEvent(new Vector2(280, 180), true);
                await Task.Delay(12000);
                var playerIds = All<PlayerProjectile>().Select(p => p.GetEntityId()).OrderBy(v => v).ToArray();
                var hostileIds = All<EnemyProjectile>().Select(p => p.GetEntityId()).OrderBy(v => v).ToArray();
                var scoutIds = All<EnemyHealth>().Select(p => p.GetEntityId()).OrderBy(v => v).ToArray();
                using (var instantiate = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Loading, "Instantiate", 2048))
                using (var destroy = Unity.Profiling.ProfilerRecorder.StartNew(new Unity.Profiling.ProfilerCategory("Other"), "Destroy", 2048))
                {
                    Require(instantiate.Valid && destroy.Valid, "Profiler markers unavailable.");
                    var frame = Time.frameCount;
                    await Task.Delay(12000);
                    instantiate.Stop(); destroy.Stop();
                    var instantiates = new List<Unity.Profiling.ProfilerRecorderSample>(); instantiate.CopyTo(instantiates);
                    var destroys = new List<Unity.Profiling.ProfilerRecorderSample>(); destroy.CopyTo(destroys);
                    var createCalls = instantiates.Sum(sample => sample.Count);
                    var destroyCalls = destroys.Sum(sample => sample.Count);
                    Require(playerIds.SequenceEqual(All<PlayerProjectile>().Select(p => p.GetEntityId()).OrderBy(v => v)) &&
                        hostileIds.SequenceEqual(All<EnemyProjectile>().Select(p => p.GetEntityId()).OrderBy(v => v)) &&
                        scoutIds.SequenceEqual(All<EnemyHealth>().Select(p => p.GetEntityId()).OrderBy(v => v)), "Pooled hierarchy grew after warmup.");
                    Require(createCalls == 0 && destroyCalls == 0, "Profiler saw Instantiate/Destroy after warmup: " + createCalls + "/" + destroyCalls +
                        "; Destroy sample counts=" + string.Join(",", destroys.Where(sample => sample.Count > 0).Select(sample => sample.Count)));
                    return new { passed = true, warmupSeconds = 12, measuredSeconds = 12, frames = Time.frameCount - frame,
                        playerPool = playerIds.Length, hostilePool = hostileIds.Length, scouts = scoutIds.Length,
                        instantiateCalls = createCalls, destroyCalls, pooledIdentityStable = true };
                }
            }
        }
        finally { Selection.objects = selection; }
    }

    public static async Task<object> ResetAndStandalone()
    {
        using (var rig = new Rig(2000, 6))
        {
            Runner.StartRun("ENCOUNTER_2", true);
            await Until(() => !Runner.HasActiveRun, 20000, "Standalone B did not finish.");
            Require(!Session.IsTerminal && Runner.CompletionCount == 1, "Standalone B created mission clear.");
            var dev = Runner.GetComponent<StarShift.Dev.DevFastStart>();
            var before = EditorJsonUtility.ToJson(dev);
            try
            {
                Set(dev, "devModeEnabled", true); Set(dev, "startPoint", "LEVEL_START"); Set(dev, "standalone", false); Set(dev, "skipIntro", true);
                Runner.StartRun("LEVEL_START", false, true, null, null);
                Session.Pause(); Require(Time.timeScale == 0, "Pause fixture failed.");
                dev.Replay();
                Require(Time.timeScale == 1 && !Session.IsPaused && Runner.CurrentIndex == 0 && Runner.IsRunning &&
                    rig.Lives.CurrentLives == 3 && !rig.Input.FireHeld, "Dev replay from Pause retained state/clock.");
                Session.Pause(); Session.enabled = false;
                Require(Time.timeScale == 1 && !AudioListener.pause, "Disable leaked Pause clock/audio.");
                Session.enabled = true;
                dev.Replay();
                await Task.Delay(100);
                Require(Runner.CurrentIndex == 0 && Runner.SpawnedCount == 1, "Replay duplicated spawn/callback.");
                return new { passed = true, standaloneNotMissionClear = true, devReplayFromPause = true,
                    disableRestoresClockAndAudio = true, noDuplicateSpawn = true };
            }
            finally { EditorJsonUtility.FromJsonOverwrite(before, dev); }
        }
    }

    public static async Task<object> TerminalReplaySequence()
    {
        var results = new List<object>();
        for (int i = 0; i < 3; i++)
        {
            var gameOver = await GameOver();
            var replay = await ResetAndStandalone();
            results.Add(new { iteration = i + 1, gameOver, replay });
        }
        return new { passed = true, samePlaySession = true, iterations = 3, results };
    }

    static T[] All<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
    sealed class Rig : IDisposable
    {
        public readonly GameObject Player = GameObject.Find("Player");
        public readonly PlayerGameplayGate Gate;
        public readonly PlayerLives Lives;
        public readonly InputSystemPointerInput Input;
        public readonly int FirstFrame = Time.frameCount;
        readonly Mouse mouse;
        readonly Touchscreen touch;
        readonly Mouse oldMouse;
        readonly List<InputDevice> disabled = new List<InputDevice>();
        readonly InputSettings.EditorInputBehaviorInPlayMode oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
        readonly InputSettings.BackgroundBehavior oldBackground = InputSystem.settings.backgroundBehavior;
        readonly object gameView;
        readonly object gameViewZoom;
        readonly Vector2 oldGameViewZoom, oldGameViewPan;
        readonly PropertyInfo selectedSize;
        readonly object oldSize;
        readonly GameplayViewController view;
        readonly GameplayViewSettings oldView;
        readonly float oldScale = Time.timeScale;
        readonly bool oldAudio = AudioListener.pause;
        readonly string[] definitions;
        Vector2 pointer = new Vector2(400, 220);
        readonly CameraProfile profile;
        public Rig(int width, float size, bool intro = false)
        {
            Require(Application.isPlaying, "Play Mode required.");
            Session.enabled = false; Runner.StopRun(); Time.timeScale = 1; AudioListener.pause = false; Session.enabled = true;
            Gate = Player.GetComponent<PlayerGameplayGate>(); Lives = Player.GetComponent<PlayerLives>(); Input = Player.GetComponent<InputSystemPointerInput>();
            var life = Player.GetComponent<PlayerDamageLifecycle>(); life.enabled = false; life.enabled = true;
            Place(new Vector2(0, -3.8f)); Protect();
            view = Camera.main.GetComponent<GameplayViewController>(); oldView = view.TargetView;
            view.RequestView(new GameplayViewSettings(size, new Rect(0, 0, 1, 1)), 0);
            var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"); gameView = EditorWindow.GetWindow(type);
            selectedSize = type.GetProperty("selectedSizeIndex"); oldSize = selectedSize.GetValue(gameView);
            gameViewZoom = gameView.GetType().GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameView);
            oldGameViewZoom = (Vector2)gameViewZoom.GetType().GetProperty("scale").GetValue(gameViewZoom);
            oldGameViewPan = (Vector2)gameViewZoom.GetType().GetProperty("translation").GetValue(gameViewZoom);
            type.GetMethod("SetCustomResolution", Fields).Invoke(gameView, new object[] { new Vector2(width, 900), "StarShift Closure QA " + width + "x900" });
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            oldMouse = Mouse.current;
            foreach (var d in InputSystem.devices)
                if (d.native && d.enabled && (d is Mouse || d is Touchscreen)) { disabled.Add(d); InputSystem.DisableDevice(d); }
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent(); touch = InputSystem.AddDevice<Touchscreen>();
            definitions = Configs().Select(EditorJsonUtility.ToJson).ToArray();
            profile = ScriptableObject.CreateInstance<CameraProfile>();
            profile.View = new GameplayViewSettings(size,new Rect(0,0,1,1));
            Runner.StartRun("LEVEL_START", false, !intro, null, profile);
        }
        public void Protect() { Player.GetComponent<PlayerDamageLifecycle>().enabled = false; Gate.SetCinematicLock(false); Gate.SetState(PlayerGameplayState.PostEntryInvincibility); UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0); }
        public void Unprotect() { Player.GetComponent<PlayerDamageLifecycle>().enabled = true; Gate.SetState(PlayerGameplayState.Active); }
        public void Place(Vector2 position) { Player.GetComponent<Rigidbody2D>().position = position; Player.transform.position = new Vector3(position.x, position.y, 0); }
        public async Task MouseEvent(Vector2 position, bool held)
        {
            pointer = position;
            var state = new MouseState { position = position };
            if (held) state = state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(mouse, state); await Task.Delay(75);
        }
        public async Task TouchEvent(int id, TouchPhase phase, Vector2 position)
        {
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = id, phase = phase, position = position }); await Task.Delay(80);
        }
        public async Task KillWithPulse(EnemyHealth target)
        {
            await MouseEvent(pointer, false); await MouseEvent(pointer, true);
            var current = Camera.main.WorldToScreenPoint(Player.transform.position);
            var wanted = Camera.main.WorldToScreenPoint(target.transform.position);
            pointer += new Vector2(wanted.x - current.x, 0);
            await MouseEvent(pointer, true);
            try { await Until(() => !target.IsAlive, 2500, "Real held Pulse did not kill Scout."); }
            catch
            {
                await Capture("pulse-failure");
                var hitList=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) {position=pointer},hitList);
                throw new InvalidOperationException("Pulse failure: held="+Input.FireHeld+" canFire="+Gate.CanFire+" state="+Gate.State+
                    " player="+Player.transform.position+" target="+target.transform.position+" hp="+target.CurrentHp+
                    " pointer="+pointer+" hits="+string.Join(",",hitList.Select(h=>h.gameObject.name))+
                    " shots="+UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length+" mouse="+mouse.deviceId+" pressed="+mouse.leftButton.isPressed+
                    " current="+Mouse.current?.deviceId+" focused="+Application.isFocused);
            }
            await MouseEvent(pointer, false);
        }
        static EncounterDefinition[] Configs() => new[] { "A", "B" }.Select(id => AssetDatabase.LoadAssetAtPath<EncounterDefinition>("Assets/StarShift/Data/Fixtures/Encounters/L1Encounter" + id + ".asset")).ToArray();
        public void Dispose()
        {
            Session.enabled = false; Runner.StopRun(); Time.timeScale = 1;
            InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(touch);
            foreach (var d in disabled) InputSystem.EnableDevice(d);
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor; InputSystem.settings.backgroundBehavior = oldBackground;
            var life = Player.GetComponent<PlayerDamageLifecycle>(); life.enabled = false; life.enabled = true; Input.CancelGesture();
            view.SetBaseline(Runner.Flow.Level.Camera.View); view.RequestView(oldView, 0); gameView.GetType().GetMethod("SizeSelectionCallback").Invoke(gameView, new object[] { oldSize, null });
            gameViewZoom.GetType().GetMethod("SetTransform").Invoke(gameViewZoom, new object[] { oldGameViewPan, oldGameViewZoom });
            UnityEngine.Object.Destroy(profile);
            Session.enabled = true; Time.timeScale = oldScale; AudioListener.pause = oldAudio;
            Require(definitions.SequenceEqual(Configs().Select(EditorJsonUtility.ToJson)), "QA mutated encounter config assets.");
        }
    }
}
