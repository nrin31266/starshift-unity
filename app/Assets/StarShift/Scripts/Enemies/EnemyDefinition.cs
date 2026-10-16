using UnityEngine;

namespace StarShift.Enemies
{
    [CreateAssetMenu(menuName = "StarShift/Enemies/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField, Min(1)] private int maxHp = 2;
        public int MaxHp => Mathf.Max(1, maxHp);

        private void OnValidate() => maxHp = Mathf.Max(1, maxHp);
    }
}
