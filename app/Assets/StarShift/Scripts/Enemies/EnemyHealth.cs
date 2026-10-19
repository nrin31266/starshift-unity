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
        public int CurrentHp { get; private set; }
        public EnemyModifierDefinition Modifier { get; private set; }
        public int MaxHp => definition != null ? Mathf.CeilToInt(definition.MaxHp * (Modifier != null ? Modifier.HpMultiplier : 1)) : 0;
        public void ConfigureModifier(EnemyModifierDefinition modifier) => Modifier = modifier;
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
            CurrentHp = MaxHp;
            if (!active.Contains(this)) active.Add(this);
        }

        public void ReceivePlayerHit(int damage, GameObject owner)
        {
            if (!IsAlive || damage <= 0) return;
            CurrentHp = Mathf.Max(0, CurrentHp - damage);
            var lethal = CurrentHp == 0;
            try { Damaged?.Invoke(); }
            finally { if (lethal) ResolveDeath(); }
        }

        private void ResolveDeath()
        {
            // HP đã khóa damage trước callback; release không phụ thuộc presentation.
            try { Died?.Invoke(); }
            finally { if (GetComponent<BossDeathSequence>() == null) gameObject.SetActive(false); }
        }
    }
}
