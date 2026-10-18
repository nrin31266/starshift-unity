using System;
using StarShift.Player;

namespace StarShift.Rewards
{
    public sealed class RewardRoller
    {
        private Random random;
        private int lowLifeCratesWithoutDefense;
        public void Reset(int seed) { random = new Random(seed); lowLifeCratesWithoutDefense = 0; }
        public PickupDefinition RollDirect(RewardDropTable table, PlayerPickupReceiver receiver, PlayerLives lives, WeaponLoadout loadout)
        {
            if (table.GuaranteedPickup != null) return table.GuaranteedPickup;
            if (table.Source == RewardSource.Resource) return table.Shard;
            if (random.NextDouble() < table.MajorChance) return SelectMajor(table, receiver, lives, loadout, false);
            return random.NextDouble() < table.ShardChance ? table.Shard : null;
        }
        public PickupDefinition SelectMajor(RewardDropTable table, PlayerPickupReceiver receiver, PlayerLives lives, WeaponLoadout loadout, bool crate)
        {
            if (table.GuaranteedPickup != null && receiver.CanUse(table.GuaranteedPickup)) return table.GuaranteedPickup;
            var pity = crate && lives.CurrentLives == 1 && lowLifeCratesWithoutDefense >= 2;
            double total = 0;
            foreach (var entry in table.MajorItems) total += Weight(entry, receiver, lives, loadout, pity);
            var roll = random.NextDouble() * total;
            foreach (var entry in table.MajorItems)
            {
                var weight = Weight(entry, receiver, lives, loadout, pity);
                if (weight <= 0) continue;
                roll -= weight;
                if (roll <= 0) return entry.Item;
            }
            return null;
        }
        private static float Weight(WeightedPickup entry, PlayerPickupReceiver receiver, PlayerLives lives, WeaponLoadout loadout, bool pity)
        {
            if (entry == null || entry.Item == null || entry.Weight <= 0 || !receiver.CanUse(entry.Item)) return 0;
            var defense = IsDefensive(entry.Item);
            if (pity && !defense) return 0;
            var weight = entry.Weight;
            if (lives.CurrentLives == 1 && defense) weight *= 3;
            if (entry.Item.Kind == PickupKind.WeaponCore && loadout.Level(entry.Item.Branch) == 0) weight *= 2;
            return weight;
        }
        public void RecordCrate(PickupDefinition item, int livesBeforeCollection)
        {
            if (IsDefensive(item) || livesBeforeCollection > 1) lowLifeCratesWithoutDefense = 0;
            else lowLifeCratesWithoutDefense++;
        }
        private static bool IsDefensive(PickupDefinition item) => item.Kind == PickupKind.Shield || item.Kind == PickupKind.ExtraLife;
    }
}
