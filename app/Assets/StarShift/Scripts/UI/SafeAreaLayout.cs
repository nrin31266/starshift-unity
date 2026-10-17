using UnityEngine;

namespace StarShift.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        private RectTransform rect;
        private Rect previous;
        private Vector2Int previousSize;
        private void Awake() => rect = GetComponent<RectTransform>();
        private void OnEnable() => Apply();
        private void Update() => Apply();
        private void Apply()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var safe = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (safe == previous && size == previousSize) return;
            previous = safe;
            previousSize = size;
            rect.anchorMin = safe.min / new Vector2(size.x, size.y);
            rect.anchorMax = safe.max / new Vector2(size.x, size.y);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
