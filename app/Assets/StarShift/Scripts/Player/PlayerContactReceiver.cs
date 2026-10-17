using StarShift.Combat;
using StarShift.Enemies;
using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerDamageLifecycle))]
    [AddComponentMenu("StarShift/Player/Player Contact Receiver")]
    public sealed class PlayerContactReceiver : MonoBehaviour, IEnemyProjectileTarget
    {
        private PlayerDamageLifecycle lifecycle;
        private void Awake() => lifecycle = GetComponent<PlayerDamageLifecycle>();
        public void ReceiveEnemyHit(GameObject owner)
        {
            if (isActiveAndEnabled) lifecycle.TryReceiveHit(owner);
        }

        private void OnTriggerEnter2D(Collider2D other) => BodyContact(other);
        private void OnTriggerStay2D(Collider2D other) => BodyContact(other);

        private void BodyContact(Collider2D other)
        {
            if (!isActiveAndEnabled) return;
            var enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy != null && enemy.IsAlive) lifecycle.TryReceiveHit(enemy.gameObject);
        }
    }
}
