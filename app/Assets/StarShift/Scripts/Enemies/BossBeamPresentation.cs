using UnityEngine;

namespace StarShift.Enemies
{
    public sealed class BossBeamPresentation : MonoBehaviour
    {
        [SerializeField] private LineRenderer column;
        [SerializeField] private LineRenderer core;
        public void Show(Bounds bounds, bool active)
        {
            var top = new Vector3(bounds.center.x, bounds.max.y, 0);
            var bottom = new Vector3(bounds.center.x, bounds.min.y, 0);
            column.enabled = true;
            column.SetPosition(0, top); column.SetPosition(1, bottom);
            column.startWidth = column.endWidth = bounds.size.x;
            column.startColor = column.endColor = active ? new Color(1,.12f,.45f,.9f) : new Color(1,.12f,.45f,.3f);
            core.enabled = active;
            if (!active) return;
            core.SetPosition(0, top); core.SetPosition(1, bottom);
            core.startWidth = core.endWidth = bounds.size.x * .25f;
        }
        public void Hide() { column.enabled = false; core.enabled = false; }
        private void OnDisable() => Hide();
    }
}
