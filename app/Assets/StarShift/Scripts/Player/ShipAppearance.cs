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
        [SerializeField] private TrailRenderer[] engineTrails;
        [SerializeField] private SpriteRenderer approachHull;
        [SerializeField] private SpriteRenderer[] enginePlumes;
        [SerializeField] private SpriteRenderer[] approachPlumes;
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
            Place(enginePlumes); Place(approachPlumes);
            ConfigureTrails();
            ResetTrail(); age = 0;
        }
        private void Place(SpriteRenderer[] plumes)
        {
            for (int i = 0; plumes != null && i < plumes.Length; i++)
            {
                var socket = chassis.EngineSockets[i % chassis.EngineSockets.Length];
                plumes[i].transform.localPosition = socket + Vector2.down * (.35555f * chassis.PlumeScale);
                plumes[i].transform.localScale = Vector3.one * chassis.PlumeScale;
            }
            UpdateTrailPositions();
        }
        private void ConfigureTrails()
        {
            var all = GetAllTrails();
            if (all.Length == 0) return;
            var gradient = CreateTrailGradient(trail != null ? trail.Id : "trail-none");
            var duration = trail != null && trail.TrailSeconds > 0 ? trail.TrailSeconds : 0.22f;
            var startW = trail != null && trail.Id == "trail-ion" ? 0.26f : 0.22f;
            foreach (var tr in all)
            {
                if (tr == null) continue;
                tr.colorGradient = gradient;
                tr.time = duration;
                tr.startWidth = startW;
                tr.endWidth = 0.02f;
            }
        }
        private static Gradient CreateTrailGradient(string trailId)
        {
            var g = new Gradient();
            if (trailId == "trail-rainbow")
            {
                g.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(1f, 0.35f, 0.4f), 0.0f),    // coral
                        new GradientColorKey(new Color(1f, 0.75f, 0.25f), 0.2f),   // amber
                        new GradientColorKey(new Color(0.95f, 0.93f, 0.55f), 0.4f),// lemon
                        new GradientColorKey(new Color(0.25f, 0.85f, 0.87f), 0.6f),// cyan
                        new GradientColorKey(new Color(0.45f, 0.55f, 1f), 0.8f),   // sky blue
                        new GradientColorKey(new Color(0.75f, 0.5f, 0.95f), 1.0f)  // purple
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
                        new GradientColorKey(new Color(0.92f, 0.98f, 1f), 0.0f),    // bright white-cyan
                        new GradientColorKey(new Color(0.25f, 0.85f, 0.95f), 0.35f),// ion cyan
                        new GradientColorKey(new Color(0.07f, 0.4f, 0.55f), 1.0f)   // deep teal
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
                        new GradientColorKey(new Color(0.25f, 0.82f, 0.87f), 0.0f), // cyan
                        new GradientColorKey(new Color(0.1f, 0.38f, 0.47f), 1.0f)   // deep teal
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
        private TrailRenderer[] GetAllTrails()
        {
            if (engineTrails != null && engineTrails.Length > 0) return engineTrails;
            return engineTrail != null ? new[] { engineTrail } : System.Array.Empty<TrailRenderer>();
        }
        private void UpdateTrailPositions()
        {
            if (chassis == null || hull == null) return;
            var all = GetAllTrails();
            if (all.Length >= 2 && chassis.EngineSockets.Length >= 2)
            {
                for (int i = 0; i < 2; i++)
                    all[i].transform.position = hull.transform.TransformPoint(chassis.EngineSockets[i]);
            }
            else if (all.Length == 1 && chassis.EngineSockets.Length >= 2)
            {
                all[0].transform.position = hull.transform.TransformPoint((chassis.EngineSockets[0] + chassis.EngineSockets[1]) * .5f);
            }
        }
        public void ResetTrail()
        {
            foreach (var tr in GetAllTrails())
            {
                if (tr == null) continue;
                tr.emitting = false; tr.Clear();
            }
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
            previousPosition = transform.position;
            UpdateTrailPositions();
            var emitting = gate.CanInput && alive;
            foreach (var tr in GetAllTrails())
            {
                if (tr == null) continue;
                tr.emitting = emitting;
                if (!emitting) tr.Clear();
            }
        }
        private void Animate(SpriteRenderer[] plumes, bool visible)
        {
            for (int i = 0; plumes != null && i < plumes.Length; i++)
            {
                plumes[i].enabled = visible;
                // 2 tia lửa sau đít tàu là ngọn lửa động cơ phản lực cố định, không đổi khi đổi vệt động cơ
                if (catalog.EngineFrames.Length > 0)
                    plumes[i].sprite = catalog.EngineFrames[(int)(age / .075f + i) % catalog.EngineFrames.Length];
            }
        }
    }
}
