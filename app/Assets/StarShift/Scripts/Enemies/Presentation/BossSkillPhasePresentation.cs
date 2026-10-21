using UnityEngine;

namespace StarShift.Enemies
{
    // Boss skill cue: PREPARE → LOCK → ATTACK → RECOVERY cho beam/barrage/cannon/escort.
    // Chỉ đọc Phase từ module hiện có; damage bounds/timing thuộc BossAttackModule/BossBarrage.
    [DisallowMultipleComponent]
    [AddComponentMenu("StarShift/Enemies/Boss Skill Phase Presentation")]
    public sealed class BossSkillPhasePresentation : MonoBehaviour
    {
        [SerializeField] private BossAttackModule module;
        [SerializeField] private BossBarrage barrage;
        [SerializeField, Tooltip("Sprite cue theo phase; 0-1 prepare, 2 lock, 3 attack.")]
        private SpriteRenderer cue;
        [SerializeField] private Sprite[] prepareFrames;
        [SerializeField] private Sprite[] lockFrames;
        [SerializeField] private Sprite[] attackFrames;
        [SerializeField] private Color prepareTint = new Color(1, .72f, .3f, .85f);
        [SerializeField] private Color lockTint = new Color(1, .25f, .35f, 1f);
        [SerializeField] private Color attackTint = new Color(1, .9f, .75f, 1f);
        [SerializeField, Min(.02f)] private float frameSeconds = .07f;
        [SerializeField, Min(.1f)] private float cueScale = 1.25f;
        [SerializeField, Min(24)] private float cueSizeAt900 = 52;
        private Camera gameplayCamera;
        private AttackPhase phase;
        private AttackPhase observedPhase;
        private int observedPulses;
        private float age;
        private EnemyHealth health;

        private void Awake()
        {
            if (module == null) module = GetComponentInParent<BossAttackModule>();
            if (barrage == null) barrage = GetComponentInParent<BossBarrage>();
            health = GetComponentInParent<EnemyHealth>();
            gameplayCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (module != null) module.PhaseChanged += OnPhase;
            if (barrage != null) barrage.PhaseChanged += OnPhase;
            observedPhase = phase = AttackPhase.Idle;
            observedPulses = 0;
            age = 0;
            if (cue != null) cue.enabled = false;
        }

        private void OnDisable()
        {
            if (module != null) module.PhaseChanged -= OnPhase;
            if (barrage != null) barrage.PhaseChanged -= OnPhase;
            if (cue != null) cue.enabled = false;
        }

        private void OnPhase(AttackPhase next)
        {
            observedPhase = next;
            if (next == AttackPhase.Idle && phase == AttackPhase.Recovery) return;
            // Tail chỉ thuộc presentation, không giữ module bận hoặc kéo dài damage window.
            phase = next == AttackPhase.Idle && phase == AttackPhase.Attack ? AttackPhase.Recovery : next;
            age = 0;
        }

        private void LateUpdate()
        {
            if (cue == null || Time.timeScale <= 0) return;
            // Module có thể ủy quyền phase cho DirectedAttack/Barrage mà không phát event riêng.
            var current=barrage!=null?barrage.Phase:module!=null?module.CurrentPhase:AttackPhase.Idle;
            if (current != observedPhase) OnPhase(current);
            if (barrage != null && observedPulses != barrage.PulsesFired)
            { observedPulses = barrage.PulsesFired; age = 0; }
            if (health != null && !health.IsAlive) phase = AttackPhase.Idle;
            if (phase == AttackPhase.Recovery && age >= frameSeconds * attackFrames.Length) phase = AttackPhase.Idle;
            var frames = phase == AttackPhase.Attack || phase == AttackPhase.Recovery ? attackFrames : phase == AttackPhase.Lock ? lockFrames : phase == AttackPhase.Prepare ? prepareFrames : null;
            cue.enabled = frames != null && frames.Length > 0;
            if (!cue.enabled) return;
            age += Time.deltaTime;
            cue.sprite = frames[Mathf.Min(frames.Length - 1, (int)(age / frameSeconds))];
            var tint = phase == AttackPhase.Attack || phase == AttackPhase.Recovery ? attackTint : phase == AttackPhase.Lock ? lockTint : prepareTint;
            if (phase == AttackPhase.Recovery) tint.a *= Mathf.Clamp01(1 - age / (frameSeconds * frames.Length));
            cue.color = tint;
            // Scale nhẹ theo phase: LOCK lớn hơn PREPARE để đọc mức nguy hiểm.
            float scale = phase == AttackPhase.Attack ? 1.15f : phase == AttackPhase.Lock ? 1f : .8f;
            if (gameplayCamera != null && cue.sprite != null)
            {
                float size=gameplayCamera.orthographicSize*2/900*cueSizeAt900*scale*cueScale;
                var parent=cue.transform.parent.lossyScale;var bounds=cue.sprite.bounds.size;
                cue.transform.localScale=new Vector3(size/Mathf.Max(.001f,bounds.x*Mathf.Abs(parent.x)),size/Mathf.Max(.001f,bounds.y*Mathf.Abs(parent.y)),1);
            }
        }
    }
}
