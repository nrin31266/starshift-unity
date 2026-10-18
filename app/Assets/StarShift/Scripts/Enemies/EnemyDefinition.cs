using UnityEngine;

namespace StarShift.Enemies
{
    [CreateAssetMenu(menuName = "StarShift/Enemies/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField, Min(1)] private int maxHp = 2;
        [SerializeField, Min(0)] private int score = 100;
        [SerializeField] private bool bodyDamagesPlayer = true;
        public int Score => score;
        public bool BodyDamagesPlayer => bodyDamagesPlayer;
        public int MaxHp => Mathf.Max(1, maxHp);

        private void OnValidate() => maxHp = Mathf.Max(1, maxHp);
    }
}
