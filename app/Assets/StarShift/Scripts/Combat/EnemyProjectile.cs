using System;
using System.Collections.Generic;
using StarShift.Enemies;
using UnityEngine;

namespace StarShift.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    [AddComponentMenu("StarShift/Combat/Enemy Projectile")]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private Rigidbody2D body;
        private BoxCollider2D hitbox;
        private readonly List<RaycastHit2D> sweepHits = new List<RaycastHit2D>(8);
        private Action<EnemyProjectile> onReleased;
        private GameObject target;
        private Vector2 direction;
        private float speed;
        private float remainingLifetime;
        private bool flying;

        public GameObject Owner { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<BoxCollider2D>();
        }

        public void Launch(SingleFireDefinition definition, Transform muzzle, GameObject owner,
            GameObject player, Action<EnemyProjectile> release)
        {
            Owner = owner;
            target = player;
            var aim = (Vector2)(player.transform.position - muzzle.position);
            direction = aim.sqrMagnitude > .000001f ? aim.normalized : (Vector2)muzzle.up;
            speed = definition.ProjectileSpeed;
            remainingLifetime = definition.ProjectileLifetime;
            onReleased = release;
            var rotation = Quaternion.FromToRotation(Vector3.up, direction);
            transform.SetPositionAndRotation(muzzle.position, rotation);
            body.position = muzzle.position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
            body.simulated = true;
            hitbox.enabled = true;
            flying = true;
            gameObject.SetActive(true);
        }

        private void FixedUpdate()
        {
            if (!flying || Time.timeScale <= 0) return;
            var step = Mathf.Min(Time.fixedDeltaTime, remainingLifetime);
            var distance = speed * step;
            body.Cast(direction, new ContactFilter2D { useTriggers = true }, sweepHits, distance);
            Collider2D nearest = null;
            var nearestDistance = float.PositiveInfinity;
            foreach (var hit in sweepHits)
            {
                if (hit.distance >= nearestDistance || !IsTarget(hit.collider)) continue;
                nearest = hit.collider;
                nearestDistance = hit.distance;
            }
            if (nearest != null)
            {
                FinishFlight(nearest.GetComponentInParent<IEnemyProjectileTarget>());
                return;
            }
            body.position += direction * distance;
            remainingLifetime -= step;
            if (remainingLifetime <= 0) FinishFlight();
        }

        private bool IsTarget(Collider2D other)
            => other != null && target != null && other.transform.IsChildOf(target.transform) &&
                (Owner == null || !other.transform.IsChildOf(Owner.transform));

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (flying && Time.timeScale > 0 && IsTarget(other))
                FinishFlight(other.GetComponentInParent<IEnemyProjectileTarget>());
        }

        private void FinishFlight(IEnemyProjectileTarget receiver = null)
        {
            if (!flying) return;
            // Khóa trước receiver: sweep/trigger hoặc nhiều hitbox chỉ resolve một contact.
            flying = false;
            hitbox.enabled = false;
            var release = onReleased;
            onReleased = null;
            try { receiver?.ReceiveEnemyHit(Owner); }
            finally
            {
                if (release != null) release(this);
                else Destroy(gameObject);
            }
        }

        public void Release() => FinishFlight();

        public void ResetForPool()
        {
            flying = false;
            onReleased = null;
            Owner = target = null;
            direction = Vector2.zero;
            speed = remainingLifetime = 0;
            sweepHits.Clear();
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
            hitbox.enabled = false;
            body.simulated = false;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            flying = false;
            onReleased = null;
        }
    }
}
