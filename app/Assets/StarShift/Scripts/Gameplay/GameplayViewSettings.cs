using System;
using UnityEngine;

namespace StarShift.Gameplay
{
    [Serializable]
    public struct GameplayViewSettings
    {
        public const float BaselineSize = 5f;

        [SerializeField, Min(BaselineSize)] private float orthographicSize;
        [SerializeField, Tooltip("Normalized viewport: full = (0, 0, 1, 1); lower 70% = (0, 0, 1, 0.7).")]
        private Rect playableViewport;

        public float OrthographicSize => orthographicSize;
        public Rect PlayableViewport => playableViewport;
        public static GameplayViewSettings Baseline => new GameplayViewSettings(BaselineSize, new Rect(0, 0, 1, 1));

        public GameplayViewSettings(float size, Rect viewport)
        {
            orthographicSize = IsFinite(size) ? Mathf.Max(BaselineSize, size) : BaselineSize;
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
