using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using StarShift.Combat;
using StarShift.Player;

// Probe chỉ được compile trong memory bởi run_script, không có MonoScript/prefab production.
public sealed class PulseHitProbe : MonoBehaviour, IPlayerProjectileTarget
{
    public int HitCount;
    public int LastDamage;
    public GameObject LastOwner;
    public readonly List<double> HitTimes = new List<double>();
    public void ReceivePlayerHit(int damage, GameObject owner)
    {
        HitCount++;
        LastDamage = damage;
        LastOwner = owner;
        HitTimes.Add(Time.fixedTimeAsDouble);
    }
}

public static class VerifyPulseLaser
{
    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var player = GameObject.Find("Player");
        var gate = player.GetComponent<PlayerGameplayGate>();
        var body = player.GetComponent<Rigidbody2D>();
        var input = player.GetComponent<InputSystemPointerInput>();
        var movement = player.GetComponent<RelativeDragMovement>();
        var muzzle = player.transform.Find("Muzzle");
        var definition = AssetDatabase.LoadAssetAtPath<PulseLaserDefinition>("Assets/StarShift/Data/Weapons/PulseLaserLv1.asset");
        var definitionBefore = EditorJsonUtility.ToJson(definition);
        var originalState = gate.State;
        var originalPosition = body.position;
        var originalRotation = body.rotation;
        var originalTimeScale = Time.timeScale;
        var originalMouse = Mouse.current;
        var originalSettings = InputSystem.settings;
        var originalEditorPolicy = originalSettings.editorInputBehaviorInPlayMode;
        var originalBackgroundPolicy = originalSettings.backgroundBehavior;
        var disabledDevices = new List<InputDevice>();
        var temporaryObjects = new List<UnityEngine.Object>();
        Mouse mouse = null;
        Touchscreen touchscreen = null;
        var results = new List<string>();
        var frameStart = Time.frameCount;
        try
        {
            // Cô lập fixture khỏi chuột thật và Editor input buffer; restore trong finally.
            originalSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            originalSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var device in InputSystem.devices)
            {
                if (!device.native || !device.enabled || (!(device is Mouse) && !(device is Touchscreen))) continue;
                InputSystem.DisableDevice(device);
                disabledDevices.Add(device);
            }
            foreach (PlayerGameplayState state in Enum.GetValues(typeof(PlayerGameplayState)))
            {
                gate.SetState(state);
                var active = state == PlayerGameplayState.Active || state == PlayerGameplayState.PostEntryInvincibility;
                Require(gate.CanInput == active && gate.CanFire == active &&
                    gate.CanTakeDamage == (state == PlayerGameplayState.Active), "Gate truth table failed for " + state);
            }
            results.Add("gate truth table: all states PASS; invincibility allows input/fire, blocks damage");
            gate.SetState(PlayerGameplayState.Entry);
            await Drain(definition);

            mouse = InputSystem.AddDevice<Mouse>("StarShiftFireVerificationMouse");
            gate.SetState(PlayerGameplayState.Active);
            await MouseEvent(mouse, new Vector2(200, 200), false);
            await Task.Delay(450);
            Require(!input.FireHeld && ActiveProjectileCount() == 0, "No pointer must mean no firing: held=" + input.FireHeld + " active=" + ActiveProjectileCount() + " mouseDown=" + mouse.leftButton.isPressed + " current=" + Mouse.current?.name);
            var pressPosition = body.position;
            await MouseEvent(mouse, new Vector2(200, 200), true);
            Require(input.FireHeld && (body.position - pressPosition).sqrMagnitude < .0001f, "Press must fire without teleport: held=" + input.FireHeld + " position=" + body.position + " anchor=" + pressPosition + " mouseDown=" + mouse.leftButton.isPressed);
            var start = body.position;
            await MouseEvent(mouse, new Vector2(240, 220), true);
            var pixels = (Vector2)(Camera.main.WorldToScreenPoint(body.position) - Camera.main.WorldToScreenPoint(start));
            Require((pixels - new Vector2(40, 20)).sqrMagnitude < .25f, "Active movement regression");
            Require(ActiveProjectileCount() > 0, "Drag must move and fire together");
            gate.SetState(PlayerGameplayState.Entry);
            var blocked = body.position;
            await MouseEvent(mouse, new Vector2(500, 400), true);
            Require((body.position - blocked).sqrMagnitude < .0001f, "Entry gate did not block input");
            gate.SetState(PlayerGameplayState.Active);
            await MouseEvent(mouse, new Vector2(540, 420), true);
            Require((body.position - blocked).sqrMagnitude < .0001f, "Held pointer resumed stale delta after gate");
            await MouseEvent(mouse, new Vector2(540, 420), false);
            await MouseEvent(mouse, new Vector2(800, 250), true);
            start = body.position;
            await MouseEvent(mouse, new Vector2(820, 260), true);
            pixels = (Vector2)(Camera.main.WorldToScreenPoint(body.position) - Camera.main.WorldToScreenPoint(start));
            Require((pixels - new Vector2(20, 10)).sqrMagnitude < .25f, "Fresh pointer after gate failed");
            await MouseEvent(mouse, new Vector2(820, 260), false);
            gate.SetState(PlayerGameplayState.Entry);
            body.position = originalPosition;
            await Drain(definition);
            results.Add("idle no-fire, press no-teleport, drag + firing, gate movement block and fresh anchor: PASS");

