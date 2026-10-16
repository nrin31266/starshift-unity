using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerMovementBounds))]
    [AddComponentMenu("StarShift/Player/Relative Drag Movement")]
    public sealed class RelativeDragMovement : MonoBehaviour
    {
        [SerializeField, Tooltip("Nguồn delta pointer; movement không đọc device trực tiếp.")]
        private PlayerPointerInput pointerInput;

        [SerializeField, Tooltip("Camera gameplay orthographic; để trống trên prefab sẽ dùng Main Camera.")]
        private Camera movementCamera;

        [SerializeField, Min(0), Tooltip("1 = tàu dịch chuyển cùng số pixel với drag. Seed để test feel.")]
        private float sensitivity = 1f;

        private Rigidbody2D body;
        private PlayerMovementBounds movementBounds;
        private Vector2 pendingDisplacement;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            movementBounds = GetComponent<PlayerMovementBounds>();
            if (movementCamera == null) movementCamera = Camera.main;
            if (pointerInput == null || movementCamera == null || !movementCamera.orthographic)
            {
                Debug.LogError("RelativeDragMovement requires a pointer input and an orthographic gameplay camera.", this);
                enabled = false;
            }
        }

        private void OnEnable() => ResetInput();
        private void OnDisable() => ResetInput();

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) ResetInput();
        }

        private void Update()
        {
            if (!pointerInput.isActiveAndEnabled || !movementCamera.isActiveAndEnabled || Time.timeScale <= 0)
            {
                ResetInput();
                return;
            }
            var delta = pointerInput.ReadDelta();
            if (movementCamera.pixelWidth <= 0 || movementCamera.pixelHeight <= 0) return;
            var worldHeight = 2f * movementCamera.orthographicSize;
            var horizontal = delta.x / movementCamera.pixelWidth * worldHeight * movementCamera.aspect;
            var vertical = delta.y / movementCamera.pixelHeight * worldHeight;
            var worldDelta = movementCamera.transform.right * horizontal + movementCamera.transform.up * vertical;
            // Gom delta qua các render frame; không nhân deltaTime vì đây là displacement.
            pendingDisplacement += (Vector2)worldDelta * sensitivity;
        }

        private void FixedUpdate()
        {
            var proposedPosition = body.position + pendingDisplacement;
            body.position = movementBounds.isActiveAndEnabled
                ? movementBounds.ClampPosition(proposedPosition, movementCamera)
                : proposedPosition;
            // Bỏ delta vượt cạnh ngay; kéo ngược không phải bù phần đã bị chặn.
            pendingDisplacement = Vector2.zero;
        }

        private void ResetInput()
        {
            pendingDisplacement = Vector2.zero;
            if (pointerInput != null) pointerInput.ResetDrag();
        }
    }
}
