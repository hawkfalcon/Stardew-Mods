using Netcode;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace BetterJunimos.Patches {
    public class Util {
        public const int DefaultRange = 8;

        public static JunimoHut GetHutFromJunimo(JunimoHarvester junimo) {
            NetGuid netHome = BetterJunimos.instance.Helper.Reflection.GetField<NetGuid>(junimo, "netHome").GetValue();
            return Game1.getFarm().buildings[netHome.Value] as JunimoHut;
        }

        public static void ReduceItemCount(NetObjectList<Item> chest, Item item) {
            if (!BetterJunimos.instance.Config.JunimoImprovements.ConsumeItemsFromChest) { return; }
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