            var cadenceProbe = CreateProbe("CadenceProbe", (Vector2)muzzle.position + Vector2.up * 2, new Vector2(2, .05f), temporaryObjects);
            Physics2D.SyncTransforms();
            gate.SetState(PlayerGameplayState.Active);
            var stationaryPosition = body.position;
            await MouseEvent(mouse, new Vector2(200, 200), true);
            await Task.Delay(1300);
            Require(cadenceProbe.HitCount >= 4, "Stationary hold did not generate sustained hits");
            Require((body.position - stationaryPosition).sqrMagnitude < .0001f, "Stationary hold moved the player");
            for (int i = 1; i < cadenceProbe.HitTimes.Count; i++)
            {
                var interval = cadenceProbe.HitTimes[i] - cadenceProbe.HitTimes[i - 1];
                Require(Math.Abs(interval - definition.FireInterval) <= Time.fixedDeltaTime + .001, "Irregular cadence: " + interval);
            }
            Require(cadenceProbe.LastDamage == definition.Damage && cadenceProbe.LastOwner == player, "Hit payload/owner mismatch");
            results.Add("stationary hold cadence: " + cadenceProbe.HitCount + " hits; interval=" + definition.FireInterval + "s within one physics step");
            var beforeDisable = cadenceProbe.HitCount;
            movement.enabled = false;
            await Task.Delay(500);
            Require(input.FireHeld && cadenceProbe.HitCount > beforeDisable,
                "Fire intent depends on movement being enabled: held=" + input.FireHeld +
                " hits=" + cadenceProbe.HitCount + " before=" + beforeDisable + " focused=" + Application.isFocused);
            movement.enabled = true;
            await AssertReleaseStopsSpawning(mouse, input, new Vector2(200, 200));
            var anchor = body.position;
            await MouseEvent(mouse, new Vector2(900, 350), true);
            Require(input.FireHeld && (body.position - anchor).sqrMagnitude < .0001f, "Re-press at another location reused stale delta");
            System.IO.Directory.CreateDirectory("Temp/StarShiftFireCheckpoint");
            ScreenCapture.CaptureScreenshot("Temp/StarShiftFireCheckpoint/hold-to-fire.png");
            results.Add("movement-independent held intent, release stops new spawns, re-press elsewhere creates fresh anchor: PASS");
            gate.SetState(PlayerGameplayState.Entry);
            await Drain(definition);
            var blockedHits = cadenceProbe.HitCount;
            foreach (var state in new[] { PlayerGameplayState.Entry, PlayerGameplayState.Dying,
                PlayerGameplayState.Respawning, PlayerGameplayState.Transition, PlayerGameplayState.GameOver, PlayerGameplayState.Result })
            {
                gate.SetState(state);
                await Task.Delay(250);
                Require(ActiveProjectileCount() == 0 && cadenceProbe.HitCount == blockedHits, "Fire escaped blocked state " + state);
            }
            gate.SetState(PlayerGameplayState.PostEntryInvincibility);
            await Task.Delay(500);
            Require(input.FireHeld && cadenceProbe.HitCount > blockedHits && !gate.CanTakeDamage,
                "Gate resume with held pointer must restore fire permission");
            Time.timeScale = 0;
            Require(!gate.CanInput && !gate.CanFire && !gate.CanTakeDamage, "Pause did not close permissions");
            var frozenTime = Time.fixedTimeAsDouble;
            await Task.Delay(200);
            Require(Time.fixedTimeAsDouble == frozenTime, "Physics advanced while paused");
            Time.timeScale = originalTimeScale;
            gate.SetState(PlayerGameplayState.Entry);
            await Drain(definition);
            cadenceProbe.gameObject.SetActive(false);
            results.Add("fire block/resume, invincibility and timeScale pause: PASS; lifetime drain leaves zero active projectiles");

