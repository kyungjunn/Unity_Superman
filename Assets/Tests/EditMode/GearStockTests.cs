using GrowNa.Gear;
using NUnit.Framework;

namespace GrowNa.Tests
{
    public class GearStockTests
    {
        static GearStock Stocked(GearSlot slot, GearTier tier, int count)
        {
            var stock = new GearStock();
            stock.Add(slot, tier, count);
            stock.Register(slot, tier);
            return stock;
        }

        [Test]
        public void New_stock_is_empty_and_gives_no_codex_bonus()
        {
            var stock = new GearStock();
            Assert.AreEqual(0, stock.TotalCount);
            Assert.AreEqual(0, stock.CodexCount);
            Assert.AreEqual(1.0, stock.CodexMultiplier, 1e-9);
        }

        [Test]
        public void First_registration_returns_true_and_repeat_returns_false()
        {
            var stock = new GearStock();
            Assert.IsTrue(stock.Register(GearSlot.Weapon, GearTier.Rare));
            Assert.IsFalse(stock.Register(GearSlot.Weapon, GearTier.Rare));
            Assert.AreEqual(1, stock.CodexCount);
        }

        [Test]
        public void Register_does_not_add_to_stock()
        {
            var stock = new GearStock();
            stock.Register(GearSlot.Weapon, GearTier.Rare);
            Assert.AreEqual(0, stock.TotalCount);
        }

        [Test]
        public void Codex_bonus_scales_with_registered_entries()
        {
            var stock = new GearStock();
            stock.Register(GearSlot.Weapon, GearTier.Rare);
            stock.Register(GearSlot.Helmet, GearTier.Rare);
            Assert.AreEqual(1.0 + GearStock.CodexBonusPerEntry * 2, stock.CodexMultiplier, 1e-9);
        }

        [Test]
        public void Full_codex_gives_thirty_two_percent()
        {
            var stock = new GearStock();
            for (int s = 0; s < GearTable.SlotCount; s++)
                for (int t = 0; t < GearTable.TierCount; t++)
                    stock.Register((GearSlot)s, (GearTier)t);

            Assert.AreEqual(GearTable.StockSize, stock.CodexCount);
            Assert.AreEqual(1.32, stock.CodexMultiplier, 1e-9);
        }

        [Test]
        public void Remove_fails_without_partial_deduction()
        {
            var stock = Stocked(GearSlot.Ring, GearTier.Heroic, 2);
            Assert.IsFalse(stock.TryRemove(GearSlot.Ring, GearTier.Heroic, 3));
            Assert.AreEqual(2, stock.Count(GearSlot.Ring, GearTier.Heroic));
        }

        [Test]
        public void Dismantle_pays_shard_per_item_and_empties_the_cell()
        {
            var stock = Stocked(GearSlot.Ring, GearTier.Legend, 3);
            double shard = stock.DismantleAll(GearSlot.Ring, GearTier.Legend);

            Assert.AreEqual(GearTable.Shard(GearTier.Legend) * 3, shard, 1e-9);
            Assert.AreEqual(0, stock.Count(GearSlot.Ring, GearTier.Legend));
        }

        [Test]
        public void Dismantle_keeps_codex_registration()
        {
            var stock = Stocked(GearSlot.Ring, GearTier.Legend, 1);
            stock.DismantleAll(GearSlot.Ring, GearTier.Legend);
            Assert.IsTrue(stock.Registered(GearSlot.Ring, GearTier.Legend));
        }

        [Test]
        public void Dismantle_up_to_tier_leaves_higher_tiers_untouched()
        {
            var stock = new GearStock();
            stock.Add(GearSlot.Weapon, GearTier.Worn, 5);
            stock.Add(GearSlot.Weapon, GearTier.Rare, 2);
            stock.Add(GearSlot.Weapon, GearTier.Legend, 1);

            double shard = stock.DismantleUpTo(GearTier.Rare);

            Assert.AreEqual(GearTable.Shard(GearTier.Worn) * 5 + GearTable.Shard(GearTier.Rare) * 2, shard, 1e-9);
            Assert.AreEqual(1, stock.Count(GearSlot.Weapon, GearTier.Legend));
            Assert.AreEqual(0, stock.Count(GearSlot.Weapon, GearTier.Worn));
        }

        [Test]
        public void Fusion_needs_four_below_myth()
        {
            var stock = Stocked(GearSlot.Armor, GearTier.Rare, 3);
            Assert.IsFalse(stock.CanFuse(GearSlot.Armor, GearTier.Rare));
            stock.Add(GearSlot.Armor, GearTier.Rare);
            Assert.IsTrue(stock.CanFuse(GearSlot.Armor, GearTier.Rare));
        }

        [Test]
        public void Fusion_needs_five_from_myth_up()
        {
            var stock = Stocked(GearSlot.Armor, GearTier.Myth, 4);
            Assert.IsFalse(stock.CanFuse(GearSlot.Armor, GearTier.Myth));
            stock.Add(GearSlot.Armor, GearTier.Myth);
            Assert.IsTrue(stock.CanFuse(GearSlot.Armor, GearTier.Myth));
        }

        [Test]
        public void Fusion_consumes_materials_and_yields_one_higher_tier()
        {
            var stock = Stocked(GearSlot.Armor, GearTier.Rare, 4);
            Assert.IsTrue(stock.TryFuse(GearSlot.Armor, GearTier.Rare, 5, out var result));

            Assert.AreEqual(GearTier.Heroic, result.tier);
            Assert.AreEqual(GearSlot.Armor, result.slot);
            Assert.AreEqual(0, stock.Count(GearSlot.Armor, GearTier.Rare));
            Assert.AreEqual(1, stock.Count(GearSlot.Armor, GearTier.Heroic));
        }

        [Test]
        public void Fusion_result_enters_the_codex()
        {
            var stock = Stocked(GearSlot.Armor, GearTier.Rare, 4);
            stock.TryFuse(GearSlot.Armor, GearTier.Rare, 5, out _);
            Assert.IsTrue(stock.Registered(GearSlot.Armor, GearTier.Heroic));
        }

        [Test]
        public void Fusion_does_not_carry_enhancement_over()
        {
            var stock = Stocked(GearSlot.Armor, GearTier.Rare, 4);
            stock.TryFuse(GearSlot.Armor, GearTier.Rare, 5, out var result);
            Assert.AreEqual(0, result.plus);
        }

        [Test]
        public void Top_tier_cannot_be_fused()
        {
            var stock = Stocked(GearSlot.Armor, GearTier.Earth, 8);
            Assert.IsFalse(stock.CanFuse(GearSlot.Armor, GearTier.Earth));
            Assert.IsFalse(stock.TryFuse(GearSlot.Armor, GearTier.Earth, 5, out _));
        }

        [Test]
        public void Load_from_snapshot_restores_counts_and_codex()
        {
            var source = Stocked(GearSlot.Charm, GearTier.Spirit, 7);
            var restored = new GearStock();
            restored.LoadFrom(source.CountsSnapshot(), source.CodexSnapshot());

            Assert.AreEqual(7, restored.Count(GearSlot.Charm, GearTier.Spirit));
            Assert.IsTrue(restored.Registered(GearSlot.Charm, GearTier.Spirit));
        }

        [Test]
        public void Load_from_null_clears_previous_state()
        {
            var stock = Stocked(GearSlot.Charm, GearTier.Spirit, 7);
            stock.LoadFrom(null, null);

            Assert.AreEqual(0, stock.TotalCount);
            Assert.AreEqual(0, stock.CodexCount);
        }
    }
}
