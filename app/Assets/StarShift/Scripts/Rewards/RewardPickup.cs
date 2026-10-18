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
        private Camera gameplayCamera;
        public PickupDefinition Item { get; private set; }
        public RewardDropTable Crate { get; private set; }
        public float Remaining => remaining;
        public EncounterObjectCategory Category => EncounterObjectCategory.Reward;
        public bool IsActive => active && isActiveAndEnabled;
        public void Launch(PickupDefinition item, RewardDropTable crate, Vector3 position, EncounterCleanup scope,
            PlayerPickupReceiver player, Func<RewardPickup, bool> onCollect, Action<RewardPickup> onRelease)
        {
            Item = item; Crate = crate; cleanup = scope; token = scope.Token; receiver = player;
            collect = onCollect; release = onRelease; remaining = crate != null ? 8 : item.Lifetime;
            graceRemaining = -1;
            previousPlayerPosition = player.transform.position;
            gameplayCamera = Camera.main;
            transform.position = position;
            baseColor = crate != null ? new Color(1, .78f, .2f) : item.Color;
            visual.color = baseColor;
            label.color = Color.white;
            label.text = crate != null ? "BOX" : item.ShortCode;
            active = cleanup.Register(this, token);
            gameObject.SetActive(active);
        }
        private void FixedUpdate()
        {
            if (!active) return;
            if (cleanup.Token != token) { Release(); return; }
            remaining -= Time.fixedDeltaTime;
            if (graceRemaining >= 0) graceRemaining = Mathf.Max(0, graceRemaining - Time.fixedDeltaTime);
            var visibleRemaining = graceRemaining >= 0 ? Mathf.Min(remaining, graceRemaining) : remaining;
            var opacity = Mathf.Clamp01(visibleRemaining / expiryFadeSeconds);
            var color = baseColor; color.a *= opacity; visual.color = color;
            label.color = new Color(1, 1, 1, opacity);
            if (remaining <= 0) { Release(); return; }
            var position = transform.position;
            position += Vector3.down * ((Item != null ? Item.DriftSpeed : .65f) * Time.fixedDeltaTime);
            var floor = gameplayCamera.ViewportToWorldPoint(new Vector3(0, .12f, -gameplayCamera.transform.position.z)).y;
            position.y = Mathf.Max(position.y, floor);
            transform.position = position;
            // Sweep đường đi của Player để drag nhanh vẫn nhận contact; không hút pickup.
            var current = receiver.transform.position;
            if (receiver.CanCollect && SegmentDistanceSquared(position, previousPlayerPosition, current) <= collectionRadius * collectionRadius)
                TryCollect();
            previousPlayerPosition = current;
        }
        private static float SegmentDistanceSquared(Vector3 point, Vector3 from, Vector3 to)
        {
            var delta = to - from;
            var t = delta.sqrMagnitude > .000001f ? Mathf.Clamp01(Vector3.Dot(point - from, delta) / delta.sqrMagnitude) : 0;
            return (point - (from + delta * t)).sqrMagnitude;
        }
        public bool TryCollect()
        {
            if (!active || cleanup.Token != token || !receiver.CanCollect) return false;
            // Khóa trước effect callback: EMP có thể giết enemy và eject thêm reward.
            active = false;
            if (!collect(this)) { active = true; return false; }
            Release(); return true;
        }
        public void StopHostility() { }
        public void PreserveForGrace(float seconds)
        {
            remaining = Mathf.Max(remaining, seconds + Time.fixedDeltaTime);
            graceRemaining = seconds;
        }
        public void Release()
        {
            active = false;
            cleanup?.Unregister(this);
            var callback = release;
            release = null; collect = null; Item = null; Crate = null; receiver = null; cleanup = null;
            if (callback != null) callback(this);
        }
        private void OnDisable() { if (active) Release(); }
    }
}
