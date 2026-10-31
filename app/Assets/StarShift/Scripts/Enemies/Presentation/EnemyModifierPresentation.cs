// EnemyModifierPresentation — Hiện kích thước và màu nhấn của biến thể địch; đặt lại khi trả pool.
using UnityEngine;

namespace StarShift.Enemies
{
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyModifierPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer accent;
        private Vector3 baseScale;
        private EnemyHealth health;
        private void Awake() { health = GetComponent<EnemyHealth>(); baseScale = transform.localScale; }
        // Áp biến thể cho lần sinh hiện tại dựa trên dữ liệu health đã nhận trước khi kích hoạt.
        private void OnEnable()
        {
            var modifier = health.Modifier;
            transform.localScale = baseScale * (modifier != null ? modifier.Scale : 1);
            accent.enabled = modifier != null;
            if (modifier != null) accent.color = modifier.Accent;
        }
        private void OnDisable() { transform.localScale = baseScale; accent.enabled = false; }
    }
}
