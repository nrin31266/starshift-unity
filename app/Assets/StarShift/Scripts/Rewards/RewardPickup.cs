using System;
using StarShift.Encounters;
using StarShift.Player;
using UnityEngine;

namespace StarShift.Rewards
{
    [DisallowMultipleComponent]
    public sealed class RewardPickup : MonoBehaviour, IEncounterBoundaryObject
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private TextMesh label;
        [SerializeField, Min(.1f)] private float collectionRadius = .6f;
        [SerializeField, Min(.01f)] private float expiryFadeSeconds = .3f;
        private EncounterCleanup cleanup;
        private PlayerPickupReceiver receiver;
        private Func<RewardPickup, bool> collect;
        private Action<RewardPickup> release;
        private int token;
        private bool active;
        private float remaining;
        private float graceRemaining = -1;
        private Color baseColor;
        private Vector3 previousPlayerPosition;
        private Vector3 previousPickupPosition;
        private float fallVelocity;
        private int bounceCount;
        private Camera gameplayCamera;
        private Vector2 ejectionVelocity;
        private Vector3 visualScale;
        private bool visualInitialized;
        private float ejectionRemaining;
        public void Eject(Vector2 velocity) { ejectionVelocity = velocity; ejectionRemaining = .45f; }
        public PickupDefinition Item { get; private set; }
        public RewardDropTable Crate { get; private set; }
        public float Remaining => remaining;
        public EncounterObjectCategory Category => EncounterObjectCategory.Reward;
        public bool IsActive => active && isActiveAndEnabled;
        public event Action Collected;
        public void Launch(PickupDefinition item, RewardDropTable crate, Vector3 position, EncounterCleanup scope,
            PlayerPickupReceiver player, Func<RewardPickup, bool> onCollect, Action<RewardPickup> onRelease)
        {
            if (!visualInitialized) { visualScale = visual.transform.localScale; visualInitialized = true; }
            Item = item; Crate = crate; cleanup = scope; token = scope.RewardToken; receiver = player;
            collect = onCollect; release = onRelease; remaining = crate != null ? 8 : item.Lifetime;
            graceRemaining = -1;
            previousPlayerPosition = player.transform.position;
            previousPickupPosition = position;
            fallVelocity = -(item != null ? item.DriftSpeed : .8f);
            bounceCount = 0;
            ejectionVelocity = Vector2.zero; ejectionRemaining = 0;
            Collected = null;
            gameplayCamera = Camera.main;
            transform.position = position;
            baseColor = crate != null ? new Color(1, .78f, .2f) : item.Color;
            visual.color = baseColor;
            visual.transform.localScale = visualScale * (item != null ? item.VisualScale : 1);
            label.color = Color.white;
            label.text = crate != null ? "BOX" : item.ShortCode;
            active = cleanup.Register(this, scope.Token);
            gameObject.SetActive(active);
        }
        private void FixedUpdate()
        {
            if (!active) return;
            if (cleanup.RewardToken != token) { Release(); return; }
            remaining -= Time.fixedDeltaTime;
            if (graceRemaining >= 0) graceRemaining = Mathf.Max(0, graceRemaining - Time.fixedDeltaTime);
            var visibleRemaining = graceRemaining >= 0 ? Mathf.Min(remaining, graceRemaining) : remaining;
            var opacity = Mathf.Clamp01(visibleRemaining / expiryFadeSeconds);
            var color = baseColor; color.a *= opacity; visual.color = color;
            label.color = new Color(1, 1, 1, opacity);
            if (remaining <= 0) { Release(); return; }
            var position = transform.position;
            if (ejectionRemaining > 0)
            {
                position += (Vector3)ejectionVelocity * (ejectionRemaining / .45f) * Time.fixedDeltaTime;
                ejectionRemaining = Mathf.Max(0, ejectionRemaining - Time.fixedDeltaTime);
            }
            bool shard = Item != null && Item.Kind == PickupKind.Shard;
            if (shard)
            {
                fallVelocity = Mathf.Max(-Item.MaximumFallSpeed, fallVelocity - Item.FallAcceleration * Time.fixedDeltaTime);
                position.y += fallVelocity * Time.fixedDeltaTime;
                var floor = gameplayCamera.ViewportToWorldPoint(new Vector3(0, .06f, -gameplayCamera.transform.position.z)).y;
                if (position.y <= floor && fallVelocity < 0 && bounceCount < 2)
                {
                    position.y = floor;
                    var height = Item.ShardBounceHeight * gameplayCamera.orthographicSize * 2 * (bounceCount == 0 ? 1 : .35f);
                    fallVelocity = Mathf.Sqrt(2 * Item.FallAcceleration * height);
                    bounceCount++;
                }
            }
            else position += Vector3.down * ((Item != null ? Item.DriftSpeed : .8f) * Time.fixedDeltaTime);
            if (graceRemaining >= 0)
            {
                var viewport = gameplayCamera.WorldToViewportPoint(position);
                viewport.x = Mathf.Clamp(viewport.x, .05f, .95f);
                viewport.y = Mathf.Max(.09f, viewport.y);
                position = gameplayCamera.ViewportToWorldPoint(viewport);
                if (viewport.y <= .09f) fallVelocity = 0;
            }
            transform.position = position;
            // Sweep đường đi của Player để drag nhanh vẫn nhận contact; không hút pickup.
            var current = receiver.transform.position;
            if (receiver.CanCollect && SegmentDistanceSquared(Vector3.zero,
                previousPlayerPosition - previousPickupPosition, current - position) <= collectionRadius * collectionRadius)
                TryCollect();
            previousPlayerPosition = current;
            previousPickupPosition = position;
            if (active && gameplayCamera.WorldToViewportPoint(position).y < -.06f) Release();
        }
        private static float SegmentDistanceSquared(Vector3 point, Vector3 from, Vector3 to)
        {
            var delta = to - from;
            var t = delta.sqrMagnitude > .000001f ? Mathf.Clamp01(Vector3.Dot(point - from, delta) / delta.sqrMagnitude) : 0;
            return (point - (from + delta * t)).sqrMagnitude;
        }
        public bool TryCollect()
        {
            if (!active || cleanup.RewardToken != token || !receiver.CanCollect) return false;
            // Khóa trước callback để effect không collect trùng khi eject thêm reward.
            active = false;
            if (!collect(this)) { active = true; return false; }
            Collected?.Invoke();
            Release(); return true;
        }
        public void StopHostility() { }
        public void PreserveForGrace(float seconds)
        {
            remaining = seconds;
            graceRemaining = seconds;
        }
        public void Release()
        {
            active = false;
            cleanup?.Unregister(this);
            var callback = release;
            release = null; collect = null; Item = null; Crate = null; receiver = null; cleanup = null;
            Collected = null;
            if (callback != null) callback(this);
        }
        private void OnDisable() { if (active) Release(); }
    }
}
