using System;
using UnityEngine;

namespace StarShift.Gameplay
{
    [Serializable]
    public struct GameplayViewSettings
    {
        public const float BaselineSize = 6f;
        // Chặn projection suy biến; đây không phải giới hạn framing của gameplay.
        public const float MinimumSize = .01f;

        [SerializeField, Min(MinimumSize)] private float orthographicSize;
        [SerializeField, Tooltip("Normalized viewport: full = (0, 0, 1, 1); lower 70% = (0, 0, 1, 0.7).")]
        // Vùng được bay theo tỷ lệ màn hình; (0,0,1,0.7) chỉ cho dùng 70% phía dưới.
        private Rect playableViewport;

        public float OrthographicSize => orthographicSize;
        public Rect PlayableViewport => playableViewport;
        public static GameplayViewSettings Baseline => new GameplayViewSettings(BaselineSize, new Rect(0, 0, 1, 1));

        // Kiểm tra vùng cấu hình và cắt về khoảng 0..1; vùng rỗng hoặc sai dùng toàn màn hình.
        public GameplayViewSettings(float size, Rect viewport)
        {
            orthographicSize = IsFinite(size) && size > 0 ? Mathf.Max(MinimumSize, size) : BaselineSize;
            playableViewport = new Rect(0, 0, 1, 1);
            if (!IsFinite(viewport.x) || !IsFinite(viewport.y) ||
                !IsFinite(viewport.width) || !IsFinite(viewport.height)) return;
            var minimum = Vector2.Max(viewport.min, Vector2.zero);
            var maximum = Vector2.Min(viewport.max, Vector2.one);
            // Region rỗng/đảo ngược không có ý nghĩa gameplay; fallback full viewport.
            if (minimum.x < maximum.x && minimum.y < maximum.y)
                playableViewport = Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }

        public GameplayViewSettings Validated() => new GameplayViewSettings(orthographicSize, playableViewport);
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
