using UnityEngine;

namespace StarShift.Enemies
{
    public enum MissileDamageTier { Normal, Large, Boss }

    [CreateAssetMenu(menuName = "StarShift/Enemies/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField, Min(1)] private int maxHp = 2;
        [SerializeField, Min(0)] private int score = 100;
        [SerializeField] private bool bodyDamagesPlayer = true;
        [SerializeField] private MissileDamageTier missileTier;
        public MissileDamageTier MissileTier => missileTier;
        public int Score => score;
        public bool BodyDamagesPlayer => bodyDamagesPlayer;
        public int MaxHp => Mathf.Max(1, maxHp);

        private void OnValidate() => maxHp = Mathf.Max(1, maxHp);
    }
}
