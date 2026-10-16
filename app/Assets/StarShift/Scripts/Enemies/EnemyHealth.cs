using System;
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

        public int CurrentHp { get; private set; }
        public int MaxHp => definition != null ? definition.MaxHp : 0;
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
            CurrentHp = definition.MaxHp;
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
            finally { gameObject.SetActive(false); }
        }
    }
}
