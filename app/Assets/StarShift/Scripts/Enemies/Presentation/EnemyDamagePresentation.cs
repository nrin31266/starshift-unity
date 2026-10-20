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
        [SerializeField, Range(0, 1)] private float criticalHealthFraction;
        [SerializeField] private Color criticalTint = new Color(1, .35f, .12f, 1);
        [SerializeField] private Sprite damagedSprite;
        [SerializeField] private Sprite criticalSprite;

        private EnemyHealth health;
        private Color normalTint;
        private Sprite intactSprite;
        public int DamageStage => health.CurrentHp <= 0 ? 3 : health.CurrentHp <= health.MaxHp * criticalHealthFraction ? 2 : health.CurrentHp <= health.MaxHp * damagedHealthFraction ? 1 : 0;
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
            intactSprite = hull.sprite;
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
            if (hull != null) hull.sprite = intactSprite;
            flashRemaining = 0;
        }

        public void ConfigureSprites(Sprite intact, Sprite damaged, Sprite critical)
        {
            intactSprite = intact;
            damagedSprite = damaged;
            criticalSprite = critical;
            Refresh();
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
            var stage = DamageStage;
            hull.color = flashRemaining > 0 ? Color.white : stage >= 2 ? criticalTint : stage == 1 ? damagedTint : normalTint;
            hull.sprite = stage >= 2 && criticalSprite != null ? criticalSprite : stage >= 1 && damagedSprite != null ? damagedSprite : intactSprite;
        }

        private void OnValidate()
        {
            flashDuration = Mathf.Max(0, flashDuration);
            criticalHealthFraction = Mathf.Clamp(criticalHealthFraction, 0, damagedHealthFraction);
        }
    }
}
