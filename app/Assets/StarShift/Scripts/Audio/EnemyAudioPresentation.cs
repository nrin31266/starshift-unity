using StarShift.Enemies;
using StarShift.Combat;
using UnityEngine;

namespace StarShift.Audio
{
    [DisallowMultipleComponent]
    public sealed class EnemyAudioPresentation : MonoBehaviour
    {
        [SerializeField] private AudioCue warning = AudioCue.HeavyCharge;
        [SerializeField] private AudioCue attackCue = AudioCue.HeavyShot;
        private AudioDirector audio;
        private DirectedAttack directed;
        private BossAttackModule module;
        private BossBarrage barrage;
        private SingleFireAttack single;
        private EnemyHealth health;
        private BossDeathSequence death;
        private AttackPhase previousPhase;
        private bool charging, dying, warned;
        private int deathBeat;
        private void Awake()
        {
            audio = FindAnyObjectByType<AudioDirector>();
            directed = GetComponent<DirectedAttack>(); module = GetComponent<BossAttackModule>(); barrage = GetComponent<BossBarrage>();
            single = GetComponent<SingleFireAttack>(); health = GetComponentInParent<EnemyHealth>();
            death = GetComponent<BossDeathSequence>();
        }
        private void OnEnable()
        {
            previousPhase = AttackPhase.Idle; charging = dying = warned = false; deathBeat = 0;
            if (single != null) single.VolleyFired += OnVolley;
            if (death != null) death.Completed += OnBossCompleted;
            // Cannon có thể kết thúc Attack ngay trong cùng tick, nên phải bắt event.
            if (directed != null) directed.PhaseChanged += OnPhase;
            if (module != null) module.PhaseChanged += OnPhase;
            if (barrage != null) barrage.PhaseChanged += OnPhase;
            if (health != null && health.gameObject == gameObject) health.HitPresented += OnHit;
        }
        private void OnHit(EnemyHitInfo hit) { if (hit.Branch == WeaponBranch.Homing) audio.Play(AudioCue.HomingImpact); }
        private void OnVolley() => audio.Play(attackCue);
        private void OnBossCompleted() => audio?.Play(AudioCue.Victory);
        private void LateUpdate()
        {
            if (audio == null || Time.timeScale <= 0) return;
            if (death != null && death.IsPlaying)
            {
                if (!dying) { dying = true; audio.Play(AudioCue.BossBurst); }
                if (deathBeat == 0 && death.Progress >= .3f) { deathBeat = 1; audio.Play(AudioCue.BossBurst); }
                if (deathBeat == 1 && death.Progress >= .79f) { deathBeat = 2; audio.Play(AudioCue.BossFinal); }
                return;
            }
            if (health == null || !health.IsAlive) return;
            if (module != null || directed != null)
            {
                var phase = module != null ? module.CurrentPhase : directed.Phase;
                OnPhase(phase);
            }
            else if (single != null)
            {
                bool next = single.IsTelegraphing;
                if (next && !charging && warning == AudioCue.HeavyCharge) audio.Play(warning);
                charging = next;
            }
        }
        private void OnPhase(AttackPhase phase)
        {
            if (phase == previousPhase) return;
            if (phase == AttackPhase.Idle) warned = false;
            if (phase == AttackPhase.Prepare && !warned) { audio?.Play(warning); warned = true; }
            if (phase == AttackPhase.Attack) audio?.Play(attackCue);
            previousPhase = phase;
        }
        private void OnDisable()
        {
            if (single != null) single.VolleyFired -= OnVolley;
            if (death != null) death.Completed -= OnBossCompleted;
            if (directed != null) directed.PhaseChanged -= OnPhase;
            if (module != null) module.PhaseChanged -= OnPhase;
            if (barrage != null) barrage.PhaseChanged -= OnPhase;
            if (health != null && health.gameObject == gameObject) health.HitPresented -= OnHit;
        }
    }
}
