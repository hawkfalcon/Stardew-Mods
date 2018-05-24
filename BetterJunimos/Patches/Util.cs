using System.Collections.Generic;
using System.Linq;
using Netcode;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace BetterJunimos.Patches {
    public class Util {
        public static bool WereJunimosPaidToday = false;
        public static Dictionary<string, List<int>> JunimoPaymentsToday = new Dictionary<string, List<int>>();

        public const int DefaultRadius = 8;
        internal static ModConfig Config = BetterJunimos.instance.Config;

        public static JunimoHut GetHutFromJunimo(JunimoHarvester junimo) {
            NetGuid netHome = BetterJunimos.instance.Helper.Reflection.GetField<NetGuid>(junimo, "netHome").GetValue();
            return Game1.getFarm().buildings[netHome.Value] as JunimoHut;
        }

        public static bool JunimoPaymentReceiveItems(JunimoHut hut) {
            Farm farm = Game1.getFarm();
            NetObjectList<Item> chest = hut.output.Value.items;
            bool paidForage = ReceiveItems(chest, Util.Config.JunimoPayment.DailyWage.ForagedItems, "Forage");
            bool paidFlowers = ReceiveItems(chest, Util.Config.JunimoPayment.DailyWage.Flowers, "Flower");
            bool paidFruit = ReceiveItems(chest, Util.Config.JunimoPayment.DailyWage.Fruit, "Fruit");
            bool paidWine = ReceiveItems(chest, Util.Config.JunimoPayment.DailyWage.Wine, "Artisan Goods");

            return paidForage && paidFlowers && paidFruit && paidWine;
        }

        public static bool ReceiveItems(NetObjectList<Item> chest, int needed, string type) {
            if (needed == 0) return true;
            List<int> items;
            if (!JunimoPaymentsToday.TryGetValue(type, out items)) {
                items = new List<int>();
                JunimoPaymentsToday[type] = items;
            }
            int paidSoFar = items.Count();
            if (paidSoFar == needed) return true;

            foreach (int i in Enumerable.Range(paidSoFar, needed)) {
                Item foundItem = chest.FirstOrDefault(item => item.getCategoryName() == type);
                if (foundItem != null) {
                    items.Add(foundItem.ParentSheetIndex);
                    ReduceItemCount(chest, foundItem);
                }
            }
            return items.Count() == needed;
        }

        public static void ReduceItemCount(NetObjectList<Item> chest, Item item) {
            if (Config.JunimoImprovements.InfiniteJunimoInventory) { return; }
            item.Stack--;
            if (item.Stack == 0) {
                chest.Remove(item);
            }
        }

        public static void AnimateJunimo(int type, JunimoHarvester junimo) {
            var netAnimationEvent = BetterJunimos.instance.Helper.Reflection.
                GetField<NetEvent1Field<int, NetInt>>(junimo, "netAnimationEvent");
            netAnimationEvent.GetValue().Fire(type);
        }
    }
}
