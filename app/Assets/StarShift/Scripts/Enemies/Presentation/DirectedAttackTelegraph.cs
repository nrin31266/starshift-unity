using UnityEngine;

namespace StarShift.Enemies
{
    // Hiển thị PREPARE → LOCK → ATTACK → RECOVERY cho Sniper beam / Interceptor strike.
    // Chỉ đọc phase từ DirectedAttack; timing/vùng damage vẫn thuộc ability state.
    [DisallowMultipleComponent, RequireComponent(typeof(DirectedAttack))]
    [AddComponentMenu("StarShift/Enemies/Directed Attack Telegraph")]
    public sealed class DirectedAttackTelegraph : MonoBehaviour
    {
        [SerializeField, Tooltip("Renderer cảnh báo đặt tại muzzle, xoay theo hướng bắn.")]
        private SpriteRenderer warning;
        [SerializeField, Tooltip("Sprite cột cảnh báo dọc; màu magenta cho danger.")]
        private Sprite[] warningFrames;
        [SerializeField] private Sprite[] recoverFrames;
        [SerializeField, Tooltip("Strike dùng vòng khóa mục tiêu thay cột beam.")]
        private bool strike;
        [SerializeField, Min(.02f)] private float frameSeconds = .07f;
        private DirectedAttack attack;
        private Camera view;
        private float age;
        private AttackPhase phase;

        private void Awake()
        {
            attack = GetComponent<DirectedAttack>();
            view = Camera.main;
        }

        private void OnEnable()
        {
            attack.PhaseChanged += OnPhase;
            age = 0;
            phase = AttackPhase.Idle;
            if (warning != null) warning.enabled = false;
        }

        private void OnDisable()
        {
            if (attack != null) attack.PhaseChanged -= OnPhase;
            if (warning != null) warning.enabled = false;
        }

        private void OnPhase(AttackPhase next)
        {
            phase = next;
            age = 0;
        }

        private void LateUpdate()
        {
            if (warning == null) return;
            bool visible = phase == AttackPhase.Prepare || phase == AttackPhase.Lock || phase == AttackPhase.Recovery;
            warning.enabled = visible && warningFrames != null && warningFrames.Length > 0;
            if (!warning.enabled) return;
            age += Time.deltaTime;
            var frames = phase == AttackPhase.Recovery && recoverFrames != null && recoverFrames.Length > 0 ? recoverFrames : warningFrames;
            warning.sprite = frames[Mathf.Min(frames.Length - 1, (int)(age / frameSeconds))];
            // PREPARE mờ, LOCK đậm dần: người chơi thấy rõ lúc khóa mục tiêu.
            float alpha = phase == AttackPhase.Prepare ? .55f
                : phase == AttackPhase.Lock ? Mathf.Lerp(.7f, 1f, Mathf.Clamp01(age / .5f))
                : .5f;
            warning.color = new Color(1, 1, 1, alpha);
            if (strike && view != null)
            {
                // Strike: cue bám mục tiêu đã khóa để đọc hướng lao, không phải lane đứng yên.
                var aim = view.ViewportToWorldPoint(new Vector3(attack.AttackAim.x, attack.AttackAim.y, -view.transform.position.z));
                aim.z = 0;
                warning.transform.position = aim;
            }
        }
    }
}
