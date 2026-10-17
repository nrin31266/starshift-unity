using UnityEngine;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
    [AddComponentMenu("StarShift/Enemies/Entry Hold Flight")]
    public sealed class EntryHoldFlight : MonoBehaviour
    {
        [SerializeField, Tooltip("Để trống trên prefab để dùng Main Camera của scene.")]
        private Camera flightCamera;
        [SerializeField] private Vector2 entryViewport = new Vector2(.5f, 1.12f);
        [SerializeField] private Vector2 holdViewport = new Vector2(.5f, .78f);
        [SerializeField, Min(.01f)] private float entryDuration = 1.2f;
        [SerializeField, Tooltip("Envelope physics dùng giữ hold target trong camera; không đọc final sprite.")]
        private BoxCollider2D collisionShape;

        private Rigidbody2D body;
        private EnemyHealth health;
        private float elapsed;
        private Vector2 entryCurve;
        private Vector2 exitViewport;
        private float holdDuration;
        private float exitDuration;

        public bool IsHolding => elapsed >= entryDuration && (exitDuration <= 0 || elapsed < entryDuration + holdDuration);
        public bool IsExiting => exitDuration > 0 && elapsed >= entryDuration + holdDuration;
        public bool HasExited => exitDuration > 0 && elapsed >= entryDuration + holdDuration + exitDuration;
        public bool CanShoot => elapsed >= entryDuration && !HasExited && IsPointVisible(transform.position);
        public bool IsOutsideView
        {
            get
            {
                if (flightCamera == null || !flightCamera.isActiveAndEnabled) return true;
                GetViewportEnvelope(out var minimum, out var maximum);
                return maximum.x < 0 || minimum.x > 1 || maximum.y < 0 || minimum.y > 1;
            }
        }

        public bool IsPointVisible(Vector3 point)
        {
            if (flightCamera == null || !flightCamera.isActiveAndEnabled) return false;
            var viewport = flightCamera.WorldToViewportPoint(point);
            return viewport.z >= flightCamera.nearClipPlane && viewport.z <= flightCamera.farClipPlane &&
                viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1;
        }

        public void Configure(Vector2 entry, Vector2 hold, Vector2 curve, float entrySeconds,
            Vector2 exit, float holdSeconds, float exitSeconds)
        {
            entryViewport = entry;
            holdViewport = hold;
            entryCurve = curve;
            entryDuration = Mathf.Max(.01f, entrySeconds);
            exitViewport = exit;
            holdDuration = Mathf.Max(0, holdSeconds);
            exitDuration = Mathf.Max(0, exitSeconds);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<EnemyHealth>();
            if (flightCamera == null) flightCamera = Camera.main;
            if (flightCamera != null && flightCamera.orthographic && collisionShape != null) return;
            Debug.LogError("EntryHoldFlight requires an orthographic camera and collision envelope.", this);
            enabled = false;
        }

        private void OnEnable()
        {
            elapsed = 0;
            ApplyPosition();
        }

        private void FixedUpdate()
        {
            if (!health.IsAlive) return;
            elapsed = Mathf.Min(entryDuration + holdDuration + exitDuration, elapsed + Time.fixedDeltaTime);
            ApplyPosition();
        }

        private void LateUpdate()
        {
            // Reproject cùng phase sau camera Update; không thêm clock thứ hai.
            if (health.IsAlive) ApplyPosition();
        }

        private void ApplyPosition()
        {
            if (flightCamera == null || !flightCamera.isActiveAndEnabled) return;
            var depth = Vector3.Dot(transform.position - flightCamera.transform.position, flightCamera.transform.forward);
            var hold = new Vector3(holdViewport.x, holdViewport.y, depth);
            var rootViewport = flightCamera.WorldToViewportPoint(transform.position);
            GetViewportEnvelope(out var minimum, out var maximum);
            minimum -= (Vector2)rootViewport;
            maximum -= (Vector2)rootViewport;
            hold.x = ClampAxis(hold.x, -minimum.x, 1 - maximum.x);
            hold.y = ClampAxis(hold.y, -minimum.y, 1 - maximum.y);
            var t = Mathf.SmoothStep(0, 1, elapsed / entryDuration);
            var viewport = Vector2.Lerp(entryViewport, (Vector2)hold, t) + entryCurve * (4 * t * (1 - t));
            if (exitDuration > 0 && elapsed > entryDuration + holdDuration)
                viewport = Vector2.Lerp((Vector2)hold, exitViewport,
                    Mathf.SmoothStep(0, 1, (elapsed - entryDuration - holdDuration) / exitDuration));
            var position = flightCamera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth));
            body.position = position;
            // Kinematic pose phải hiện ngay trong render frame, không chờ physics step kế tiếp.
            transform.position = position;
        }

        private static float ClampAxis(float value, float minimum, float maximum)
            => minimum <= maximum ? Mathf.Clamp(value, minimum, maximum) : (minimum + maximum) * .5f;

        private void GetViewportEnvelope(out Vector2 minimum, out Vector2 maximum)
        {
            minimum = Vector2.positiveInfinity;
            maximum = Vector2.negativeInfinity;
            var half = collisionShape.size * .5f;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                var local = collisionShape.offset + new Vector2(x * half.x, y * half.y);
                var viewport = (Vector2)flightCamera.WorldToViewportPoint(collisionShape.transform.TransformPoint(local));
                minimum = Vector2.Min(minimum, viewport);
                maximum = Vector2.Max(maximum, viewport);
            }
        }

        private void OnValidate()
        {
            entryDuration = Mathf.Max(.01f, entryDuration);
            holdViewport = Vector2.Min(Vector2.one, Vector2.Max(Vector2.zero, holdViewport));
        }
    }
}
