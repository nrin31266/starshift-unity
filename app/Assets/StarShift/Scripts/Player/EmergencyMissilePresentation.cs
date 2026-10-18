using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    public sealed class EmergencyMissilePresentation : MonoBehaviour
    {
        [SerializeField] private EmergencyMissileReserve reserve;
        [SerializeField] private LineRenderer burst;
        [SerializeField] private StarShift.Gameplay.GameplayViewController view;
        [SerializeField] private StarShift.Persistence.CampaignLifecycle persistence;
        [SerializeField] private SpriteRenderer rocket;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private SpriteRenderer flash;
        [SerializeField, Min(.05f)] private float duration = .45f;
        private float remaining;
        private void OnEnable() { reserve.Fired += OnFired; reserve.Detonated += OnDetonated; ResetForSession(); }
        private void OnFired()
        {
            ResetForSession();
            if (rocket != null) rocket.enabled = true;
        }
        private void OnDetonated()
        {
            if (rocket != null) rocket.enabled = false;
            if (trail != null) trail.emitting = false;
            remaining = duration; burst.enabled = true;
            if (persistence.PersistentSnapshot?.Settings.ScreenShakeEnabled != false) view.TriggerReserveImpact();
        }
        public void ResetForSession()
        {
            remaining = 0;
            if (burst != null) burst.enabled = false;
            if (rocket != null) rocket.enabled = false;
            if (flash != null) flash.enabled = false;
            if (trail != null) { trail.emitting = false; trail.Clear(); }
            if (view != null) view.CancelImpact();
        }
        private void LateUpdate()
        {
            var camera = Camera.main;
            if (reserve.IsInFlight && rocket != null)
            {
                var from = camera.ViewportToWorldPoint(new Vector3(reserve.LaunchViewport.x, reserve.LaunchViewport.y, -camera.transform.position.z));
                var center = camera.ViewportToWorldPoint(new Vector3(.5f, .5f, -camera.transform.position.z));
                var progress = reserve.FlightProgress;
                rocket.transform.position = Vector3.Lerp(from, center, progress * progress);
                var direction = center - from;
                if (direction.sqrMagnitude > .0001f) rocket.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
                rocket.enabled = true;
                if (trail != null) trail.emitting = Time.timeScale > 0;
            }
            else if (rocket != null) { rocket.enabled = false; if (trail != null) trail.emitting = false; }
            if (remaining <= 0) return;
            remaining = Mathf.Max(0, remaining - Time.deltaTime);
            var t = 1 - remaining / duration;
            burst.transform.position = camera.ViewportToWorldPoint(new Vector3(.5f,.5f, -camera.transform.position.z));
            burst.transform.localScale = Vector3.one * Mathf.Lerp(.3f, camera.orthographicSize * camera.aspect * 4, t);
            burst.startColor = burst.endColor = new Color(1,.65f,.2f, .8f*(1-t));
            burst.widthMultiplier = Mathf.Lerp(.09f,.025f,t);
            burst.enabled = remaining > 0;
            if (flash != null)
            {
                flash.transform.position = burst.transform.position;
                flash.transform.localScale = Vector3.one * Mathf.Lerp(.5f, 2.4f, t);
                flash.color = new Color(1, .85f, .45f, Mathf.Clamp01(1 - t * 4) * .75f);
                flash.enabled = flash.color.a > 0;
            }
        }
        private void OnDisable() { reserve.Fired -= OnFired; reserve.Detonated -= OnDetonated; ResetForSession(); }
    }
}
