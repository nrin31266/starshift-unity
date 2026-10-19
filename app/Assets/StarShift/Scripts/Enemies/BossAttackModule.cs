using StarShift.Combat;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEngine;
using UnityEngine.Pool;

namespace StarShift.Enemies
{
    public sealed class BossAttackModule : MonoBehaviour
    {
        [SerializeField] private SingleFireDefinition definition;
        [SerializeField] private Transform[] muzzles;
        [SerializeField] private SpriteRenderer telegraph;
        [SerializeField] private SpriteRenderer[] extraTelegraphs = System.Array.Empty<SpriteRenderer>();
        private Color[] extraIdleColors;
        [SerializeField] private BossBeamPresentation beam;
        [SerializeField, Min(.1f)] private float beamSeconds = 1.5f;
        [SerializeField, Range(.03f,.2f)] private float beamViewportWidth = .10f;
        private readonly Collider2D[] beamHits = new Collider2D[8];
        private ContactFilter2D beamFilter;
        private Camera gameplayCamera;
        private EntryHoldFlight flight;
        private float beamX;
        public bool IsBeamActive { get; private set; }
        public Bounds BeamBounds { get; private set; }
        private EnemyHealth health;
        private EnemyFireGate gate;
        private PlayerGameplayGate player;
        private ObjectPool<EnemyProjectile> pool;
        private Transform projectileRoot;
        private Color idleColor;
        private float remaining;
        private bool shuttingDown;
        public bool IsBusy { get; private set; }
        public int ShotsFired { get; private set; }
        private void Awake()
        {
            health = GetComponentInParent<EnemyHealth>();
            flight = health.GetComponent<EntryHoldFlight>();
            gate = FindAnyObjectByType<EnemyFireGate>();
            player = FindAnyObjectByType<PlayerGameplayGate>();
            idleColor = telegraph.color;
            extraIdleColors = System.Array.ConvertAll(extraTelegraphs, cue => cue.color);
            gameplayCamera = Camera.main;
            beamFilter.SetLayerMask(1 << player.GetComponentInChildren<Collider2D>().gameObject.layer);
            beamFilter.useTriggers = true;
            if (beam != null) { beam.Hide(); return; }
            projectileRoot = new GameObject("BossProjectilePool").transform;
            projectileRoot.SetParent(health.transform.parent, false);
            pool = new ObjectPool<EnemyProjectile>(() =>
            {
                var shot = Instantiate(definition.ProjectilePrefab, projectileRoot);
                shot.ResetForPool(); return shot;
            }, null, shot => shot.ResetForPool(), shot => { if (shot != null) Destroy(shot.gameObject); }, true, 8, 32);
        }
        public bool BeginAttack()
        {
            if (IsBusy || !health.IsAlive || !gate.CanFire) return false;
            IsBusy = true; IsBeamActive = false; remaining = definition.TelegraphDuration;
            if (beam != null)
            {
                flight.SetMotionPaused(true);
                beamX = gameplayCamera.WorldToViewportPoint(muzzles[0].position).x;
            }
            return true;
        }
        public void CancelAttack()
        {
            IsBusy = IsBeamActive = false;
            if (telegraph != null) telegraph.color = idleColor;
            if (extraIdleColors != null) for (int i = 0; i < extraTelegraphs.Length; i++) extraTelegraphs[i].color = extraIdleColors[i];
            if (beam != null) { beam.Hide(); flight?.SetMotionPaused(false); }
        }
        private void FixedUpdate()
        {
            if (!IsBusy) return;
            if (!health.IsAlive || !gate.CanFire) { CancelAttack(); return; }
            remaining -= Time.fixedDeltaTime;
            telegraph.color = Color.Lerp(idleColor, Color.white, .5f + .5f * Mathf.Sin(remaining * 24));
            for (int i = 0; i < extraTelegraphs.Length; i++) extraTelegraphs[i].color = Color.Lerp(extraIdleColors[i], Color.white, .5f + .5f * Mathf.Sin(remaining * 24));
            if (beam != null)
            {
                var top = muzzles[0].position;
                var bottom = gameplayCamera.ViewportToWorldPoint(new Vector3(beamX, -.02f, -gameplayCamera.transform.position.z));
                float width = beamViewportWidth * gameplayCamera.orthographicSize * 2 * gameplayCamera.aspect;
                BeamBounds = new Bounds(new Vector3(bottom.x,(top.y+bottom.y)*.5f,0),new Vector3(width,top.y-bottom.y,1));
                beam.Show(BeamBounds, IsBeamActive);
                if (IsBeamActive)
                {
                    int count = Physics2D.OverlapBox(BeamBounds.center, BeamBounds.size, 0, beamFilter, beamHits);
                    for (int i = 0; i < count; i++) beamHits[i].GetComponentInParent<PlayerContactReceiver>()?.ReceiveEnemyHit(health.gameObject);
                }
                if (remaining > 0) return;
                if (!IsBeamActive) { IsBeamActive = true; remaining = beamSeconds; ShotsFired++; }
                else CancelAttack();
                return;
            }
            if (remaining > 0) return;
            foreach (var muzzle in muzzles)
            for (int i = 0; i < definition.ProjectileCount; i++)
            {
                var angle = definition.ProjectileCount == 1 ? 0 : definition.SpreadDegrees * (i / (float)(definition.ProjectileCount - 1) - .5f);
                pool.Get().Launch(definition, muzzle, health.gameObject, player.gameObject, Release, angle);
                ShotsFired++;
            }
            CancelAttack();
        }
        private void Release(EnemyProjectile shot) { if (shuttingDown) Destroy(shot.gameObject); else pool.Release(shot); }
        private void OnDisable() => CancelAttack();
        private void OnDestroy() { shuttingDown = true; pool?.Dispose(); if (projectileRoot != null) Destroy(projectileRoot.gameObject); }
    }
}
