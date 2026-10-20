using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Encounters;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Persistence;
using StarShift.Player;
using StarShift.Rewards;
using StarShift.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyM8Art
{
    const string EvidenceRoot = "../.design/m8/";
    static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    static EncounterRunner Runner => UnityEngine.Object.FindAnyObjectByType<EncounterRunner>();
    static GameplaySession Session => Runner.GetComponent<GameplaySession>();
    static GameShell Shell => Runner.GetComponent<GameShell>();
    static ShellView View => UnityEngine.Object.FindAnyObjectByType<ShellView>();
    static CampaignLifecycle Save => Runner.GetComponent<CampaignLifecycle>();
    static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static void Write(int width, object result) => File.WriteAllText(EvidenceRoot + "runtime-" + width + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
    static async Task Until(Func<bool> condition, int milliseconds, string message)
    { var end = Time.realtimeSinceStartup + milliseconds / 1000f; while (!condition() && Time.realtimeSinceStartup < end) await Task.Delay(30); Require(condition(), message); }
    static async Task Capture(string name, int settleMs = 200)
    {
        if (settleMs > 0) await Task.Delay(settleMs); Canvas.ForceUpdateCanvases();
        var path = Path.GetFullPath(EvidenceRoot + "screenshots/" + name + ".png"); ScreenCapture.CaptureScreenshot(path);
        await Task.Delay(180); Require(File.Exists(path), "Screenshot missing: " + name);
    }
    public static object Begin(int width)
    {
        Require(Application.isPlaying && (width == 1600 || width == 2000), "Play Mode / target width required");
        Require(!Runner.HasActiveRun, "Return to Menu before isolated art QA; active user campaign must not be interrupted");
        Directory.CreateDirectory(EvidenceRoot + "screenshots"); Write(width, new { status = "RUNNING" });
        _ = Run(width); return new { started = width };
    }
    static async Task Run(int width)
    {
        object result = null; Mouse mouse = null; var oldMouse = Mouse.current;
        var disabled = new List<InputDevice>(); var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        var background = InputSystem.settings.backgroundBehavior; var originalSize = (object)null;
        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var gameView = Resources.FindObjectsOfTypeAll(gameViewType).FirstOrDefault() ?? EditorWindow.GetWindow(gameViewType);
        var sizeProperty = gameViewType.GetProperty("selectedSizeIndex"); originalSize = sizeProperty.GetValue(gameView);
        var production = Path.Combine(Application.persistentDataPath, "starshift-save-v1.json");
        var hashes = new Dictionary<string, byte[]>(); foreach (var suffix in new[] { "", ".bak", ".invalid" }) hashes[suffix] = File.Exists(production + suffix) ? File.ReadAllBytes(production + suffix) : null;
        var player = GameObject.Find("Player"); var damage = player.GetComponent<PlayerDamageLifecycle>(); var damageEnabled = damage.enabled;
        var gate = player.GetComponent<PlayerGameplayGate>(); var weapon = player.GetComponent<PulseLaserWeapon>();
        Action restoreInput = () => {
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled) if (device.added) InputSystem.EnableDevice(device);
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput; InputSystem.settings.backgroundBehavior = background;
        };
        Action<PlayModeStateChange> onState = state => { if (state == PlayModeStateChange.ExitingPlayMode) restoreInput(); };
        EditorApplication.LockReloadAssemblies(); EditorApplication.playModeStateChanged += onState;
        try
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var d in InputSystem.devices) if (d.native && d.enabled && (d is UnityEngine.InputSystem.Pointer || d is Keyboard || d is Gamepad)) { disabled.Add(d); InputSystem.DisableDevice(d); }
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            Save.UseTestStorage(Path.Combine(Path.GetTempPath(), "starshift-m8-" + Guid.NewGuid().ToString("N"), "save.json")); Shell.Boot();
            gameViewType.GetMethod("SetCustomResolution", Fields).Invoke(gameView, new object[] { new Vector2(width, 900), "StarShift M8 " + width + "x900" });
            await Until(() => Screen.width == width && Screen.height == 900, 3000, "Actual Game View resolution mismatch");
            await Capture("menu-" + width);
            View.NewGame.onClick.Invoke();
            await Until(() => !Shell.IsBusy && Shell.Screen == ShellScreen.Playing && Session.Flow.CanContinue, 6000, "Menu to mission entry");
            Session.BeginMission(); await Until(() => Runner.Stage == EncounterStage.Running, 15000, "Real combat did not begin");
            damage.enabled = false; gate.SetState(PlayerGameplayState.Active); UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0);
            await Until(() => EnemyHealth.Active.Count(e => e.IsAlive && e.GetComponent<EntryHoldFlight>() != null && e.GetComponent<EntryHoldFlight>().IsHolding) >= 4, 10000, "Real formation missing");
            var frameStart = Time.frameCount; var enemy = EnemyHealth.Active.First(e => e.IsAlive); var enemyStart = enemy.transform.position;
            var playerStart = player.transform.position; int shots = weapon.ShotsFired;
            var point = new Vector2(width * .52f, 220);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left)); await Task.Delay(120);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point + new Vector2(140, 40) }.WithButton(MouseButton.Left)); await Task.Delay(800);
            Require(Vector3.Distance(playerStart, player.transform.position) > .15f, "Actual relative drag did not move player");
            Require(weapon.ShotsFired > shots, "Actual hold-to-fire did not emit projectiles");
            Require(Time.frameCount > frameStart + 10, "Player loop did not advance");
            Require(Vector3.Distance(enemyStart, enemy.transform.position) > .01f, "Formation did not move");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); await Task.Delay(100);
            var rewardDirector = Runner.GetComponent<RewardDirector>();
            var shard = AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/StellarShard.asset");
            var pickup = rewardDirector.Spawn(shard, null, Camera.main.ViewportToWorldPoint(new Vector3(.38f, .28f, 10)));
            Require(pickup.GetComponentInChildren<SpriteRenderer>().sprite == shard.Sprite, "Shard sprite routing failed");
            Require(pickup.GetComponentInChildren<TextMesh>().text == "", "Legacy text covers native shard art");
            await Capture("gameplay-" + width);
            await Until(() => EnemyHealth.Active.Any(e => e.IsAlive && e.GetComponent<SingleFireAttack>() != null && e.GetComponent<SingleFireAttack>().IsTelegraphing), 10000, "Real hostile telegraph missing");
            var shooter = EnemyHealth.Active.First(e => e.IsAlive && e.GetComponent<SingleFireAttack>() != null && e.GetComponent<SingleFireAttack>().IsTelegraphing);
            var cue = shooter.GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "CorePlaceholder");
            Require(cue.enabled && cue.sprite.name == "enemy-telegraph", "Telegraph visual disabled or stale");
            await Capture("telegraph-" + width);
            await Until(() => UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length > 0, 4000, "Hostile projectile missing");
            await Capture("hostile-shots-" + width);
            var victim = EnemyHealth.Active.First(e => e.CanReceiveDamage && e.GetComponent<BossPhaseRunner>() == null);
            victim.ReceivePlayerHit(victim.CurrentHp, player); await Task.Delay(70);
            var burst = UnityEngine.Object.FindObjectsByType<SpriteRenderer>().First(r => r.name == "AsteroidBurst"); var frame = burst.sprite.name;
            await Task.Delay(90); Require(burst.sprite.name != frame, "Explosion did not advance frames");
            await Capture("explosion-" + width, 0); await Task.Delay(600);
            Require(!burst.gameObject.activeSelf, "Explosion did not release to pool");
            pickup.Release();
            var other = AssetDatabase.LoadAssetAtPath<PickupDefinition>("Assets/StarShift/Data/Rewards/ShieldCell.asset");
            Require(other != null, "Shield fallback fixture missing"); { var fallback = rewardDirector.Spawn(other, null, Camera.main.ViewportToWorldPoint(new Vector3(.4f, .3f, 10))); Require(fallback.GetComponentInChildren<SpriteRenderer>().sprite != shard.Sprite, "Pooled pickup leaked shard sprite"); fallback.Release(); }
            Session.Pause(); await Capture("pause-" + width); View.Resume.onClick.Invoke(); await Task.Delay(150);
            Runner.StopRun(); Runner.Flow.LoadLevel(Runner.GetComponent<RegionCampaignFlow>().FindMission("L2"));
            Runner.StartRun("ENCOUNTER_3", true, true, null, null); gate.SetState(PlayerGameplayState.Active); UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0);
            await Until(() => EnemyHealth.Active.Any(e => e.IsAlive && e.name.Contains("Bomber")), 12000, "Production heavy encounter missing");
            await Until(() => EnemyHealth.Active.Any(e => e.IsAlive && e.name.Contains("Bomber") && e.GetComponent<EntryHoldFlight>().IsHolding), 9000, "Heavy did not reach formation"); await Capture("heavy-" + width);
            Runner.StopRun(); Runner.Flow.LoadLevel(Runner.GetComponent<RegionCampaignFlow>().FindMission("L4"));
            Runner.StartRun("BOSS", true, true, null, null); gate.SetState(PlayerGameplayState.Active); UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0);
            await Until(() => EnemyHealth.Active.Any(e => e.IsAlive && e.GetComponent<BossPhaseRunner>() != null), 10000, "Production boss missing");
            await Until(() => EnemyHealth.Active.Any(e => e.IsAlive && e.GetComponent<BossPhaseRunner>() != null && e.GetComponent<EntryHoldFlight>().IsHolding), 10000, "Boss did not reach hold"); await Task.Delay(1600); await Capture("boss-module-" + width);
            var boss = EnemyHealth.Active.First(e => e.IsAlive && e.GetComponent<BossPhaseRunner>() != null);
            Require(boss.GetComponentsInChildren<SpriteRenderer>().Any(r => r.sprite != null && r.sprite.name == "boss-module"), "Boss module not integrated");
            result = new { status = "PASS", actualResolution = Screen.width + "x" + Screen.height, frameStart, frameEnd = Time.frameCount, inputDrag = true, inputFire = true, formationMovement = true, hostileTelegraph = true, hostileShots = true, shardRouting = true, pickupPoolReset = true, explosionFrameAdvance = true, explosionPoolRelease = true, heavyProductionEncounter = true, bossProductionEncounter = true, protectedQaPlayer = true, ppu = 56.25f, physicsAndBalance = "compare contracts-before.json and contracts-after.json" };
        }
        catch (Exception e) { result = new { status = "FAILED", error = e.ToString() }; }
        finally
        {
            try
            {
                damage.enabled = damageEnabled;
                Runner.StopRun(); Save.ExitIsolated(); Save.UseTestStorage(production); Shell.Boot();
                foreach (var pair in hashes)
                {
                    var after = File.Exists(production + pair.Key) ? File.ReadAllBytes(production + pair.Key) : null;
                    Require(pair.Value == null ? after == null : after != null && pair.Value.SequenceEqual(after), "Production save changed during M8 QA: " + pair.Key);
                }
            }
            catch (Exception e) { result = new { status = "FAILED", cleanupError = e.ToString() }; }
            finally
            {
                restoreInput(); EditorApplication.playModeStateChanged -= onState;
                sizeProperty.SetValue(gameView, originalSize); EditorApplication.UnlockReloadAssemblies();
            }
            Write(width, result ?? new { status = "FAILED", error = "No result" });
        }
    }
}
