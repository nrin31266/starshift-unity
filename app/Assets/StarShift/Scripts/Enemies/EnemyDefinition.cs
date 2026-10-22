// EnemyDefinition — Cấu hình địch: máu gốc, điểm và sát thương va chạm; HP hiện tại do EnemyHealth giữ.
using UnityEngine;

namespace StarShift.Enemies
{
    public enum MissileDamageTier { Normal, Large, Boss }

    [CreateAssetMenu(menuName = "StarShift/Enemies/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        // Máu gốc của loại địch; nhóm spawn hoặc modifier có thể thay đổi máu thực tế.
        [SerializeField, Min(1)] private int maxHp = 2;
        // Điểm gốc khi hạ loại địch này; RewardDirector áp quy tắc thưởng của lượt.
        [SerializeField, Min(0)] private int score = 100;
        // Quyết định thân địch có gửi hit khi va chạm tàu; không phải sát thương của viên đạn.
        [SerializeField] private bool bodyDamagesPlayer = true;
        [SerializeField] private MissileDamageTier missileTier;
        public MissileDamageTier MissileTier => missileTier;
        public int Score => score;
        public bool BodyDamagesPlayer => bodyDamagesPlayer;
        public int MaxHp => Mathf.Max(1, maxHp);

        private void OnValidate() => maxHp = Mathf.Max(1, maxHp);
    }
}
