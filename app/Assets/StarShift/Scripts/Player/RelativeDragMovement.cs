using UnityEngine;
using StarShift.Gameplay;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerMovementBounds))]
    [RequireComponent(typeof(PlayerGameplayGate))]
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
        private PlayerGameplayGate gameplayGate;
        private GameplayViewController gameplayView;
        private PlayerRespawnEntrance respawnEntrance;
        private Vector2 pendingPointerDelta;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            movementBounds = GetComponent<PlayerMovementBounds>();
            gameplayGate = GetComponent<PlayerGameplayGate>();
            respawnEntrance = GetComponent<PlayerRespawnEntrance>();
            if (movementCamera == null) movementCamera = Camera.main;
            if (movementCamera != null) gameplayView = movementCamera.GetComponent<GameplayViewController>();
            if (pointerInput == null || movementCamera == null || !movementCamera.orthographic)
            {
                Debug.LogError("RelativeDragMovement requires a pointer input and an orthographic gameplay camera.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (gameplayGate != null) gameplayGate.StateChanged += OnGameplayStateChanged;
            ResetInput();
        }

        private void OnDisable()
        {
            if (gameplayGate != null) gameplayGate.StateChanged -= OnGameplayStateChanged;
            ResetInput();
        }

        private void OnGameplayStateChanged()
        {
            if (!gameplayGate.CanInput) ResetInput();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) ResetInput();
        }

        private void Update()
        {
            if (!gameplayGate.CanInput || !pointerInput.isActiveAndEnabled || !movementCamera.isActiveAndEnabled)
            {
                ResetInput();
                return;
            }
            // Giữ pixel tới physics step để không dùng projection cũ sau zoom/aspect change.
            pendingPointerDelta += pointerInput.ReadDelta();
        }

        private void FixedUpdate()
        {
            if (respawnEntrance != null && respawnEntrance.IsEntering)
            {
                ResetInput();
                ApplyEntrancePosition();
                return;
            }
            if (!gameplayGate.CanInput)
            {
                ResetInput();
                return;
            }
            if (movementCamera == null || !movementCamera.isActiveAndEnabled ||
                movementCamera.pixelWidth <= 0 || movementCamera.pixelHeight <= 0)
            {
                ResetInput();
                return;
            }
            var worldHeight = 2f * movementCamera.orthographicSize;
            var horizontal = pendingPointerDelta.x / movementCamera.pixelWidth * worldHeight * movementCamera.aspect;
            var vertical = pendingPointerDelta.y / movementCamera.pixelHeight * worldHeight;
            var worldDelta = movementCamera.transform.right * horizontal + movementCamera.transform.up * vertical;
            var proposedPosition = body.position + (Vector2)worldDelta * sensitivity;
            body.position = movementBounds.isActiveAndEnabled
                ? movementBounds.ClampPosition(proposedPosition, movementCamera, PlayableViewport)
                : proposedPosition;
            // Bỏ delta vượt cạnh ngay; kéo ngược không phải bù phần đã bị chặn.
            pendingPointerDelta = Vector2.zero;
        }

        private Rect PlayableViewport => gameplayView != null
            ? gameplayView.CurrentPlayableViewport : new Rect(0, 0, 1, 1);

        private void LateUpdate()
        {
            if (body == null || movementCamera == null || !movementCamera.isActiveAndEnabled ||
                !movementBounds.isActiveAndEnabled) return;
            if (respawnEntrance != null && respawnEntrance.IsEntering)
            {
                ApplyEntrancePosition();
                return;
            }
            // Framing có thể co trong render frame hoặc khi gate khóa input; movement vẫn là writer duy nhất.
            var clamped = movementBounds.ClampPosition(body.position, movementCamera, PlayableViewport);
            if (clamped != body.position) body.position = clamped;
        }

        public void ResetForCheckpoint(PlayDirection direction)
        {
            ResetInput();
            var normalized = PlayableViewport.min + Vector2.Scale(PlayableViewport.size,
                PlayDirectionSpace.Point(new Vector2(.5f, .18f), direction));
            var depth = Vector3.Dot(transform.position - movementCamera.transform.position, movementCamera.transform.forward);
            body.position = movementBounds.ClampPosition(movementCamera.ViewportToWorldPoint(
                new Vector3(normalized.x, normalized.y, depth)), movementCamera, PlayableViewport);
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
            transform.position = new Vector3(body.position.x, body.position.y, transform.position.z);
        }

        internal void ApplyEntrancePosition()
        {
            if (movementCamera == null || !movementCamera.isActiveAndEnabled) return;
            body.position = respawnEntrance.GetPosition(movementCamera, movementBounds, PlayableViewport);
            transform.position = new Vector3(body.position.x, body.position.y, transform.position.z);
        }

        private void ResetInput()
        {
            pendingPointerDelta = Vector2.zero;
            if (pointerInput != null) pointerInput.ResetDrag();
        }
    }
}
