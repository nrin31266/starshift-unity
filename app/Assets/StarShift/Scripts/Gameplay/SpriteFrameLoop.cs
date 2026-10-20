using UnityEngine;

namespace StarShift.Gameplay
{
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteFrameLoop : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private SpriteRenderer visibilitySource;
        [SerializeField, Min(.01f)] private float frameSeconds = .1f;
        private SpriteRenderer visual;
        private float age;
        public int FrameIndex { get; private set; }
        private void Awake() => visual = GetComponent<SpriteRenderer>();
        private void OnEnable() { age = 0; FrameIndex = 0; Apply(); }
        private void LateUpdate()
        {
            if (visibilitySource != null) visual.enabled = visibilitySource.enabled;
            if (frames == null || frames.Length == 0) return;
            age += Time.deltaTime;
            FrameIndex = (int)(age / frameSeconds) % frames.Length;
            Apply();
        }
        private void Apply()
        {
            if (visual != null && frames != null && frames.Length > 0) visual.sprite = frames[FrameIndex];
        }
        private void OnDisable() { age = 0; FrameIndex = 0; Apply(); }
        private void OnValidate() => frameSeconds = Mathf.Max(.01f, frameSeconds);
    }
}