            body.rotation = -90;
            player.transform.rotation = Quaternion.Euler(0, 0, -90);
            Physics2D.SyncTransforms();
            var rightProbe = CreateProbe("RightDirectionProbe", body.position + Vector2.right * 2.5f, new Vector2(.05f, 2), temporaryObjects);
            Physics2D.SyncTransforms();
            gate.SetState(PlayerGameplayState.Active);
            await Task.Delay(700);
            Require(rightProbe.HitCount >= 2, "Weapon did not follow gameplay facing Right");
            gate.SetState(PlayerGameplayState.Entry);
            await Drain(definition);
            rightProbe.gameObject.SetActive(false);
            body.rotation = originalRotation;
            player.transform.rotation = Quaternion.Euler(0, 0, originalRotation);
            results.Add("weapon projectile directions Up and Right via Muzzle.up: PASS");

            var testMuzzle = new GameObject("VerificationMuzzle") { hideFlags = HideFlags.DontSave };
            temporaryObjects.Add(testMuzzle);
            testMuzzle.transform.position = new Vector3(20, 20, 0);
            var ownerProbe = CreateProbe("OwnerProbe", new Vector2(20, 20), Vector2.one, temporaryObjects);
            var hitProbe = CreateProbe("SingleHitProbe", new Vector2(20, 22), new Vector2(1, .02f), temporaryObjects);
            var ignored = new GameObject("UnsupportedCollider") { hideFlags = HideFlags.DontSave };
            temporaryObjects.Add(ignored);
            ignored.transform.position = new Vector3(20, 21, 0);
            ignored.AddComponent<BoxCollider2D>().size = new Vector2(1, .02f);
            var fastDefinition = UnityEngine.Object.Instantiate(definition);
            temporaryObjects.Add(fastDefinition);
            var fastSettings = new SerializedObject(fastDefinition);
            fastSettings.FindProperty("projectileSpeed").floatValue = 200;
            fastSettings.ApplyModifiedPropertiesWithoutUndo();
            var projectile = UnityEngine.Object.Instantiate(definition.ProjectilePrefab);
            temporaryObjects.Add(projectile.gameObject);
            int releases = 0;
            Action<PlayerProjectile> release = p => { releases++; p.ResetForPool(); };
            Physics2D.SyncTransforms();
            projectile.Launch(fastDefinition, testMuzzle.transform, ownerProbe.gameObject, release);
            await Task.Delay(150);
            Require(hitProbe.HitCount == 1 && releases == 1 && !projectile.gameObject.activeSelf,
                "Swept duplicate-collider hit must resolve/release exactly once");
            Require(ownerProbe.HitCount == 0 && hitProbe.LastOwner == ownerProbe.gameObject, "Ownership filter failed");
            projectile.Launch(fastDefinition, testMuzzle.transform, ownerProbe.gameObject, release);
            await Task.Delay(150);
            Require(hitProbe.HitCount == 2 && releases == 2, "Reused projectile retained stale hit flag");
            hitProbe.gameObject.SetActive(false);
            projectile.Launch(definition, testMuzzle.transform, ownerProbe.gameObject, release);
            await Task.Delay(120);
            Require(projectile.gameObject.activeSelf && projectile.transform.position.y > 20 &&
                Mathf.Abs(projectile.transform.position.x - 20) < .001, "Projectile movement/direction failed");
            await Drain(definition);
            Require(releases == 3 && !projectile.gameObject.activeSelf && projectile.Owner == null,
                "Lifetime/release/reset contract failed");
            Require(ownerProbe.HitCount == 0, "Projectile hit its owner");
            Require(EditorJsonUtility.ToJson(definition) == definitionBefore, "Runtime mutated static definition asset");
            results.Add("thin-target sweep at speed 200, compound single-hit, owner/unsupported-collider filter, reuse reset, lifetime release: PASS");

            await MouseEvent(mouse, new Vector2(900, 350), false);
            touchscreen = InputSystem.AddDevice<Touchscreen>("StarShiftHeldFireTouch");
            gate.SetState(PlayerGameplayState.Active);
            InputSystem.QueueStateEvent(touchscreen, new TouchState { touchId = 1,
                phase = UnityEngine.InputSystem.TouchPhase.Began, position = new Vector2(600, 300) });
            await Task.Delay(550);
            Require(input.FireHeld && ActiveProjectileCount() >= 2, "Stationary touch hold must sustain fire");
            InputSystem.QueueStateEvent(touchscreen, new TouchState { touchId = 1,
                phase = UnityEngine.InputSystem.TouchPhase.Ended, position = new Vector2(600, 300) });
            await Task.Delay(100);
            Require(!input.FireHeld, "Touch release did not clear fire intent");
            gate.SetState(PlayerGameplayState.Entry);
            await Drain(definition);
            results.Add("synthetic touch stationary hold/release: PASS; same abstraction as mouse");

