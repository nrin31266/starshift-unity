using StarShift.Gameplay;
using UnityEngine;
namespace StarShift.Enemies
{
    [RequireComponent(typeof(EnemyHealth), typeof(EntryHoldFlight))]
    public sealed class BossPhaseRunner : MonoBehaviour
    {
        [SerializeField] private BossPhaseDefinition definition;
        [SerializeField] private BossAttackModule[] modules;
        [SerializeField] private SpriteRenderer phaseCue;
        private EnemyHealth health;
        private EntryHoldFlight flight;
        private EnemyFireGate gate;
        private int next;
        private float remaining;
        private BossAttackModule current;
        public bool IsPhaseB { get; private set; }
        public BossPhaseDefinition Definition => definition;
        public EnemyHealth Health => health;
        private void Awake() { health = GetComponent<EnemyHealth>(); flight = GetComponent<EntryHoldFlight>(); gate = FindAnyObjectByType<EnemyFireGate>();
            if (definition == null || modules == null || !definition.IsValid(modules.Length) || System.Array.Exists(modules, m => m == null) || phaseCue == null || gate == null)
                throw new System.InvalidOperationException("Invalid Boss composition.");
        }
        private void OnEnable() { IsPhaseB = false; next = 0; remaining = 1.5f; current = null; phaseCue.enabled = false; }
        private void FixedUpdate()
        {
            if (!health.IsAlive) { CancelModules(); return; }
            if (!IsPhaseB && health.CurrentHp <= health.MaxHp * definition.PhaseBThreshold)
            {
                IsPhaseB = true; CancelModules(); next = 0; remaining = definition.TransitionSeconds; phaseCue.enabled = true;
            }
            if (!gate.CanFire || !flight.IsHolding) { CancelModules(); remaining = Mathf.Max(remaining, 1); return; }
            if (current != null)
            {
                if (current.IsBusy) return;
                current = null; remaining = IsPhaseB ? definition.PhaseBRestSeconds : definition.PhaseARestSeconds;
            }
            remaining -= Time.fixedDeltaTime;
            if (remaining > 0) return;
            phaseCue.enabled = false;
            var order = IsPhaseB ? definition.PhaseBOrder : definition.PhaseAOrder;
            var module = modules[order[next++ % order.Length]];
            if (module.BeginAttack()) current = module;
        }
        public void CancelModules() { foreach (var module in modules) module.CancelAttack(); current = null; }
        private void OnDisable() { CancelModules(); phaseCue.enabled = false; }
    }
}
