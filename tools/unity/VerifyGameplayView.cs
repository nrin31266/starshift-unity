using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;
using StarShift.Gameplay;
using StarShift.Combat;
using StarShift.Player;

// Quan sát sau LateUpdate của movement, không thêm verifier vào production assembly.
[DefaultExecutionOrder(1000)]
public sealed class GameplayViewProbe : MonoBehaviour
{
    public Rigidbody2D Body;
    public GameplayViewController View;
    public Camera Camera;
    public Vector2 Footprint;
    public bool Monitor;
    public string Failure;
    public int Samples;
    public float MaximumStep;
    private Vector2 previous;

    public void Begin()
    {
        Failure = null; Samples = 0; MaximumStep = 0; previous = Body.position; Monitor = true;
    }

    private void LateUpdate()
    {
        if (!Monitor) return;
        Samples++;
        MaximumStep = Mathf.Max(MaximumStep, Vector2.Distance(previous, Body.position));
        previous = Body.position;
        try { VerifyGameplayView.AssertFootprint(Body, Camera, Footprint, View.CurrentPlayableViewport); }
        catch (Exception error) { Failure = error.Message; }
    }
}

public static class VerifyGameplayView
{
    public static async Task<object> InspectorDelayedFields()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var view = Camera.main.GetComponent<GameplayViewController>();
        var previous = view.TargetView;
        var window = ScriptableObject.CreateInstance<ViewSettingsTypingWindow>();
        try
        {
            view.RequestView(GameplayViewSettings.Baseline, 0);
            var serialized = new SerializedObject(view);
            var property = serialized.FindProperty("targetView");
            property.isExpanded = true;
            var drawer = new StarShift.Editor.GameplayViewSettingsDrawer();
            window.rootVisualElement.Add(drawer.CreatePropertyGUI(property));
            window.titleContent = new GUIContent("Viewport binding verification");
            window.position = new Rect(100, 100, 460, 400);
            window.ShowUtility();
            await Task.Delay(180);
            var height = window.rootVisualElement.Q<FloatField>("targetView.playableViewport.height");
            Require(height != null && height.isDelayed, "Height must use delayed input.");
            var text = height.Q<VisualElement>("unity-text-input").Q<TextElement>();
            foreach (var pending in new[] { "", "0", "0.", "0.7" })
            {
                text.text = pending;
                await Task.Delay(30);
                Require(view.TargetView.PlayableViewport.height == 1 && height.text == pending,
                    "Uncommitted text changed/reset region: " + pending + " -> " + height.text);
            }
            height.value = .7f;
            await Task.Delay(750);
            Require(Mathf.Abs(view.TargetView.PlayableViewport.height - .7f) < .0001f &&
                Mathf.Abs(view.CurrentPlayableViewport.height - .7f) < .0001f, "Committed field did not apply lower 70%.");
            height.value = 1;
            await Task.Delay(750);
            Require(view.TargetView.PlayableViewport.height == 1 && view.CurrentPlayableViewport.height == 1,
                "Committed field did not restore full viewport.");
            return new { passed = true, delayedField = true, pendingTextPreserved = true,
                committedBinding07 = true, committedBinding1 = true };
        }
        finally { window.Close(); view.RequestView(previous, 0); }
    }

    public static async Task<object> InspectorRequest()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var camera = Camera.main;
        var view = camera.GetComponent<GameplayViewController>();
        var oldTarget = view.TargetView;
        var body = GameObject.Find("Player").GetComponent<Rigidbody2D>();
        var oldPosition = body.position;
        try
        {
            var serialized = new SerializedObject(view);
            var target = serialized.FindProperty("targetView");
            target.FindPropertyRelative("orthographicSize").floatValue = 7.1f;
            target.FindPropertyRelative("playableViewport").rectValue = new Rect(0, 0, 1, .7f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            await Task.Delay(90);
            Require(view.IsTransitioning && camera.orthographicSize > GameplayViewSettings.BaselineSize && camera.orthographicSize < 7.1f,
                "Inspector edit did not request a transition.");
            await AwaitTransition(view);
            Require(Mathf.Abs(camera.orthographicSize - 7.1f) < .00001f &&
                view.CurrentPlayableViewport == new Rect(0, 0, 1, .7f), "Inspector target endpoint failed.");
            return new { passed = true, path = "SerializedObject (Inspector) -> same runtime request path" };
        }
        finally { view.RequestView(oldTarget, 0); body.position = oldPosition; }
    }

    public static async Task<object> Run(int width)
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var player = GameObject.Find("Player");
        var body = player.GetComponent<Rigidbody2D>();
        var input = player.GetComponent<InputSystemPointerInput>();
        var gate = player.GetComponent<PlayerGameplayGate>();
        var bounds = player.GetComponent<PlayerMovementBounds>();
        var footprint = new SerializedObject(bounds).FindProperty("footprintSize").vector2Value;
        var camera = Camera.main;
        var view = camera.GetComponent<GameplayViewController>();
        Require(view != null && view.enabled, "GameplayViewController missing/disabled.");
        var oldView = view.TargetView;
        var oldPosition = body.position;
        var oldState = gate.State;
        var oldTimeScale = Time.timeScale;
        var oldMouse = Mouse.current;
        var settings = InputSystem.settings;
        var oldEditorPolicy = settings.editorInputBehaviorInPlayMode;
        var oldBackgroundPolicy = settings.backgroundBehavior;
        var oldRunInBackground = Application.runInBackground;
        var disabledDevices = new List<InputDevice>();
        Mouse mouse = null;
        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var gameView = EditorWindow.GetWindow(gameViewType);
        var selectedSize = gameViewType.GetProperty("selectedSizeIndex");
        var oldSize = selectedSize.GetValue(gameView);
        var setResolution = gameViewType.GetMethod("SetCustomResolution", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(setResolution != null, "Expected Game View resolution API not available.");
        var observer = new GameObject("GameplayViewVerification") { hideFlags = HideFlags.DontSave };
        var probe = observer.AddComponent<GameplayViewProbe>();
        probe.Body = body; probe.View = view; probe.Camera = camera; probe.Footprint = footprint;
        var results = new List<string>();
        var frameStart = Time.frameCount;
        try
        {
            Application.runInBackground = true;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var device in InputSystem.devices)
            {
                if (!device.native || !device.enabled || (!(device is Mouse) && !(device is Touchscreen))) continue;
                InputSystem.DisableDevice(device); disabledDevices.Add(device);
            }
            mouse = InputSystem.AddDevice<Mouse>("StarShiftViewVerificationMouse");
            gate.SetState(PlayerGameplayState.Active);
            camera.ResetAspect();
            setResolution.Invoke(gameView, new object[] { new Vector2(width, 900), "StarShift View " + width + "x900" });
            gameView.Repaint();
            await Task.Delay(350);
            Require(camera.pixelWidth == width && camera.pixelHeight == 900, "Game View did not adopt requested resolution.");
            var full = new Rect(0, 0, 1, 1);
            var restricted = new Rect(0, 0, 1, .7f);
            foreach (var target in new[] { GameplayViewSettings.Baseline, new GameplayViewSettings(5.25f, full),
                new GameplayViewSettings(6.8f, full), new GameplayViewSettings(8.8f, full),
                new GameplayViewSettings(8.8f, restricted) })
            {
                view.RequestView(target, 0);
                await Task.Delay(50);
                Require(Mathf.Abs(camera.orthographicSize - target.OrthographicSize) < .00001f,
                    "Camera did not apply requested framing: " + target.OrthographicSize);
                foreach (var direction in new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down,
                    new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) })
                {
                    var pointer = new Vector2(width * .5f, 450);
                    await MouseEvent(mouse, pointer, false);
                    var center = target.PlayableViewport.center;
                    body.position = camera.ViewportToWorldPoint(new Vector3(center.x, center.y, 10));
                    await MouseEvent(mouse, pointer, true);
                    pointer += new Vector2(direction.x * width * 2, direction.y * 1800);
                    await MouseEvent(mouse, pointer, true);
                    AssertFootprint(body, camera, footprint, target.PlayableViewport, direction);
                    var atEdge = body.position;
                    pointer += direction * 100;
                    await MouseEvent(mouse, pointer, true);
                    Require((body.position - atEdge).sqrMagnitude < 1e-8f, "Outward drag jitter.");
                    pointer -= direction * 12;
                    await MouseEvent(mouse, pointer, true);
                    var screenDelta = (Vector2)(camera.WorldToScreenPoint(body.position) - camera.WorldToScreenPoint(atEdge));
                    Require((screenDelta + direction * 12).sqrMagnitude < .25f, "Reverse drag lag: " + screenDelta);
                    await MouseEvent(mouse, pointer, false);
                }
                results.Add("size=" + target.OrthographicSize + " region=" + target.PlayableViewport + ": 4 edges + 4 corners, footprint, stable outward, reverse 12px PASS");
            }

            // Region có offset kiểm tra cả xMin/yMin, không chỉ lower-region case.
            var offset = new Rect(.1f, .1f, .8f, .6f);
            view.RequestView(new GameplayViewSettings(7.3f, offset), 0);
            await Task.Delay(70);
            AssertFootprint(body, camera, footprint, offset);
            results.Add("offset viewport region (0.1, 0.1, 0.8, 0.6): PASS");

            gate.SetState(PlayerGameplayState.Entry);
            await Task.Delay(1650);
            Require(UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length == 0,
                "Old projectiles did not drain before transition fire test.");
            gate.SetState(PlayerGameplayState.Active);
            view.RequestView(new GameplayViewSettings(8.2f, full), 0);
            body.position = camera.ViewportToWorldPoint(new Vector3(.85f, .9f, 10));
            var beforeTransition = body.position;
            var pointerHeld = new Vector2(200, 200);
            await MouseEvent(mouse, pointerHeld, true);
            probe.Begin();
            view.RequestView(new GameplayViewSettings(7.4f, restricted), .6f);
            Require(body.position == beforeTransition, "Request itself teleported the player.");
            await AwaitTransition(view);
            Require(probe.Failure == null && probe.Samples > 2 && probe.MaximumStep < .75f,
                "Shrink transition: " + probe.Failure + " samples=" + probe.Samples + " maxStep=" + probe.MaximumStep);
            Require(body.position.y < beforeTransition.y && input.FireHeld, "Shrink/held intent failed.");
            Require(UnityEngine.Object.FindObjectsByType<PlayerProjectile>().Length >= 2,
                "Stationary hold stopped spawning during view transition.");
            probe.Monitor = false;
            var afterTransition = body.position;
            await Task.Delay(120);
            Require((body.position - afterTransition).sqrMagnitude < 1e-8f, "Stationary held pointer retained stale delta.");
            pointerHeld.y -= 12;
            await MouseEvent(mouse, pointerHeld, true);
            var reverse = (Vector2)(camera.WorldToScreenPoint(body.position) - camera.WorldToScreenPoint(afterTransition));
            Require((reverse - new Vector2(0, -12)).sqrMagnitude < .25f, "View transition lost current drag anchor.");
            results.Add("gradual shrink with player outside future region, stationary hold firing, reverse without release: PASS");

            gate.SetState(PlayerGameplayState.Entry);
            view.RequestView(new GameplayViewSettings(8.2f, full), 0);
            body.position = camera.ViewportToWorldPoint(new Vector3(.9f, .9f, 10));
            probe.Begin();
            view.ReturnToBaseline();
            await AwaitTransition(view);
            Require(probe.Failure == null && !gate.CanInput && !gate.CanFire && probe.Samples > 2,
                "Gate-blocked return did not constrain player: " + probe.Failure);
            Require(Mathf.Abs(camera.orthographicSize - GameplayViewSettings.BaselineSize) < .00001f && view.CurrentPlayableViewport == full,
                "ReturnToBaseline did not reach default size/full.");
            probe.Monitor = false;
            gate.SetState(PlayerGameplayState.Active);
            var resumedPosition = body.position;
            pointerHeld += new Vector2(30, 20);
            await MouseEvent(mouse, pointerHeld, true);
            Require(input.FireHeld && (body.position - resumedPosition).sqrMagnitude < .0001f,
                "Gate resume must keep fire held, but require fresh movement press.");
            await MouseEvent(mouse, pointerHeld, false);
            var anchor = body.position;
            await MouseEvent(mouse, new Vector2(900, 250), true);
            Require((body.position - anchor).sqrMagnitude < .0001f, "Re-press reused stale delta.");
            await MouseEvent(mouse, new Vector2(900, 250), false);
            results.Add("return to baseline/full while state gate blocks input, held resume/fresh anchor: PASS");

            probe.Begin();
            view.RequestView(new GameplayViewSettings(8.2f, restricted), .6f);
            await Task.Delay(160);
            var intermediate = camera.orthographicSize;
            Require(intermediate > GameplayViewSettings.BaselineSize && intermediate < 8.2f, "Transition did not interpolate.");
            view.RequestView(new GameplayViewSettings(6.8f, full), .3f);
            Require(Mathf.Abs(camera.orthographicSize - intermediate) < .0001f, "Retarget jumped framing.");
            Time.timeScale = 0;
            await Task.Delay(120);
            Require(Mathf.Abs(camera.orthographicSize - intermediate) < .0001f, "View transition ignored scaled pause.");
            Time.timeScale = 1;
            await AwaitTransition(view);
            Require(probe.Failure == null && Mathf.Abs(camera.orthographicSize - 6.8f) < .00001f, "Retarget endpoint failed.");
            probe.Monitor = false;
            view.ReturnToBaseline();
            await AwaitTransition(view);
            results.Add("mid-transition retarget, smooth endpoint, scaled pause, return: PASS");

            body.position = camera.ViewportToWorldPoint(new Vector3(.99f, .5f, 10));
            await Task.Delay(50);
            probe.Begin();
            var otherWidth = width == 2000 ? 1600 : 2000;
            setResolution.Invoke(gameView, new object[] { new Vector2(otherWidth, 900), "StarShift View " + otherWidth + "x900" });
            gameView.Repaint();
            await Task.Delay(350);
            Require(camera.pixelWidth == otherWidth && probe.Failure == null && probe.Samples > 2,
                "Live aspect change escaped viewport: " + probe.Failure);
            probe.Monitor = false;
            results.Add("live aspect change while parked at right boundary: PASS");

            var invalid = new GameplayViewSettings(float.NaN, new Rect(2, 2, -1, 0));
            Require(invalid.OrthographicSize == GameplayViewSettings.BaselineSize && invalid.PlayableViewport == full,
                "Invalid settings fallback failed.");
            Require(new GameplayViewSettings(4.9f, full).OrthographicSize == 4.9f,
                "Baseline must not act as a gameplay minimum.");
            foreach (var invalidSize in new[] { 0f, -1f, float.PositiveInfinity })
                Require(new GameplayViewSettings(invalidSize, full).OrthographicSize == GameplayViewSettings.BaselineSize,
                    "Non-positive/non-finite size did not fall back to baseline.");
            var smallRegion = new Rect(.4f, .4f, .001f, .001f);
            var tiny = bounds.ClampPosition(body.position, camera, smallRegion);
            var projected = (Vector2)camera.WorldToViewportPoint(tiny);
            Require((projected - smallRegion.center).sqrMagnitude < 1e-8f, "Undersized region midpoint fallback failed.");
            results.Add("invalid config fallback, close framing below baseline, undersized-region deterministic fallback: PASS");
            return new { passed = true, gameView = width + "x900", frameStart, frameEnd = Time.frameCount, results };
        }
        finally
        {
            probe.Monitor = false;
            UnityEngine.Object.DestroyImmediate(observer);
            Time.timeScale = oldTimeScale;
            view.RequestView(oldView, 0);
            gate.SetState(oldState);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (var device in disabledDevices) if (device.added) InputSystem.EnableDevice(device);
            settings.editorInputBehaviorInPlayMode = oldEditorPolicy;
            settings.backgroundBehavior = oldBackgroundPolicy;
            if (oldMouse != null && oldMouse.added) oldMouse.MakeCurrent();
            input.ResetDrag();
            body.position = oldPosition;
            selectedSize.SetValue(gameView, oldSize);
            gameView.Repaint(); camera.ResetAspect();
            Application.runInBackground = oldRunInBackground;
        }
    }

    public static void AssertFootprint(Rigidbody2D body, Camera camera, Vector2 size, Rect region, Vector2 direction = default)
    {
        var minimum = Vector2.positiveInfinity;
        var maximum = Vector2.negativeInfinity;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        {
            var corner = (Vector3)body.position + body.transform.TransformVector(new Vector3(x * size.x * .5f, y * size.y * .5f));
            var viewport = (Vector2)camera.WorldToViewportPoint(corner);
            minimum = Vector2.Min(minimum, viewport); maximum = Vector2.Max(maximum, viewport);
        }
        const float epsilon = .0001f;
        Require(minimum.x >= region.xMin - epsilon && minimum.y >= region.yMin - epsilon &&
            maximum.x <= region.xMax + epsilon && maximum.y <= region.yMax + epsilon,
            "Footprint escaped: " + minimum + " .. " + maximum + " region=" + region);
        Require((direction.x >= 0 || Mathf.Abs(minimum.x - region.xMin) < epsilon) &&
            (direction.x <= 0 || Mathf.Abs(maximum.x - region.xMax) < epsilon) &&
            (direction.y >= 0 || Mathf.Abs(minimum.y - region.yMin) < epsilon) &&
            (direction.y <= 0 || Mathf.Abs(maximum.y - region.yMax) < epsilon), "Requested edge not reached.");
    }

    private static async Task AwaitTransition(GameplayViewController view)
    {
        var deadline = Time.realtimeSinceStartup + 4;
        while (view.IsTransitioning && Time.realtimeSinceStartup < deadline) await Task.Delay(25);
        await Task.Delay(40);
        Require(!view.IsTransitioning, "Transition timed out.");
    }

    private static async Task MouseEvent(Mouse mouse, Vector2 position, bool down)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, down));
        await Task.Delay(70);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

public sealed class ViewSettingsTypingWindow : EditorWindow { }
