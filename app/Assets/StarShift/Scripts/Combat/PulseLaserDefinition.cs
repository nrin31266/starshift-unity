// PulseLaserDefinition — Cấu hình vũ khí chính và từng cấp; dùng chung cho các nhánh súng, không giữ nhiệt hay cấp đang chơi.
using System;
using UnityEngine;

namespace StarShift.Combat
{
    [Serializable]
    public sealed class WeaponLevelPattern
    {
        // Số viên trong mỗi loạt ở cấp này; khác số loạt được bắn trong một giây.
        [Min(1)] public int ProjectileCount = 1;
        // Tổng góc xòe của loạt nhiều viên; không phải góc riêng của từng viên.
        [Min(0)] public float SpreadDegrees;
        [Min(0)] public float LateralSpacing = .22f;
        // Sát thương mỗi viên của cấp này; viên giữa có thể được cộng CenterBonus.
        [Min(1)] public int Damage = 1;
        [Min(0)] public int CenterBonus;
        // Khoảng giữa hai loạt tính bằng giây; giảm giá trị để tăng nhịp bắn.
        [Min(.02f)] public float FireInterval = .17f;
        [Min(0)] public float SplashRadius;
    }

    // Giữ class/GUID cũ để prefab và regression fixtures không mất reference.
    [CreateAssetMenu(menuName = "StarShift/Weapons/Pulse Laser Definition")]
    public sealed class PulseLaserDefinition : ScriptableObject
    {
        [SerializeField] private WeaponBranch branch;
        // Mỗi phần tử cấu hình một cấp; thuật toán bắn đọc cấp đang dùng, không chỉ dữ liệu dự phòng.
        [SerializeField] private WeaponLevelPattern[] levels;
        [SerializeField] private Color projectileColor = new Color(.3f, .9f, 1);
        [SerializeField, Min(0)] private float trackingDegreesPerSecond;
        [SerializeField, Min(0)] private float trackingRange = 18;
        [SerializeField, Min(0)] private float trackingLaunchSeconds = .16f;
        [SerializeField, Range(0, 60)] private float trackingLaunchArcDegrees = 9;
        public WeaponBranch Branch => branch;
        [SerializeField] private Vector2 projectileScale = Vector2.one;
        [SerializeField] private Sprite projectileSprite;
        [SerializeField, Min(0)] private float trailSeconds;
        public Sprite ProjectileSprite => projectileSprite;
        public float TrailSeconds => trailSeconds;
        public Vector2 ProjectileScale => projectileScale;
        public Color ProjectileColor => projectileColor;
        public float TrackingDegreesPerSecond => trackingDegreesPerSecond;
        public float TrackingRange => trackingRange;
        public float TrackingLaunchSeconds => trackingLaunchSeconds;
        public float TrackingLaunchArcDegrees => trackingLaunchArcDegrees;
        // Cấp hiển thị bắt đầu từ 1, còn chỉ số mảng bắt đầu từ 0.
        public WeaponLevelPattern Pattern(int level) => levels != null && level > 0 && level <= levels.Length ? levels[level - 1] : null;
        [SerializeField] private PlayerProjectile projectilePrefab;
        [SerializeField, Min(.02f)] private float fireInterval = .2f;
        // Tốc độ di chuyển đạn trong thế giới, không phải nhịp tạo đạn.
        [SerializeField, Min(.01f)] private float projectileSpeed = 12f;
        [SerializeField, Min(1)] private int damage = 1;
        // Thời gian sống tối đa của đạn người chơi trước khi thu hồi nếu chưa trúng.
        [SerializeField, Min(.02f)] private float projectileLifetime = 1.5f;
        [SerializeField] private WeaponThermalProfile thermalProfile;

        public PlayerProjectile ProjectilePrefab => projectilePrefab;
        public float FireInterval => fireInterval;
        public float ProjectileSpeed => projectileSpeed;
        public int Damage => damage;
        public float ProjectileLifetime => projectileLifetime;
        public WeaponThermalProfile ThermalProfile => thermalProfile;

        private void OnValidate()
        {
            fireInterval = Mathf.Max(.02f, fireInterval);
            projectileSpeed = Mathf.Max(.01f, projectileSpeed);
            damage = Mathf.Max(1, damage);
            projectileLifetime = Mathf.Max(.02f, projectileLifetime);
        }
    }
}
