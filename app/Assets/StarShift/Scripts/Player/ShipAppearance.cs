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
        [SerializeField] private SpriteRenderer[] enginePlumes;
        [SerializeField] private SpriteRenderer[] approachPlumes;
        [SerializeField] private SpriteRenderer[] ribbons;
        [SerializeField] private SpriteRenderer[] approachRibbons;
        private PlayerGameplayGate gate;
        private CosmeticOption chassis, trail;
        private Vector3 previousPosition;
        private float age, bank;
        public CosmeticOption Chassis => chassis;
        public Vector3 MuzzlePosition => hull.transform.TransformPoint(chassis.MuzzleSocket);
        private void Awake() { gate = GetComponent<PlayerGameplayGate>(); previousPosition = transform.position; }
        private void Start() => Apply(persistence.PersistentSnapshot?.Profile);
        public void Apply(PlayerProfile profile)
        {
            chassis = catalog.Equipped(profile, CosmeticSlot.Chassis);
            var amber = catalog.Equipped(profile, CosmeticSlot.Skin).Id == "skin-amber";
            trail = catalog.Equipped(profile, CosmeticSlot.Trail);
            foreach (var target in new[] { hull, approachHull })
            {
                if (target == null) continue;
                target.sprite = amber && chassis.AmberSprite != null ? chassis.AmberSprite : chassis.Sprite;
                target.flipY = chassis.FlipY; target.color = Color.white;
            }
            GetComponent<PlayerDamagePresentation>().SetHullTint(Color.white);
            Place(enginePlumes, ribbons); Place(approachPlumes, approachRibbons);
            engineTrail.startColor = trail.Tint;
            engineTrail.endColor = new Color(trail.Tint.r, trail.Tint.g, trail.Tint.b, 0);
            engineTrail.time = trail.TrailSeconds;
            ResetTrail(); age = 0;
        }
        private void Place(SpriteRenderer[] plumes, SpriteRenderer[] tails)
        {
            for (int i = 0; plumes != null && i < plumes.Length; i++)
            {
                var socket = chassis.EngineSockets[i % chassis.EngineSockets.Length];
                plumes[i].transform.localPosition = socket + Vector2.down * (.35555f * chassis.PlumeScale);
                plumes[i].transform.localScale = Vector3.one * chassis.PlumeScale;
                tails[i].transform.localPosition = socket + Vector2.down * (trail.Id == "trail-none" ? .34f : .48f);
                tails[i].transform.localScale = new Vector3(chassis.PlumeScale, trail.Id == "trail-none" ? .45f : .7f, 1);
            }
            engineTrail.transform.position = hull.transform.TransformPoint((chassis.EngineSockets[0] + chassis.EngineSockets[1]) * .5f);
        }
        public void ResetTrail() { if (engineTrail == null) return; engineTrail.emitting = false; engineTrail.Clear(); }
        private void OnDisable() { ResetTrail(); if (hull != null) hull.transform.localRotation = Quaternion.identity; }
        private void LateUpdate()
        {
            if (chassis == null) return;
            age += Time.deltaTime;
            var alive = gate.State != PlayerGameplayState.Dying && gate.State != PlayerGameplayState.GameOver;
            Animate(enginePlumes, ribbons, alive && hull.enabled);
            Animate(approachPlumes, approachRibbons, approachHull != null && approachHull.enabled);
            var lateral = Vector3.Dot(transform.position - previousPosition, hull.transform.parent.right);
            if (Time.deltaTime > 0) bank = Mathf.Lerp(bank, gate.CanInput ? Mathf.Clamp(-lateral / Time.deltaTime * 1.2f, -8, 8) : 0, 1 - Mathf.Exp(-Time.deltaTime * 12));
            hull.transform.localRotation = Quaternion.Euler(0, 0, bank);
            previousPosition = transform.position;
            engineTrail.emitting = gate.CanInput && alive;
            if (!engineTrail.emitting) engineTrail.Clear();
        }
        private void Animate(SpriteRenderer[] plumes, SpriteRenderer[] tails, bool visible)
        {
            for (int i = 0; plumes != null && i < plumes.Length; i++)
            {
                plumes[i].enabled = tails[i].enabled = visible;
                if (catalog.EngineFrames.Length > 0) plumes[i].sprite = catalog.EngineFrames[(int)(age / .075f + i) % catalog.EngineFrames.Length];
                if (trail.AnimationFrames.Length > 0) tails[i].sprite = trail.AnimationFrames[(int)(age / .08f + i) % trail.AnimationFrames.Length];
            }
        }
    }
}
