using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarShift.Enemies
{
    // Heavy/boss destruction: nổ cục bộ theo cụm module, mảnh văng và phá theo giai đoạn.
    // Chỉ chạy presentation; phase/death authority vẫn ở BossDeathSequence/EnemyHealth.
    [DisallowMultipleComponent]
    [AddComponentMenu("StarShift/Enemies/Staged Destruction Presentation")]
    public sealed class StagedDestructionPresentation : MonoBehaviour
    {
        [Serializable]
        private sealed class Stage
        {
            [Tooltip("Renderer cụm module bị phá ở giai đoạn này.")]
            public SpriteRenderer module;
            [SerializeField] public SpriteRenderer blast;
            [SerializeField] public Sprite[] blastFrames;
            [SerializeField] public Sprite[] fragmentFrames;
            [Min(0)] public float delay;
            [Min(.05f)] public float duration = .45f;
        }

        [SerializeField] private Stage[] stages;
        [SerializeField] private SpriteRenderer[] fragments;
        [SerializeField] private Sprite[] fragmentFrames;
        [SerializeField, Min(.1f)] private float totalSeconds = 2.4f;
        private float elapsed;
        private bool playing;

        public bool IsPlaying => playing;

        public void Begin()
        {
            if (playing) return;
            playing = true;
            elapsed = 0;
        }

        private void OnDisable()
        {
            playing = false;
            ResetVisuals();
        }

        private void Update()
        {
            if (!playing) return;
            elapsed += Time.deltaTime;
            for (int i = 0; i < stages.Length; i++)
            {
                var stage = stages[i];
                if (stage == null) continue;
                float local = elapsed - stage.delay;
                bool within = local >= 0 && local < stage.duration;
                if (stage.module != null && local >= 0) stage.module.enabled = false;
                if (stage.blast != null)
                {
                    stage.blast.enabled = within;
                    if (within && stage.blastFrames != null && stage.blastFrames.Length > 0)
                    {
                        float t = local / stage.duration;
                        stage.blast.sprite = stage.blastFrames[Mathf.Min(stage.blastFrames.Length - 1, (int)(t * stage.blastFrames.Length))];
                        stage.blast.transform.localScale = Vector3.one * Mathf.Lerp(.4f, 1.6f, t);
                    }
                }
                if (within && stage.fragmentFrames != null && stage.fragmentFrames.Length > 0 && fragments != null && i < fragments.Length && fragments[i] != null)
                {
                    float t = local / stage.duration;
                    var fragment = fragments[i];
                    fragment.enabled = true;
                    fragment.sprite = stage.fragmentFrames[Mathf.Min(stage.fragmentFrames.Length - 1, (int)(t * stage.fragmentFrames.Length))];
                    fragment.transform.localPosition = Vector3.Lerp(Vector3.zero, new Vector3(i % 2 == 0 ? -.4f : .4f, -.5f, 0), t);
                    fragment.transform.localRotation = Quaternion.Euler(0, 0, t * (i % 2 == 0 ? -120 : 120));
                }
            }
            for (int i = 0; i < fragments.Length; i++)
            {
                var fragment = fragments[i];
                if (fragment == null) continue;
                if (fragmentFrames != null && fragmentFrames.Length > 0)
                    fragment.sprite = fragmentFrames[Mathf.Min(fragmentFrames.Length - 1, (int)(elapsed / Mathf.Max(.05f, totalSeconds) * fragmentFrames.Length))];
            }
            if (elapsed >= totalSeconds) playing = false;
        }

        private void ResetVisuals()
        {
            if (stages != null)
                foreach (var stage in stages)
                {
                    if (stage == null) continue;
                    if (stage.module != null) stage.module.enabled = true;
                    if (stage.blast != null) stage.blast.enabled = false;
                }
            if (fragments != null)
                foreach (var fragment in fragments) if (fragment != null) fragment.enabled = false;
            elapsed = 0;
        }
    }
}
