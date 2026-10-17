using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StarShift.Combat;
using StarShift.Enemies;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public sealed class EnemyHitProbe : MonoBehaviour, IEnemyProjectileTarget
{
    public int Hits;
    public GameObject LastOwner;
    public bool FailOnHit;
    public void ReceiveEnemyHit(GameObject owner)
    {
        Hits++; LastOwner = owner;
        if (FailOnHit) throw new InvalidOperationException("Expected enemy receiver failure");
    }
}

public static class VerifyScoutFire
{
    const string ScoutPath = "Assets/StarShift/Prefabs/Enemies/Scout.prefab";
    const string DefinitionPath = "Assets/StarShift/Data/Enemies/ScoutSingleFire.asset";

    public static async Task<object> Run(int width)
    {
        Require(Application.isPlaying, "Play Mode required.");
        var camera = Camera.main;
        var view = camera.GetComponent<GameplayViewController>();
        var oldView = view.TargetView;
        var player = GameObject.Find("Player");
        var movement = player.GetComponent<RelativeDragMovement>();
        var movementEnabled = movement.enabled;
        var gate = player.GetComponent<PlayerGameplayGate>();
        var oldState = gate.State;
        var oldPosition = player.transform.position;
        var oldTimeScale = Time.timeScale;
        var sceneScout = SceneScout();
        var wasActive = sceneScout.activeSelf;
        var definition = AssetDatabase.LoadAssetAtPath<SingleFireDefinition>(DefinitionPath);
        var before = EditorJsonUtility.ToJson(definition);
        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var gameView = EditorWindow.GetWindow(gameViewType);
        var selectedSize = gameViewType.GetProperty("selectedSizeIndex");
        var oldSize = selectedSize.GetValue(gameView);
        var setResolution = gameViewType.GetMethod("SetCustomResolution", BindingFlags.Instance | BindingFlags.NonPublic);
        GameObject scout = null;
        var results = new List<string>();
        var startFrame = Time.frameCount;
        try
        {
            sceneScout.SetActive(false);
            Time.timeScale = 1;
            movement.enabled = false;
            gate.SetState(PlayerGameplayState.Entry);
            setResolution.Invoke(gameView, new object[] { new Vector2(width, 900), "StarShift Fire " + width + "x900" });
            camera.ResetAspect();
            gameView.Repaint();
            await Task.Delay(350);
            Require(camera.pixelWidth == width && camera.pixelHeight == 900, "Incorrect Game View resolution.");
            Directory.CreateDirectory("Temp/StarShiftScoutFireCheckpoint");
            foreach (var size in new[] { GameplayViewSettings.BaselineSize, 6.8f })
            {
                view.RequestView(new GameplayViewSettings(size, new Rect(0, 0, 1, 1)), 0);
                Position(player, camera.ViewportToWorldPoint(new Vector3(.5f, .18f, 10)));
                scout = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ScoutPath));
                var flight = scout.GetComponent<EntryHoldFlight>();
                var attack = scout.GetComponent<SingleFireAttack>();
                var core = scout.transform.Find("Visual/CorePlaceholder").GetComponent<SpriteRenderer>();
                var normal = core.color;
                var muzzle = scout.transform.Find("Muzzle");
                Require(!flight.IsHolding && !attack.IsTelegraphing && Shots(scout).Length == 0, "Entry already firing.");
                await Until(() => flight.IsHolding, 2200, "No Hold.", () =>
                    Require(!attack.IsTelegraphing && Shots(scout).Length == 0, "Fired during Entry."));
                var holdTime = Time.fixedTime;
                await Until(() => attack.IsTelegraphing, 1300, "No telegraph.");
                var chargeTime = Time.fixedTime;
                Require(chargeTime - holdTime >= definition.InitialDelay - .06f, "Initial delay too short.");
                Require(Shots(scout).Length == 0, "Shot preceded telegraph.");
                await Task.Delay(100);
                Require(core.color != normal, "Core did not show telegraph.");
                ScreenCapture.CaptureScreenshot("Temp/StarShiftScoutFireCheckpoint/" + width + "-" + size + "-charge.png");
                Position(player, camera.ViewportToWorldPoint(new Vector3(.25f, .18f, 10)));
                var expected = ((Vector2)(player.transform.position - muzzle.position)).normalized;
                await Until(() => Shots(scout).Length > 0, 800, "No shot after charge.");
                var shotTime = Time.fixedTime;
                var first = Shots(scout).Single();
                var direction = Read<Vector2>(first, "direction");
                Require(shotTime - chargeTime >= definition.TelegraphDuration - .04f &&
                    shotTime - chargeTime <= definition.TelegraphDuration + .08f, "Wrong telegraph duration.");
                Require(!attack.IsTelegraphing && Vector2.Dot(direction, expected) > .99999f,
                    "Shot did not aim at fire-time Player position.");
                Require(first.Owner == scout && !first.transform.IsChildOf(scout.transform), "Wrong ownership/parenting.");
                var position = first.GetComponent<Rigidbody2D>().position;
                var sampleTime = Time.fixedTime;
                Position(player, camera.ViewportToWorldPoint(new Vector3(.9f, .18f, 10)));
                await Task.Delay(260);
                var delta = first.GetComponent<Rigidbody2D>().position - position;
                Require(Read<Vector2>(first, "direction") == direction &&
                    (delta - direction * definition.ProjectileSpeed * (Time.fixedTime - sampleTime)).magnitude < .015f,
                    "Projectile homed or moved at wrong speed.");
                Require(core.color == normal, "Telegraph cue did not clear after shot.");
                ScreenCapture.CaptureScreenshot("Temp/StarShiftScoutFireCheckpoint/" + width + "-" + size + "-dodge.png");
                await Until(() => attack.IsTelegraphing, 2300, "No repeated fire charge.");
                Require(Time.fixedTime - shotTime >= definition.Cooldown - .04f, "Cooldown too short.");
                Require(Shots(scout).Length == 1, "Cooldown spawned extra shots.");
                await Until(() => Shots(scout).Length == 2, 800, "Repeated single shot missing.");
                attack.enabled = false;
                Require(!attack.IsTelegraphing, "Disable retained telegraph.");
                await Until(() => !first.gameObject.activeSelf, 6500, "Projectile did not expire.");
                Require(Time.fixedTime - shotTime >= definition.ProjectileLifetime - .04f,
                    "Projectile expired before configured lifetime without collision.");
                Require(first.Owner == null && !first.GetComponent<Rigidbody2D>().simulated &&
                    !first.GetComponent<BoxCollider2D>().enabled, "Pool did not reset projectile.");
                UnityEngine.Object.DestroyImmediate(scout);
                scout = null;
                await Task.Delay(60);
                results.Add("size=" + size + ": entry/hold, delay/charge/single/cooldown, fire-time aim, dodge/no-homing, speed/lifetime/pool PASS");
            }
            Require(before == EditorJsonUtility.ToJson(definition), "Test mutated production config.");
            Require(Time.frameCount > startFrame + 20, "Player loop did not advance.");
            return new { passed = true, width, height = 900, frames = Time.frameCount - startFrame, results };
        }
        finally
        {
            Time.timeScale = oldTimeScale;
            if (scout != null) UnityEngine.Object.DestroyImmediate(scout);
            view.RequestView(oldView, 0);
            Position(player, oldPosition);
            movement.enabled = movementEnabled;
            gate.SetState(oldState);
            sceneScout.SetActive(wasActive);
            selectedSize.SetValue(gameView, oldSize);
            camera.ResetAspect();
        }
    }

    public static async Task<object> Lifecycle()
    {
        Require(Application.isPlaying, "Play Mode required.");
        var sceneScout = SceneScout();
        var wasActive = sceneScout.activeSelf;
        var oldTimeScale = Time.timeScale;
        GameObject scout = null;
        var results = new List<string>();
        try
        {
            sceneScout.SetActive(false);
            Time.timeScale = 1;
            scout = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ScoutPath));
            var attack = scout.GetComponent<SingleFireAttack>();
            var health = scout.GetComponent<EnemyHealth>();
            var flight = scout.GetComponent<EntryHoldFlight>();
            var core = scout.transform.Find("Visual/CorePlaceholder").GetComponent<SpriteRenderer>();
            var normal = core.color;
            await Until(() => attack.IsTelegraphing, 3000, "No initial charge.");
            Time.timeScale = 0;
            var remaining = Read<float>(attack, "remaining");
            await Task.Delay(450);
            Require(attack.IsTelegraphing && remaining == Read<float>(attack, "remaining") && Shots(scout).Length == 0,
                "Pause advanced charge or spawned a shot.");
            Time.timeScale = 1;
            health.ReceivePlayerHit(health.MaxHp, null);
            Require(!scout.activeSelf && !attack.IsTelegraphing && core.color == normal, "Death retained charge.");
            await Task.Delay(650);
            Require(Shots(scout).Length == 0, "Dead Scout spawned delayed shot.");
            results.Add("pause during charge and death cancellation PASS");

            scout.SetActive(true);
            Require(!flight.IsHolding && !attack.IsTelegraphing && health.IsAlive, "Root reuse retained state.");
            await Until(() => attack.IsTelegraphing, 3000, "No reused charge.");
            scout.SetActive(false);
            await Task.Delay(650);
            Require(Shots(scout).Length == 0 && !attack.IsTelegraphing, "Inactive Scout spawned delayed shot.");
            scout.SetActive(true);
            await Until(() => attack.IsTelegraphing, 3000, "No charge after root enable.");
            attack.enabled = false;
            await Task.Delay(650);
            Require(!attack.IsTelegraphing && Shots(scout).Length == 0 && core.color == normal, "Disabled attack retained charge.");
            attack.enabled = true;
            await Task.Delay(220);
            Require(!attack.IsTelegraphing && Shots(scout).Length == 0, "Attack enable skipped initial delay.");
            await Until(() => attack.IsTelegraphing, 1200, "Attack did not resume.");
            flight.enabled = false;
            await Task.Delay(80);
            Require(!attack.IsTelegraphing && Shots(scout).Length == 0, "Disabled flight allowed pending shot.");
            flight.enabled = true;
            Require(!flight.IsHolding, "Flight did not restart entry.");
            scout.GetComponent<EnemyFirePresentation>().enabled = false;
            await Until(() => Shots(scout).Length == 1, 3400, "No shot after flight resumes.");
            var shot = Shots(scout).Single();
            var shotPosition = shot.GetComponent<Rigidbody2D>().position;
            var lifetime = Read<float>(shot, "remainingLifetime");
            Time.timeScale = 0;
            await Task.Delay(350);
            Require(shot.GetComponent<Rigidbody2D>().position == shotPosition &&
                lifetime == Read<float>(shot, "remainingLifetime"), "Pause advanced projectile.");
            Time.timeScale = 1;
            var root = shot.transform.parent.gameObject;
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(scout);
            scout = null;
            await Task.Delay(100);
            Require(root == null && shot == null, "Destroy leaked projectile root.");
            results.Add("root/component disable/re-enable, flight block, presentation independence, projectile pause and root-first teardown PASS");
            return new { passed = true, results };
        }
        finally
        {
            Time.timeScale = oldTimeScale;
            if (scout != null) UnityEngine.Object.DestroyImmediate(scout);
            sceneScout.SetActive(wasActive);
        }
    }

    public static async Task<object> Collision()
    {
        Require(Application.isPlaying, "Play Mode required.");
        var sceneScout = SceneScout();
        var wasActive = sceneScout.activeSelf;
        var player = GameObject.Find("Player");
        var gate = player.GetComponent<PlayerGameplayGate>();
        var oldState = gate.State;
        var oldPosition = player.transform.position;
        var movement = player.GetComponent<RelativeDragMovement>();
        var movementEnabled = movement.enabled;
        var definition = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<SingleFireDefinition>(DefinitionPath));
        var settings = new SerializedObject(definition);
        settings.FindProperty("projectileSpeed").floatValue = 400;
        settings.FindProperty("projectileLifetime").floatValue = .25f;
        settings.ApplyModifiedPropertiesWithoutUndo();
        var muzzle = new GameObject("EnemyFireVerificationMuzzle");
        var owner = new GameObject("EnemyFireVerificationOwner");
        var duplicate = new GameObject("EnemyFireCompoundHitbox");
        var collisionRoot = player.transform.Find("Collision").gameObject;
        var probe = collisionRoot.AddComponent<EnemyHitProbe>();
        var productionReceiver = player.GetComponent<PlayerContactReceiver>();
        var receiverEnabled = productionReceiver.enabled;
        EnemyProjectile shot = null;
        int releases = 0;
        try
        {
            sceneScout.SetActive(false);
            gate.SetState(PlayerGameplayState.Entry);
            movement.enabled = false;
            Position(player, new Vector3(30, 30, 0));
            duplicate.transform.SetParent(collisionRoot.transform, false);
            duplicate.AddComponent<CircleCollider2D>().isTrigger = true;
            muzzle.transform.position = player.transform.position + Vector3.up * 2;
            owner.transform.position = muzzle.transform.position;
            owner.AddComponent<BoxCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();
            shot = UnityEngine.Object.Instantiate(definition.ProjectilePrefab);
            shot.ResetForPool();
            Action<EnemyProjectile> release = p => { releases++; p.ResetForPool(); };
            shot.Launch(definition, muzzle.transform, owner, player, release);
            await Until(() => releases == 1, 700, "Fast hostile projectile missed Player.");
            Require(probe.Hits == 1 && probe.LastOwner == owner, "Compound/sweep/trigger duplicated hit or hit owner.");
            Require(shot.Owner == null && !shot.gameObject.activeSelf, "Release did not reset state.");
            shot.Launch(definition, muzzle.transform, owner, player, release);
            await Until(() => releases == 2, 700, "Reused hostile projectile missed.");
            Require(probe.Hits == 2, "Reuse retained hit flag or duplicated hit.");
            shot.Launch(definition, muzzle.transform, owner, player, release);
            Position(player, new Vector3(35, 30, 0));
            await Until(() => releases == 3, 900, "Lifetime did not release missed shot.");
            Require(probe.Hits == 2, "Missed shot homed or notified receiver.");
            probe.FailOnHit = true;
            shot.Launch(definition, muzzle.transform, owner, player, release);
            bool expectedFailure = false;
            try
            {
                typeof(EnemyProjectile).GetMethod("FinishFlight", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(shot, new object[] { probe });
            }
            catch (TargetInvocationException error) when (error.InnerException is InvalidOperationException)
            { expectedFailure = true; }
            Require(expectedFailure && releases == 4 && !shot.gameObject.activeSelf,
                "Receiver exception prevented release.");
            // Receiver bị tắt: contact vẫn release, không phát damage notification.
            UnityEngine.Object.DestroyImmediate(probe);
            productionReceiver.enabled = false;
            Position(player, new Vector3(30, 30, 0));
            shot.Launch(definition, muzzle.transform, owner, player, release);
            await Until(() => releases == 5, 700, "Player contact without receiver did not release.");
            return new { passed = true, highSpeed = 400, compoundHitCount = 2, releases,
                reused = true, ownerIgnored = true, disabledReceiverSafe = true, lifetimeMiss = true,
                receiverFailureStillReleases = true };
        }
        finally
        {
            if (shot != null) UnityEngine.Object.DestroyImmediate(shot.gameObject);
            if (probe != null) UnityEngine.Object.DestroyImmediate(probe);
            productionReceiver.enabled = receiverEnabled;
            UnityEngine.Object.DestroyImmediate(duplicate);
            UnityEngine.Object.DestroyImmediate(muzzle);
            UnityEngine.Object.DestroyImmediate(owner);
            UnityEngine.Object.DestroyImmediate(definition);
            Position(player, oldPosition);
            movement.enabled = movementEnabled;
            gate.SetState(oldState);
            sceneScout.SetActive(wasActive);
        }
    }

    public static async Task<object> Dodge(int width)
    {
        Require(Application.isPlaying, "Play Mode required.");
        var sceneScout = SceneScout();
        var wasActive = sceneScout.activeSelf;
        var player = GameObject.Find("Player");
        var gate = player.GetComponent<PlayerGameplayGate>();
        var oldState = gate.State;
        var oldPosition = player.transform.position;
        var view = Camera.main.GetComponent<GameplayViewController>();
        var oldView = view.TargetView;
        var settings = InputSystem.settings;
        var oldEditorPolicy = settings.editorInputBehaviorInPlayMode;
        var oldBackgroundPolicy = settings.backgroundBehavior;
        var oldMouse = Mouse.current;
        var disabled = new List<InputDevice>();
        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var gameView = EditorWindow.GetWindow(gameViewType);
        var selectedSize = gameViewType.GetProperty("selectedSizeIndex");
        var oldSize = selectedSize.GetValue(gameView);
        Mouse mouse = null;
        GameObject scout = null;
        var probe = player.transform.Find("Collision").gameObject.AddComponent<EnemyHitProbe>();
        try
        {
            sceneScout.SetActive(false);
            gate.SetState(PlayerGameplayState.Entry);
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var device in InputSystem.devices)
            {
                if (!device.native || !device.enabled || (!(device is Mouse) && !(device is Touchscreen))) continue;
                InputSystem.DisableDevice(device); disabled.Add(device);
            }
            mouse = InputSystem.AddDevice<Mouse>("ScoutDodgeVerificationMouse");
            gameViewType.GetMethod("SetCustomResolution", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(gameView, new object[] { new Vector2(width, 900), "StarShift Dodge " + width + "x900" });
            Camera.main.ResetAspect();
            view.RequestView(GameplayViewSettings.Baseline, 0);
            // Đạn đã bắn vẫn sống sau disable Scout; chờ hết cả hai pool trước probe dodge.
            await Task.Delay(6250);
            Require(Camera.main.pixelWidth == width && Camera.main.pixelHeight == 900, "Wrong dodge resolution.");
            Position(player, Camera.main.ViewportToWorldPoint(new Vector3(.25f, .18f, 10)));
            probe.Hits = 0;
            scout = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ScoutPath));
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(300, 250) });
            await Task.Delay(60);
            gate.SetState(PlayerGameplayState.Active);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(300, 250) }.WithButton(MouseButton.Left, true));
            await Until(() => Shots(scout).Length == 1, 3400, "Dodge test Scout did not fire.");
            var shot = Shots(scout).Single();
            var firstDirection = Read<Vector2>(shot, "direction");
            var before = Camera.main.WorldToScreenPoint(player.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(650, 250) }.WithButton(MouseButton.Left, true));
            await Task.Delay(120);
            var after = Camera.main.WorldToScreenPoint(player.transform.position);
            Require(Mathf.Abs(after.x - before.x - 350) < 2 && Mathf.Abs(after.y - before.y) < 2,
                "Relative drag did not move by 350px.");
            Require(player.GetComponent<InputSystemPointerInput>().FireHeld, "Drag lost held fire intent.");
            Require(Read<Vector2>(shot, "direction") == firstDirection, "Drag steered enemy projectile.");
            scout.GetComponent<SingleFireAttack>().enabled = false;
            var downwardSpeed = -firstDirection.y * Read<float>(shot, "speed");
            Require(downwardSpeed > 0, "Expected downward hostile shot for dodge fixture.");
            var travelSeconds = (shot.GetComponent<Rigidbody2D>().position.y - player.transform.position.y + 1) / downwardSpeed;
            await Until(() => shot.GetComponent<Rigidbody2D>().position.y < player.transform.position.y - 1,
                Mathf.CeilToInt((travelSeconds + .5f) * 1000), "Hostile projectile did not pass old Player location.");
            Require(probe.Hits == 0, "Relative drag failed to dodge hostile shot.");
            return new { passed = true, width, height = 900, size = GameplayViewSettings.BaselineSize, relativeDragPixels = after.x - before.x,
                heldFireDuringDodge = true, hostileHits = probe.Hits, virtualInput = true };
        }
        finally
        {
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (var device in disabled) InputSystem.EnableDevice(device);
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            settings.editorInputBehaviorInPlayMode = oldEditorPolicy;
            settings.backgroundBehavior = oldBackgroundPolicy;
            if (scout != null) UnityEngine.Object.DestroyImmediate(scout);
            UnityEngine.Object.DestroyImmediate(probe);
            view.RequestView(oldView, 0);
            Position(player, oldPosition);
            gate.SetState(oldState);
            sceneScout.SetActive(wasActive);
            selectedSize.SetValue(gameView, oldSize);
            Camera.main.ResetAspect();
        }
    }

    static EnemyProjectile[] Shots(GameObject owner)
        => UnityEngine.Object.FindObjectsByType<EnemyProjectile>()
            .Where(p => p.Owner == owner).ToArray();

    static GameObject SceneScout()
        => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<EnemyHealth>(true))
            .Single(health => health.name == "Scout_EntryHold").gameObject;

    static void Position(GameObject target, Vector3 position)
    {
        target.GetComponent<Rigidbody2D>().position = position;
        target.transform.position = position;
        Physics2D.SyncTransforms();
    }

    static T Read<T>(object target, string field)
        => (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

    static async Task Until(Func<bool> condition, int timeoutMs, string message, Action observe = null)
    {
        var deadline = Time.realtimeSinceStartup + timeoutMs / 1000f;
        while (!condition() && Time.realtimeSinceStartup < deadline)
        {
            observe?.Invoke();
            await Task.Delay(10);
        }
        Require(condition(), message);
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
