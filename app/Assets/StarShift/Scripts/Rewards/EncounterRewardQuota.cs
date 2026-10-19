using System;

namespace StarShift.Rewards
{
    public sealed class EncounterRewardQuota
    {
        private Random random;
        private int remainingEnemies;
        public int Remaining { get; private set; }

        public void Reset(int seed, int enemies, int quota)
        {
            if (enemies < 0 || quota < 0 || quota > enemies) throw new ArgumentOutOfRangeException(nameof(quota));
            random = new Random(seed);
            remainingEnemies = enemies;
            Remaining = quota;
        }

        public bool ResolveKill(bool satisfiesFloor)
        {
            if (remainingEnemies <= 0) return false;
            bool force = !satisfiesFloor && Remaining > 0 && random.Next(remainingEnemies) < Remaining;
            remainingEnemies--;
            if (satisfiesFloor || force) Remaining = Math.Max(0, Remaining - 1);
            return force;
        }

        public void ResolveEscape() => remainingEnemies = Math.Max(0, remainingEnemies - 1);
        public bool ResolveFallback()
        {
            if (Remaining <= 0) return false;
            Remaining--;
            return true;
        }

        public static int Seed(string mission, string encounter, int seed)
        {
            unchecked
            {
                uint hash = (uint)seed;
                foreach (char c in mission + "/" + encounter) hash = (hash ^ c) * 16777619;
                return (int)hash;
            }
        }
    }
}
