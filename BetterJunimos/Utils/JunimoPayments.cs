using System.Collections.Generic;
using System.Linq;
using StardewValley.Buildings;
using StardewValley.Objects;

namespace BetterJunimos.Utils {
    public class JunimoPayments {
        private readonly ModConfig.JunimoPayments _payment;

        public bool WereJunimosPaidToday;
        internal readonly Dictionary<int, List<int>> JunimoPaymentsToday = new();

        internal JunimoPayments(ModConfig.JunimoPayments payment) {
            _payment = payment;
        }

        public bool ReceivePaymentItems(JunimoHut hut) {
            var chest = hut.output.Value;
            var paidForage = ReceiveItems(chest, _payment.DailyWage.ForagedItems, Util.ForageCategory);
            var paidFlowers = ReceiveItems(chest, _payment.DailyWage.Flowers, Util.FlowerCategory);
            var paidFruit = ReceiveItems(chest, _payment.DailyWage.Fruit, Util.FruitCategory);
            var paidWine = ReceiveItems(chest, _payment.DailyWage.Wine, Util.WineCategory);

            return paidForage && paidFlowers && paidFruit && paidWine;
        }

        private bool ReceiveItems(Chest chest, int needed, int type) {
            if (needed == 0) return true;
            if (!JunimoPaymentsToday.TryGetValue(type, out var items)) {
                items = new List<int>();
                JunimoPaymentsToday[type] = items;
            }
            var paidSoFar = items.Count;
            if (paidSoFar == needed) return true;

            foreach (var unused in Enumerable.Range(paidSoFar, needed)) {
                var foundItem = chest.items.FirstOrDefault(item => item != null && item.Category == type);
                if (foundItem == null) continue;
                items.Add(foundItem.ParentSheetIndex);
                Util.RemoveItemFromChest(chest, foundItem);
            }
            return items.Count == needed;
        }
    }
}
