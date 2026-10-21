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
        private Vector3 launchScale;
        private Sprite launchSprite;
        private bool enteredView;

        public GameObject Owner { get; private set; }
        public float VisibleFlightTimeLimit
        {
            get
            {
                if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled || !gameplayCamera.orthographic || speed <= 0) return 0;
                var point = gameplayCamera.WorldToViewportPoint(new Vector3(body.position.x, body.position.y, transform.position.z));
                if (point.x < -.03f || point.x > 1.03f || point.y < -.03f || point.y > 1.03f ||
                    point.z < gameplayCamera.nearClipPlane || point.z > gameplayCamera.farClipPlane) return 0;
                var next = gameplayCamera.WorldToViewportPoint(new Vector3(body.position.x + direction.x * speed,
                    body.position.y + direction.y * speed, transform.position.z));
                var velocity = next - point;
                float x = Mathf.Abs(velocity.x) < .000001f ? float.PositiveInfinity
                    : ((velocity.x > 0 ? 1.03f : -.03f) - point.x) / velocity.x;
                float y = Mathf.Abs(velocity.y) < .000001f ? float.PositiveInfinity
                    : ((velocity.y > 0 ? 1.03f : -.03f) - point.y) / velocity.y;
                return Mathf.Max(0, Mathf.Min(x, y));
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitbox = GetComponent<BoxCollider2D>();
            visual = GetComponentInChildren<SpriteRenderer>();
            launchColor = visual.color;
            launchScale = visual.transform.localScale;
            launchSprite = visual.sprite;
        }

        public void Launch(SingleFireDefinition definition, Transform muzzle, GameObject owner,
            GameObject player, Action<EnemyProjectile> release, float aimOffset = 0, Vector2? forward = null)
        {
            Owner = owner;
            target = player;
            var aim = forward ?? (Vector2)(player.transform.position - muzzle.position);
            direction = aim.sqrMagnitude > .000001f ? aim.normalized : (Vector2)muzzle.up;
            direction = Quaternion.Euler(0, 0, aimOffset) * direction;
            speed = definition.ProjectileSpeed;
            remainingLifetime = definition.ProjectileLifetime;
            gameplayCamera = Camera.main;
            // Visual theo definition: coral/lemon normal, magenta special; prefab fallback giữ cũ.
            visual.color = definition.HasProjectileVisual ? definition.ProjectileColor : launchColor;
            visual.sprite = definition.HasProjectileVisual ? definition.ProjectileSprite : launchSprite;
            visual.transform.localScale = definition.HasProjectileVisual
                ? Vector3.Scale(launchScale, new Vector3(definition.ProjectileScale.x, definition.ProjectileScale.y, 1))
                : launchScale;
            onReleased = release;
            var rotation = Quaternion.FromToRotation(Vector3.up, direction);
            transform.SetPositionAndRotation(muzzle.position, rotation);
            body.position = muzzle.position;
            enteredView = VisibleFlightTimeLimit > 0;
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
            // Hết lifetime vẫn bay nếu còn trong view; qua mép thì recycle ngay, không fade.
            if (VisibleFlightTimeLimit > 0) enteredView = true;
            else if (enteredView || remainingLifetime <= 0) FinishFlight();
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
            gameplayCamera = null;
            direction = Vector2.zero;
            speed = remainingLifetime = 0;
            enteredView = false;
            visual.color = launchColor;
            visual.sprite = launchSprite;
            visual.transform.localScale = launchScale;
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
