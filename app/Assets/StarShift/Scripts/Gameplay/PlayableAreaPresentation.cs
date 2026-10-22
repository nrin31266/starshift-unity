using StarShift.Player;
using UnityEngine;

namespace StarShift.Gameplay
{
    [DisallowMultipleComponent, RequireComponent(typeof(GameplayViewController))]
    public sealed class PlayableAreaPresentation : MonoBehaviour
    {
        [SerializeField] private RelativeDragMovement movement;
        [SerializeField] private Color hintColor = new Color(.45f, .72f, .80f, .24f);
        [SerializeField, Min(.05f)] private float holdSeconds = .2f;
        [SerializeField, Range(.05f, .25f)] private float segmentLength = .14f;
        private Camera gameplayCamera;
        private GameplayViewController view;
        private SpriteRenderer hint;
        private Sprite rectangle;
        private float pressedSeconds, opacity;

        private void Awake()
        {
            gameplayCamera = GetComponent<Camera>(); view = GetComponent<GameplayViewController>();
            if (movement == null) { enabled = false; return; }
            rectangle = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * .5f, Texture2D.whiteTexture.width);
            var child = new GameObject("BoundaryHint"); child.transform.SetParent(transform, false);
            hint = child.AddComponent<SpriteRenderer>(); hint.sprite = rectangle; hint.sortingOrder = -5; hint.enabled = false;
        }

        // Chỉ vẽ gợi ý khi giữ kéo vào mép trên/phải bị hạn chế; việc chặn tàu thuộc PlayerMovementBounds.
        private void LateUpdate()
        {
            if (hint == null || Time.timeScale <= 0) return;
            var region = view.CurrentPlayableViewport;
            var pressure = movement.BoundaryPressure;
            var right = region.xMax < .999f && pressure.x > .1f;
            var top = region.yMax < .999f && pressure.y > .1f;
            var pressed = !view.IsTransitioning && (right || top);
            pressedSeconds = pressed ? pressedSeconds + Time.deltaTime : 0;
            var target = pressedSeconds >= holdSeconds ? .45f + .55f * Mathf.Pow(Mathf.Sin((pressedSeconds - holdSeconds) * 8), 2) : 0;
            opacity = Mathf.MoveTowards(opacity, target, Time.deltaTime / .12f);
            hint.enabled = opacity > .001f;
            if (!hint.enabled) return;
            if (pressed)
            {
                var player = gameplayCamera.WorldToViewportPoint(movement.transform.position);
                var half = segmentLength * .5f;
                var center = Mathf.Clamp(right ? player.y : player.x, half, 1 - half);
                var depth = -gameplayCamera.transform.position.z;
                var a = gameplayCamera.ViewportToWorldPoint(right ? new Vector3(region.xMax, center - half, depth) : new Vector3(center - half, region.yMax, depth));
                var b = gameplayCamera.ViewportToWorldPoint(right ? new Vector3(region.xMax, center + half, depth) : new Vector3(center + half, region.yMax, depth));
                var thickness = 2 * gameplayCamera.orthographicSize / Mathf.Max(1, gameplayCamera.pixelHeight) * 2;
                hint.transform.position = (a + b) * .5f;
                hint.transform.localScale = new Vector3(Mathf.Max(thickness, b.x - a.x), Mathf.Max(thickness, b.y - a.y), 1);
            }
            var color = hintColor; color.a *= opacity; hint.color = color;
        }

        private void OnDisable() { pressedSeconds = opacity = 0; if (hint != null) hint.enabled = false; }
        private void OnDestroy() { if (rectangle != null) Destroy(rectangle); }
    }
}
