using UnityEngine;
using UnityEngine.UI;

namespace StarShift.UI
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-1), RequireComponent(typeof(CanvasGroup))]
    public sealed class GameplayAnnouncement : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField, Min(.1f)] private float duration = 1.6f;
        [SerializeField, Min(.01f)] private float fadeDuration = .2f;
        private CanvasGroup group;
        private float elapsed;
        private bool showing;
        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = group.interactable = false;
            Hide();
        }
        public void Show(string copy)
        {
            label.text = copy;
            elapsed = 0;
            showing = true;
            group.alpha = 0;
        }
        public void Hide()
        {
            showing = false;
            group.alpha = 0;
        }
        private void Update()
        {
            if (!showing) return;
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(Mathf.Min(elapsed, duration - elapsed) / fadeDuration);
            if (elapsed >= duration) Hide();
        }
        private void OnValidate()
        {
            duration = Mathf.Max(.1f, duration);
            fadeDuration = Mathf.Clamp(fadeDuration, .01f, duration / 2);
        }
    }
}
