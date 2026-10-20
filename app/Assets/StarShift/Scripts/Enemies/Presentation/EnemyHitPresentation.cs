using UnityEngine;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent, RequireComponent(typeof(EnemyHealth))]
    [AddComponentMenu("StarShift/Enemies/Enemy Hit Presentation")]
    public sealed class EnemyHitPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sparks;
        [SerializeField] private SpriteRenderer smoke;
        [SerializeField] private Sprite[] hitFrames;
        [SerializeField] private Sprite[] smokeFrames;
        [Header("Impact theo loại trúng")]
        [SerializeField, Tooltip("Renderer impact đặt tại điểm trúng; để trống thì dùng sparks cũ.")]
        private SpriteRenderer impact;
        [SerializeField] private Sprite[] hullImpactFrames;
        [SerializeField] private Sprite[] shieldImpactFrames;
        [SerializeField] private Sprite[] moduleImpactFrames;
        [SerializeField, Min(.02f)] private float impactSeconds = .22f;
        private EnemyHealth health;
        private EnemyDamagePresentation damage;
        private float remaining;
        private float impactRemaining;
        private float age;
        private Sprite[] activeFrames;
        private bool impactFlipX;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            damage = GetComponent<EnemyDamagePresentation>();
        }

        private void OnEnable()
        {
            health.HitPresented += OnHit;
            health.Damaged += OnDamaged;
            remaining = impactRemaining = age = 0;
            if (sparks != null) sparks.enabled = false;
            if (smoke != null) smoke.enabled = false;
            if (impact != null) impact.enabled = false;
        }

        private void OnHit(EnemyHitInfo hit)
        {
            remaining = .2f;
            // Shield block có cue riêng, không dùng spark hull.
            if (hit.Kind == EnemyHitKind.ShieldBlock)
            {
                if (impact == null) return;
                activeFrames = shieldImpactFrames;
                PlaceImpact(hit);
                return;
            }
            activeFrames = hit.Kind == EnemyHitKind.Module ? moduleImpactFrames : hullImpactFrames;
            PlaceImpact(hit);
        }

        private void PlaceImpact(EnemyHitInfo hit)
        {
            if (impact == null || activeFrames == null || activeFrames.Length == 0) return;
            impactRemaining = impactSeconds;
            // Đưa về local của root để impact bám đúng mép cánh/module, không luôn ở tâm.
            var local = transform.InverseTransformPoint(hit.Point);
            impact.transform.localPosition = new Vector3(local.x, local.y, 0);
            impactFlipX = local.x < 0;
            impact.transform.localScale = Vector3.one / Mathf.Max(.01f, transform.lossyScale.x);
        }

        private void OnDamaged()
        {
            if (impact != null || sparks == null) return;
            // Prefab cũ chưa có impact renderer: giữ spark cũ để không mất feedback.
            remaining = .2f;
        }

        private void LateUpdate()
        {
            age += Time.deltaTime;
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            impactRemaining = Mathf.Max(0, impactRemaining - Time.deltaTime);

            if (sparks != null)
            {
                sparks.enabled = remaining > 0 && health.IsAlive && impact == null;
                if (sparks.enabled && hitFrames != null && hitFrames.Length > 0)
                    sparks.sprite = hitFrames[Mathf.Min(hitFrames.Length - 1, (int)((.2f - remaining) / .05f))];
            }
            if (smoke != null)
            {
                smoke.enabled = health.IsAlive && damage != null && damage.DamageStage >= 2;
                if (smoke.enabled && smokeFrames != null && smokeFrames.Length > 0)
                    smoke.sprite = smokeFrames[(int)(age / .18f) % smokeFrames.Length];
            }
            if (impact != null)
            {
                bool shown = impactRemaining > 0 && health.IsAlive;
                impact.enabled = shown;
                if (!shown) return;
                float t = 1 - impactRemaining / impactSeconds;
                impact.sprite = activeFrames[Mathf.Min(activeFrames.Length - 1, (int)(t * activeFrames.Length))];
                impact.flipX = impactFlipX;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.HitPresented -= OnHit;
                health.Damaged -= OnDamaged;
            }
            if (sparks != null) sparks.enabled = false;
            if (smoke != null) smoke.enabled = false;
            if (impact != null) impact.enabled = false;
            remaining = impactRemaining = age = 0;
        }
    }
}
