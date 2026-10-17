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
        private Camera gameplayCamera;
        private SpriteRenderer visual;
        private Color launchColor;
        private bool cleaningUp;
        private bool fading;
        private float cleanupRemaining;
        private float fadeSeconds;
        private float fadeRemaining;

        public GameObject Owner { get; private set; }
        public float VisibleFlightTimeLimit
        {
            get
            {
                if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled || !gameplayCamera.orthographic || speed <= 0) return 0;
                var point = gameplayCamera.WorldToViewportPoint(new Vector3(body.position.x, body.position.y, transform.position.z));
                if (point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1 ||
                    point.z < gameplayCamera.nearClipPlane || point.z > gameplayCamera.farClipPlane) return 0;
                // Đường chéo view là upper bound thời gian bay ra ngoài theo đường thẳng.
                return 2 * gameplayCamera.orthographicSize * Mathf.Sqrt(1 + gameplayCamera.aspect * gameplayCamera.aspect) / speed;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<BoxCollider2D>();
            visual = GetComponentInChildren<SpriteRenderer>();
            launchColor = visual.color;
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
            gameplayCamera = Camera.main;
            cleaningUp = fading = false;
            cleanupRemaining = fadeRemaining = fadeSeconds = 0;
            visual.color = launchColor;
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
            if (cleaningUp)
            {
                if (VisibleFlightTimeLimit <= 0) { FinishFlight(); return; }
                if (!fading)
                {
                    cleanupRemaining -= Time.fixedDeltaTime;
                    if (cleanupRemaining <= 0) BeginFade();
                }
                else
                {
                    fadeRemaining = Mathf.Max(0, fadeRemaining - Time.fixedDeltaTime);
                    var color = launchColor;
                    color.a *= fadeRemaining / fadeSeconds;
                    visual.color = color;
                    if (fadeRemaining <= 0) { FinishFlight(); return; }
                }
            }
            var step = remainingLifetime > 0 ? Mathf.Min(Time.fixedDeltaTime, remainingLifetime) : Time.fixedDeltaTime;
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
            remainingLifetime = Mathf.Max(0, remainingLifetime - step);
            // Lifetime không được cắt viên đạn còn visible giữa đường.
            if (remainingLifetime <= 0 && VisibleFlightTimeLimit <= 0) FinishFlight();
        }

        private bool IsTarget(Collider2D other)
            => !fading && other != null && target != null && other.transform.IsChildOf(target.transform) &&
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

        public void BeginCleanup(float graceSeconds, float duration)
        {
            if (!flying || cleaningUp) return;
            cleaningUp = true;
            cleanupRemaining = Mathf.Max(0, graceSeconds);
            fadeSeconds = Mathf.Max(.01f, duration);
            if (cleanupRemaining <= 0) BeginFade();
        }

        private void BeginFade()
        {
            fading = true;
            fadeRemaining = fadeSeconds;
            // Khóa cả trigger lẫn sweep trước khi visual bắt đầu fade.
            hitbox.enabled = false;
        }

        public void ResetForPool()
        {
            flying = false;
            onReleased = null;
            Owner = target = null;
            gameplayCamera = null;
            direction = Vector2.zero;
            speed = remainingLifetime = 0;
            cleaningUp = fading = false;
            cleanupRemaining = fadeRemaining = fadeSeconds = 0;
            visual.color = launchColor;
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
