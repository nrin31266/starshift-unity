using UnityEngine;
using UnityEngine.Rendering;

namespace StarShift.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-200)]
    [AddComponentMenu("StarShift/Gameplay/Gameplay View Controller")]
    public sealed class GameplayViewController : MonoBehaviour
    {
        [SerializeField, Tooltip("Sửa trong Play Mode để request view mới; không sửa trực tiếp Camera Size khi controller đang chạy.")]
        private GameplayViewSettings targetView = GameplayViewSettings.Baseline;
        [SerializeField, Min(0), Tooltip("Giây scaled time; smoothstep không overshoot. 0 = áp ngay.")]
        private float transitionDuration = .6f;

        [SerializeField, Range(.1f,.18f)] private float reserveImpactDuration = .14f;
        [SerializeField, Range(0,.032f), Tooltip("Biên độ theo tỷ lệ khung hình; chỉ lệch projection lúc render.")]
        private float reserveImpactStrength = .024f;
        private float impactRemaining;
        private float impactElapsed;
        private bool ownsProjection;
        public float ImpactRemaining => impactRemaining;
        public float ImpactStrength => reserveImpactStrength;
        private Camera gameplayCamera;
        private GameplayViewSettings transitionTarget;
        private GameplayViewSettings baselineView = GameplayViewSettings.Baseline;
        private float startSize;
        private Rect startViewport;
        private float elapsed;
        private float duration;

        public Rect CurrentPlayableViewport { get; private set; } = new Rect(0, 0, 1, 1);
        public GameplayViewSettings TargetView => targetView;
        public bool IsTransitioning { get; private set; }

        private void Awake()
        {
            gameplayCamera = GetComponent<Camera>();
            if (gameplayCamera.orthographic) return;
            Debug.LogError("GameplayViewController requires an orthographic camera.", this);
            enabled = false;
        }

        private void OnEnable()
        {
            if (gameplayCamera == null) gameplayCamera = GetComponent<Camera>();
            RequestView(targetView, 0);
            RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
            RenderPipelineManager.endCameraRendering += EndCameraRendering;
        }

        public void TriggerReserveImpact()
        {
            impactRemaining = reserveImpactDuration;
            impactElapsed = 0;
        }
        public void CancelImpact()
        {
            impactRemaining = impactElapsed = 0;
            if (gameplayCamera != null) gameplayCamera.ResetProjectionMatrix();
            ownsProjection = false;
        }
        private void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != gameplayCamera || impactRemaining <= 0 || Time.timeScale <= 0) return;
            var projection = camera.projectionMatrix;
            var fade = impactRemaining / reserveImpactDuration;
            var amplitude = Mathf.Clamp(reserveImpactStrength, 0, .032f) * fade * fade * 2;
            projection.m03 += Mathf.Sin(impactElapsed * 173 + 1.2f) * amplitude;
            projection.m13 += Mathf.Sin(impactElapsed * 131 + .5f) * amplitude;
            camera.projectionMatrix = projection;
            ownsProjection = true;
        }
        private void EndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != gameplayCamera || !ownsProjection) return;
            camera.ResetProjectionMatrix();
            ownsProjection = false;
        }
        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= EndCameraRendering;
            CancelImpact();
        }

        public void RequestView(GameplayViewSettings settings) => RequestView(settings, transitionDuration);

        public void RequestView(GameplayViewSettings settings, float seconds)
        {
            settings = settings.Validated();
            // Handoff và EncounterStarted có thể cùng request một đích; giữ nguyên đường zoom đang chạy.
            if (seconds > 0 && settings.OrthographicSize == transitionTarget.OrthographicSize &&
                settings.PlayableViewport == transitionTarget.PlayableViewport) return;
            targetView = settings;
            transitionTarget = targetView;
            startSize = gameplayCamera.orthographicSize;
            startViewport = CurrentPlayableViewport;
            elapsed = 0;
            duration = float.IsNaN(seconds) || float.IsInfinity(seconds) ? 0 : Mathf.Max(0, seconds);
            IsTransitioning = duration > 0;
            if (!IsTransitioning) Apply(1);
        }

        public void SetBaseline(GameplayViewSettings settings) => baselineView = settings.Validated();

        [ContextMenu("Return To Baseline (Play Mode)")]
        public void ReturnToBaseline()
        {
            if (Application.isPlaying) RequestView(baselineView);
        }

        private void Update()
        {
            if (impactRemaining > 0)
            {
                impactElapsed += Time.deltaTime;
                impactRemaining = Mathf.Max(0, impactRemaining - Time.deltaTime);
            }
            // Inspector dùng cùng đường request với gameplay caller; retarget từ state hiện tại.
            if (targetView.OrthographicSize != transitionTarget.OrthographicSize ||
                targetView.PlayableViewport != transitionTarget.PlayableViewport)
                RequestView(targetView);
            if (!IsTransitioning) return;
            elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
            Apply(Mathf.SmoothStep(0, 1, elapsed / duration));
            IsTransitioning = elapsed < duration;
        }

        private void Apply(float t)
        {
            gameplayCamera.orthographicSize = Mathf.Max(GameplayViewSettings.MinimumSize,
                Mathf.Lerp(startSize, transitionTarget.OrthographicSize, t));
            var target = transitionTarget.PlayableViewport;
            CurrentPlayableViewport = Rect.MinMaxRect(
                Mathf.Lerp(startViewport.xMin, target.xMin, t), Mathf.Lerp(startViewport.yMin, target.yMin, t),
                Mathf.Lerp(startViewport.xMax, target.xMax, t), Mathf.Lerp(startViewport.yMax, target.yMax, t));
        }

        private void OnValidate()
        {
            targetView = targetView.Validated();
            if (float.IsNaN(transitionDuration) || float.IsInfinity(transitionDuration)) transitionDuration = 0;
            transitionDuration = Mathf.Max(0, transitionDuration);
        }

        private void OnDrawGizmosSelected()
        {
            var camera = GetComponent<Camera>();
            if (!camera.orthographic) return;
            var previous = Gizmos.color;
            DrawRegion(camera, new Rect(0, 0, 1, 1), Color.white);
            DrawRegion(camera, Application.isPlaying ? CurrentPlayableViewport : targetView.Validated().PlayableViewport, Color.cyan);
            Gizmos.color = previous;
        }

        private static void DrawRegion(Camera camera, Rect region, Color color)
        {
            var depth = Mathf.Max(camera.nearClipPlane, Vector3.Dot(-camera.transform.position, camera.transform.forward));
            var a = camera.ViewportToWorldPoint(new Vector3(region.xMin, region.yMin, depth));
            var b = camera.ViewportToWorldPoint(new Vector3(region.xMax, region.yMin, depth));
            var c = camera.ViewportToWorldPoint(new Vector3(region.xMax, region.yMax, depth));
            var d = camera.ViewportToWorldPoint(new Vector3(region.xMin, region.yMax, depth));
            Gizmos.color = color;
            Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, c); Gizmos.DrawLine(c, d); Gizmos.DrawLine(d, a);
        }
    }
}
