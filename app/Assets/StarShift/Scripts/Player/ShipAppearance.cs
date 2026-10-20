using StarShift.Persistence;
using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    public sealed class ShipAppearance : MonoBehaviour
    {
        [SerializeField] private CosmeticCatalog catalog;
        [SerializeField] private CampaignLifecycle persistence;
        [SerializeField] private SpriteRenderer hull;
        [SerializeField] private TrailRenderer engineTrail;
        [SerializeField] private SpriteRenderer approachHull;
        private Vector2 hullSize, approachSize;
        private PlayerGameplayGate gate;
        private bool showTrail;
        private void Awake()
        {
            gate = GetComponent<PlayerGameplayGate>();
            hullSize = Vector2.Scale(hull.sprite.bounds.size, hull.transform.localScale);
            approachSize = Vector2.Scale(approachHull.sprite.bounds.size, approachHull.transform.localScale);
        }
        private void Start() => Apply(persistence.PersistentSnapshot?.Profile);
        public void Apply(PlayerProfile profile)
        {
            var chassis = catalog.Equipped(profile, CosmeticSlot.Chassis);
            var tint = catalog.Equipped(profile, CosmeticSlot.Skin).Tint;
            ApplyHull(hull, chassis, tint, hullSize);
            ApplyHull(approachHull, chassis, tint, approachSize);
            GetComponent<PlayerDamagePresentation>().SetHullTint(tint);
            var trail = catalog.Equipped(profile, CosmeticSlot.Trail);
            showTrail = trail.Tint.a > 0;
            engineTrail.startColor = trail.Tint;
            engineTrail.endColor = new Color(trail.Tint.r, trail.Tint.g, trail.Tint.b, 0);
            engineTrail.Clear();
        }
        private static void ApplyHull(SpriteRenderer target, CosmeticOption chassis, Color tint, Vector2 envelope)
        {
            target.sprite = chassis.Sprite; target.flipY = chassis.FlipY; target.color = tint;
            var size = chassis.Sprite.bounds.size;
            target.transform.localScale = Vector3.one * Mathf.Min(envelope.x / size.x, envelope.y / size.y);
        }
        public void ResetTrail() { engineTrail.emitting = false; engineTrail.Clear(); }
        private void OnDisable() => ResetTrail();
        private void LateUpdate()
        {
            engineTrail.emitting = showTrail && gate.CanInput;
            if (!gate.CanInput) engineTrail.Clear();
        }
    }
}
