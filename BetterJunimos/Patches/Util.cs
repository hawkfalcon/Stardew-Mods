using System.Linq;
using Netcode;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace BetterJunimos.Patches {
    public class Util {
        public static bool WereJunimosPaidToday = false;
        public static int JunimoPaymentToday = 0;
        public const int DefaultRadius = 8;
        
        internal static ModConfig Config = BetterJunimos.instance.Config;

        public static JunimoHut GetHutFromJunimo(JunimoHarvester junimo) {
            NetGuid netHome = BetterJunimos.instance.Helper.Reflection.GetField<NetGuid>(junimo, "netHome").GetValue();
            return Game1.getFarm().buildings[netHome.Value] as JunimoHut;
        }

        public static bool JunimoPaymentUseItem(JunimoHut hut) {
            Farm farm = Game1.getFarm();
            NetObjectList<Item> chest = hut.output.Value.items;

            int needed = Util.Config.JunimoPayment.DailyWage.ForagedItems;
            foreach (int i in Enumerable.Range(JunimoPaymentToday, needed)) {
                Item forage = chest.FirstOrDefault(item => item.getCategoryName().Equals("Forage"));

                if (forage != null) {
                    ReduceItemCount(chest, forage);
                    JunimoPaymentToday++;
                }
            }
            Util.WereJunimosPaidToday = JunimoPaymentToday == needed;
            return Util.WereJunimosPaidToday;
        }

        public static void ReduceItemCount(NetObjectList<Item> chest, Item item) {
            if (!Config.JunimoImprovements.InfiniteJunimoInventory) { return; }
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
