using UnityEngine;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent, RequireComponent(typeof(EnemyHealth), typeof(EntryHoldFlight))]
    public sealed class BossShieldNetwork : MonoBehaviour
    {
        [SerializeField] private EnemyHealth[] nodes;
        [SerializeField] private SpriteRenderer shield;
        [SerializeField] private SpriteRenderer[] breaks;
        private bool[] intact;
        private float[] breakTimers;
        private float hitFlash;
        [SerializeField, Min(.1f)] private float collapseSeconds = 1.2f;
        private EnemyHealth core;
        private EntryHoldFlight flight;
        private float collapseRemaining;
        public int RemainingNodes { get; private set; }
        public int CurrentShieldHp { get { int total = 0; foreach (var node in nodes) if (node.IsAlive) total += node.CurrentHp; return total; } }
        public int MaximumShieldHp { get { int total = 0; foreach (var node in nodes) total += node.MaxHp; return total; } }
        public bool IsExposed => RemainingNodes == 0 && collapseRemaining <= 0;
        private void Awake()
        {
            core = GetComponent<EnemyHealth>(); flight = GetComponent<EntryHoldFlight>();
            if (nodes == null || nodes.Length == 0 || shield == null || breaks == null || breaks.Length != nodes.Length || System.Array.Exists(breaks, b => b == null) || System.Array.Exists(nodes, n => n == null || n == core))
                throw new System.InvalidOperationException("Invalid boss shield network.");
        }
        private void OnEnable()
        {
            hitFlash = 0;
            intact = new bool[nodes.Length]; breakTimers = new float[nodes.Length];
            for (int i = 0; i < nodes.Length; i++) { intact[i] = true; breaks[i].enabled = false; }
            core.BlockedHit += OnBlockedHit;
            RemainingNodes = nodes.Length; collapseRemaining = collapseSeconds; core.SetDamageBlocked(true);
            foreach (var node in nodes) { node.gameObject.SetActive(true); node.SetDamageBlocked(true); }
            shield.enabled = true;
        }
        private void FixedUpdate()
        {
            RemainingNodes = 0;
            hitFlash = Mathf.Max(0, hitFlash - Time.fixedDeltaTime);
            for (int i = 0; i < nodes.Length; i++)
            {
                var node = nodes[i];
                if (intact[i] && !node.IsAlive) { intact[i] = false; breakTimers[i] = .4f; }
                breakTimers[i] = Mathf.Max(0, breakTimers[i] - Time.fixedDeltaTime);
                breaks[i].enabled = breakTimers[i] > 0;
                breaks[i].transform.localScale = Vector3.one * (.5f + (1 - breakTimers[i] / .4f));
                node.SetDamageBlocked(!core.IsAlive || !flight.IsHolding);
                if (node.IsAlive) RemainingNodes++;
            }
            if (RemainingNodes == 0 && collapseRemaining == collapseSeconds) flight.Reposition(new Vector2(0, .035f), collapseSeconds);
            if (RemainingNodes == 0) collapseRemaining = Mathf.Max(0, collapseRemaining - Time.fixedDeltaTime);
            core.SetDamageBlocked(!IsExposed);
            shield.enabled = core.IsAlive && !IsExposed;
            if (shield.enabled) shield.color = new Color(.15f, .8f, 1, RemainingNodes == 0 ? .2f + .4f * Mathf.Abs(Mathf.Sin(collapseRemaining * 24)) : hitFlash > 0 ? .7f : .32f);
        }
        private void OnBlockedHit() => hitFlash = .1f;
        private void OnDisable() { if (core != null) core.BlockedHit -= OnBlockedHit; if (shield != null) shield.enabled = false; }
    }
}
