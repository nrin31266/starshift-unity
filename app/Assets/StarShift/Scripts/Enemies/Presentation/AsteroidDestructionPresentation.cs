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
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Sprite[] enemyFrames;
        [SerializeField] private Sprite[] heavyFrames;
        [SerializeField, Tooltip("Burst riêng cho Supply Drone: hộp quà bung ra.")]
        private Sprite[] supplyFrames;
        [SerializeField, Min(.01f)] private float seconds = .35f;
        private EncounterRunner runner;
        private ObjectPool<SpriteRenderer> pool;
        private readonly List<Burst> active = new List<Burst>();
        private sealed class Burst { public SpriteRenderer Visual; public float Age; public float Scale; public float Seconds; public Sprite[] Frames; }

        private void Awake()
        {
            runner = GetComponent<EncounterRunner>();
            pool = new ObjectPool<SpriteRenderer>(() =>
            {
                var visual = new GameObject("DeathBurst").AddComponent<SpriteRenderer>();
                visual.transform.SetParent(transform, false);
                visual.sprite = burstSprite;
                visual.sortingOrder = 9;
                return visual;
            }, v => v.gameObject.SetActive(true), v => v.gameObject.SetActive(false), v => Destroy(v.gameObject));
        }
        private void OnEnable() { runner.EnemyDestroyed += Show; runner.RunStarted += Clear; }
        private void OnDisable() { runner.EnemyDestroyed -= Show; runner.RunStarted -= Clear; Clear(); }
        private void Show(EnemyHealth enemy, RewardDropTable table, Vector3 position)
        {
            bool supply = !enemy.Definition.BodyDamagesPlayer && table != null && table.Source == RewardSource.MajorCrate;
            bool asteroid = enemy.GetComponent<AsteroidVisualMotion>() != null;
            var visual = pool.Get();
            visual.transform.position = position;
            // Burst thuộc scene nên không biến mất khi hull trả về pool trong cùng frame.
            bool heavy = enemy.Definition.MissileTier == MissileDamageTier.Large;
            var chosen = supply ? supplyFrames : asteroid ? frames : heavy && heavyFrames != null && heavyFrames.Length > 0 ? heavyFrames : enemyFrames;
            visual.sprite = chosen != null && chosen.Length > 0 ? chosen[0] : burstSprite;
            visual.color = Color.white;
            active.Add(new Burst { Visual = visual, Frames = chosen, Seconds = !supply && !asteroid ? .5f : seconds,
                Scale = supply ? .9f : asteroid ? enemy.transform.localScale.x : Mathf.Clamp(enemy.transform.lossyScale.x * 1.3f, .9f, 2f) });
        }
        private void Update()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var burst = active[i]; burst.Age += Time.deltaTime;
                float t = Mathf.Clamp01(burst.Age / burst.Seconds);
                bool animated = burst.Frames != null && burst.Frames.Length > 0;
                if (animated) burst.Visual.sprite = burst.Frames[Mathf.Min(burst.Frames.Length - 1, (int)(t * burst.Frames.Length))];
                burst.Visual.transform.localScale = Vector3.one * burst.Scale * (animated ? 1 : Mathf.Lerp(.25f, 1.4f, t));
                burst.Visual.color = animated ? Color.white : new Color(1, Mathf.Lerp(.85f, .25f, t), .12f, 1 - t);
                if (t < 1) continue;
                pool.Release(burst.Visual); active.RemoveAt(i);
            }
        }
        private void Clear() { foreach (var burst in active) pool.Release(burst.Visual); active.Clear(); }
        private void OnDestroy() => pool?.Dispose();
    }
}
