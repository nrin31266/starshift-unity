using UnityEngine;

namespace StarShift.Enemies
{
    public sealed class AttackBeamPresentation : MonoBehaviour
    {
        [SerializeField] private LineRenderer column;
        [SerializeField] private LineRenderer core;
        [SerializeField] private LineRenderer sweepGuide;
        public void Show(Bounds bounds, bool active)
        {
            var top = new Vector3(bounds.center.x, bounds.max.y, 0);
            var bottom = new Vector3(bounds.center.x, bounds.min.y, 0);
            ShowSegment(top, bottom, bounds.size.x, active);
        }
        public void ShowSegment(Vector3 from, Vector3 to, float width, bool active, bool locked = true)
        {
            column.enabled = true;
            column.SetPosition(0, from); column.SetPosition(1, to);
            column.startWidth = column.endWidth = active ? width : Mathf.Min(width, locked ? .06f : .035f);
            column.startColor = column.endColor = active ? new Color(1,.12f,.45f,.9f) : locked ? new Color(1,.55f,.12f,.85f) : new Color(1,.8f,.25f,.5f);
            core.enabled = active;
            if (!active) return;
            core.SetPosition(0, from); core.SetPosition(1, to);
            core.startWidth = core.endWidth = width * .25f;
        }

        public void ShowCurtain(Vector3 from, Vector3 to, float width, bool active, Color tint, bool positiveEdge)
        {
            column.enabled = core.enabled = true;
            column.SetPosition(0, from); column.SetPosition(1, to);
            column.startWidth = column.endWidth = width;
            var fill = tint; fill.a = active ? .13f : .035f;
            column.startColor = column.endColor = fill;
            var along = (to - from).normalized;
            var edge = new Vector3(-along.y, along.x, 0) * width * .5f * (positiveEdge ? 1 : -1);
            core.SetPosition(0, from + edge); core.SetPosition(1, to + edge);
            core.startWidth = core.endWidth = active ? .05f : .03f;
            tint.a = active ? .95f : .65f;
            core.startColor = core.endColor = tint;
        }

        public void ShowSweepDirection(Vector3 from, Vector3 to, bool visible)
        {
            if (sweepGuide == null) return;
            sweepGuide.enabled = visible;
            if (!visible) return;
            var forward = (to - from).normalized;
            var side = new Vector3(-forward.y, forward.x, 0);
            sweepGuide.SetPosition(0, from); sweepGuide.SetPosition(1, to);
            sweepGuide.SetPosition(2, to - forward * .35f + side * .20f);
            sweepGuide.SetPosition(3, to); sweepGuide.SetPosition(4, to - forward * .35f - side * .20f);
        }
        public void Hide() { column.enabled = false; core.enabled = false; if (sweepGuide != null) sweepGuide.enabled = false; }
        private void OnDisable() => Hide();
    }
}
