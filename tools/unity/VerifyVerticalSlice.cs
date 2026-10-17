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
        var enemies = Enemies.ToDictionary(e => e, e => e.transform.position);
        var timers = Enemies.ToDictionary(e => e, e => Timer(e.GetComponent<SingleFireAttack>(), "remaining"));
        var projectiles = UnityEngine.Object.FindObjectsByType<EnemyProjectile>().ToDictionary(p => p, p => p.transform.position);
        var lifecycle = GameObject.Find("Player").GetComponent<PlayerDamageLifecycle>();
        var lifeTimer = Timer(lifecycle, "remaining");
        var grace = Timer(UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>(), "graceRemaining");
        await Task.Delay(220);
        Require(Runner.Elapsed == elapsed && enemies.All(e => e.Key != null && e.Key.transform.position == e.Value) &&
            projectiles.All(p => p.Key != null && p.Key.transform.position == p.Value) &&
            timers.All(e => Timer(e.Key.GetComponent<SingleFireAttack>(), "remaining") == e.Value) &&
            Timer(lifecycle, "remaining") == lifeTimer && Timer(UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>(), "graceRemaining") == grace,
            "Simulation/timer changed while paused.");
        Session.Resume(); Require(!Session.IsPaused && Time.timeScale == 1, "Pause did not resume prior clock.");
    }

    public static async Task<object> FullRun(int width, float size, bool withDeath)
    {
        using (var rig = new Rig(width, size))
        {
            var order = new List<string>();
            Action<EncounterDefinition> completed = d => order.Add(d.Id);
            var clearCount = 0;
            Action cleared = () => clearCount++;
            Runner.Completed += completed; Runner.MissionCompleted += cleared;
            try
            {
                await Task.Delay(500);
                Require(Ui<Text>("Title").text == GameplayCopy.Pause, "Hidden overlay copy invalid.");
                var startCapture = await Capture("mission-start-" + width);
                var announcement = UnityEngine.Object.FindAnyObjectByType<GameplayAnnouncement>();
                Require(announcement.GetComponent<CanvasGroup>().alpha > 0, "Mission announcement did not appear.");
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
                await Until(() => Runner.CompletionCount == 1, 700, "A did not complete.");
                Require(Runner.HasActiveRun && !Runner.IsRunning, "A/B handoff boundary missing.");
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
                    realPulseKills = 3, realHostileDeath = withDeath, pauseCombatAndHandoff = true, startCapture, bCapture, clearCapture,
                    frames = Time.frameCount - rig.FirstFrame };
            }
            finally { Runner.Completed -= completed; Runner.MissionCompleted -= cleared; }
        }
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
                Require(createCalls == 0 && destroyCalls == 0, "Profiler saw Instantiate/Destroy after warmup: " + createCalls + "/" + destroyCalls);
                return new { passed = true, warmupSeconds = 12, measuredSeconds = 12, frames = Time.frameCount - frame,
                    playerPool = playerIds.Length, hostilePool = hostileIds.Length, scouts = scoutIds.Length,
                    instantiateCalls = createCalls, destroyCalls, pooledIdentityStable = true };
            }
        }
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
                Set(dev, "devModeEnabled", true); Set(dev, "startPoint", "LEVEL_START"); Set(dev, "standalone", false);
                Runner.StartRun("LEVEL_START", false);
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
        readonly PropertyInfo selectedSize;
        readonly object oldSize;
        readonly GameplayViewController view;
        readonly GameplayViewSettings oldView;
        readonly float oldScale = Time.timeScale;
        readonly bool oldAudio = AudioListener.pause;
        readonly string[] definitions;
        Vector2 pointer = new Vector2(400, 220);
        public Rig(int width, float size)
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
            type.GetMethod("SetCustomResolution", Fields).Invoke(gameView, new object[] { new Vector2(width, 900), "StarShift Closure QA" });
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            oldMouse = Mouse.current;
            foreach (var d in InputSystem.devices)
                if (d.native && d.enabled && (d is Mouse || d is Touchscreen)) { disabled.Add(d); InputSystem.DisableDevice(d); }
            mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent(); touch = InputSystem.AddDevice<Touchscreen>();
            definitions = Configs().Select(EditorJsonUtility.ToJson).ToArray();
            Runner.StartRun("LEVEL_START", false);
        }
        public void Protect() { Player.GetComponent<PlayerDamageLifecycle>().enabled = false; Gate.SetState(PlayerGameplayState.PostEntryInvincibility); UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0); }
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
            await Until(() => !target.IsAlive, 2500, "Real held Pulse did not kill Scout.");
            await MouseEvent(pointer, false);
        }
        static EncounterDefinition[] Configs() => new[] { "A", "B" }.Select(id => AssetDatabase.LoadAssetAtPath<EncounterDefinition>("Assets/StarShift/Data/Encounters/L1Encounter" + id + ".asset")).ToArray();
        public void Dispose()
        {
            Session.enabled = false; Runner.StopRun(); Time.timeScale = 1;
            InputSystem.RemoveDevice(mouse); InputSystem.RemoveDevice(touch);
            foreach (var d in disabled) InputSystem.EnableDevice(d);
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor; InputSystem.settings.backgroundBehavior = oldBackground;
            var life = Player.GetComponent<PlayerDamageLifecycle>(); life.enabled = false; life.enabled = true; Input.CancelGesture();
            view.RequestView(oldView, 0); selectedSize.SetValue(gameView, oldSize);
            Session.enabled = true; Time.timeScale = oldScale; AudioListener.pause = oldAudio;
            Require(definitions.SequenceEqual(Configs().Select(EditorJsonUtility.ToJson)), "QA mutated encounter config assets.");
        }
    }
}
