using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarShift.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    [AddComponentMenu("StarShift/Combat/Player Projectile")]
    public sealed class PlayerProjectile : MonoBehaviour
    {
        private Rigidbody2D body;
        private BoxCollider2D hitbox;
        private readonly List<RaycastHit2D> sweepHits = new List<RaycastHit2D>(8);
        private Action<PlayerProjectile> onReleased;
        private Vector2 direction;
        private float speed;
        private float remainingLifetime;
        private int damage;
        private bool flying;

        public GameObject Owner { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<BoxCollider2D>();
        }

        public void Launch(PulseLaserDefinition definition, Transform muzzle, GameObject owner, Action<PlayerProjectile> release)
        {
            Owner = owner;
            direction = muzzle.up;
            speed = definition.ProjectileSpeed;
            damage = definition.Damage;
            remainingLifetime = definition.ProjectileLifetime;
            onReleased = release;
            transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
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
            if (!flying) return;
            var step = Mathf.Min(Time.fixedDeltaTime, remainingLifetime);
            var distance = speed * step;
            var filter = new ContactFilter2D { useTriggers = true };
            body.Cast(direction, filter, sweepHits, distance);
            Collider2D nearestTarget = null;
            var nearestDistance = float.PositiveInfinity;
            foreach (var hit in sweepHits)
            {
                if (hit.distance >= nearestDistance || FindTarget(hit.collider) == null) continue;
                nearestTarget = hit.collider;
                nearestDistance = hit.distance;
            }
            if (nearestTarget != null)
            {
                FinishFlight(FindTarget(nearestTarget));
                return;
            }
            body.position += direction * distance;
            remainingLifetime -= step;
            if (remainingLifetime <= 0) FinishFlight();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!flying) return;
            var target = FindTarget(other);
            if (target != null) FinishFlight(target);
        }

        private IPlayerProjectileTarget FindTarget(Collider2D other)
        {
            if (other == null || (Owner != null && other.transform.IsChildOf(Owner.transform))) return null;
            return other.GetComponentInParent<IPlayerProjectileTarget>();
        }

        private void FinishFlight(IPlayerProjectileTarget target = null)
        {
            if (!flying) return;
            // Khóa trước callback: nhiều collider/sweep/trigger vẫn chỉ resolve một hit.
            flying = false;
            hitbox.enabled = false;
            var release = onReleased;
            onReleased = null;
            try { target?.ReceivePlayerHit(damage, Owner); }
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
            Owner = null;
            direction = Vector2.zero;
            speed = remainingLifetime = 0;
            damage = 0;
            sweepHits.Clear();
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
