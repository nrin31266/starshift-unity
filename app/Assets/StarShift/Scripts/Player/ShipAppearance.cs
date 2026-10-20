using StarShift.Persistence;
using UnityEngine;
using UnityEngine.Serialization;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    public sealed class ShipAppearance : MonoBehaviour
    {
        [SerializeField] private CosmeticCatalog catalog;
        [SerializeField] private CampaignLifecycle persistence;
        [SerializeField] private SpriteRenderer hull;
        [FormerlySerializedAs("engineTrail")]
        [SerializeField] private TrailRenderer movementTrail;
        [SerializeField] private SpriteRenderer approachHull;
        [SerializeField] private SpriteRenderer[] enginePlumes;
        [SerializeField] private SpriteRenderer[] approachPlumes;
        private PlayerGameplayGate gate;
        private CosmeticOption chassis, trail;
        private Vector3 previousPosition;
        private float age, bank;
        public CosmeticOption Chassis => chassis;
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
            Place(enginePlumes); Place(approachPlumes);
            ConfigureTrails();
            ResetTrail(); age = 0;
        }
        private void Place(SpriteRenderer[] plumes)
        {
            for (int i = 0; plumes != null && i < plumes.Length; i++)
            {
                if (plumes[i] == null) continue;
                if (chassis.EngineSockets.Length == 0) { plumes[i].enabled = false; continue; }
                var socket = chassis.EngineSockets[i % chassis.EngineSockets.Length];
                plumes[i].transform.localPosition = socket + Vector2.down * (.35555f * chassis.PlumeScale);
                plumes[i].transform.localScale = Vector3.one * chassis.PlumeScale;
            }
            UpdateTrailPositions();
        }
        private void ConfigureTrails()
        {
            if (movementTrail == null) return;
            movementTrail.colorGradient = CreateTrailGradient(trail.Id);
            movementTrail.time = Mathf.Min(trail.TrailSeconds > 0 ? trail.TrailSeconds : .12f, .16f);
            movementTrail.startWidth = trail.Id == "trail-ion" ? .09f : .07f;
            movementTrail.endWidth = 0;
        }
        private static Gradient CreateTrailGradient(string trailId)
        {
            var g = new Gradient();
            if (trailId == "trail-rainbow")
            {
                g.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(1f, 0.35f, 0.4f), 0.0f),
                        new GradientColorKey(new Color(1f, 0.75f, 0.25f), 0.2f),
                        new GradientColorKey(new Color(0.95f, 0.93f, 0.55f), 0.4f),
                        new GradientColorKey(new Color(0.25f, 0.85f, 0.87f), 0.6f),
                        new GradientColorKey(new Color(0.45f, 0.55f, 1f), 0.8f),
                        new GradientColorKey(new Color(0.75f, 0.5f, 0.95f), 1.0f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(0.85f, 0.0f),
                        new GradientAlphaKey(0.65f, 0.6f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
            }
            else if (trailId == "trail-ion")
            {
                g.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.92f, 0.98f, 1f), 0.0f),
                        new GradientColorKey(new Color(0.25f, 0.85f, 0.95f), 0.35f),
                        new GradientColorKey(new Color(0.07f, 0.4f, 0.55f), 1.0f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(0.95f, 0.0f),
                        new GradientAlphaKey(0.75f, 0.5f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
            }
            else
            {
                g.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.25f, 0.82f, 0.87f), 0.0f),
                        new GradientColorKey(new Color(0.1f, 0.38f, 0.47f), 1.0f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(0.8f, 0.0f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
            }
            return g;
        }
        private void UpdateTrailPositions()
        {
            if (movementTrail != null)
                movementTrail.transform.position = hull.transform.TransformPoint(chassis.MovementTrailSocket);
        }
        public void ResetTrail()
        {
            if (movementTrail == null) return;
            movementTrail.emitting = false;
            movementTrail.Clear();
            previousPosition = transform.position;
        }
        private void OnDisable()
        {
            ResetTrail();
            if (hull != null) hull.transform.localRotation = Quaternion.identity;
        }
        private void LateUpdate()
        {
            if (chassis == null) return;
            age += Time.deltaTime;
            var alive = gate.State != PlayerGameplayState.Dying && gate.State != PlayerGameplayState.GameOver;
            Animate(enginePlumes, alive && hull.enabled);
            Animate(approachPlumes, approachHull != null && approachHull.enabled);
            var lateral = Vector3.Dot(transform.position - previousPosition, hull.transform.parent.right);
            if (Time.deltaTime > 0) bank = Mathf.Lerp(bank, gate.CanInput ? Mathf.Clamp(-lateral / Time.deltaTime * 1.2f, -8, 8) : 0, 1 - Mathf.Exp(-Time.deltaTime * 12));
            hull.transform.localRotation = Quaternion.Euler(0, 0, bank);
            var moved = (transform.position - previousPosition).sqrMagnitude > .000001f;
            previousPosition = transform.position;
            UpdateTrailPositions();
            if (movementTrail != null)
            {
                movementTrail.emitting = gate.CanInput && alive && hull.enabled && moved && Time.deltaTime > 0;
                if (!gate.CanInput || !alive || !hull.enabled) movementTrail.Clear();
            }
        }
        private void Animate(SpriteRenderer[] plumes, bool visible)
        {
            for (int i = 0; plumes != null && i < plumes.Length; i++)
            {
                plumes[i].enabled = visible;
                // Plume thuộc chassis, không phụ thuộc cosmetic vệt di chuyển.
                if (catalog.EngineFrames.Length > 0)
                    plumes[i].sprite = catalog.EngineFrames[(int)(age / .075f + i) % catalog.EngineFrames.Length];
            }
        }
    }
}
