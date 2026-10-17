using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public static class VerifyPlayerDamageLifecycle
{
    const string ScoutPath = "Assets/StarShift/Prefabs/Enemies/Scout.prefab";
    const string FirePath = "Assets/StarShift/Data/Enemies/ScoutSingleFire.asset";

    public static void IsolateLegacy()
    {
        var player = GameObject.Find("Player");
        player.GetComponent<PlayerDamageLifecycle>().enabled = false;
        player.GetComponent<PlayerGameplayGate>().SetState(PlayerGameplayState.Active);
        UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>().ResumeAfter(0);
        SceneScout().SetActive(false);
    }

    public static async Task<object> HostilePool()
    {
        Require(Application.isPlaying, "Play Mode required.");
        var fireGate = UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>();
        var scout = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ScoutPath));
        EnemyProjectile Shot() => UnityEngine.Object.FindObjectsByType<EnemyProjectile>().FirstOrDefault(p => p.Owner == scout);
        var oldScale = Time.timeScale;
        try
        {
            fireGate.ResumeAfter(0);
            await Until(() => Shot() != null, 3300, "Production Scout did not fire into its pool.");
            var first = Shot();
            fireGate.Block();
            fireGate.ClearProjectiles();
            Require(!first.gameObject.activeSelf && first.Owner == null && Read<GameObject>(first,"target") == null &&
                Read<Vector2>(first,"direction") == Vector2.zero && Read<float>(first,"remainingLifetime") == 0 &&
                Read<float>(first,"speed") == 0 && !Read<bool>(first,"flying") &&
                Read<Action<EnemyProjectile>>(first,"onReleased") == null &&
                first.GetComponent<Rigidbody2D>().linearVelocity == Vector2.zero &&
                first.GetComponent<Rigidbody2D>().angularVelocity == 0 && !first.GetComponent<Rigidbody2D>().simulated,
                "Production clear did not fully reset pooled projectile.");
            fireGate.ResumeAfter(.1f);
            await Until(() => Shot() != null, 1800, "Scout did not resume after pool clear.");
            Require(Shot() == first, "Production pool did not reuse released projectile.");
            var position = first.GetComponent<Rigidbody2D>().position;
            var lifetime = Read<float>(first,"remainingLifetime");
            Time.timeScale = 0;
            await Task.Delay(120);
            Require(position == first.GetComponent<Rigidbody2D>().position && lifetime == Read<float>(first,"remainingLifetime"),
                "Reused hostile projectile advanced while paused.");
            Time.timeScale = 1;
            UnityEngine.Object.DestroyImmediate(first.transform.parent.gameObject);
            UnityEngine.Object.DestroyImmediate(scout);
            scout = null;
            await Task.Delay(50);
            return new { passed = true, productionPoolClear = true, sameInstanceReused = true, fullReset = true, pause = true, rootFirstTeardown = true };
        }
        finally
        {
            Time.timeScale = oldScale;
            if (scout != null) UnityEngine.Object.DestroyImmediate(scout);
            fireGate.ResumeAfter(0);
        }
    }

    public static async Task<object> Presentation()
    {
        var player = GameObject.Find("Player");
        var life = player.GetComponent<PlayerDamageLifecycle>();
        var gate = player.GetComponent<PlayerGameplayGate>();
        var hull = player.GetComponentInChildren<SpriteRenderer>(true);
        var position = player.transform.position;
        try
        {
            Reset(life);
            await Task.Delay(35);
            var normal = hull.color;
            life.TryReceiveHit(null);
            await Task.Delay(35);
            Require(hull.enabled && hull.color == Color.white, "Death placeholder cue missing.");
            System.IO.Directory.CreateDirectory("Temp/StarShiftLifecycleCheckpoint");
            ScreenCapture.CaptureScreenshot("Temp/StarShiftLifecycleCheckpoint/death.png");
            await Until(() => gate.State == PlayerGameplayState.PostEntryInvincibility, 1700, "Visual test did not respawn.");
            await Task.Delay(35);
            Require(hull.color != normal && hull.color != Color.white, "Protection tint missing.");
            await Until(() => !hull.enabled, 400, "Protection did not blink off.");
            await Until(() => hull.enabled, 400, "Protection did not blink on.");
            ScreenCapture.CaptureScreenshot("Temp/StarShiftLifecycleCheckpoint/protected.png");
            await Task.Delay(30);
            return new { passed = true, deathFlash = true, protectionTint = true, blink = true };
        }
        finally { Reset(life); Position(player, position); }
    }

    public static async Task<object> Run(int width)
    {
        Require(Application.isPlaying, "Play Mode required.");
        var player = GameObject.Find("Player");
        var gate = player.GetComponent<PlayerGameplayGate>();
        var lives = player.GetComponent<PlayerLives>();
        var lifecycle = player.GetComponent<PlayerDamageLifecycle>();
        var receiver = player.GetComponent<PlayerContactReceiver>();
        var fireGate = UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>();
        var camera = Camera.main;
        var view = camera.GetComponent<GameplayViewController>();
        var oldView = view.TargetView;
        var oldPosition = player.transform.position;
        var oldScale = Time.timeScale;
        var sceneScout = SceneScout();
        var wasActive = sceneScout.activeSelf;
        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var gameView = EditorWindow.GetWindow(gameViewType);
        var selectedSize = gameViewType.GetProperty("selectedSizeIndex");
        var oldSize = selectedSize.GetValue(gameView);
        var gameViewZoom = gameView.GetType().GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameView);
        var oldGameViewZoom = (Vector2)gameViewZoom.GetType().GetProperty("scale").GetValue(gameViewZoom);
        var oldGameViewPan = (Vector2)gameViewZoom.GetType().GetProperty("translation").GetValue(gameViewZoom);
        var profile = AssetDatabase.LoadAssetAtPath<SingleFireDefinition>(FirePath);
        var configBefore = EditorJsonUtility.ToJson(profile);
        var projectiles = new List<EnemyProjectile>();
        var friendly = new List<PlayerProjectile>();
        GameObject scout = null;
        var muzzle = new GameObject("LifecycleVerificationMuzzle");
        int deaths = 0, gameOvers = 0, releases = 0;
        Action stateChanged = () => { if (gate.State == PlayerGameplayState.Dying) deaths++; };
        Action gameOver = () => gameOvers++;
        gate.StateChanged += stateChanged;
        lifecycle.GameOver += gameOver;
        var results = new List<object>();
        var startFrame = Time.frameCount;
        try
        {
            sceneScout.SetActive(false);
            Time.timeScale = 1;
            gameViewType.GetMethod("SetCustomResolution", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(gameView, new object[] { new Vector2(width, 900), "StarShift Lifecycle " + width + "x900" });
            camera.ResetAspect();
            await Task.Delay(250);
            Require(camera.pixelWidth == width && camera.pixelHeight == 900, "Wrong Game View dimensions.");
            foreach (var size in new[] { 6f, 5.25f, 6.8f })
            {
                var region = new Rect(0, 0, 1, size == 6.8f ? .7f : 1);
                view.RequestView(new GameplayViewSettings(size, region), 0);
                Reset(lifecycle);
                deaths = gameOvers = 0;
                Position(player, camera.ViewportToWorldPoint(new Vector3(.5f, region.height * .18f, 10)));
                scout = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ScoutPath));
                var attack = scout.GetComponent<SingleFireAttack>();
                await Until(() => attack.IsTelegraphing, 3200, "Scout did not telegraph before death.");
                var scoutPosition = scout.transform.position;
                var pulse = AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>("Assets/StarShift/Data/Weapons/PulseLaserLv1.asset");
                muzzle.transform.position = new Vector3(40, 40, 0);
                muzzle.transform.rotation = Quaternion.identity;
                var playerShot = UnityEngine.Object.Instantiate(pulse.ProjectilePrefab);
                friendly.Add(playerShot);
                playerShot.ResetForPool();
                playerShot.Launch(pulse, muzzle.transform, player, p => p.ResetForPool());
                EnemyProjectile Shot(Vector3 origin)
                {
                    var p = UnityEngine.Object.Instantiate(profile.ProjectilePrefab);
                    projectiles.Add(p);
                    p.ResetForPool();
                    muzzle.transform.position = origin;
                    p.Launch(profile, muzzle.transform, scout, player, shot => { releases++; shot.ResetForPool(); });
                    return p;
                }
                Shot(player.transform.position + Vector3.up * .8f);
                Shot(player.transform.position + Vector3.up * .8f);
                Shot(player.transform.position + Vector3.up * 1.2f);
                await Until(() => gate.State == PlayerGameplayState.Dying, 800, "Hostile hit did not start death.");
                Require(lives.CurrentLives == 2 && deaths == 1 && !gate.CanInput && !gate.CanFire && !gate.CanTakeDamage,
                    "Duplicate contacts consumed lives or permissions remained open.");
                Require(!fireGate.CanFire && !attack.IsTelegraphing, "Death did not cancel pending attack.");
                Require(UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length == 0, "Hostile cleanup leaked active shots.");
                Require(playerShot.gameObject.activeSelf, "Death incorrectly cleared friendly projectile.");
                Require(player.GetComponent<PulseLaserWeapon>().CurrentLevel == 1, "Lv1 downgrade must stay Lv1.");
                foreach (var shot in projectiles)
                    Require(shot.Owner == null && !shot.GetComponent<Rigidbody2D>().simulated &&
                        !shot.GetComponent<BoxCollider2D>().enabled && shot.GetComponent<Rigidbody2D>().linearVelocity == Vector2.zero,
                        "Hostile pool reset incomplete.");
                var remaining = Read<float>(lifecycle, "remaining");
                Time.timeScale = 0;
                await Task.Delay(120);
                Require(gate.State == PlayerGameplayState.Dying && Read<float>(lifecycle, "remaining") == remaining,
                    "Pause advanced death clock.");
                Time.timeScale = 1;
                await Until(() => gate.State == PlayerGameplayState.Respawning, 700, "No respawn entrance.");
                await Task.Delay(30);
                Require(camera.WorldToViewportPoint(player.transform.position).y < 0 && !gate.CanInput && !gate.CanFire && !gate.CanTakeDamage,
                    "Entrance did not start outside bottom or permissions opened early.");
                Shot(player.transform.position + Vector3.up * .5f);
                receiver.ReceiveEnemyHit(scout);
                await Until(() => gate.State == PlayerGameplayState.PostEntryInvincibility, 1500, "Entrance never reached safe position.");
                Require(lives.CurrentLives == 2 && gate.CanInput && gate.CanFire && !gate.CanTakeDamage, "Entrance protection failed.");
                AssertInside(player, camera, region);
                Require(!fireGate.CanFire, "Enemy grace did not begin after entrance.");
                Require(scout.transform.position == scoutPosition && scout.activeSelf, "Death removed or moved hold Scout.");
                var invincibleAt = Time.fixedTime;
                Shot(player.transform.position + Vector3.up * .2f);
                await Task.Delay(130);
                Require(lives.CurrentLives == 2 && deaths == 1, "Invincibility accepted damage.");
                Require(!UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Any(p => p.transform.parent != null &&
                    p.transform.parent.name == "EnemyProjectiles"), "Scout fired during grace.");
                await Until(() => fireGate.CanFire, 1300, "Enemy grace did not expire.");
                Require(gate.State == PlayerGameplayState.PostEntryInvincibility && !gate.CanTakeDamage,
                    "Enemy grace was coupled to Player invincibility.");
                await Until(() => gate.State == PlayerGameplayState.Active, 2200, "Player invincibility did not expire.");
                Require(Time.fixedTime - invincibleAt >= 2.46f, "Invincibility started before entry finished.");
                Require(UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Any(p => p.transform.parent != null &&
                    p.transform.parent.name == "EnemyProjectiles"), "Scout did not resume a fresh attack after grace.");
                // Body hit dùng production Scout và compound hitbox, không gọi receiver trực tiếp.
                scout.GetComponent<EntryHoldFlight>().enabled = false;
                var compound = new GameObject("LifecycleCompoundBody");
                compound.transform.SetParent(scout.transform, false);
                compound.layer = 9;
                compound.AddComponent<BoxCollider2D>().isTrigger = true;
                Position(scout, player.transform.position);
                await Until(() => lives.CurrentLives == 1, 600, "Enemy body contact did not damage Player.");
                Require(deaths == 2 && gate.State == PlayerGameplayState.Dying, "Body callbacks duplicated death.");
                await Until(() => gate.State == PlayerGameplayState.PostEntryInvincibility, 1700, "Second respawn failed.");
                AssertInside(player, camera, region);
                Require(Vector2.Distance(player.transform.position, scout.transform.position) > .9f,
                    "Respawn ended overlapping enemy body.");
                Position(scout, player.transform.position);
                await Task.Delay(120);
                Require(lives.CurrentLives == 1, "Protected body overlap consumed a life.");
                await Until(() => lives.CurrentLives == 0, 2900, "Persistent body overlap did not resolve after protection expiry.");
                await Until(() => gate.State == PlayerGameplayState.GameOver, 700, "Zero lives did not reach Game Over.");
                Require(deaths == 3 && gameOvers == 1 && !gate.CanInput && !gate.CanFire && !gate.CanTakeDamage && !fireGate.CanFire,
                    "Game Over terminal permissions/signal failed.");
                Require(UnityEngine.Object.FindObjectsByType<EnemyProjectile>().Length == 0, "Game Over retained hostile projectiles.");
                receiver.ReceiveEnemyHit(scout);
                await Task.Delay(150);
                Require(lives.CurrentLives == 0 && gameOvers == 1 && gate.State == PlayerGameplayState.GameOver,
                    "Game Over accepted damage or respawned again.");
                foreach (var shot in projectiles) if (shot != null) UnityEngine.Object.DestroyImmediate(shot.gameObject);
                projectiles.Clear();
                UnityEngine.Object.DestroyImmediate(scout);
                scout = null;
                results.Add(new { size, region = region.ToString(), scenarios = "A-F PASS", lives = "3->2->1->0",
                    deaths, gameOvers, level1Downgrade = true, safeBodyAvoidance = true, graceIndependent = true });
            }
            Require(configBefore == EditorJsonUtility.ToJson(profile), "Test mutated original fire config.");
            return new { passed = true, width, height = 900, frames = Time.frameCount - startFrame, releases, results };
        }
        finally
        {
            gate.StateChanged -= stateChanged;
            lifecycle.GameOver -= gameOver;
            Time.timeScale = oldScale;
            if (scout != null) UnityEngine.Object.DestroyImmediate(scout);
            foreach (var p in projectiles) if (p != null) UnityEngine.Object.DestroyImmediate(p.gameObject);
            foreach (var p in friendly) if (p != null) UnityEngine.Object.DestroyImmediate(p.gameObject);
            UnityEngine.Object.DestroyImmediate(muzzle);
            Reset(lifecycle);
            view.RequestView(oldView, 0);
            Position(player, oldPosition);
            sceneScout.SetActive(wasActive);
            gameView.GetType().GetMethod("SizeSelectionCallback").Invoke(gameView, new object[] { oldSize, null });
            gameViewZoom.GetType().GetMethod("SetTransform").Invoke(gameViewZoom, new object[] { oldGameViewPan, oldGameViewZoom });
            camera.ResetAspect();
        }
    }

    public static async Task<object> PointerAndLifecycle()
    {
        var player = GameObject.Find("Player");
        var gate = player.GetComponent<PlayerGameplayGate>();
        var life = player.GetComponent<PlayerDamageLifecycle>();
        var lives = player.GetComponent<PlayerLives>();
        var fireGate = UnityEngine.Object.FindAnyObjectByType<EnemyFireGate>();
        var oldPosition = player.transform.position;
        var sceneScout = SceneScout();
        var wasActive = sceneScout.activeSelf;
        var settings = InputSystem.settings;
        var oldPolicy = settings.editorInputBehaviorInPlayMode;
        var oldBackground = settings.backgroundBehavior;
        var oldMouse = Mouse.current;
        var devices = new List<InputDevice>();
        Mouse mouse = null;
        GameObject blocker = null;
        GameObject movingScout = null;
        var presentation = player.GetComponent<PlayerDamagePresentation>();
        var presentationEnabled = presentation.enabled;
        try
        {
            sceneScout.SetActive(false);
            Reset(life);
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var d in InputSystem.devices)
                if (d.native && d.enabled && (d is Mouse || d is Touchscreen)) { InputSystem.DisableDevice(d); devices.Add(d); }
            mouse = InputSystem.AddDevice<Mouse>("LifecyclePointerMouse");
            await MouseEvent(mouse, new Vector2(200,200), false);
            await MouseEvent(mouse, new Vector2(200,200), true);
            Require(player.GetComponent<InputSystemPointerInput>().FireHeld, "No held pointer.");
            movingScout = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ScoutPath));
            life.TryReceiveHit(null);
            await MouseEvent(mouse, new Vector2(800,600), true);
            await Until(() => gate.State == PlayerGameplayState.PostEntryInvincibility, 1700, "Pointer scenario failed respawn.");
            Require(movingScout.GetComponent<EntryHoldFlight>().IsHolding && !fireGate.CanFire,
                "Enemy entry froze while death/respawn fire permission was blocked.");
            var safePosition = player.GetComponent<Rigidbody2D>().position;
            await MouseEvent(mouse, new Vector2(900,650), true);
            Require(player.GetComponent<InputSystemPointerInput>().FireHeld &&
                (player.GetComponent<Rigidbody2D>().position - safePosition).sqrMagnitude < .00001f,
                "Held pointer carried stale movement through respawn.");
            await Task.Delay(230);
            Require(UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length > 0, "Held fire did not resume after entrance.");
            await MouseEvent(mouse, new Vector2(900,650), false);
            await MouseEvent(mouse, new Vector2(300,250), true);
            var before = Camera.main.WorldToScreenPoint(player.transform.position);
            await MouseEvent(mouse, new Vector2(330,270), true);
            var delta = (Vector2)(Camera.main.WorldToScreenPoint(player.transform.position) - before);
            Require((delta - new Vector2(30,20)).sqrMagnitude < 1, "Fresh press after respawn did not move correctly.");
            await MouseEvent(mouse, new Vector2(330,270), false);
            Reset(life);
            presentation.enabled = false;
            Action broken = () => { if (gate.State == PlayerGameplayState.Dying) throw new InvalidOperationException("Expected lifecycle listener failure"); };
            gate.StateChanged += broken;
            bool caught = false;
            try { life.TryReceiveHit(null); }
            catch (InvalidOperationException error) when (error.Message == "Expected lifecycle listener failure") { caught = true; }
            finally { gate.StateChanged -= broken; }
            Require(caught && lives.CurrentLives == 2 && gate.State == PlayerGameplayState.Dying && !fireGate.CanFire,
                "Feedback exception broke mandatory damage work.");
            await Until(() => gate.State == PlayerGameplayState.Respawning, 700, "Callback exception stalled death.");
            Time.timeScale = 0;
            var position = player.transform.position;
            await Task.Delay(180);
            Require(player.transform.position == position && gate.State == PlayerGameplayState.Respawning && !gate.CanInput,
                "Pause advanced entrance.");
            Time.timeScale = 1;
            player.SetActive(false);
            await Task.Delay(80);
            player.SetActive(true);
            Require(lives.CurrentLives == 3 && gate.State == PlayerGameplayState.Active && !player.GetComponent<PlayerRespawnEntrance>().IsEntering,
                "Root re-enable retained pending lifecycle.");
            await Task.Delay(400);
            Require(gate.State == PlayerGameplayState.Active && lives.CurrentLives == 3, "Stale sequence resumed after re-enable.");
            UnityEngine.Object.DestroyImmediate(movingScout);
            movingScout = null;
            gate.SetState(PlayerGameplayState.Entry);
            await Until(() => UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length == 0,
                2000, "Friendly shots did not drain before blocked-candidate fixture.");
            gate.SetState(PlayerGameplayState.Active);
            blocker = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ScoutPath));
            blocker.GetComponent<EntryHoldFlight>().enabled = false;
            Position(blocker, Vector3.zero);
            blocker.GetComponentInChildren<BoxCollider2D>().size = new Vector2(100,100);
            Physics2D.SyncTransforms();
            life.TryReceiveHit(null);
            await Until(() => gate.State == PlayerGameplayState.Respawning, 700, "Blocked-target test did not enter respawn.");
            await Task.Delay(1100);
            Require(blocker.GetComponent<EnemyHealth>().IsAlive, "Blocked-candidate fixture was destroyed by old friendly shots.");
            Require(gate.State == PlayerGameplayState.Respawning && !gate.CanInput && !gate.CanFire &&
                !player.GetComponent<PlayerRespawnEntrance>().SafeTargetFound, "Unsafe candidate opened gameplay permissions.");
            UnityEngine.Object.DestroyImmediate(blocker);
            blocker = null;
            await Until(() => gate.State == PlayerGameplayState.PostEntryInvincibility, 600, "Cleared candidate did not finish entrance.");
            return new { passed = true, heldFireResumes = true, staleDragBlocked = true, freshPressDelta = delta.ToString(),
                callbackFailureSafe = true, pauseEntrance = true, rootReuseReset = true,
                enemyEntryDuringGrace = true, allCandidatesBlockedSafe = true, presentationIndependent = true };
        }
        finally
        {
            Time.timeScale = 1;
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (var d in devices) if (d.added) InputSystem.EnableDevice(d);
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            settings.editorInputBehaviorInPlayMode = oldPolicy;
            settings.backgroundBehavior = oldBackground;
            if (movingScout != null) UnityEngine.Object.DestroyImmediate(movingScout);
            if (blocker != null) UnityEngine.Object.DestroyImmediate(blocker);
            presentation.enabled = presentationEnabled;
            Reset(life);
            Position(player, oldPosition);
            sceneScout.SetActive(wasActive);
        }
    }

    static void Reset(PlayerDamageLifecycle life) { life.enabled = false; life.enabled = true; }
    static async Task MouseEvent(Mouse mouse, Vector2 position, bool down)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, down));
        await Task.Delay(65);
    }
    static void AssertInside(GameObject player, Camera camera, Rect region)
    {
        var half = new Vector2(.4f,.5f);
        for (int x=-1;x<=1;x+=2)
        for (int y=-1;y<=1;y+=2)
        {
            var p = camera.WorldToViewportPoint(player.transform.TransformPoint(new Vector3(x*half.x,y*half.y,0)));
            Require(p.x >= region.xMin-.0001f && p.x <= region.xMax+.0001f && p.y >= region.yMin-.0001f && p.y <= region.yMax+.0001f,
                "Respawn footprint outside playable region: " + p);
        }
    }
    static GameObject SceneScout() => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<EnemyHealth>(true)).Single(h => h.name == "Scout_EntryHold").gameObject;
    static void Position(GameObject target, Vector3 position)
    {
        target.GetComponent<Rigidbody2D>().position = position;
        target.transform.position = position;
        Physics2D.SyncTransforms();
    }
    static T Read<T>(object o, string field) => (T)o.GetType().GetField(field, BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static async Task Until(Func<bool> predicate, int ms, string message)
    {
        var deadline = Time.realtimeSinceStartup + ms/1000f;
        while (!predicate() && Time.realtimeSinceStartup < deadline) await Task.Delay(10);
        Require(predicate(), message);
    }
    static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
