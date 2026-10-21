using UnityEngine;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent, RequireComponent(typeof(EnemyHealth))]
    public sealed class BossArmorBreakPresentation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] fragments;
        [SerializeField] private SpriteRenderer[] bursts;
        [SerializeField] private Sprite[] burstFrames;
        [SerializeField] private Vector2[] anchors = { new Vector2(-2.8f, 0), new Vector2(2.8f, 0) };
        [SerializeField] private float[] healthFractions = { .5f, .25f };
        [SerializeField, Min(.1f)] private float seconds = .75f;
        private EnemyHealth health;
        private readonly bool[] emitted = new bool[2];
        private readonly bool[] playing = new bool[2];
        private readonly float[] elapsed = new float[2];
        private readonly Vector3[] origins = new Vector3[2];

        private void Awake() => health = GetComponent<EnemyHealth>();
        private void OnEnable() { Clear(); health.Damaged += OnDamaged; }
        private void OnDisable() { health.Damaged -= OnDamaged; Clear(); }
        private void OnDamaged()
        {
            for (int i = 0; i < 2; i++)
                if (!emitted[i] && health.CurrentHp <= health.MaxHp * healthFractions[i])
                {
                    emitted[i] = playing[i] = true; elapsed[i] = 0;
                    origins[i] = transform.TransformPoint(anchors[i]);
                }
        }
        private void LateUpdate()
        {
            for (int i = 0; i < 2; i++)
            {
                if (!playing[i]) continue;
                elapsed[i] += Time.deltaTime;
                float t = elapsed[i] / seconds;
                if (t >= 1) { playing[i] = false; fragments[i].enabled = bursts[i].enabled = false; continue; }
                fragments[i].enabled = true;
                var outward = transform.right * (i == 0 ? -1 : 1);
                fragments[i].transform.position = origins[i] + outward * (t * 2) - transform.up * (t * t);
                fragments[i].transform.rotation = transform.rotation * Quaternion.Euler(0, 0, (i == 0 ? -1 : 1) * t * 200);
                fragments[i].transform.localScale = Vector3.one / Mathf.Max(.01f, transform.lossyScale.x);
                fragments[i].color = new Color(1, 1, 1, Mathf.Clamp01((1 - t) * 2));
                bursts[i].enabled = t < .5f;
                bursts[i].transform.position = origins[i];
                if (bursts[i].enabled && burstFrames.Length > 0)
                    bursts[i].sprite = burstFrames[Mathf.Min(burstFrames.Length - 1, (int)(t * 2 * burstFrames.Length))];
            }
        }
        private void Clear()
        {
            for (int i = 0; i < 2; i++)
            {
                emitted[i] = playing[i] = false; elapsed[i] = 0;
                if (fragments != null && i < fragments.Length && fragments[i] != null) fragments[i].enabled = false;
                if (bursts != null && i < bursts.Length && bursts[i] != null) bursts[i].enabled = false;
            }
        }
    }
}
