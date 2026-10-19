using System;
using System.Collections.Generic;
using UnityEngine;
using StarShift.Enemies;

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
        private EnemyHealth trackingTarget;
        private float turnRate;
        private float trackingRange;
        private void AcquireTarget(Vector2 position)
        {
            trackingTarget = null;
            var nearest = trackingRange * trackingRange;
            foreach (var enemy in EnemyHealth.Active)
            {
                var delta = (Vector2)enemy.transform.position - position;
                if (!enemy.CanReceiveDamage || enemy.gameObject.scene != gameObject.scene || Vector2.Dot(direction, delta) <= 0 || delta.sqrMagnitude >= nearest) continue;
                nearest = delta.sqrMagnitude; trackingTarget = enemy; trackingVersion = enemy.SpawnVersion;
            }
        }
        private int trackingVersion;
        private float splashRadius;
        private SpriteRenderer visual;
        private Vector3 normalVisualScale;
        private Sprite normalSprite;
        private TrailRenderer trail;
        private readonly List<EnemyHealth> splashTargets = new List<EnemyHealth>(12);

        public GameObject Owner { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<BoxCollider2D>();
            visual = GetComponentInChildren<SpriteRenderer>();
            if (visual != null) { normalVisualScale = visual.transform.localScale; normalSprite = visual.sprite; }
            trail = GetComponentInChildren<TrailRenderer>();
        }

        public void Launch(PulseLaserDefinition definition, Transform muzzle, GameObject owner, Action<PlayerProjectile> release)
            => Launch(definition, muzzle.position, muzzle.up, definition.Damage, 0, owner, release);

        public void Launch(PulseLaserDefinition definition, Vector3 position, Vector2 forward, int hitDamage,
            float splash, GameObject owner, Action<PlayerProjectile> release)
        {
            Owner = owner;
            direction = forward.normalized;
            speed = definition.ProjectileSpeed;
            damage = hitDamage;
            splashRadius = splash;
            turnRate = definition.TrackingDegreesPerSecond;
            trackingTarget = null;
            trackingRange = definition.TrackingRange;
            if (turnRate > 0) AcquireTarget(position);
            if (visual != null)
            {
                visual.color = definition.ProjectileColor;
                visual.sprite = definition.ProjectileSprite != null ? definition.ProjectileSprite : normalSprite;
                visual.transform.localScale = Vector3.Scale(normalVisualScale, new Vector3(definition.ProjectileScale.x, definition.ProjectileScale.y, 1));
            }
            remainingLifetime = definition.ProjectileLifetime;
            onReleased = release;
            transform.SetPositionAndRotation(position, Quaternion.FromToRotation(Vector3.up, direction));
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0;
            body.simulated = true;
            hitbox.enabled = true;
            flying = true;
            gameObject.SetActive(true);
            if (trail != null)
            {
                trail.Clear();
                trail.time = definition.TrailSeconds;
                trail.startColor = definition.ProjectileColor;
                trail.endColor = new Color(definition.ProjectileColor.r, definition.ProjectileColor.g, definition.ProjectileColor.b, 0);
                trail.emitting = definition.TrailSeconds > 0;
                trail.enabled = trail.emitting;
            }
        }

        private void FixedUpdate()
        {
            if (!flying) return;
            var step = Mathf.Min(Time.fixedDeltaTime, remainingLifetime);
            if (turnRate > 0 && (trackingTarget == null || !trackingTarget.CanReceiveDamage || trackingTarget.SpawnVersion != trackingVersion)) AcquireTarget(body.position);
            if (trackingTarget != null && trackingTarget.CanReceiveDamage && trackingTarget.SpawnVersion == trackingVersion)
            {
                var targetDirection = (Vector2)trackingTarget.transform.position - body.position;
                var angle = Vector2.SignedAngle(direction, targetDirection);
                direction = Quaternion.AngleAxis(Mathf.Clamp(angle, -turnRate * step, turnRate * step), Vector3.forward) * direction;
                transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            }
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
            var target = other.GetComponentInParent<IPlayerProjectileTarget>();
            return target is EnemyHealth enemy && !enemy.CanReceiveDamage ? null : target;
        }

        private void FinishFlight(IPlayerProjectileTarget target = null)
        {
            if (!flying) return;
            // Khóa trước callback: nhiều collider/sweep/trigger vẫn chỉ resolve một hit.
            flying = false;
            hitbox.enabled = false;
            var release = onReleased;
            onReleased = null;
            try
            {
                splashTargets.Clear();
                // Splash quanh target bị hit, độc lập khoảng sweep còn lại của physics step.
                var impact = target is EnemyHealth hitEnemy ? (Vector2)hitEnemy.transform.position : body.position;
                if (target != null && splashRadius > 0)
                    foreach (var enemy in EnemyHealth.Active)
                        if (enemy.IsAlive && enemy.gameObject.scene == gameObject.scene && !ReferenceEquals(enemy, target) &&
                            ((Vector2)enemy.transform.position - impact).sqrMagnitude <= splashRadius * splashRadius)
                            splashTargets.Add(enemy);
                target?.ReceivePlayerHit(damage, Owner);
                foreach (var enemy in splashTargets) if (enemy != null && enemy.IsAlive) enemy.ReceivePlayerHit(damage, Owner);
            }
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
            trackingTarget = null;
            turnRate = splashRadius = 0;
            trackingVersion = 0;
            splashTargets.Clear();
            sweepHits.Clear();
            hitbox.enabled = false;
            body.simulated = false;
            if (visual != null) { visual.transform.localScale = normalVisualScale; visual.sprite = normalSprite; }
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            flying = false;
            onReleased = null;
            if (trail != null) { trail.emitting = false; trail.Clear(); }
        }
    }
}
