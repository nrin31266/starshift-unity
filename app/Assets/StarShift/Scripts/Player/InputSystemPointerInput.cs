using UnityEngine;
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
            CancelPointer();
        }

        private void OnDisable()
        {
            CancelPointer();
            EnhancedTouchSupport.Disable();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelPointer();
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
                        CancelPointer();
                        return Vector2.zero;
                    }
                    return DeltaFrom(touch.screenPosition);
                }
                CancelPointer();
                return Vector2.zero;
            }

            if (touches.Count > 0)
            {
                if (activeMouse != null) CancelPointer();
                foreach (var touch in touches)
                {
                    if (touch.phase != TouchPhase.Began) continue;
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
                CancelPointer();
                return Vector2.zero;
            }
            if (activeMouse != mouse || mouse.leftButton.wasPressedThisFrame)
            {
                // Sau disable/focus loss phải có press mới, không nhận lại drag đang giữ.
                if (!mouse.leftButton.wasPressedThisFrame) return Vector2.zero;
                // Release/re-press trong cùng frame cũng phải tạo intent/anchor mới.
                SetFireHeld(false);
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

        private void CancelPointer()
        {
            activeFinger = null;
            activeMouse = null;
            activeTouchId = 0;
            ResetDrag();
            SetFireHeld(false);
        }

        private void SetFireHeld(bool held)
        {
            if (pointerHeld == held) return;
            pointerHeld = held;
            NotifyFireIntentChanged();
        }

        private Vector2 DeltaFrom(Vector2 position)
        {
            if (!movementTracking) return Vector2.zero;
            var delta = position - previousPosition;
            previousPosition = position;
            return delta;
        }
    }
}
