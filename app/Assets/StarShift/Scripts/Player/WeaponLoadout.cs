using System;
using StarShift.Combat;
using UnityEngine;

namespace StarShift.Player
{
    [DisallowMultipleComponent]
    public sealed class WeaponLoadout : MonoBehaviour
    {
        [SerializeField] private PulseLaserDefinition[] weapons;
        private readonly int[] levels = { 1, 0, 0 };
        public WeaponBranch ActiveBranch { get; private set; }
        public int ActiveLevel => Level(ActiveBranch);
        public PulseLaserDefinition ActiveDefinition => Definition(ActiveBranch);
        public event Action Changed;
        public int Level(WeaponBranch branch) => levels[(int)branch];
        public PulseLaserDefinition Definition(WeaponBranch branch)
        {
            if (weapons != null) foreach (var item in weapons) if (item != null && item.Branch == branch) return item;
            return null;
        }
        public bool CanUpgrade(WeaponBranch branch) => Definition(branch) != null && Level(branch) < 3;
        public bool ApplyCore(WeaponBranch branch)
        {
            if (!CanUpgrade(branch)) return false;
            levels[(int)branch]++;
            Changed?.Invoke();
            return true;
        }
        public bool Cycle()
        {
            for (var step = 1; step < levels.Length; step++)
            {
                var next = (WeaponBranch)(((int)ActiveBranch + step) % levels.Length);
                if (Level(next) <= 0 || Definition(next) == null) continue;
                ActiveBranch = next;
                Changed?.Invoke();
                return true;
            }
            return false;
        }
        public void ApplyDeathDowngrade()
        {
            levels[(int)ActiveBranch] = Mathf.Max(1, ActiveLevel - 1);
            Changed?.Invoke();
        }
        public void ResetForSession()
        {
            levels[0] = 1; levels[1] = levels[2] = 0;
            ActiveBranch = WeaponBranch.Pulse;
            Changed?.Invoke();
        }
    }
}
