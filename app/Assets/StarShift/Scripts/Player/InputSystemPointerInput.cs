using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    [AddComponentMenu("StarShift/Player/Input System Pointer Input")]
    public sealed class InputSystemPointerInput : PlayerPointerInput
    {
        private Finger activeFinger;
        private int activeTouchId;
        private Mouse activeMouse;
        private Vector2 previousPosition;
        private Vector2 frameDelta;
        private bool movementTracking;
        private bool pointerHeld;

        public override bool FireHeld => isActiveAndEnabled && pointerHeld;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            CancelGesture();
        }

        private void OnDisable()
        {
            CancelGesture();
            EnhancedTouchSupport.Disable();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelGesture();
        }

        private void Update() => frameDelta = SamplePointer();

        public override Vector2 ReadDelta() => frameDelta;

        private Vector2 SamplePointer()
        {
            if (!isActiveAndEnabled) return Vector2.zero;
            var touches = Touch.activeTouches;
            if (activeFinger != null)
            {
                foreach (var touch in touches)
                {
                    if (touch.finger != activeFinger || touch.touchId != activeTouchId) continue;
                    if (!touch.isInProgress)
                    {
                        CancelGesture();
                        return Vector2.zero;
                    }
                    return DeltaFrom(touch.screenPosition);
                }
                CancelGesture();
                return Vector2.zero;
            }

            if (touches.Count > 0)
            {
                if (activeMouse != null) CancelGesture();
                foreach (var touch in touches)
                {
                    if (touch.phase != TouchPhase.Began || StartsOnUi(touch.screenPosition)) continue;
                    activeFinger = touch.finger;
                    activeTouchId = touch.touchId;
                    previousPosition = touch.screenPosition;
                    movementTracking = true;
                    SetFireHeld(true);
                    break;
                }
                // Touch mới chỉ đặt mốc; không kéo tàu tới điểm chạm.
                return Vector2.zero;
            }

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed)
            {
                CancelGesture();
                return Vector2.zero;
            }
            if (activeMouse != mouse || mouse.leftButton.wasPressedThisFrame)
            {
                // Sau disable/focus loss phải có press mới, không nhận lại drag đang giữ.
                if (!mouse.leftButton.wasPressedThisFrame) return Vector2.zero;
                // Re-press trên UI cùng frame phải hủy cả ownership của press gameplay cũ.
                CancelGesture();
                if (StartsOnUi(mouse.position.ReadValue())) return Vector2.zero;
                activeMouse = mouse;
                previousPosition = mouse.position.ReadValue();
                movementTracking = true;
                SetFireHeld(true);
                return Vector2.zero;
            }
            return DeltaFrom(mouse.position.ReadValue());
        }

        public override void ResetDrag()
        {
            frameDelta = Vector2.zero;
            movementTracking = false;
            previousPosition = Vector2.zero;
        }

        public void CancelGesture()
        {
            activeFinger = null;
            activeMouse = null;
            activeTouchId = 0;
            ResetDrag();
            SetFireHeld(false);
        }

        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        private bool StartsOnUi(Vector2 position)
        {
            var events = EventSystem.current;
            if (events == null) return false;
            uiHits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = position }, uiHits);
            return uiHits.Count > 0;
        }

        private void SetFireHeld(bool held)
        {
            if (pointerHeld == held) return;
            pointerHeld = held;
            NotifyFireIntentChanged();
        }

        private Vector2 DeltaFrom(Vector2 position)
        {
            if (!movementTracking)
            {
                // Sau gate lock, lấy mốc mới cho pointer còn giữ để không kéo bù đoạn bị khóa.
                previousPosition = position;
                movementTracking = true;
                return Vector2.zero;
            }
            var delta = position - previousPosition;
            previousPosition = position;
            return delta;
        }
    }
}
