using UnityEngine;
namespace StarShift.Enemies
{
    [RequireComponent(typeof(EntryHoldFlight))]
    public sealed class EnemyEntryTelegraph : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer cue;
        private EntryHoldFlight flight;
        private Camera view;
        private void Awake() { flight = GetComponent<EntryHoldFlight>(); view = Camera.main; }
        private void LateUpdate()
        {
            cue.enabled = flight.PhaseIndex == 0;
            if (!cue.enabled) return;
            var point = view.WorldToViewportPoint(transform.position);
            point.x = Mathf.Clamp01(point.x); point.y = .96f;
            cue.transform.position = view.ViewportToWorldPoint(point);
        }
        private void OnDisable() => cue.enabled = false;
    }
}
