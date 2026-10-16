using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using StarShift.Player;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public static class VerifyPlayerDrag
{
    public static async Task<object> Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var player = GameObject.Find("Player");
        var body = player.GetComponent<Rigidbody2D>();
        var input = player.GetComponent<InputSystemPointerInput>();
        var movement = player.GetComponent<RelativeDragMovement>();
        var bounds = player.GetComponent<PlayerMovementBounds>();
        var boundsWereEnabled = bounds != null && bounds.enabled;
        var camera = Camera.main;
        var originalPosition = body.position;
        var originalSize = camera.orthographicSize;
        var originalAspect = camera.aspect;
        var originalMouse = Mouse.current;
        var mouse = InputSystem.AddDevice<Mouse>("StarShiftVerificationMouse");
        Touchscreen touch = null;
        var results = new List<string>();
        var frame = Time.frameCount;
        try
        {
            // Suite này đo delta input độc lập; clamp được kiểm tra trong VerifyPlayerBounds.
            if (bounds != null) bounds.enabled = false;
            await MouseEvent(mouse, new Vector2(100, 200), false);
            input.ResetDrag();
            await Drag(mouse, body, camera, new Vector2(150, 200), new Vector2(24, 12), results, "short-left");
            await Drag(mouse, body, camera, new Vector2(camera.pixelWidth - 170, 200), new Vector2(24, 12), results, "short-right");
            await Drag(mouse, body, camera, new Vector2(150, 200), new Vector2(300, 120), results, "long");
            var released = body.position;
            await MouseEvent(mouse, new Vector2(30, 30), false);
            AssertNear(body.position, released, "hover/release has no movement");
            results.Add("release/hover: PASS");

            foreach (var aspect in new[] { 16f / 9f, 20f / 9f })
            {
                camera.aspect = aspect;
                foreach (var size in new[] { 5f, 6.8f })
                {
                    camera.orthographicSize = size;
                    await Drag(mouse, body, camera, new Vector2(180, 220), new Vector2(60, 30), results,
                        "projection aspect=" + aspect.ToString("F3") + " size=" + size);
                }
            }
            camera.aspect = originalAspect;
            camera.orthographicSize = originalSize;

            await MouseEvent(mouse, new Vector2(100, 100), true);
            movement.enabled = false;
            await MouseEvent(mouse, new Vector2(500, 300), true);
            var disabledPosition = body.position;
            movement.enabled = true;
            await MouseEvent(mouse, new Vector2(550, 350), true);
            AssertNear(body.position, disabledPosition, "re-enable during held press cannot jump");
            await MouseEvent(mouse, new Vector2(550, 350), false);
            await Drag(mouse, body, camera, new Vector2(900, 300), new Vector2(-50, 20), results, "fresh-press-after-disable");

            touch = InputSystem.AddDevice<Touchscreen>("StarShiftVerificationTouch");
            var beforeTouch = body.position;
            await TouchEvent(touch, 1, TouchPhase.Began, new Vector2(200, 200));
            AssertNear(body.position, beforeTouch, "touch begin cannot teleport");
            await TouchEvent(touch, 1, TouchPhase.Moved, new Vector2(250, 220));
            AssertDrag(camera, beforeTouch, body.position, new Vector2(50, 20), "touch displacement");
            var firstFingerPosition = body.position;
            await TouchEvent(touch, 2, TouchPhase.Began, new Vector2(800, 400));
            await TouchEvent(touch, 2, TouchPhase.Moved, new Vector2(900, 450));
            AssertNear(body.position, firstFingerPosition, "second finger cannot hijack drag");
            await TouchEvent(touch, 1, TouchPhase.Ended, new Vector2(250, 220));
            await TouchEvent(touch, 2, TouchPhase.Moved, new Vector2(950, 480));
            AssertNear(body.position, firstFingerPosition, "held second finger is not a new drag");
            await TouchEvent(touch, 2, TouchPhase.Ended, new Vector2(950, 480));
            await TouchEvent(touch, 3, TouchPhase.Began, new Vector2(700, 200));
            AssertNear(body.position, firstFingerPosition, "new touch starts from a fresh anchor");
            await TouchEvent(touch, 3, TouchPhase.Moved, new Vector2(670, 240));
            AssertDrag(camera, firstFingerPosition, body.position, new Vector2(-30, 40), "touch re-drag");
            await TouchEvent(touch, 3, TouchPhase.Ended, new Vector2(670, 240));
            results.Add("touch/re-drag/second-finger isolation: PASS (synthetic Input System events)");
            return new { passed = true, frameStart = frame, frameEnd = Time.frameCount, results };
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            if (touch != null) InputSystem.RemoveDevice(touch);
            if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
            input.ResetDrag();
            movement.enabled = true;
            body.position = originalPosition;
            camera.orthographicSize = originalSize;
            camera.ResetAspect();
            if (bounds != null) bounds.enabled = boundsWereEnabled;
        }
    }

    static async Task Drag(Mouse mouse, Rigidbody2D body, Camera camera, Vector2 start, Vector2 delta,
        List<string> results, string label)
    {
        await MouseEvent(mouse, start, false);
        var origin = body.position;
        await MouseEvent(mouse, start, true);
        AssertNear(body.position, origin, label + ": press cannot teleport");
        await MouseEvent(mouse, start + delta / 2, true);
        await MouseEvent(mouse, start + delta, true);
        AssertDrag(camera, origin, body.position, delta, label + ": displacement");
        await MouseEvent(mouse, start + delta, false);
        var released = body.position;
        await Task.Delay(100);
        AssertNear(body.position, released, label + ": no inertia");
        results.Add(label + ": PASS");
    }

    static void AssertDrag(Camera camera, Vector2 origin, Vector2 actual, Vector2 pixels, string label)
    {
        var observed = (Vector2)(camera.WorldToScreenPoint(actual) - camera.WorldToScreenPoint(origin));
        if ((observed - pixels).sqrMagnitude > .25f)
            throw new InvalidOperationException(label + ": screen delta=" + observed + " expected=" + pixels);
    }

    static async Task MouseEvent(Mouse mouse, Vector2 position, bool down)
    {
        mouse.MakeCurrent();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, down));
        await Task.Delay(120);
    }

    static async Task TouchEvent(Touchscreen device, int id, TouchPhase phase, Vector2 position)
    {
        InputSystem.QueueStateEvent(device, new TouchState { touchId = id, phase = phase, position = position });
        await Task.Delay(120);
    }

    static void AssertNear(Vector2 actual, Vector2 expected, string label)
    {
        if ((actual - expected).sqrMagnitude > .0001f)
            throw new InvalidOperationException(label + ": actual=" + actual.ToString("F4") + " expected=" + expected.ToString("F4"));
    }
}
