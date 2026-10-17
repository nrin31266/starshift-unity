using UnityEngine;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    [AddComponentMenu("StarShift/Enemies/Enemy Damage Presentation")]
    public sealed class EnemyDamagePresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer hull;
        [SerializeField] private Color damagedTint = new Color(.9f, .28f, .3f, 1);
        [SerializeField, Range(0, 1)] private float damagedHealthFraction = .5f;
        [SerializeField, Min(0)] private float flashDuration = .08f;

        private EnemyHealth health;
        private Color normalTint;
        private float flashRemaining;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            if (hull == null)
            {
                Debug.LogError("EnemyDamagePresentation requires a hull renderer.", this);
                enabled = false;
                return;
            }
            normalTint = hull.color;
        }

        private void OnEnable()
        {
            flashRemaining = 0;
            health.Damaged += OnDamaged;
            Refresh();
        }

        private void OnDisable()
        {
            if (health != null) health.Damaged -= OnDamaged;
            if (hull != null) hull.color = normalTint;
            flashRemaining = 0;
        }

        private void OnDamaged()
        {
            flashRemaining = flashDuration;
            Refresh();
        }

        private void Update()
        {
            if (flashRemaining <= 0) return;
            flashRemaining = Mathf.Max(0, flashRemaining - Time.deltaTime);
            Refresh();
        }

        private void Refresh()
        {
            hull.color = flashRemaining > 0 ? Color.white
                : health.CurrentHp <= health.MaxHp * damagedHealthFraction ? damagedTint : normalTint;
        }

        private void OnValidate() => flashDuration = Mathf.Max(0, flashDuration);
    }
}