            var clone = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/StarShift/Prefabs/Player/Player.prefab"));
            clone.name = "PoolTeardownVerificationPlayer";
            temporaryObjects.Add(clone);
            await MouseEvent(mouse, new Vector2(600, 300), true);
            GameObject cloneProjectiles = null;
            foreach (var shot in UnityEngine.Object.FindObjectsByType<PlayerProjectile>())
                if (shot.Owner == clone) cloneProjectiles = shot.transform.parent.gameObject;
            Require(cloneProjectiles != null, "Teardown fixture did not populate its pool.");
            temporaryObjects.Add(cloneProjectiles);
            await MouseEvent(mouse, new Vector2(600, 300), false);
            clone.GetComponent<PlayerGameplayGate>().SetState(PlayerGameplayState.Entry);
            await Drain(definition);
            string teardownError = null;
            Application.LogCallback onLog = (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) teardownError = message;
            };
            Application.logMessageReceived += onLog;
            try
            {
                // Mô phỏng đúng thứ tự gây lỗi khi scene hủy world-root trước owner.
                UnityEngine.Object.DestroyImmediate(cloneProjectiles);
                UnityEngine.Object.DestroyImmediate(clone);
            }
            finally { Application.logMessageReceived -= onLog; }
            Require(teardownError == null, "Pool teardown error: " + teardownError);
            results.Add("projectile root destroyed before weapon/pool disposal: PASS");
            return new { passed = true, frameStart, frameEnd = Time.frameCount, results };
        }
        finally
        {
            Time.timeScale = originalTimeScale;
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (touchscreen != null) InputSystem.RemoveDevice(touchscreen);
            foreach (var device in disabledDevices) if (device.added) InputSystem.EnableDevice(device);
            originalSettings.editorInputBehaviorInPlayMode = originalEditorPolicy;
            originalSettings.backgroundBehavior = originalBackgroundPolicy;
            if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
            foreach (var value in temporaryObjects) if (value != null) UnityEngine.Object.Destroy(value);
            player.GetComponent<InputSystemPointerInput>().ResetDrag();
            movement.enabled = true;
            body.position = originalPosition;
            body.rotation = originalRotation;
            player.transform.rotation = Quaternion.Euler(0, 0, originalRotation);
            gate.SetState(originalState);
        }
    }

    static PulseHitProbe CreateProbe(string name, Vector2 position, Vector2 size, List<UnityEngine.Object> cleanup)
    {
        var root = new GameObject(name) { hideFlags = HideFlags.DontSave };
        cleanup.Add(root);
        root.transform.position = position;
        var probe = root.AddComponent<PulseHitProbe>();
        root.AddComponent<BoxCollider2D>().size = size;
        var child = new GameObject("SecondCollider") { hideFlags = HideFlags.DontSave };
        child.transform.SetParent(root.transform, false);
        child.AddComponent<BoxCollider2D>().size = size;
        return probe;
    }

    static async Task MouseEvent(Mouse mouse, Vector2 position, bool pressed)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, pressed));
        await Task.Delay(100);
    }

    static int ActiveProjectileCount() => UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length;

    static async Task AssertReleaseStopsSpawning(Mouse mouse, PlayerPointerInput input, Vector2 position)
    {
        Dictionary<PlayerProjectile, Vector2> remaining = null;
        Action onIntent = () =>
        {
            if (input.FireHeld) return;
            remaining = new Dictionary<PlayerProjectile, Vector2>();
            foreach (var p in UnityEngine.Object.FindObjectsByType<PlayerProjectile>()) remaining[p] = p.transform.position;
        };
        input.FireIntentChanged += onIntent;
        try { await MouseEvent(mouse, position, false); }
        finally { input.FireIntentChanged -= onIntent; }
        Require(remaining != null && !input.FireHeld, "Release did not clear held intent");
        for (int sample = 0; sample < 16; sample++)
        {
            await Task.Delay(25);
            var ended = new List<PlayerProjectile>();
            foreach (var p in remaining.Keys) if (p == null || !p.gameObject.activeSelf) ended.Add(p);
            foreach (var p in ended) remaining.Remove(p);
            foreach (var p in UnityEngine.Object.FindObjectsByType<PlayerProjectile>())
            {
                Require(remaining.TryGetValue(p, out var previous), "New projectile spawned after pointer release");
                Require(p.transform.position.y >= previous.y - .001f, "Pooled projectile relaunched after release");
                remaining[p] = p.transform.position;
            }
        }
    }
    static Task Drain(PulseLaserDefinition definition) => Task.Delay((int)((definition.ProjectileLifetime + .15f) * 1000));
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
