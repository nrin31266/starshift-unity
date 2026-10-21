using StarShift.Gameplay;
using StarShift.Player;
using UnityEngine;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent, DefaultExecutionOrder(40)]
    public sealed class DirectedAttack : MonoBehaviour
    {
        [SerializeField] private DirectedAttackDefinition definition;
        [SerializeField] private Transform muzzle;
        [SerializeField] private AttackBeamPresentation presentation;
        private EnemyHealth health;
        private EntryHoldFlight flight;
        private EnemyFireGate gate;
        private PlayerGameplayGate player;
        private Camera view;
        private BeamDamageQuery beamDamage;
        private Vector2 aim;
        private float elapsed;
        private float cooldown;
        public bool IsBusy { get; private set; }
        public bool IsActive { get; private set; }
        public int AttacksFired { get; private set; }
        // Phase chỉ đọc cho presentation; damage vẫn do ActiveSeconds/tracking hiện có quyết.
        public AttackPhase Phase { get; private set; }
        public event System.Action<AttackPhase> PhaseChanged;
        private void SetPhase(AttackPhase next)
        {
            if (Phase == next) return;
            Phase = next;
            PhaseChanged?.Invoke(next);
        }
        public Bounds DamageBounds { get; private set; }
        // Aim đã khóa (viewport) để telegraph strike bám đúng điểm lao.
        public Vector2 AttackAim => aim;
        public DirectedAttackDefinition Definition => definition;
        private void Awake()
        {
            health = GetComponentInParent<EnemyHealth>();
            flight = GetComponentInParent<EntryHoldFlight>();
            gate = FindAnyObjectByType<EnemyFireGate>();
            player = FindAnyObjectByType<PlayerGameplayGate>();
            view = Camera.main;
            if (definition == null || !definition.IsValid || muzzle == null || presentation == null || health == null || flight == null || gate == null || player == null || view == null)
                throw new System.InvalidOperationException("Invalid directed attack composition: " + name);
            beamDamage = new BeamDamageQuery(player);
        }
        private bool Eligible => isActiveAndEnabled && health.IsAlive && gate.CanFire && flight.CanShoot && flight.IsHolding && !flight.IsStriking && flight.IsPointVisible(muzzle.position);
        public bool TryRequestAttack()
        {
            if (IsBusy || recovery > 0 || cooldown > 0 || !Eligible) return false;
            IsBusy = true; IsActive = false; elapsed = 0;
            aim = view.WorldToViewportPoint(player.transform.position);
            SetPhase(AttackPhase.Prepare);
            return true;
        }
        private void OnEnable() { AttacksFired = 0; DamageBounds = default; recovery = 0; SetPhase(AttackPhase.Idle); CancelAttack(); }
        private void Recover()
        {
            // Giữ cue thêm .18 s để đọc pha hồi; damage đã dừng cùng ActiveSeconds.
            recovery = recoverySeconds;
            SetPhase(AttackPhase.Recovery);
            presentation.Hide();
            IsBusy = IsActive = false;
            cooldown = definition != null ? definition.CooldownSeconds : 1;
        }
        [SerializeField, Min(0)] private float recoverySeconds = .18f;
        private float recovery;
        public void CancelAttack()
        {
            beamDamage?.Reset();
            IsBusy = IsActive = false;
            if (Phase != AttackPhase.Idle) SetPhase(AttackPhase.Idle);
            cooldown = definition != null ? (AttacksFired == 0 ? Mathf.Min(.35f, definition.CooldownSeconds) : definition.CooldownSeconds) : 1;
            if (presentation != null) presentation.Hide();
        }
        private void FixedUpdate()
        {
            cooldown = Mathf.Max(0, cooldown - Time.fixedDeltaTime);
            if (recovery > 0)
            {
                recovery = Mathf.Max(0, recovery - Time.fixedDeltaTime);
                if (recovery <= 0 && !IsBusy) SetPhase(AttackPhase.Idle);
            }
            if (!IsBusy) { beamDamage.Reset(); return; }
            if (!health.IsAlive || !gate.CanFire || !isActiveAndEnabled || flight.IsExiting) { CancelAttack(); return; }
            elapsed += Time.fixedDeltaTime;
            if (!IsActive && elapsed <= definition.TrackingSeconds) aim = view.WorldToViewportPoint(player.transform.position);
            // LOCK: hết cửa sổ tracking, aim đã khóa; ATTACK bắt đầu ở ChargeSeconds.
            if (!IsActive && elapsed > definition.TrackingSeconds && elapsed < definition.ChargeSeconds) SetPhase(AttackPhase.Lock);
            if (!IsActive && elapsed >= definition.ChargeSeconds)
            {
                IsActive = true; elapsed = 0; AttacksFired++;
                SetPhase(AttackPhase.Attack);
                if (definition.Kind == DirectedAttackKind.Strike)
                {
                    presentation.Hide();
                    flight.BeginStrike(aim, definition.ActiveSeconds, definition.ReturnSeconds);
                }
            }
            if (definition.Kind == DirectedAttackKind.Strike && IsActive)
            {
                if (!flight.IsStriking)
                {
                    // Strike xong, Interceptor quay về: chuyển sang RECOVERY thay vì cắt cue tức thời.
                    Recover();
                }
                return;
            }
            if (definition.Kind == DirectedAttackKind.Strike) { presentation.Hide(); return; }
            var from = (Vector2)view.WorldToViewportPoint(muzzle.position);
            var to = aim;
            if (definition.Sweep)
            {
                var reverse = definition.AlternateSweep && AttacksFired % 2 == (IsActive ? 0 : 1);
                float progress = IsActive ? elapsed / definition.ActiveSeconds : 0;
                float lane = Mathf.Lerp(definition.SweepLanes.x, definition.SweepLanes.y, reverse ? 1 - progress : progress);
                if (flight.Direction == PlayDirection.Right) { from.y = lane; to = new Vector2(-.05f, lane); }
                else { from.x = lane; to = new Vector2(lane, -.05f); }
            }
            else if (definition.Kind == DirectedAttackKind.Beam)
            {
                var delta = (to - from).normalized;
                to = from + delta * 2;
            }
            var a = view.ViewportToWorldPoint(new Vector3(from.x, from.y, -view.transform.position.z));
            var b = view.ViewportToWorldPoint(new Vector3(to.x, to.y, -view.transform.position.z));
            a.z = b.z = 0;
            presentation.ShowSegment(a, b, definition.BeamWidth, IsActive, IsActive || elapsed >= definition.TrackingSeconds);
            if (definition.Sweep)
            {
                var along = flight.Direction == PlayDirection.Right ? Vector3.up : Vector3.right;
                if (definition.AlternateSweep && AttacksFired % 2 == 1) along = -along;
                var extent = (definition.SweepLanes.y - definition.SweepLanes.x) * view.orthographicSize * 2 * (flight.Direction == PlayDirection.Right ? 1 : view.aspect);
                var hint = a + (b - a).normalized * .65f;
                presentation.ShowSweepDirection(hint, hint + along * extent, !IsActive);
            }
            if (IsActive)
            {
                var delta = b - a;
                var size = new Vector2(delta.magnitude, definition.BeamWidth);
                var center = (a + b) * .5f;
                DamageBounds = new Bounds(center, new Vector3(Mathf.Abs(delta.x) + definition.BeamWidth, Mathf.Abs(delta.y) + definition.BeamWidth, 1));
                beamDamage.Apply(center, size, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, health.gameObject);
                if (elapsed >= definition.ActiveSeconds)
                {
                    // RECOVERY: beam tắt, cue giữ ngắn rồi mới về Idle/cooldown.
                    Recover();
                }
            }
            else beamDamage.Reset();
        }
        private void OnDisable() => CancelAttack();
    }
}
