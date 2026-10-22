// EnemyHealth — Máu và nhận sát thương của địch; quản lý HP riêng cho mỗi đối tượng đang chơi.
using System;
using System.Collections.Generic;
using UnityEngine;
using StarShift.Combat;

namespace StarShift.Enemies
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    [AddComponentMenu("StarShift/Enemies/Enemy Health")]
    public sealed class EnemyHealth : MonoBehaviour, IPlayerProjectileTarget
    {
        [SerializeField] private EnemyDefinition definition;
        private static readonly List<EnemyHealth> active = new List<EnemyHealth>();
        public static IReadOnlyList<EnemyHealth> Active => active;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => active.Clear();
        private void OnDisable() => active.Remove(this);

        public EnemyDefinition Definition => definition;
        public int SpawnVersion { get; private set; }
        // Máu riêng của con địch này; không trừ trực tiếp vào asset cấu hình dùng chung.
        public int CurrentHp { get; private set; }
        public EnemyModifierDefinition Modifier { get; private set; }
        private int spawnHp;
        private EntryHoldFlight flight;
        private float visibleSeconds;
        private bool protectEntry;
        private bool damageBlocked;
        public void SetDamageBlocked(bool blocked) => damageBlocked = blocked;
        public bool BlocksPlayerProjectiles => damageBlocked && Definition.MissileTier == MissileDamageTier.Boss && flight != null && flight.IsHolding;
        public event Action BlockedHit;
        // Presentation thuần: point/normal/kind đi kèm hit, damage/splash không đổi.
        public event Action<EnemyHitInfo> HitPresented;
        // Quyền nhận sát thương có xét còn sống, bảo vệ lúc vào màn và khóa sát thương.
        public bool CanReceiveDamage
        {
            get
            {
                if (damageBlocked || !IsAlive || (flight != null && flight.IsOutsideView)) return false;
                return !protectEntry || (visibleSeconds >= .2f && (Definition.MissileTier != MissileDamageTier.Boss || flight.IsHolding));
            }
        }
        private void Awake()
        {
            flight = GetComponent<EntryHoldFlight>();
            protectEntry = flight != null && definition != null && definition.BodyDamagesPlayer && GetComponent<AsteroidVisualMotion>() == null;
        }
        private void FixedUpdate()
        {
            if (flight != null && !flight.IsOutsideView) visibleSeconds += Time.fixedDeltaTime;
        }
        // Máu tối đa tính từ HP ghi đè của nhóm nếu có, nếu không dùng definition, rồi áp hệ số modifier.
        public int MaxHp => definition != null ? Mathf.CeilToInt((spawnHp > 0 ? spawnHp : definition.MaxHp) * (Modifier != null ? Modifier.HpMultiplier : 1)) : 0;
        public void ConfigureModifier(EnemyModifierDefinition modifier, int hpOverride = 0) { Modifier = modifier; spawnHp = Mathf.Max(0, hpOverride); }
        public bool IsAlive => isActiveAndEnabled && CurrentHp > 0;
        public event Action Damaged;
        public event Action Died;

        private void OnEnable()
        {
            if (definition == null)
            {
                Debug.LogError("EnemyHealth requires an enemy definition.", this);
                enabled = false;
                return;
            }
            SpawnVersion++;
            damageBlocked = false;
            visibleSeconds = 0;
            // Khởi tạo lại HP khi địch được bật, kể cả lần lấy lại từ pool.
            CurrentHp = MaxHp;
            if (!active.Contains(this)) active.Add(this);
        }

        public void ReceivePlayerHit(int damage, GameObject owner)
            => ReceivePlayerHit(damage, owner, EnemyHitInfo.AtCenter(transform, EnemyHitKind.Hull, damage));

        // Đầu vào sát thương của địch; xét khiên và bảo vệ trước khi cập nhật HP.
        public void ReceivePlayerHit(int damage, GameObject owner, EnemyHitInfo hit)
        {
            if (damage <= 0) return;
            if (BlocksPlayerProjectiles)
            {
                try { BlockedHit?.Invoke(); }
                finally { HitPresented?.Invoke(new EnemyHitInfo(EnemyHitKind.ShieldBlock, hit.Point, hit.Normal, damage, hit.Branch)); }
                return;
            }
            if (!CanReceiveDamage) return;
            // Chỉ hit hợp lệ mới trừ HP; chặn số âm để xác định chết đúng tại 0.
            CurrentHp = Mathf.Max(0, CurrentHp - damage);
            var lethal = CurrentHp == 0;
            try
            {
                Damaged?.Invoke();
                HitPresented?.Invoke(new EnemyHitInfo(hit.Kind, hit.Point, hit.Normal, damage, hit.Branch));
            }
            finally { if (lethal) ResolveDeath(); }
        }

        // Báo chết rồi tắt địch thường; boss có chuỗi chết riêng nên chưa tắt ngay tại đây.
        private void ResolveDeath()
        {
            // HP đã khóa damage trước callback; release không phụ thuộc presentation.
            try { Died?.Invoke(); }
            finally { if (GetComponent<BossDeathSequence>() == null) gameObject.SetActive(false); }
        }
    }
}
