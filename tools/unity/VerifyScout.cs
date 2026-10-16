using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using StarShift.Combat;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;

[DefaultExecutionOrder(1000)]
public sealed class ScoutFlightObserver : MonoBehaviour
{
    public Camera Camera;
    public EntryHoldFlight Flight;
    public float MaximumRenderError;
    public int Samples;

    private void LateUpdate()
    {
        if (Flight == null || !Flight.IsHolding) return;
        Samples++;
        MaximumRenderError = Mathf.Max(MaximumRenderError,
            Mathf.Abs(Camera.WorldToViewportPoint(Flight.transform.position).y - .78f));
    }
}

public static class VerifyScout
{
    const string PrefabPath = "Assets/StarShift/Prefabs/Enemies/Scout.prefab";
    const string DefinitionPath = "Assets/StarShift/Data/Enemies/Scout.asset";

    public static void SetSceneScoutActive(bool active)
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play Mode required.");
        SceneScout().gameObject.SetActive(active);
    }

    public static async Task<object> Run(int width)
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play Mode required.");
        var camera = Camera.main;
        var view = camera.GetComponent<GameplayViewController>();
        var oldView = view.TargetView;
        var player = GameObject.Find("Player");
        var body = player.GetComponent<Rigidbody2D>();
        var gate = player.GetComponent<PlayerGameplayGate>();
        var input = player.GetComponent<InputSystemPointerInput>();
        var oldPosition = body.position;
        var oldState = gate.State;
        var sceneScout = SceneScout();
        var wasActive = sceneScout.gameObject.activeSelf;
        var settings = InputSystem.settings;
        var oldEditorPolicy = settings.editorInputBehaviorInPlayMode;
        var oldBackgroundPolicy = settings.backgroundBehavior;
        var oldRunInBackground = Application.runInBackground;
        var oldMouse = Mouse.current;
        var disabled = new List<InputDevice>();
        Mouse mouse = null;
        GameObject target = null;
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(DefinitionPath);
        var definitionBefore = EditorJsonUtility.ToJson(definition);
        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var gameView = EditorWindow.GetWindow(gameViewType);
        var selectedSize = gameViewType.GetProperty("selectedSizeIndex");
        var oldSize = selectedSize.GetValue(gameView);
        var setResolution = gameViewType.GetMethod("SetCustomResolution", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(setResolution != null, "Game View resolution API unavailable.");
        var results = new List<string>();
        var frameStart = Time.frameCount;
        try
        {
            sceneScout.gameObject.SetActive(false);
            Application.runInBackground = true;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var device in InputSystem.devices)
            {
                if (!device.native || !device.enabled || (!(device is Mouse) && !(device is Touchscreen))) continue;
                InputSystem.DisableDevice(device); disabled.Add(device);
            }
            mouse = InputSystem.AddDevice<Mouse>("ScoutVerificationMouse");
            gate.SetState(PlayerGameplayState.Entry);
            await Task.Delay(1650);
            camera.ResetAspect();
            setResolution.Invoke(gameView, new object[] { new Vector2(width, 900), "StarShift Scout " + width + "x900" });
            gameView.Repaint();
            await Task.Delay(350);
            Require(camera.pixelWidth == width && camera.pixelHeight == 900, "Incorrect Game View dimensions.");
            Directory.CreateDirectory("Temp/StarShiftScoutCheckpoint");
            foreach (var size in new[] { 5f, 6.8f })
            {
                view.RequestView(new GameplayViewSettings(size, new Rect(0, 0, 1, 1)), 0);
                target = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
                var health = target.GetComponent<EnemyHealth>();
                var flight = target.GetComponent<EntryHoldFlight>();
                var renderer = target.transform.Find("Visual/HullPlaceholder").GetComponent<SpriteRenderer>();
                var normal = renderer.color;
                int damageEvents = 0, deaths = 0;
                var hitHp = new List<int>();
                var hitColors = new List<Color>();
                health.Damaged += () => { damageEvents++; hitHp.Add(health.CurrentHp); hitColors.Add(renderer.color); };
                health.Died += () => deaths++;
                Require(health.CurrentHp == 2 && health.MaxHp == 2 && health.IsAlive, "Scout HP seed/runtime initialization failed.");
                await Task.Delay(40);
                var entry = camera.WorldToViewportPoint(target.transform.position);
                Require(entry.y > 1 && !flight.IsHolding, "Scout did not start outside at entry: " + entry + " holding=" + flight.IsHolding);
                await Until(() => flight.IsHolding, 2200, "Scout entry did not finish.");
                await Task.Delay(40);
                var held = camera.WorldToViewportPoint(target.transform.position);
                Require(Mathf.Abs(held.x - .5f) < .001f && Mathf.Abs(held.y - .78f) < .001f, "Wrong normalized hold anchor: " + held);
                AssertVisible(camera, target);
                var heldPosition = target.transform.position;
                await Task.Delay(120);
                Require((target.transform.position - heldPosition).sqrMagnitude < 1e-8f, "Hold drift/jitter.");
                body.position = camera.ViewportToWorldPoint(new Vector3(.5f, .18f, 10));
                gate.SetState(PlayerGameplayState.Active);
                await MouseEvent(mouse, false);
                await MouseEvent(mouse, true);
                await Until(() => damageEvents > 0, 1600, "Real Pulse weapon did not hit production Scout.");
                Require(damageEvents == 1 && health.CurrentHp == 1 && health.IsAlive && hitHp[0] == 1,
                    "First projectile must cause exactly one damage and leave Scout alive.");
                Require(hitColors[0] == Color.white, "First hit flash missing.");
                await Task.Delay(105);
                var damagedTint = new SerializedObject(target.GetComponent<EnemyDamagePresentation>()).FindProperty("damagedTint").colorValue;
                Require(health.IsAlive && renderer.color == damagedTint && renderer.color != normal, "Persistent Damaged presentation missing.");
                ScreenCapture.CaptureScreenshot("Temp/StarShiftScoutCheckpoint/" + width + "-size-" + size + "-damaged.png");
                await Until(() => !target.activeSelf, 700, "Second hit did not release Scout.");
                Require(health.CurrentHp == 0 && !health.IsAlive && damageEvents == 2 && deaths == 1 && hitHp[1] == 0,
                    "Second hit/death count failed.");
                for (int i = 0; i < 5; i++) health.ReceivePlayerHit(1, player);
                Require(damageEvents == 2 && deaths == 1, "Dead Scout accepted damage or resolved death twice.");
                await MouseEvent(mouse, false);
                Require(!input.FireHeld, "Release left fire intent held.");
                gate.SetState(PlayerGameplayState.Entry);
                await Task.Delay(1650);
                UnityEngine.Object.DestroyImmediate(target);
                target = null;
                results.Add("size=" + size + ": production prefab entry/hold, real held Pulse 2->1 flash/Damaged ->0 disabled, one death PASS");
            }

            target = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var movingFlight = target.GetComponent<EntryHoldFlight>();
            var observer = target.AddComponent<ScoutFlightObserver>();
            observer.Camera = camera; observer.Flight = movingFlight;
            view.RequestView(new GameplayViewSettings(8.2f, new Rect(0, 0, 1, .7f)), .5f);
            await Until(() => movingFlight.IsHolding && !view.IsTransitioning, 2300, "Entry during zoom failed.");
            AssertVisible(camera, target);
            view.ReturnToBaseline();
            await Until(() => !view.IsTransitioning, 1200, "Return transition failed.");
            await Task.Delay(40);
            AssertVisible(camera, target);
            Require(Mathf.Abs(camera.WorldToViewportPoint(target.transform.position).y - .78f) < .001f,
                "Scout hold cached old world rectangle.");
            Require(observer.Samples > 2 && observer.MaximumRenderError < .001f,
                "Hold render lag during zoom: " + observer.MaximumRenderError);
            Require(EditorJsonUtility.ToJson(definition) == definitionBefore, "Runtime mutated Scout definition.");
            results.Add("entry during dynamic zoom + lower-player-region, hold follows current full camera, return to 5 PASS");
            return new { passed = true, gameView = width + "x900", frameStart, frameEnd = Time.frameCount, results };
        }
        finally
        {
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
            gate.SetState(oldState);
            view.RequestView(oldView, 0);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled) if (device.added) InputSystem.EnableDevice(device);
            settings.editorInputBehaviorInPlayMode = oldEditorPolicy;
            settings.backgroundBehavior = oldBackgroundPolicy;
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            input.ResetDrag(); body.position = oldPosition;
            selectedSize.SetValue(gameView, oldSize); gameView.Repaint(); camera.ResetAspect();
            sceneScout.gameObject.SetActive(wasActive);
            Application.runInBackground = oldRunInBackground;
        }
    }

    public static async Task<object> EdgeCases()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Play Mode required.");
        var sceneScout = SceneScout();
        var wasActive = sceneScout.gameObject.activeSelf;
        var gate = GameObject.Find("Player").GetComponent<PlayerGameplayGate>();
        var oldState = gate.State;
        var clone = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        var health = clone.GetComponent<EnemyHealth>();
        var flight = clone.GetComponent<EntryHoldFlight>();
        var presentation = clone.GetComponent<EnemyDamagePresentation>();
        var renderer = clone.transform.Find("Visual/HullPlaceholder").GetComponent<SpriteRenderer>();
        var normalColor = renderer.color;
        var body = clone.GetComponent<Rigidbody2D>();
        var muzzle = new GameObject("ScoutVerificationMuzzle");
        var definition = AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>("Assets/StarShift/Data/Weapons/PulseLaserLv1.asset");
        int damageEvents = 0, deaths = 0, releases = 0;
        health.Damaged += () => damageEvents++;
        health.Died += () => deaths++;
        var projectiles = new List<PlayerProjectile>();
        var pool = new ObjectPool<PlayerProjectile>(() =>
        {
            var p = UnityEngine.Object.Instantiate(definition.ProjectilePrefab);
            p.ResetForPool(); projectiles.Add(p); return p;
        }, null, p => p.ResetForPool(), p => { if (p != null) UnityEngine.Object.DestroyImmediate(p.gameObject); });
        try
        {
            sceneScout.gameObject.SetActive(false);
            gate.SetState(PlayerGameplayState.Entry);
            flight.enabled = false;
            body.position = new Vector2(20, 22);
            clone.transform.position = new Vector3(20, 22, 0);
            muzzle.transform.position = new Vector3(20, 20, 0);
            var duplicate = new GameObject("CompoundHitbox");
            duplicate.transform.SetParent(clone.transform, false);
            var extraCollider = duplicate.AddComponent<BoxCollider2D>();
            extraCollider.isTrigger = true; extraCollider.size = new Vector2(1.28f, .8f);
            Physics2D.SyncTransforms();
            var first = pool.Get();
            first.Launch(definition, muzzle.transform, gate.gameObject, p => { releases++; pool.Release(p); });
            await Until(() => releases == 1, 900, "Compound target did not receive hit.");
            Require(health.CurrentHp == 1 && damageEvents == 1 && deaths == 0, "Compound hit duplicated damage.");
            var reused = pool.Get();
            Require(reused == first, "Expected same pooled projectile instance.");
            reused.Launch(definition, muzzle.transform, gate.gameObject, p => { releases++; pool.Release(p); });
            await Until(() => releases == 2, 900, "Reused projectile failed to hit.");
            Require(health.CurrentHp == 0 && damageEvents == 2 && deaths == 1 && !clone.activeSelf, "Reused projectile retained stale state.");
            clone.SetActive(true);
            Require(health.CurrentHp == 2 && health.IsAlive && renderer.color == normalColor, "Scout reuse did not reset health/presentation.");
            body.position = new Vector2(20, 22);
            clone.transform.position = new Vector3(20, 22, 0);
            Physics2D.SyncTransforms();
            for (int i = 0; i < 3; i++)
            {
                var shot = pool.Get();
                shot.Launch(definition, muzzle.transform, gate.gameObject, p => { releases++; pool.Release(p); });
            }
            await Until(() => deaths == 2, 900, "Rapid volley did not kill Scout.");
            Require(health.CurrentHp == 0 && damageEvents == 4 && deaths == 2, "Rapid compound hits duplicated damage/death.");
            await Until(() => releases == 5, 2000, "Unused volley projectile did not expire/release.");
            clone.SetActive(true);
            presentation.enabled = false;
            health.ReceivePlayerHit(1, gate.gameObject);
            Require(health.CurrentHp == 1 && health.IsAlive, "Health depends on presentation.");
            presentation.enabled = true;
            Require(renderer.color != normalColor, "Presentation did not read existing damaged state.");
            health.ReceivePlayerHit(0, gate.gameObject);
            health.ReceivePlayerHit(-1, gate.gameObject);
            Require(health.CurrentHp == 1 && damageEvents == 5, "Non-positive damage changed state.");
            clone.SetActive(false); clone.SetActive(true); flight.enabled = true;
            Require(!flight.IsHolding && health.CurrentHp == 2 && renderer.color == normalColor, "Flight/presentation reset failed.");
            health.ReceivePlayerHit(2, gate.gameObject);
            var deadPosition = body.position;
            await Task.Delay(180);
            Require(!clone.activeSelf && body.position == deadPosition && deaths == 3, "Death during entry did not stop movement.");
            clone.SetActive(true);
            Require(!flight.IsHolding && health.CurrentHp == 2, "Re-enable retained dead/hold state.");
            await Until(() => flight.IsHolding, 2200, "Re-enabled Scout failed entry/hold.");
            AssertVisible(Camera.main, clone);
            bool callbackErrorObserved = false;
            Action brokenFeedback = () => throw new InvalidOperationException("Expected feedback failure");
            health.Damaged += brokenFeedback;
            try { health.ReceivePlayerHit(2, gate.gameObject); }
            catch (InvalidOperationException error) when (error.Message == "Expected feedback failure") { callbackErrorObserved = true; }
            finally { health.Damaged -= brokenFeedback; }
            Require(callbackErrorObserved && !clone.activeSelf && health.CurrentHp == 0 && deaths == 4,
                "Feedback callback blocked lethal release/death notification.");
            return new { passed = true, compound = "one damage per projectile", reusedProjectile = true,
                rapidVolley = "3 projectiles, 2 damage events, 1 death", presentationIndependent = true,
                nonPositiveIgnored = true, deathDuringEntry = true, reuseResets = "HP/flash/flight",
                feedbackFailureCannotBlockDeath = true };
        }
        finally
        {
            foreach (var p in projectiles) if (p != null) UnityEngine.Object.DestroyImmediate(p.gameObject);
            pool.Dispose();
            UnityEngine.Object.DestroyImmediate(clone);
            UnityEngine.Object.DestroyImmediate(muzzle);
            sceneScout.gameObject.SetActive(wasActive);
            gate.SetState(oldState);
        }
    }

    private static EnemyHealth SceneScout()
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        foreach (var health in root.GetComponentsInChildren<EnemyHealth>(true))
            if (health.name == "Scout_EntryHold") return health;
        throw new InvalidOperationException("Scene Scout not found.");
    }

    private static void AssertVisible(Camera camera, GameObject scout)
    {
        foreach (var renderer in scout.GetComponentsInChildren<SpriteRenderer>())
        {
            var minimum = camera.WorldToViewportPoint(renderer.bounds.min);
            var maximum = camera.WorldToViewportPoint(renderer.bounds.max);
            Require(minimum.x >= -.0001f && minimum.y >= -.0001f && maximum.x <= 1.0001f && maximum.y <= 1.0001f,
                "Scout visual escaped camera while holding.");
        }
    }

    private static async Task MouseEvent(Mouse mouse, bool down)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(300, 250) }.WithButton(MouseButton.Left, down));
        await Task.Delay(60);
    }

    private static async Task Until(Func<bool> predicate, int timeoutMs, string message)
    {
        var deadline = Time.realtimeSinceStartup + timeoutMs / 1000f;
        while (!predicate() && Time.realtimeSinceStartup < deadline) await Task.Delay(10);
        Require(predicate(), message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
