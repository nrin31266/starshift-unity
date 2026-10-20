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
        private float blinkTime;
        private void Awake()
        {
            gate = GetComponent<PlayerGameplayGate>();
            if (hull == null) { Debug.LogError("PlayerDamagePresentation requires a hull renderer.", this); enabled = false; return; }
            normalTint = hull.color;
            normalEnabled = hull.enabled;
        }

        private void LateUpdate()
        {
            blinkTime += Time.deltaTime;
            var dying = gate.State == PlayerGameplayState.Dying;
            var protectedEntry = gate.State == PlayerGameplayState.PostEntryInvincibility;
            hull.color = dying ? Color.white : protectedEntry ? new Color(.15f, 1, .85f, normalTint.a) : normalTint;
            hull.enabled = normalEnabled && gate.State != PlayerGameplayState.GameOver &&
                (!protectedEntry || Mathf.Repeat(blinkTime, .2f) < .12f);
        }
        public void SetHullTint(Color tint) { normalTint = tint; hull.color = tint; }

        private void OnDisable()
        {
            if (hull != null) { hull.color = normalTint; hull.enabled = normalEnabled; }
            blinkTime = 0;
        }
    }
}
