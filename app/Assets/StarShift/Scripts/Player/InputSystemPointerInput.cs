using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("StarShift/Player/Input System Pointer Input")]
    public sealed class InputSystemPointerInput : PlayerPointerInput
    {
        private Finger activeFinger;
        private int activeTouchId;
        private Mouse activeMouse;
        private Vector2 previousPosition;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            ResetDrag();
        }

        private void OnDisable()
        {
            ResetDrag();
            EnhancedTouchSupport.Disable();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) ResetDrag();
        }

        public override Vector2 ReadDelta()
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
                        ResetDrag();
                        return Vector2.zero;
                    }
                    return DeltaFrom(touch.screenPosition);
                }
                ResetDrag();
                return Vector2.zero;
            }

            if (touches.Count > 0)
            {
                activeMouse = null;
                foreach (var touch in touches)
                {
                    if (touch.phase != TouchPhase.Began) continue;
                    activeFinger = touch.finger;
                    activeTouchId = touch.touchId;
                    previousPosition = touch.screenPosition;
                    break;
                }
                // Touch mới chỉ đặt mốc; không kéo tàu tới điểm chạm.
                return Vector2.zero;
            }

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.isPressed)
            {
                ResetDrag();
                return Vector2.zero;
            }
            if (activeMouse != mouse)
            {
                // Sau disable/focus loss phải có press mới, không nhận lại drag đang giữ.
                if (!mouse.leftButton.wasPressedThisFrame) return Vector2.zero;
                activeMouse = mouse;
                previousPosition = mouse.position.ReadValue();
                return Vector2.zero;
            }
            return DeltaFrom(mouse.position.ReadValue());
        }

        public override void ResetDrag()
        {
            activeFinger = null;
            activeMouse = null;
            activeTouchId = 0;
            previousPosition = Vector2.zero;
        }

        private Vector2 DeltaFrom(Vector2 position)
        {
            var delta = position - previousPosition;
            previousPosition = position;
            return delta;
        }
    }
}
