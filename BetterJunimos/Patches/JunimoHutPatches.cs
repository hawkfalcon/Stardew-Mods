using Netcode;
using System.Linq;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace BetterJunimos.Patches {
    // JunimoHut - areThereMatureCropsWithinRadius
    internal class PatchPathfindHut {
        public static void Postfix(JunimoHut __instance, ref bool __result) {
            if (__instance.lastKnownCropLocation.Equals(Point.Zero)) {
                __result = pathFindTilledDirt(__instance);
            }
        }

        internal static bool pathFindTilledDirt(JunimoHut hut) {
            for (int index1 = (int)((NetFieldBase<int, NetInt>)hut.tileX) + 1 - 8; index1 < (int)((NetFieldBase<int, NetInt>)hut.tileX) + 2 + 8; ++index1) {
                for (int index2 = (int)((NetFieldBase<int, NetInt>)hut.tileY) - 8 + 1; index2 < (int)((NetFieldBase<int, NetInt>)hut.tileY) + 2 + 8; ++index2) {
                    Vector2 pos = new Vector2((float)index1, (float)index2);
                    if (JunimoPlanter.IsPlantable(pos) && JunimoPlanter.AreThereSeeds(hut)) {
                        hut.lastKnownCropLocation = new Point(index1, index2);
                        return true;
                    }
                }
            }
            return false;
        }
    }

    internal class PatchJunimosInRain {
        public static void Postfix(JunimoHut __instance) {
            var junimoSendOutTimer = BetterJunimos.instance.Helper.Reflection.GetField<int>(__instance, "junimoSendOutTimer");
            if (junimoSendOutTimer.GetValue() > 0 || __instance.myJunimos.Count<JunimoHarvester>() >= 3 || Game1.IsWinter || (!__instance.areThereMatureCropsWithinRadius() || Game1.farmEvent != null))
                return;
            junimoSendOutTimer.SetValue(5000);
            BetterJunimos.instance.spawnJunimoAtHut(__instance);
        }
    }
}
