// BossPhaseRunner — Điều phối giai đoạn và thứ tự đòn của boss; từng module thực hiện đòn đánh.
using StarShift.Gameplay;
using StarShift.Encounters;
using UnityEngine;
namespace StarShift.Enemies
{
    [RequireComponent(typeof(EnemyHealth), typeof(EntryHoldFlight))]
    public sealed class BossPhaseRunner : MonoBehaviour
    {
        [SerializeField] private BossPhaseDefinition definition;
        [SerializeField] private BossAttackModule[] modules;
        [SerializeField] private SpriteRenderer phaseCue;
        private BossShieldNetwork shields;
        public BossShieldNetwork Shields => shields;
        private EnemyHealth health;
        private EntryHoldFlight flight;
        private EnemyFireGate gate;
        private int next;
        private float remaining;
        private BossAttackModule current;
        private EncounterFireCoordinator coordinator;
        public bool IsAttacking => current != null && current.IsBusy;
        // Ghi nhớ boss đã chuyển giai đoạn để không lặp bước chuyển mỗi khung hình.
        public bool IsPhaseB { get; private set; }
        public BossPhaseDefinition Definition => definition;
        public EnemyHealth Health => health;
        private void Awake() { coordinator = FindAnyObjectByType<EncounterFireCoordinator>(); shields = GetComponent<BossShieldNetwork>(); health = GetComponent<EnemyHealth>(); flight = GetComponent<EntryHoldFlight>(); gate = FindAnyObjectByType<EnemyFireGate>();
            if (definition == null || modules == null || !definition.IsValid(modules.Length) || System.Array.Exists(modules, m => m == null) || phaseCue == null || gate == null)
                throw new System.InvalidOperationException("Invalid Boss composition.");
        }
        private void OnEnable() { IsPhaseB = false; next = 0; remaining = 1.5f; current = null; phaseCue.enabled = false; }
        // Xét điều kiện chuyển pha rồi chọn module theo thứ tự cấu hình, có nhịp nghỉ giữa các đòn.
        private void FixedUpdate()
        {
            if (Time.timeScale <= 0) return;
            if (!health.IsAlive) { CancelModules(); return; }
            // Chuyển giai đoạn theo mạng khiên nếu có; boss thường dùng ngưỡng tỷ lệ HP trong cấu hình.
            if (!IsPhaseB && (shields != null ? shields.IsExposed : health.CurrentHp <= health.MaxHp * definition.PhaseBThreshold))
            {
                IsPhaseB = true; CancelModules(); next = 0; remaining = definition.TransitionSeconds; phaseCue.enabled = true;
                flight.Reposition(definition.PhaseBOffset, definition.TransitionSeconds, definition.PhaseBMotionMultiplier);
            }
            if (!gate.CanContinueAttack || !flight.IsHolding) { CancelModules(); remaining = Mathf.Max(remaining, 1); return; }
            if (current != null)
            {
                if (current.IsBusy) return;
                current = null; remaining = IsPhaseB ? definition.PhaseBRestSeconds : definition.PhaseARestSeconds;
            }
            if (!gate.CanFire) return;
            remaining -= Time.fixedDeltaTime;
            if (remaining > 0) return;
            if (coordinator != null && !coordinator.CanStartBossAttack) return;
            phaseCue.enabled = false;
            var order = IsPhaseB ? definition.PhaseBOrder : definition.PhaseAOrder;
            var module = modules[order[next++ % order.Length]];
            if (module.BeginAttack()) { current = module; coordinator?.RecordBossAttack(); }
        }
        public void CancelModules() { foreach (var module in modules) if (module != null) module.CancelAttack(); current = null; }
        private void OnDisable() { CancelModules(); phaseCue.enabled = false; }
    }
}
