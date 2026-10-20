using UnityEngine;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent, RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyHitPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sparks;
        [SerializeField] private SpriteRenderer smoke;
        [SerializeField] private Sprite[] hitFrames;
        [SerializeField] private Sprite[] smokeFrames;
        private EnemyHealth health;
        private EnemyDamagePresentation damage;
        private float remaining;
        private float age;
        private void Awake() { health = GetComponent<EnemyHealth>(); damage = GetComponent<EnemyDamagePresentation>(); }
        private void OnEnable() { health.Damaged += OnHit; remaining = age = 0; sparks.enabled = smoke.enabled = false; }
        private void OnHit() => remaining = .2f;
        private void LateUpdate()
        {
            age += Time.deltaTime;
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            sparks.enabled = remaining > 0 && health.IsAlive;
            if (sparks.enabled && hitFrames.Length > 0) sparks.sprite = hitFrames[Mathf.Min(hitFrames.Length - 1, (int)((.2f - remaining) / .05f))];
            smoke.enabled = health.IsAlive && damage != null && damage.DamageStage >= 2;
            if (smoke.enabled && smokeFrames.Length > 0) smoke.sprite = smokeFrames[(int)(age / .18f) % smokeFrames.Length];
        }
        private void OnDisable() { if (health != null) health.Damaged -= OnHit; sparks.enabled = smoke.enabled = false; remaining = age = 0; }
    }
}
