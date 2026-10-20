using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerGameplayGate))]
    [AddComponentMenu("StarShift/Player/Player Damage Presentation")]
    public sealed class PlayerDamagePresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer hull;
        private PlayerGameplayGate gate;
        private Color normalTint;
        private bool normalEnabled;
        private float deathTime;
        private bool wasDying;
        private void Awake()
        {
            gate = GetComponent<PlayerGameplayGate>();
            if (hull == null) { Debug.LogError("PlayerDamagePresentation requires a hull renderer.", this); enabled = false; return; }
            normalTint = hull.color;
            normalEnabled = hull.enabled;
        }

        private void LateUpdate()
        {
            var dying = gate.State == PlayerGameplayState.Dying;
            deathTime = dying ? (wasDying ? deathTime + Time.deltaTime : 0) : 0;
            wasDying = dying;
            hull.color = dying ? Color.white : normalTint;
            hull.enabled = normalEnabled && gate.State != PlayerGameplayState.GameOver &&
                (!dying || deathTime < .06f);
        }
        public void SetHullTint(Color tint) { normalTint = tint; hull.color = tint; }

        private void OnDisable()
        {
            if (hull != null) { hull.color = normalTint; hull.enabled = normalEnabled; }
            deathTime = 0; wasDying = false;
        }
    }
}
