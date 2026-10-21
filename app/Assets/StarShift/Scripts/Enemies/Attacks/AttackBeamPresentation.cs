using UnityEngine;

namespace StarShift.Enemies
{
    public sealed class AttackBeamPresentation : MonoBehaviour
    {
        [SerializeField] private LineRenderer column;
        [SerializeField] private LineRenderer core;
        [SerializeField] private SpriteRenderer sweepArrow;
        private Camera gameplayCamera;
        private void Awake() => gameplayCamera = Camera.main;
        public void ShowSegment(Vector3 from, Vector3 to, float width, bool active, bool locked = true)
        {
            column.enabled = true;
            column.SetPosition(0, from); column.SetPosition(1, to);
            column.startWidth = column.endWidth = active ? width : Mathf.Min(width, locked ? .06f : .035f);
            column.startColor = column.endColor = active ? new Color(1,.25f,.65f,.88f+.1f*Mathf.Sin(Time.time*28)) : locked ? new Color(1,.65f,.25f,1) : new Color(1,.85f,.45f,.8f);
            core.enabled = active;
            if (!active) return;
            core.SetPosition(0, from); core.SetPosition(1, to);
            core.startWidth = core.endWidth = width * .25f;
            core.startColor = core.endColor = new Color(1,.94f,1,1);
        }

        public void ShowCurtain(Vector3 from, Vector3 to, float width, bool active, Color tint)
        {
            column.enabled = true;
            core.enabled = false;
            column.SetPosition(0, from); column.SetPosition(1, to);
            column.startWidth = column.endWidth = width;
            var fill = tint; fill.a = active ? .35f+.035f*Mathf.Sin(Time.time*20) : .09f;
            column.startColor = column.endColor = fill;
        }

        public void ShowSweepDirection(Vector3 from, Vector3 to, bool visible)
        {
            if (sweepArrow == null) return;
            sweepArrow.gameObject.SetActive(visible);
            sweepArrow.enabled = visible;
            if (!visible) return;
            var forward = (to - from).normalized;
            sweepArrow.transform.SetPositionAndRotation(from + forward * .7f, Quaternion.FromToRotation(Vector3.up, forward));
            if (gameplayCamera != null && sweepArrow.sprite != null)
            {
                float size = gameplayCamera.orthographicSize * 2 / 900 * 48;
                var scale = sweepArrow.transform.parent.lossyScale;
                var bounds = sweepArrow.sprite.bounds.size;
                sweepArrow.transform.localScale = new Vector3(size / Mathf.Max(.001f, bounds.x * Mathf.Abs(scale.x)), size / Mathf.Max(.001f, bounds.y * Mathf.Abs(scale.y)), 1);
            }
        }
        public void Hide() { column.enabled = false; core.enabled = false; if (sweepArrow != null) sweepArrow.gameObject.SetActive(false); }
        private void OnDisable() => Hide();
    }
}
