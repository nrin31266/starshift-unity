using StarShift.Combat;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEngine;
using UnityEngine.Pool;

namespace StarShift.Enemies
{
    [DefaultExecutionOrder(40)]
    public sealed class BossAttackModule : MonoBehaviour
    {
        [SerializeField] private SingleFireDefinition definition;
        [SerializeField] private DirectedAttack directed;
        [SerializeField] private BossBarrage barrage;
        [SerializeField] private EscortSpawner escorts;
        [SerializeField, Min(1)] private int escortVolleyCount = 1;
        [SerializeField] private Transform[] muzzles;
        [SerializeField] private SpriteRenderer telegraph;
        [SerializeField] private SpriteRenderer[] extraTelegraphs = System.Array.Empty<SpriteRenderer>();
        private Color[] extraIdleColors;
        [SerializeField] private AttackBeamPresentation beam;
        [SerializeField, Min(.1f)] private float beamSeconds = 1.5f;
        [SerializeField, Range(.03f,.2f)] private float beamViewportWidth = .10f;
        [SerializeField, Range(.05f, .4f)] private float beamTrackingSpeed = .18f;
        private BeamDamageQuery beamDamage;
        private Camera gameplayCamera;
        private EntryHoldFlight flight;
        private float beamX;
        public bool IsBeamActive { get; private set; }
        public Bounds BeamBounds { get; private set; }
        // Phase chỉ để presentation đọc; BeginAttack/cancel/timing giữ nguyên.
        public AttackPhase Phase { get; private set; }
        public event System.Action<AttackPhase> PhaseChanged;
        private void SetPhase(AttackPhase next) { if (Phase == next) return; Phase = next; PhaseChanged?.Invoke(next); }
        public AttackPhase CurrentPhase
        {
            get
            {
                if (barrage != null) return barrage.Phase;
                if (directed != null) return directed.Phase;
                return Phase;
            }
        }
        private EnemyHealth health;
        private EnemyFireGate gate;
        private PlayerGameplayGate player;
        private ObjectPool<EnemyProjectile> pool;
        private Transform projectileRoot;
        private Color idleColor;
        private float remaining;
        private bool shuttingDown;
        private bool busy;
        public bool IsBusy => barrage != null ? barrage.IsBusy : directed != null ? directed.IsBusy : busy;
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
            beamDamage = new BeamDamageQuery(player);
            if (barrage != null || directed != null || escorts != null) return;
            if (beam != null) { beam.Hide(); return; }
            projectileRoot = new GameObject("BossProjectilePool").transform;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(projectileRoot.gameObject, gameObject.scene);
            pool = new ObjectPool<EnemyProjectile>(() =>
            {
                var shot = Instantiate(definition.ProjectilePrefab, projectileRoot);
                shot.ResetForPool(); return shot;
            }, null, shot => shot.ResetForPool(), shot => { if (shot != null) Destroy(shot.gameObject); }, true, 8, 32);
        }
        private void OnEnable() { ShotsFired = 0; }
        public bool BeginAttack()
        {
            if (!isActiveAndEnabled || IsBusy || !health.IsAlive || !gate.CanFire) return false;
            if (barrage != null) return barrage.BeginAttack();
            if (directed != null) return directed.TryRequestAttack();
            busy = true; IsBeamActive = false; remaining = definition.TelegraphDuration;
            SetPhase(AttackPhase.Prepare);
            if (beam != null)
            {
                flight.SetMotionPaused(true);
                beamX = gameplayCamera.WorldToViewportPoint(muzzles[0].position).x;
            }
            return true;
        }
        public void CancelAttack()
        {
            beamDamage?.Reset();
            barrage?.CancelAttack();
            directed?.CancelAttack();
            busy = IsBeamActive = false;
            SetPhase(AttackPhase.Idle);
            if (telegraph != null) telegraph.color = idleColor;
            if (extraIdleColors != null) for (int i = 0; i < extraTelegraphs.Length; i++) extraTelegraphs[i].color = extraIdleColors[i];
            if (beam != null) { beam.Hide(); flight?.SetMotionPaused(false); }
        }
        private void FixedUpdate()
        {
            if (barrage != null || directed != null || !IsBusy) return;
            if (!health.IsAlive || !gate.CanContinueAttack) { CancelAttack(); return; }
            remaining -= Time.fixedDeltaTime;
            // LOCK ở nửa sau telegraph để cue đọc đúng lúc sắp bắn.
            if (!IsBeamActive && remaining <= definition.TelegraphDuration * .5f) SetPhase(AttackPhase.Lock);
            telegraph.color = Color.Lerp(idleColor, Color.white, .5f + .5f * Mathf.Sin(remaining * 24));
            for (int i = 0; i < extraTelegraphs.Length; i++) extraTelegraphs[i].color = Color.Lerp(extraIdleColors[i], Color.white, .5f + .5f * Mathf.Sin(remaining * 24));
            if (beam != null)
            {
                if (IsBeamActive || remaining > definition.TelegraphDuration * .45f)
                    beamX = Mathf.MoveTowards(beamX, Mathf.Clamp(gameplayCamera.WorldToViewportPoint(player.transform.position).x, .08f, .92f), beamTrackingSpeed * Time.fixedDeltaTime);
                var top = muzzles[0].position;
                var bottom = gameplayCamera.ViewportToWorldPoint(new Vector3(beamX, -.02f, -gameplayCamera.transform.position.z));
                float width = beamViewportWidth * gameplayCamera.orthographicSize * 2 * gameplayCamera.aspect;
                var delta = bottom - top;
                var center = (top + bottom) * .5f;
                BeamBounds = new Bounds(center,new Vector3(Mathf.Abs(delta.x)+width,Mathf.Abs(delta.y)+width,1));
                beam.ShowSegment(top, bottom, width, IsBeamActive);
                if (IsBeamActive)
                {
                    beamDamage.Apply(center,new Vector2(delta.magnitude,width),Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,health.gameObject);
                }
                else beamDamage.Reset();
                if (remaining > 0) return;
                if (!IsBeamActive) { IsBeamActive = true; remaining = beamSeconds; ShotsFired++; SetPhase(AttackPhase.Attack); }
                else CancelAttack();
                return;
            }
            if (remaining > 0) return;
            if (escorts != null)
            {
                for (int i = 0; i < escortVolleyCount; i++) if (escorts.SpawnEscort()) ShotsFired++;
                CancelAttack(); return;
            }
            SetPhase(AttackPhase.Attack);
            foreach (var muzzle in muzzles)
            for (int i = 0; i < definition.ProjectileCount; i++)
            {
                var angle = definition.ProjectileCount == 1 ? 0 : definition.SpreadDegrees * (i / (float)(definition.ProjectileCount - 1) - .5f);
                pool.Get().Launch(definition, muzzle, health.gameObject, player.gameObject, Release, angle);
                ShotsFired++;
            }
            // RECOVERY ngắn trước khi về Idle; không giữ damage/motion pause.
            SetPhase(AttackPhase.Recovery);
            CancelAttack();
        }
        private void Release(EnemyProjectile shot) { if (shuttingDown) Destroy(shot.gameObject); else pool.Release(shot); }
        private void OnDisable() => CancelAttack();
        private void OnDestroy() { shuttingDown = true; pool?.Dispose(); if (projectileRoot != null) Destroy(projectileRoot.gameObject); }
    }
}
