using UnityEngine;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SingleFireAttack))]
    [AddComponentMenu("StarShift/Enemies/Enemy Fire Presentation")]
    public sealed class EnemyFirePresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer core;
        [SerializeField] private Sprite[] chargeFrames;
        [SerializeField] private Color chargedTint = new Color(1, .2f, .8f, 1);
        [SerializeField, Tooltip("Sprite charge theo family; nếu trống dùng tint cũ.")]
        private SpriteRenderer chargeCue;
        [SerializeField, Tooltip("Magenta dành cho special danger; normal hostile dùng coral/lemon.")]
        private bool specialDanger;
        private SingleFireAttack attack;
        private Color normalTint;
        private float age;
        private bool wasCharging;

        private void Awake()
        {
            attack = GetComponent<SingleFireAttack>();
            if (core == null)
            {
                Debug.LogError("EnemyFirePresentation requires a core renderer.", this);
                enabled = false;
                return;
            }
            normalTint = core.color;
        }

        private void OnEnable() => age = 0;
        private void OnDisable()
        {
            if (core != null) core.color = normalTint;
            if (chargeCue != null) chargeCue.enabled = false;
        }

        private void LateUpdate()
        {
            bool charging = attack.isActiveAndEnabled && attack.IsTelegraphing;
            // Mỗi telegraph bắt đầu từ frame 0; không giữ frame cũ khi bắn lại.
            if (charging && !wasCharging) age = 0;
            wasCharging = charging;
            if (charging) age += Time.deltaTime;
            core.color = charging
                ? Color.Lerp(chargedTint * new Color(1, 1, 1, .55f), chargedTint, .5f + .5f * Mathf.Sin(Time.time * 24))
                : normalTint;
            if (chargeCue == null) return;
            chargeCue.enabled = charging && chargeFrames != null && chargeFrames.Length > 0;
            if (!chargeCue.enabled) return;
            // Vòng co vào lõi trong lúc telegraph; reset frame mỗi lần bắt đầu charge.
            int frame = Mathf.Clamp((int)(age / .07f), 0, chargeFrames.Length - 1);
            chargeCue.sprite = chargeFrames[frame];
            chargeCue.color = specialDanger ? new Color(1, .55f, .95f, 1) : Color.white;
        }
    }
}
