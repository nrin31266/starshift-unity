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
        [SerializeField] private SpriteRenderer launch;
        [SerializeField] private Sprite[] launchFrames;
        private Vector3 launchOrigin;
        [SerializeField, Tooltip("Strike dùng vòng khóa mục tiêu thay cột beam.")]
        private bool strike;
        [SerializeField, Min(48)] private float strikeCueSizeAt900 = 96;
        [SerializeField, Min(.02f)] private float frameSeconds = .07f;
        private DirectedAttack attack;
        private Camera view;
        private float age;
        private float chargeAge;
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
            chargeAge = 0;
            phase = AttackPhase.Idle;
            if (warning != null) warning.enabled = false;
            if (launch != null) launch.enabled = false;
        }

        private void OnDisable()
        {
            if (attack != null) attack.PhaseChanged -= OnPhase;
            if (warning != null) warning.enabled = false;
            if (launch != null) launch.enabled = false;
        }

        private void OnPhase(AttackPhase next)
        {
            phase = next;
            age = 0;
            if (next == AttackPhase.Prepare) chargeAge = 0;
            if (strike && next == AttackPhase.Attack) launchOrigin = transform.position;
        }

        private void LateUpdate()
        {
            age += Time.deltaTime;
            bool charging = phase == AttackPhase.Prepare || phase == AttackPhase.Lock;
            if (charging) chargeAge += Time.deltaTime;
            float chargeProgress = Mathf.Clamp01(chargeAge / attack.Definition.ChargeSeconds);
            if (launch != null)
            {
                launch.enabled = strike && (charging || phase == AttackPhase.Attack && age < .24f) && launchFrames != null && launchFrames.Length > 0;
                if (launch.enabled)
                {
                    // Nạp sáng trên chính con lao; burst chỉ ở lại điểm xuất phát khi commit.
                    launch.transform.position = charging ? transform.position : launchOrigin;
                    float progress = charging ? chargeProgress * .5f : age / .24f;
                    launch.sprite = launchFrames[Mathf.Min(launchFrames.Length - 1, (int)(progress * launchFrames.Length))];
                    SetScreenSize(launch, strikeCueSizeAt900 * (charging ? .65f : 1));
                }
            }
            if (warning == null) return;
            bool visible = charging || (!strike && phase == AttackPhase.Recovery);
            warning.enabled = visible && warningFrames != null && warningFrames.Length > 0;
            if (!warning.enabled) return;
            var frames = phase == AttackPhase.Recovery && recoverFrames != null && recoverFrames.Length > 0 ? recoverFrames : warningFrames;
            warning.sprite = frames[Mathf.Min(frames.Length - 1, (int)(age / frameSeconds))];
            // PREPARE mờ, LOCK đậm dần: người chơi thấy rõ lúc khóa mục tiêu.
            float alpha = phase == AttackPhase.Prepare ? .82f
                : phase == AttackPhase.Lock ? Mathf.Lerp(.9f, 1f, Mathf.Clamp01(age / .5f))
                : .65f;
            warning.color = new Color(1, 1, 1, alpha);
            SetScreenSize(warning, strike ? strikeCueSizeAt900 * Mathf.Lerp(1.3f, 1, chargeProgress) : 48);
            if (strike && view != null)
            {
                // Strike: cue bám mục tiêu đã khóa để đọc hướng lao, không phải lane đứng yên.
                var aim = view.ViewportToWorldPoint(new Vector3(attack.AttackAim.x, attack.AttackAim.y, -view.transform.position.z));
                aim.z = 0;
                warning.transform.position = aim;
            }
        }

        private void SetScreenSize(SpriteRenderer visual, float pixelsAt900)
        {
            if (view == null || visual.sprite == null) return;
            float size = view.orthographicSize * 2 / 900 * pixelsAt900;
            var parent = visual.transform.parent.lossyScale;
            var bounds = visual.sprite.bounds.size;
            visual.transform.localScale = new Vector3(size / Mathf.Max(.001f, bounds.x * Mathf.Abs(parent.x)),
                size / Mathf.Max(.001f, bounds.y * Mathf.Abs(parent.y)), 1);
        }
    }
}
