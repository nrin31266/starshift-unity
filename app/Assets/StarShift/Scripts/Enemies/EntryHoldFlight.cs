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

        public bool IsHolding => elapsed >= entryDuration;

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
            elapsed = Mathf.Min(entryDuration, elapsed + Time.fixedDeltaTime);
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
            var entry = flightCamera.ViewportToWorldPoint(new Vector3(entryViewport.x, entryViewport.y, depth));
            var hold = new Vector3(holdViewport.x, holdViewport.y, depth);
            var rootViewport = flightCamera.WorldToViewportPoint(transform.position);
            var minimum = Vector2.positiveInfinity;
            var maximum = Vector2.negativeInfinity;
            var half = collisionShape.size * .5f;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                var local = collisionShape.offset + new Vector2(x * half.x, y * half.y);
                var offset = (Vector2)(flightCamera.WorldToViewportPoint(collisionShape.transform.TransformPoint(local)) - rootViewport);
                minimum = Vector2.Min(minimum, offset);
                maximum = Vector2.Max(maximum, offset);
            }
            hold.x = ClampAxis(hold.x, -minimum.x, 1 - maximum.x);
            hold.y = ClampAxis(hold.y, -minimum.y, 1 - maximum.y);
            var t = Mathf.SmoothStep(0, 1, elapsed / entryDuration);
            var position = Vector3.Lerp(entry, flightCamera.ViewportToWorldPoint(hold), t);
            body.position = position;
            // Kinematic pose phải hiện ngay trong render frame, không chờ physics step kế tiếp.
            transform.position = position;
        }

        private static float ClampAxis(float value, float minimum, float maximum)
            => minimum <= maximum ? Mathf.Clamp(value, minimum, maximum) : (minimum + maximum) * .5f;

        private void OnValidate()
        {
            entryDuration = Mathf.Max(.01f, entryDuration);
            holdViewport = Vector2.Min(Vector2.one, Vector2.Max(Vector2.zero, holdViewport));
        }
    }
}
