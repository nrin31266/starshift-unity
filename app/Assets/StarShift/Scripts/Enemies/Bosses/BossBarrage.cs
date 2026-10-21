using StarShift.Combat;
using StarShift.Gameplay;
using StarShift.Player;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent, DefaultExecutionOrder(40)]
    public sealed class BossBarrage : MonoBehaviour
    {
        [SerializeField] private BossBarrageDefinition definition;
        [SerializeField, FormerlySerializedAs("lower")] private AttackBeamPresentation lowerBandPresentation;
        [SerializeField, FormerlySerializedAs("upper")] private AttackBeamPresentation upperBandPresentation;
        private EnemyHealth bossHealth;
        private EntryHoldFlight bossFlight;
        private EnemyFireGate fireGate;
        private PlayerGameplayGate playerGate;
        private Camera gameplayCamera;
        private BeamDamageQuery lowerDamage, upperDamage;
        private ObjectPool<EnemyProjectile> projectilePool;
        private Transform projectileRoot, emitter;
        private bool shuttingDown;
        private float phaseRemaining;
        private float originForward;
        private float projectileHalfWidth;
        private int pulseIndex;
        private float initialSafeLane;
        public bool IsBusy { get; private set; }
        public bool IsActive { get; private set; }
        // Phase đọc bởi presentation; charge/pulse/active timing không đổi.
        public AttackPhase Phase { get; private set; }
        public event System.Action<AttackPhase> PhaseChanged;
        private void SetPhase(AttackPhase next) { if (Phase == next) return; Phase = next; PhaseChanged?.Invoke(next); }
        public float SafeLane { get; private set; }
        public int PulsesFired { get; private set; }
        public int ShotsFired { get; private set; }
        public Bounds LowerBounds { get; private set; }
        public Bounds UpperBounds { get; private set; }
        public BossBarrageDefinition Definition => definition;

        private void Awake()
        {
            bossHealth = GetComponentInParent<EnemyHealth>(); bossFlight = bossHealth != null ? bossHealth.GetComponent<EntryHoldFlight>() : null;
            fireGate = FindAnyObjectByType<EnemyFireGate>(); playerGate = FindAnyObjectByType<PlayerGameplayGate>(); gameplayCamera = Camera.main;
            if (definition == null || !definition.IsValid || lowerBandPresentation == null || upperBandPresentation == null || bossFlight == null || fireGate == null || playerGate == null || gameplayCamera == null)
                throw new System.InvalidOperationException("Invalid boss barrage composition: " + name);
            lowerDamage = new BeamDamageQuery(playerGate); upperDamage = new BeamDamageQuery(playerGate);
            if (definition.Kind != BossBarrageKind.ProjectileCurtain) return;
            var projectilePrefab = definition.Projectile.ProjectilePrefab;
            projectileHalfWidth = projectilePrefab.GetComponent<BoxCollider2D>().size.x * projectilePrefab.transform.lossyScale.x * .5f;
            projectileRoot = new GameObject("BarrageProjectiles").transform;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(projectileRoot.gameObject, gameObject.scene);
            emitter = new GameObject("RowEmitter").transform; emitter.SetParent(transform, false);
            projectilePool = new ObjectPool<EnemyProjectile>(() => {
                var shot = Instantiate(definition.Projectile.ProjectilePrefab, projectileRoot); shot.ResetForPool(); return shot;
            }, null, shot => shot.ResetForPool(), shot => { if (shot != null) Destroy(shot.gameObject); }, true, 32, 96);
        }
        private void OnEnable() { PulsesFired = ShotsFired = 0; CancelAttack(); }
        public bool BeginAttack()
        {
            if (!isActiveAndEnabled || IsBusy || !bossHealth.IsAlive || !fireGate.CanFire || !bossFlight.IsHolding) return false;
            var position = PlayDirectionSpace.LocalPoint(gameplayCamera.WorldToViewportPoint(playerGate.transform.position), bossFlight.Direction);
            SafeLane = position.x < .5f ? .75f : .25f;
            initialSafeLane = SafeLane;
            var bossPosition = PlayDirectionSpace.LocalPoint(gameplayCamera.WorldToViewportPoint(bossHealth.transform.position), bossFlight.Direction);
            originForward = Mathf.Clamp(bossPosition.y - .10f, .60f, .86f);
            pulseIndex = 0; phaseRemaining = definition.ChargeSeconds; IsBusy = true; IsActive = false;
            SetPhase(AttackPhase.Prepare);
            return true;
        }
        public void CancelAttack()
        {
            lowerDamage?.Reset(); upperDamage?.Reset();
            IsBusy = IsActive = false; phaseRemaining = 0; pulseIndex = 0;
            SetPhase(AttackPhase.Idle);
            if (lowerBandPresentation != null) lowerBandPresentation.Hide(); if (upperBandPresentation != null) upperBandPresentation.Hide();
        }
        private Vector3 GetWorldPoint(float lateral, float forward)
        {
            var p = PlayDirectionSpace.Point(new Vector2(lateral, forward), bossFlight.Direction);
            var world = gameplayCamera.ViewportToWorldPoint(new Vector3(p.x, p.y, -gameplayCamera.transform.position.z)); world.z = 0; return world;
        }
        private Bounds GetBandBounds(float from, float to)
        {
            var a = GetWorldPoint(from, -.02f); var b = GetWorldPoint(to, originForward);
            return new Bounds((a + b) * .5f, new Vector3(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), 1));
        }
        private void DrawBand(AttackBeamPresentation visual, Bounds bounds, Color tint)
        {
            bool right = bossFlight.Direction == PlayDirection.Right;
            var along = right ? Vector3.right : Vector3.up;
            float length = right ? bounds.size.x : bounds.size.y;
            visual.ShowCurtain(bounds.center - along * length * .5f, bounds.center + along * length * .5f,
                right ? bounds.size.y : bounds.size.x, IsActive, tint);
        }
        private void FixedUpdate()
        {
            if (!IsBusy || Time.timeScale <= 0) return;
            if (!bossHealth.IsAlive || !fireGate.CanFire || bossFlight.IsExiting) { CancelAttack(); return; }
            bool laser = definition.Kind == BossBarrageKind.IonShelter;
            if (!laser && IsActive && pulseIndex < definition.PulseCount && phaseRemaining <= definition.PulseInterval - .28f)
            {
                IsActive = false;
                SafeLane = initialSafeLane + (pulseIndex % 2 == 1 ? (initialSafeLane < .5f ? .28f : -.28f) : 0);
                SetPhase(AttackPhase.Prepare);
            }
            LowerBounds = GetBandBounds(0, SafeLane - definition.OpeningWidth * .5f);
            UpperBounds = GetBandBounds(SafeLane + definition.OpeningWidth * .5f, 1);
            if (laser || !IsActive)
            {
                var tint = laser ? new Color(1, .12f, .45f) : new Color(1, .65f, .18f);
                DrawBand(lowerBandPresentation, LowerBounds, tint); DrawBand(upperBandPresentation, UpperBounds, tint);
            }
            if (laser && IsActive)
            {
                lowerDamage.Apply(LowerBounds.center, LowerBounds.size, 0, bossHealth.gameObject);
                upperDamage.Apply(UpperBounds.center, UpperBounds.size, 0, bossHealth.gameObject);
            }
            else { lowerDamage.Reset(); upperDamage.Reset(); }
            phaseRemaining -= Time.fixedDeltaTime;
            // LOCK ở nửa sau charge: người chơi thấy khe an toàn đã chốt.
            if (!IsActive && Phase == AttackPhase.Prepare && phaseRemaining <= definition.ChargeSeconds * .45f) SetPhase(AttackPhase.Lock);
            if (phaseRemaining > 0) return;
            if (laser)
            {
                if (!IsActive) { IsActive = true; PulsesFired++; phaseRemaining = definition.ActiveSeconds; SetPhase(AttackPhase.Attack); }
                else if (++pulseIndex >= definition.PulseCount) CancelAttack();
                else
                {
                    // Đổi hành lang chỉ trong khoảng báo trước; không dời vùng damage đang bật.
                    IsActive = false; SafeLane = 1 - SafeLane; phaseRemaining = definition.ChargeSeconds + definition.PulseInterval;
                    SetPhase(AttackPhase.Prepare);
                }
            }
            else if (pulseIndex < definition.PulseCount)
            {
                IsActive = true; lowerBandPresentation.Hide(); upperBandPresentation.Hide(); FireRow(); pulseIndex++; PulsesFired++; SetPhase(AttackPhase.Attack);
                float distance = Vector3.Distance(GetWorldPoint(.5f, originForward), GetWorldPoint(.5f, -.05f));
                phaseRemaining = pulseIndex == definition.PulseCount ? distance / definition.Projectile.ProjectileSpeed : definition.PulseInterval;
            }
            else CancelAttack();
        }
        private void FireRow()
        {
            var forward = PlayDirectionSpace.Offset(Vector2.down, bossFlight.Direction);
            float edgeMargin = projectileHalfWidth / Vector3.Distance(GetWorldPoint(0, originForward), GetWorldPoint(1, originForward));
            float rowOffset = pulseIndex % 2 == 0 ? 0 : .5f;
            for (int i = 0; i < definition.ProjectilesPerRow; i++)
            {
                // Hàng lẻ lệch nửa slot để tạo nhịp xen kẽ; khe đã báo trước vẫn giữ nguyên.
                float lane = .02f + .96f * (i + rowOffset) / (definition.ProjectilesPerRow - 1);
                if (lane > .98f) continue;
                if (Mathf.Abs(lane - SafeLane) <= definition.OpeningWidth * .5f + edgeMargin) continue;
                emitter.position = GetWorldPoint(lane, originForward);
                projectilePool.Get().Launch(definition.Projectile, emitter, bossHealth.gameObject, playerGate.gameObject, Release, 0, forward); ShotsFired++;
            }
        }
        private void Release(EnemyProjectile shot) { if (shuttingDown) Destroy(shot.gameObject); else projectilePool.Release(shot); }
        private void OnDisable() => CancelAttack();
        private void OnDestroy() { shuttingDown = true; projectilePool?.Dispose(); if (projectileRoot != null) Destroy(projectileRoot.gameObject); }
    }
}
