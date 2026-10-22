// EntryHoldFlight — Đường bay và đội hình địch.
using UnityEngine;
using StarShift.Encounters;
using StarShift.Gameplay;

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
        private bool continuousTransit;

        private FlightSequence sequence;
        private Vector2 slotOffset;
        private PlayDirection direction;
        private float timelineOffset;
        private Vector2? entryOrigin;
        public void SetEntryOrigin(Vector2 viewport) { entryOrigin = viewport; ApplyPosition(); }
        public PlayDirection Direction => direction;
        private Vector2 strikeFrom, strikeTarget;
        private float strikeElapsed, strikeSeconds, returnSeconds;
        public bool IsStriking { get; private set; }
        public void BeginStrike(Vector2 target, float seconds, float returnDuration)
        {
            strikeFrom = flightCamera.WorldToViewportPoint(transform.position);
            strikeTarget = target + (target - strikeFrom).normalized * .12f;
            strikeSeconds = seconds; returnSeconds = returnDuration; strikeElapsed = 0; IsStriking = true;
        }
        private int phaseIndex;
        private bool sequenceFinished;
        private Vector2 combatOffset;
        private Vector2 offsetFrom;
        private Vector2 offsetTarget;
        private float offsetElapsed;
        private float offsetSeconds;
        private float motionMultiplier = 1;
        public bool MotionPaused { get; private set; }
        public void SetMotionPaused(bool paused) => MotionPaused = paused;
        public void Reposition(Vector2 offset, float seconds, float motionScale = 1)
        { offsetFrom = combatOffset; offsetTarget = offset; offsetElapsed = 0; offsetSeconds = Mathf.Max(.01f, seconds); motionMultiplier = motionScale; }
        public void BeginRetreat(float seconds = 1.1f)
        {
            var start = (Vector2)flightCamera.WorldToViewportPoint(transform.position);
            // Rút theo hướng đi ra phía trên đội hình, giữ flight là pose writer duy nhất.
            var end = direction == PlayDirection.Right ? new Vector2(1.3f, start.y) : new Vector2(start.x, 1.3f);
            Configure(start, start, Vector2.zero, .01f, end, 0, seconds, true);
            elapsed = 0; MotionPaused = false;
        }
        public int PhaseIndex => sequence != null ? phaseIndex : elapsed < entryDuration ? 0 : 2;

        public bool IsHolding => sequence != null ? !sequenceFinished && (sequence.Phases[phaseIndex].Kind == FlightPhaseKind.Hold || sequence.Phases[phaseIndex].Kind == FlightPhaseKind.Combat) : elapsed >= entryDuration && (exitDuration <= 0 || elapsed < entryDuration + holdDuration);
        public bool IsExiting => sequence != null ? sequence.Phases[phaseIndex].Kind == FlightPhaseKind.Exit : exitDuration > 0 && elapsed >= entryDuration + holdDuration;
        public bool HasExited => sequence != null ? sequenceFinished : exitDuration > 0 && elapsed >= entryDuration + holdDuration + exitDuration;
        // Đường bay phải cho phép và địch phải nằm trong vùng nhìn mới được bắn.
        public bool CanShoot => (sequence != null ? !sequenceFinished && sequence.Phases[phaseIndex].CanShoot : elapsed >= entryDuration && !HasExited) && IsPointVisible(transform.position);
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

        // Gán các mốc vào, giữ và ra; thiên thạch có thể dùng chế độ bay xuyên liên tục.
        public void Configure(Vector2 entry, Vector2 hold, Vector2 curve, float entrySeconds,
            Vector2 exit, float holdSeconds, float exitSeconds, bool continuous = false)
        {
            IsStriking = false;
            sequence = null;
            continuousTransit = continuous;
            entryViewport = entry;
            holdViewport = hold;
            entryCurve = curve;
            entryDuration = Mathf.Max(.01f, entrySeconds);
            exitViewport = exit;
            holdDuration = Mathf.Max(0, holdSeconds);
            exitDuration = Mathf.Max(0, exitSeconds);
        }

        // Nhận chuỗi pha bay cùng độ lệch vị trí đội hình; không sửa asset đường bay dùng chung.
        public void ConfigureSequence(FlightSequence config, Vector2 offset, PlayDirection playDirection, float encounterTime = 0)
        {
            sequence = config;
            continuousTransit = false;
            slotOffset = offset;
            direction = playDirection;
            timelineOffset = encounterTime;
            phaseIndex = 0;
            sequenceFinished = false;
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
            phaseIndex = 0;
            sequenceFinished = false;
            combatOffset = offsetFrom = offsetTarget = Vector2.zero;
            offsetElapsed = offsetSeconds = 0; motionMultiplier = 1;
            MotionPaused = IsStriking = false;
            entryOrigin = null;
            ApplyPosition();
        }

        private void FixedUpdate()
        {
            if (!health.IsAlive || MotionPaused) return;
            if (IsStriking)
            {
                strikeElapsed += Time.fixedDeltaTime;
                if (strikeElapsed >= strikeSeconds + returnSeconds) IsStriking = false;
                ApplyPosition(); return;
            }
            if (offsetElapsed < offsetSeconds)
            { offsetElapsed += Time.fixedDeltaTime; combatOffset = Vector2.Lerp(offsetFrom,offsetTarget,Mathf.SmoothStep(0,1,offsetElapsed/offsetSeconds)); }
            elapsed = sequence != null ? elapsed + Time.fixedDeltaTime : Mathf.Min(entryDuration + holdDuration + exitDuration, elapsed + Time.fixedDeltaTime);
            ApplyPosition();
        }

        private void LateUpdate()
        {
            // Reproject cùng phase sau camera Update; không thêm clock thứ hai.
            if (health.IsAlive) ApplyPosition();
        }

        // Tính vị trí theo pha bay hiện tại; luồng thiên thạch liên tục không dừng tại mốc giữa.
        private void ApplyPosition()
        {
            if (flightCamera == null || !flightCamera.isActiveAndEnabled) return;
            if (IsStriking)
            {
                var point = strikeElapsed <= strikeSeconds
                    ? Vector2.Lerp(strikeFrom, strikeTarget, strikeElapsed / strikeSeconds)
                    : Vector2.Lerp(strikeTarget, strikeFrom, Mathf.SmoothStep(0, 1, (strikeElapsed - strikeSeconds) / returnSeconds));
                var world = flightCamera.ViewportToWorldPoint(new Vector3(point.x, point.y, -flightCamera.transform.position.z));
                body.position = world; transform.position = world; return;
            }
            if (sequence != null) { ApplySequencePosition(); return; }
            var depth = Vector3.Dot(transform.position - flightCamera.transform.position, flightCamera.transform.forward);
            if (continuousTransit)
            {
                // Luồng xuyên màn hình không clamp mốc giữa hoặc dừng ở biên viewport.
                var transit = Vector2.Lerp(entryViewport, exitViewport, elapsed / (entryDuration + holdDuration + exitDuration));
                var world = flightCamera.ViewportToWorldPoint(new Vector3(transit.x, transit.y, depth));
                body.position = world;
                transform.position = world;
                return;
            }
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

        private void ApplySequencePosition()
        {
            float clock = elapsed;
            phaseIndex = 0;
            while (phaseIndex < sequence.Phases.Length - 1 && clock >= sequence.Phases[phaseIndex].Duration)
            {
                clock -= sequence.Phases[phaseIndex].Duration;
                phaseIndex++;
            }
            var phase = sequence.Phases[phaseIndex];
            sequenceFinished = phaseIndex == sequence.Phases.Length - 1 && !phase.HoldUntilResolved && clock >= phase.Duration;
            var depth = Vector3.Dot(transform.position - flightCamera.transform.position, flightCamera.transform.forward);
            Vector2 Project(Vector2 point, bool clamp)
            {
                var result = PlayDirectionSpace.Point(point + slotOffset + combatOffset, direction);
                if (!clamp) return result;
                var root = (Vector2)flightCamera.WorldToViewportPoint(transform.position);
                GetViewportEnvelope(out var min, out var max);
                min -= root; max -= root;
                return new Vector2(ClampAxis(result.x, -min.x, 1-max.x), ClampAxis(result.y, -min.y, 1-max.y));
            }
            // Clamp cùng điểm nối để entry/hold/exit không nhảy khi camera đổi framing.
            bool fromClamp = phase.ClampToView || (phaseIndex > 0 && sequence.Phases[phaseIndex-1].ClampToView);
            bool toClamp = phase.ClampToView || (phaseIndex + 1 < sequence.Phases.Length && sequence.Phases[phaseIndex+1].ClampToView);
            var from = Project(phase.From, fromClamp);
            if (phaseIndex == 0 && entryOrigin.HasValue) from = entryOrigin.Value;
            var to = Project(phase.To, toClamp);
            var progress = phase.RepeatMotion ? Mathf.Repeat((clock + (phase.MotionPeriod > 0 ? timelineOffset : 0)) / (phase.MotionPeriod > 0 ? phase.MotionPeriod : phase.Duration), 1) : clock / phase.Duration;
            var local = phase.Evaluate(progress, phase.From, phase.To);
            var linear = Vector2.Lerp(phase.From, phase.To, phase.Smooth ? Mathf.SmoothStep(0,1,progress) : Mathf.Clamp01(progress));
            var t = phase.Smooth ? Mathf.SmoothStep(0,1,progress) : Mathf.Clamp01(progress);
            var blend = phase.MotionPeriod > 0 ? Mathf.SmoothStep(0, 1, clock / .6f) : 1;
            var viewport = Vector2.Lerp(from, to, t) + PlayDirectionSpace.Offset((local - linear) * (motionMultiplier * blend), direction);
            var position = flightCamera.ViewportToWorldPoint(new Vector3(viewport.x, viewport.y, depth));
            body.position = position;
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
