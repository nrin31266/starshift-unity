using System.Collections.Generic;
using StarShift.Encounters;
using StarShift.Rewards;
using UnityEngine;
using UnityEngine.Pool;

namespace StarShift.Enemies
{
    [RequireComponent(typeof(EncounterRunner))]
    public sealed class AsteroidDestructionPresentation : MonoBehaviour
    {
        [SerializeField] private Sprite burstSprite;
        [SerializeField, Min(.01f)] private float seconds = .35f;
        private EncounterRunner runner;
        private ObjectPool<SpriteRenderer> pool;
        private readonly List<Burst> active = new List<Burst>();
        private sealed class Burst { public SpriteRenderer Visual; public float Age; public float Scale; }

        private void Awake()
        {
            runner = GetComponent<EncounterRunner>();
            pool = new ObjectPool<SpriteRenderer>(() =>
            {
                var visual = new GameObject("AsteroidBurst").AddComponent<SpriteRenderer>();
                visual.transform.SetParent(transform, false);
                visual.sprite = burstSprite;
                visual.sortingOrder = 9;
                return visual;
            }, v => v.gameObject.SetActive(true), v => v.gameObject.SetActive(false), v => Destroy(v.gameObject));
        }
        private void OnEnable() { runner.EnemyDefeated += Show; runner.RunStarted += Clear; }
        private void OnDisable() { runner.EnemyDefeated -= Show; runner.RunStarted -= Clear; Clear(); }
        private void Show(EnemyHealth enemy, RewardDropTable table, Vector3 position)
        {
            bool supply = !enemy.Definition.BodyDamagesPlayer && table != null && table.Source == RewardSource.MajorCrate;
            if (!supply && enemy.GetComponent<AsteroidVisualMotion>() == null) return;
            var visual = pool.Get();
            visual.transform.position = position;
            active.Add(new Burst { Visual = visual, Scale = supply ? .6f : enemy.transform.localScale.x });
        }
        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var burst = active[i]; burst.Age += Time.deltaTime;
                float t = Mathf.Clamp01(burst.Age / seconds);
                burst.Visual.transform.localScale = Vector3.one * burst.Scale * Mathf.Lerp(.25f, 1.4f, t);
                burst.Visual.color = new Color(1, Mathf.Lerp(.85f, .25f, t), .12f, 1 - t);
                if (t < 1) continue;
                pool.Release(burst.Visual); active.RemoveAt(i);
            }
        }
        private void Clear() { foreach (var burst in active) pool.Release(burst.Visual); active.Clear(); }
        private void OnDestroy() => pool?.Dispose();
    }
}
