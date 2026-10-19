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
        [SerializeField] private Vector2 minorAnchor = new Vector2(.5f, .87f);
        [SerializeField, Min(1)] private int majorTitleSize = 42;
        [SerializeField, Min(1)] private int minorTitleSize = 25;
        private CanvasGroup group;
        private float elapsed;
        private float activeDuration;
        private bool showing;
        public bool IsMissionTitle { get; private set; }
        public bool IsPersistent { get; private set; }
        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.blocksRaycasts = group.interactable = false;
            Hide();
        }
        public void Show(string copy, string detail = "", float seconds = 0)
        {
            IsMissionTitle = copy != null && copy.StartsWith("NHIỆM VỤ");
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
            rect.anchorMin = rect.anchorMax = major ? majorAnchor : IsMissionTitle ? new Vector2(.5f,.67f) : minorAnchor;
            rect.pivot = major ? new Vector2(0, .5f) : new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            label.alignment = description.alignment = major ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            label.fontSize = major ? majorTitleSize : IsMissionTitle ? 42 : minorTitleSize;
            label.fontStyle = IsMissionTitle ? FontStyle.Bold : FontStyle.Normal;
            description.fontSize = IsMissionTitle ? 22 : 18;
            rect.sizeDelta = new Vector2(1100, major ? 180 : IsMissionTitle ? 120 : 80);
            var titleRect = (RectTransform)label.transform;
            var detailRect = (RectTransform)description.transform;
            titleRect.anchorMin = titleRect.anchorMax = detailRect.anchorMin = detailRect.anchorMax = new Vector2(.5f,.5f);
            titleRect.pivot = detailRect.pivot = new Vector2(.5f,.5f);
            titleRect.sizeDelta = new Vector2(1100, IsMissionTitle || major ? 58 : 36);
            detailRect.sizeDelta = new Vector2(1100,32);
            titleRect.anchoredPosition = new Vector2(0, IsMissionTitle || major ? 20 : 12);
            detailRect.anchoredPosition = new Vector2(0, IsMissionTitle || major ? -28 : -20);
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
            if (Time.timeScale <= 0) { group.alpha = 0; return; }
            elapsed += Time.deltaTime;
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
