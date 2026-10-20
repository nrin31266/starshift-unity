using UnityEngine;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SingleFireAttack))]
    [AddComponentMenu("StarShift/Enemies/Enemy Fire Presentation")]
    public sealed class EnemyFirePresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer core;
        [SerializeField] private Color chargedTint = new Color(1, .2f, .8f, 1);
        private SingleFireAttack attack;
        private Color normalTint;

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

        private void LateUpdate()
            => core.color = attack.isActiveAndEnabled && attack.IsTelegraphing
                ? Color.Lerp(chargedTint * new Color(1, 1, 1, .55f), chargedTint, .5f + .5f * Mathf.Sin(Time.time * 24)) : normalTint;

        private void OnDisable()
        {
            if (core != null) core.color = normalTint;
        }
    }
}
