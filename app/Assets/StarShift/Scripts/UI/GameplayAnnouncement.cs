using UnityEngine;
using UnityEngine.UI;

namespace StarShift.UI
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-1), RequireComponent(typeof(CanvasGroup))]
    public sealed class GameplayAnnouncement : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Text description;
        [SerializeField, Min(.1f)] private float duration = 1.6f;
        [SerializeField, Min(.01f)] private float fadeDuration = .2f;
        [SerializeField] private Vector2 majorAnchor = new Vector2(.065f, .4f);
        [SerializeField] private Vector2 minorAnchor = new Vector2(.5f, .5f);
        [SerializeField, Min(1)] private int majorTitleSize = 42;
        [SerializeField, Min(1)] private int minorTitleSize = 34;
        private CanvasGroup group;
        private float elapsed;
        private float activeDuration;
        private bool showing;
        public bool IsPersistent { get; private set; }
        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = group.interactable = false;
            Hide();
        }
        public void Show(string copy, string detail = "", float seconds = 0)
        {
            SetLayout(false);
            IsPersistent = false;
            label.text = copy;
            description.text = detail;
            elapsed = 0;
            activeDuration = seconds > 0 ? seconds : duration;
            showing = true;
            group.alpha = 0;
        }
        public void ShowPersistent(string copy, string detail = "")
        {
            SetLayout(true);
            IsPersistent = true;
            label.text = copy;
            description.text = detail;
            showing = false;
            group.alpha = 1;
        }
        private void SetLayout(bool major)
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = major ? majorAnchor : minorAnchor;
            rect.pivot = major ? new Vector2(0, .5f) : new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            label.alignment = description.alignment = major ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            label.fontSize = major ? majorTitleSize : minorTitleSize;
        }
        public void Hide()
        {
            IsPersistent = false;
            showing = false;
            group.alpha = 0;
        }
        private void Update()
        {
            if (!showing) return;
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(Mathf.Min(elapsed, activeDuration - elapsed) / Mathf.Min(fadeDuration, activeDuration / 2));
            if (elapsed >= activeDuration) Hide();
        }
        private void OnValidate()
        {
            duration = Mathf.Max(.1f, duration);
            fadeDuration = Mathf.Clamp(fadeDuration, .01f, duration / 2);
        }
    }
}
