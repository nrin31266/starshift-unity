using System;
using StarShift.Gameplay;
using UnityEngine;
namespace StarShift.Enemies
{
    public sealed class BossDeathSequence : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] explosions;
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
        }
        private void Update()
        {
            if (!IsPlaying) return;
            elapsed += Time.deltaTime;
            for (int i = 0; i < explosions.Length; i++)
            {
                var local = elapsed - i * .35f;
                explosions[i].enabled = local >= 0 && local < .35f;
                explosions[i].transform.localScale = Vector3.one * Mathf.Max(.01f, local * (i == explosions.Length - 1 ? 7 : 3));
            }
            if (elapsed < duration) return;
            IsPlaying = false;
            view.TriggerReserveImpact();
            var callback = completed; completed = null; callback?.Invoke();
        }
        private void OnDisable() { IsPlaying = false; completed = null; foreach (var explosion in explosions) explosion.enabled = false; }
    }
}
