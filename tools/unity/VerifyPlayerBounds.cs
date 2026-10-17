using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using StarShift.Player;
using StarShift.Gameplay;

public static class VerifyPlayerBounds
{
    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var player = GameObject.Find("Player");
        var body = player.GetComponent<Rigidbody2D>();
        var input = player.GetComponent<InputSystemPointerInput>();
        var renderer = player.GetComponentInChildren<SpriteRenderer>();
        var camera = Camera.main;
        if (Mathf.Abs(camera.orthographicSize - GameplayViewSettings.BaselineSize) > .001f)
            throw new InvalidOperationException("This checkpoint requires the default gameplay view baseline.");
        var originalPosition = body.position;
        var originalMouse = Mouse.current;
        var inputSettings = InputSystem.settings;
        var originalEditorPolicy = inputSettings.editorInputBehaviorInPlayMode;
        var originalBackgroundPolicy = inputSettings.backgroundBehavior;
        var disabledDevices = new List<InputDevice>();
        Mouse mouse = null;
        var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var gameView = EditorWindow.GetWindow(gameViewType);
        var selectedSize = gameViewType.GetProperty("selectedSizeIndex");
        var originalSizeIndex = selectedSize.GetValue(gameView);
        var gameViewZoom = gameView.GetType().GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameView);
        var oldGameViewZoom = (Vector2)gameViewZoom.GetType().GetProperty("scale").GetValue(gameViewZoom);
        var oldGameViewPan = (Vector2)gameViewZoom.GetType().GetProperty("translation").GetValue(gameViewZoom);
        // API Editor nội bộ chỉ dùng trong verification, không đi vào runtime.
        var setResolution = gameViewType.GetMethod("SetCustomResolution", BindingFlags.Instance | BindingFlags.NonPublic);
        if (setResolution == null) throw new InvalidOperationException("This Editor cannot configure the Game View with the expected API.");
        var results = new List<object>();
        var directions = new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down,
            new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) };
        var frameStart = Time.frameCount;
        try
        {
            inputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            inputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            foreach (var device in InputSystem.devices)
            {
                if (!device.native || !device.enabled || (!(device is Mouse) && !(device is Touchscreen))) continue;
                InputSystem.DisableDevice(device);
                disabledDevices.Add(device);
            }
            mouse = InputSystem.AddDevice<Mouse>("StarShiftBoundsVerificationMouse");
            foreach (var width in new[] { 1600, 2000 })
            {
                camera.ResetAspect();
                setResolution.Invoke(gameView, new object[] { new Vector2(width, 900), "StarShift Bounds " + (width == 1600 ? "16:9" : "20:9") });
                gameView.Repaint();
                await Task.Delay(500);
                if (camera.pixelWidth != width || camera.pixelHeight != 900)
                    throw new InvalidOperationException("Game View did not adopt " + width + "x900; actual=" + camera.pixelWidth + "x" + camera.pixelHeight);
                foreach (var direction in directions)
                {
                    var pointer = new Vector2(width * .5f, 450);
                    await MouseEvent(mouse, pointer, false);
                    input.ResetDrag();
                    body.position = camera.ViewportToWorldPoint(new Vector3(.5f, .5f, 10));
                    await Task.Delay(80);
                    await MouseEvent(mouse, pointer, true);
                    pointer += new Vector2(direction.x * width * 1.5f, direction.y * 1350);
                    await MouseEvent(mouse, pointer, true);
                    AssertVisible(camera, renderer.bounds, direction, true);
                    var edgePosition = body.position;
                    for (int step = 0; step < 3; step++)
                    {
                        pointer += direction * 50;
                        await MouseEvent(mouse, pointer, true);
                        if ((body.position - edgePosition).sqrMagnitude > .00000001f)
                            throw new InvalidOperationException("Boundary jitter at " + direction + ": " + body.position + " vs " + edgePosition);
                    }
                    if (direction == new Vector2(1, 1))
                    {
                        System.IO.Directory.CreateDirectory("Temp/StarShiftBoundsCheckpoint");
                        ScreenCapture.CaptureScreenshot("Temp/StarShiftBoundsCheckpoint/" + width + "x900-corner.png");
                        await Task.Delay(150);
                    }
                    pointer -= direction * 12;
                    await MouseEvent(mouse, pointer, true);
                    var screenDelta = (Vector2)(camera.WorldToScreenPoint(body.position) - camera.WorldToScreenPoint(edgePosition));
                    if ((screenDelta + direction * 12).sqrMagnitude > .25f)
                        throw new InvalidOperationException("Reverse drag lag at " + direction + ": " + screenDelta);
                    AssertVisible(camera, renderer.bounds, direction, false);
                    await MouseEvent(mouse, pointer, false);
                }
                results.Add(new { gameView = width + "x900", aspect = camera.aspect, cameraSize = camera.orthographicSize,
                    edges = 4, corners = 4, continuedOutwardDrag = "stable", reverseWithoutRelease = "12px immediate" });
            }
            return new { passed = true, frameStart, frameEnd = Time.frameCount, results };
        }
        finally
        {
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            foreach (var device in disabledDevices) if (device.added) InputSystem.EnableDevice(device);
            inputSettings.editorInputBehaviorInPlayMode = originalEditorPolicy;
            inputSettings.backgroundBehavior = originalBackgroundPolicy;
            if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
            input.ResetDrag();
            body.position = originalPosition;
            gameView.GetType().GetMethod("SizeSelectionCallback").Invoke(gameView, new object[] { originalSizeIndex, null });
            gameViewZoom.GetType().GetMethod("SetTransform").Invoke(gameViewZoom, new object[] { oldGameViewPan, oldGameViewZoom });
            gameView.Repaint();
            camera.ResetAspect();
        }
    }

    static void AssertVisible(Camera camera, Bounds hull, Vector2 direction, bool requireEdge)
    {
        var minimum = (Vector2)camera.WorldToViewportPoint(hull.min);
        var maximum = (Vector2)camera.WorldToViewportPoint(hull.max);
        const float tolerance = .0001f;
        if (minimum.x < -tolerance || minimum.y < -tolerance || maximum.x > 1 + tolerance || maximum.y > 1 + tolerance)
            throw new InvalidOperationException("Visible hull escaped viewport: " + minimum + " .. " + maximum);
        if (!requireEdge) return;
        if ((direction.x < 0 && Mathf.Abs(minimum.x) > tolerance) ||
            (direction.x > 0 && Mathf.Abs(maximum.x - 1) > tolerance) ||
            (direction.y < 0 && Mathf.Abs(minimum.y) > tolerance) ||
            (direction.y > 0 && Mathf.Abs(maximum.y - 1) > tolerance))
            throw new InvalidOperationException("Player did not reach requested edge " + direction + ": " + minimum + " .. " + maximum + " held=" + GameObject.Find("Player").GetComponent<InputSystemPointerInput>().FireHeld + " state=" + GameObject.Find("Player").GetComponent<PlayerGameplayGate>().State + " mouse=" + Mouse.current?.leftButton.isPressed + " focus=" + Application.isFocused);
    }

    static async Task MouseEvent(Mouse mouse, Vector2 position, bool down)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, down));
        await Task.Delay(100);
    }
}
