// RelativeDragMovement — Di chuyển tàu bằng độ kéo; đổi tọa độ màn hình sang thế giới và giới hạn vùng chơi.
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
        // Độ nhạy kéo: cùng độ lệch tay, giá trị lớn hơn làm tàu dịch chuyển xa hơn.
        private float sensitivity = 1f;

        private Rigidbody2D body;
        private PlayerMovementBounds movementBounds;
        private PlayerGameplayGate gameplayGate;
        private GameplayViewController gameplayView;
        private PlayerRespawnEntrance respawnEntrance;
        // Gom độ kéo từ khung hình để bước vật lý dùng một lần, tránh mất input giữa các nhịp cập nhật.
        private Vector2 pendingPointerDelta;
        private Vector2 travelBias, appliedTravelBias;
        public Vector2 BoundaryPressure { get; private set; }
        public void SetTravelBias(Vector2 normalizedOffset) => travelBias = normalizedOffset;

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

        // Áp dụng độ kéo ở bước vật lý; khi hồi sinh dùng vị trí đường bay thay cho input.
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
            // Quy đổi theo kích thước camera để cùng độ kéo trên màn hình giữ cảm giác tương đương khi đổi zoom.
            var worldHeight = 2f * movementCamera.orthographicSize;
            var horizontal = pendingPointerDelta.x / movementCamera.pixelWidth * worldHeight * movementCamera.aspect;
            var vertical = pendingPointerDelta.y / movementCamera.pixelHeight * worldHeight;
            var worldDelta = movementCamera.transform.right * horizontal + movementCamera.transform.up * vertical;
            var biasDelta = travelBias - appliedTravelBias;
            appliedTravelBias = travelBias;
            var travelDelta = new Vector2(biasDelta.x * worldHeight * movementCamera.aspect, biasDelta.y * worldHeight);
            // Tính vị trí muốn tới trước; ClampPosition bên dưới cắt phần vượt vùng thay vì cho tàu ra ngoài.
            var proposedPosition = body.position + (Vector2)worldDelta * sensitivity + travelDelta;
            body.position = movementBounds.isActiveAndEnabled
                ? movementBounds.ClampPosition(proposedPosition, movementCamera, PlayableViewport)
                : proposedPosition;
            var requested = (Vector2)worldDelta * sensitivity;
            var excess = proposedPosition - body.position;
            if (!pointerInput.FireHeld || !movementBounds.isActiveAndEnabled) BoundaryPressure = Vector2.zero;
            else if (requested.sqrMagnitude > .000001f)
                BoundaryPressure = Vector2.Dot(requested, excess) > .000001f ? excess.normalized : Vector2.zero;
            else if (BoundaryPressure != Vector2.zero)
            {
                // Giữ phản hồi khi còn giữ tay; mở rộng camera phải bỏ áp lực cạnh cũ.
                var probe = body.position + BoundaryPressure * .01f;
                if ((probe - movementBounds.ClampPosition(probe, movementCamera, PlayableViewport)).sqrMagnitude < .000001f)
                    BoundaryPressure = Vector2.zero;
            }
            // Bỏ delta vượt cạnh ngay; kéo ngược không phải bù phần đã bị chặn.
            pendingPointerDelta = Vector2.zero;
        }

        // Lấy vùng đang cho phép từ camera; chỉ dùng toàn màn hình khi không có view controller.
        private Rect PlayableViewport => gameplayView != null
            ? gameplayView.CurrentPlayableViewport : new Rect(0, 0, 1, 1);

        // Kẹp lại sau thay đổi camera/vùng bay trong khung hình, kể cả lúc không nhận input.
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
            // Vùng vừa co lại có thể bao ngoài vị trí cũ của tàu; cập nhật về vị trí hợp lệ ngay.
            var clamped = movementBounds.ClampPosition(body.position, movementCamera, PlayableViewport);
            if (clamped != body.position) body.position = clamped;
        }

        public void SetCinematicPosition(Vector3 position)
        {
            travelBias = appliedTravelBias = Vector2.zero;
            body.position = position; body.linearVelocity = Vector2.zero; transform.position = position;
        }
        public Vector2 CheckpointPosition(PlayDirection direction)
        {
            var normalized = PlayableViewport.min + Vector2.Scale(PlayableViewport.size,
                PlayDirectionSpace.Point(new Vector2(.5f, .18f), direction));
            var depth = Vector3.Dot(transform.position - movementCamera.transform.position, movementCamera.transform.forward);
            return movementBounds.ClampPosition(movementCamera.ViewportToWorldPoint(
                new Vector3(normalized.x, normalized.y, depth)), movementCamera, PlayableViewport);
        }
        // Xóa vận tốc và input cũ trước khi đặt tàu về vị trí bắt đầu lại đợt.
        public void ResetForCheckpoint(PlayDirection direction)
        {
            travelBias = appliedTravelBias = Vector2.zero;
            ResetInput();
            body.position = CheckpointPosition(direction);
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
            BoundaryPressure = Vector2.zero;
            if (pointerInput != null) pointerInput.ResetDrag();
        }
    }
}
