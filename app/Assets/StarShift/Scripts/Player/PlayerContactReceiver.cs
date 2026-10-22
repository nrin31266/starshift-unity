// PlayerContactReceiver — Nhận đạn địch và va chạm thân; chuyển hit sang PlayerDamageLifecycle để xét mất mạng.
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
        // Đạn địch gửi hit qua đây; quyết định mất mạng thuộc PlayerDamageLifecycle.
        public void ReceiveEnemyHit(GameObject owner)
        {
            if (isActiveAndEnabled) lifecycle.TryReceiveHit(owner);
        }

        private void OnTriggerEnter2D(Collider2D other) => BodyContact(other);
        private void OnTriggerStay2D(Collider2D other) => BodyContact(other);

        // Chỉ thân địch còn sống và có cấu hình gây sát thương mới gửi hit cho tàu.
        private void BodyContact(Collider2D other)
        {
            if (!isActiveAndEnabled) return;
            var enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy != null && enemy.IsAlive && enemy.Definition.BodyDamagesPlayer) lifecycle.TryReceiveHit(enemy.gameObject);
        }
    }
}
