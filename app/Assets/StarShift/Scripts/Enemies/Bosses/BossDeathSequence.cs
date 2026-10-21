using System;
using StarShift.Gameplay;
using UnityEngine;
namespace StarShift.Enemies
{
    public sealed class BossDeathSequence : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] explosions;
        [SerializeField] private Sprite[] explosionFrames;
        [SerializeField, Tooltip("Staged module/fragment destruction; tùy chọn, không đổi timing death.")]
        private StagedDestructionPresentation staged;
        [SerializeField, Min(.1f)] private float duration = 2.4f;
        private float elapsed;
        private Action completed;
        private GameplayViewController view;
        public bool IsPlaying { get; private set; }
        public void Begin(Action onCompleted)
        {
            if (IsPlaying) return;
            IsPlaying = true; elapsed = 0; completed = onCompleted;
            view = Camera.main.GetComponent<GameplayViewController>();
            staged?.Begin();
        }
        private void Update()
        {
            if (!IsPlaying) return;
            elapsed += Time.deltaTime;
            for (int i = 0; i < explosions.Length; i++)
            {
                bool final = i == explosions.Length - 1;
                float seconds = final ? .5f : .35f;
                var local = elapsed - (final ? duration - seconds : i * .35f);
                explosions[i].enabled = local >= 0 && local < seconds;
                if (!explosions[i].enabled) continue;
                float t = local / seconds;
                explosions[i].transform.localScale = Vector3.one * Mathf.Lerp(.3f, final ? 9.6f : 1.1f, t);
                if (explosionFrames != null && explosionFrames.Length > 0)
                    explosions[i].sprite = explosionFrames[Mathf.Min(explosionFrames.Length - 1, (int)(t * explosionFrames.Length))];
            }
            if (elapsed < duration) return;
            IsPlaying = false;
            view.TriggerReserveImpact();
            var callback = completed; completed = null; callback?.Invoke();
        }
        private void OnDisable() { IsPlaying = false; completed = null; foreach (var explosion in explosions) explosion.enabled = false; }
    }
}
