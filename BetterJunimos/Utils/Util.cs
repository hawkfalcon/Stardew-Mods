using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace BetterJunimos.Patches {
    public class Util {
        internal const int DefaultRadius = 8;
        internal const int UnpaidRadius = 3;
        public static int MaxRadius;

        internal static ModConfig Config;
        internal static IReflectionHelper Reflection;
        internal static JunimoAbilities Abilities;
        internal static JunimoPayments Payments;

        public static JunimoHut GetHutFromJunimo(JunimoHarvester junimo) {
            NetGuid netHome = Reflection.GetField<NetGuid>(junimo, "netHome").GetValue();
            return Game1.getFarm().buildings[netHome.Value] as JunimoHut;
        }

        public static void ReduceItemCount(NetObjectList<Item> chest, Item item) {
            if (Config.FunChanges.InfiniteJunimoInventory) { return; }
            item.Stack--;
            if (item.Stack == 0) {
                chest.Remove(item);
            }
        }

        internal static bool ShouldAvoidHarvesting(Vector2 pos) {
            if (!Config.JunimoImprovements.AvoidHarvestingFlowers) return false;
            Farm farm = Game1.getFarm();
            if (farm.terrainFeatures.ContainsKey(pos) && farm.terrainFeatures[pos] is HoeDirt hd) {
                if (!hd.readyForHarvest()) return false;
                if (new SObject(pos, hd.crop.indexOfHarvest.Value, 0).getCategoryName() == "Flower") {
                    return true;
                }
            }
            return false;
        }

        public static void AnimateJunimo(int type, JunimoHarvester junimo) {
            var netAnimationEvent = Reflection.GetField<NetEvent1Field<int, NetInt>>(junimo, "netAnimationEvent");
            netAnimationEvent.GetValue().Fire(type);
        }

        public static void SpawnJunimoAtHut(JunimoHut hut) {
            Vector2 pos = new Vector2((float)hut.tileX.Value + 1, (float)hut.tileY.Value + 1) * 64f + new Vector2(0.0f, 32f);
            SpawnJunimoAtPosition(pos, hut, hut.getUnusedJunimoNumber());
        }

        public static void SpawnJunimoAtPosition(Vector2 pos, JunimoHut hut, int junimoNumber) {
            if (hut == null) return;
            Farm farm = Game1.getFarm();
            JunimoHarvester junimoHarvester = new JunimoHarvester(pos, hut, junimoNumber);
            farm.characters.Add((NPC)junimoHarvester);
            hut.myJunimos.Add(junimoHarvester);

            if (Game1.isRaining) {
                var alpha = Reflection.GetField<float>(junimoHarvester, "alpha");
                alpha.SetValue(Config.FunChanges.RainyJunimoSpiritFactor);
            }
            if (!Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, farm))
                return;
            farm.playSound("junimoMeep1");
        }

        public static void SendMessage(string msg) {
            if (!Config.Other.ReceiveMessages) return;

            Game1.addHUDMessage(new HUDMessage(msg, 3) {
                noIcon = true,
                timeLeft = HUDMessage.defaultTime
            });
        }
    }
}
