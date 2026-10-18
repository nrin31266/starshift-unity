using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerEffectPresentation : MonoBehaviour
    {
        [SerializeField] private PlayerTemporaryEffects effects;
        [SerializeField] private SpriteRenderer shield;
        private void LateUpdate() => shield.enabled = effects.HasShield;
        private void OnDisable() { if (shield != null) shield.enabled = false; }
    }
}
