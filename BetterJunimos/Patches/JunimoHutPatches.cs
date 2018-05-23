using System.Linq;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;

namespace BetterJunimos.Patches {
    // areThereMatureCropsWithinRadius
    internal class PatchSearchAroundHut {
        public static void Postfix(JunimoHut __instance, ref bool __result) {
            if (Util.Config.JunimoPayment.WorkForWages) {
                __result = false;
                return;
            }
            if (__result)
                return;
            
            int range = Util.Config.JunimoImprovements.WorkRangeRadius;
            if (range > Util.DefaultRange) {
                __result = pathFindExtraHarvestableDirt(__instance, range);
            }

            if (__instance.lastKnownCropLocation.Equals(Point.Zero)) {
                __result = pathFindTilledDirt(__instance, range);
            }
        }

        internal static bool pathFindExtraHarvestableDirt(JunimoHut hut, int range) {
            Farm farm = Game1.getFarm();
            for (int x = hut.tileX.Value + 1 - range + Util.DefaultRange; x < hut.tileX.Value + 2 + range; ++x) {
                for (int y = hut.tileY.Value + 1 - range + Util.DefaultRange; y < hut.tileY.Value + 2 + range; ++y) {
                    if (farm.isCropAtTile(x, y) && (farm.terrainFeatures[new Vector2((float)x, (float)y)] as HoeDirt).readyForHarvest()) {
                        hut.lastKnownCropLocation = new Point(x, y);
                        return true;
                    }
                }
            }
            hut.lastKnownCropLocation = Point.Zero;
            return false;
        }


        internal static bool pathFindTilledDirt(JunimoHut hut, int range) {
            for (int x = hut.tileX.Value + 1 - range; x < hut.tileX.Value + 2 + range; ++x) {
                for (int y = hut.tileY.Value + 1 - range; y < hut.tileY.Value + 2 + range; ++y) {
                    Vector2 pos = new Vector2((float)x, (float)y);
                    if (JunimoPlanter.IsPlantable(pos) && JunimoPlanter.AreThereSeeds(hut)) {
                        hut.lastKnownCropLocation = new Point(x, y);
                        return true;
                    }
                }
            }
            return false;
        }
    }

    // Update
    internal class PatchJunimosInRain {
        public static void Postfix(JunimoHut __instance) {
            var junimoSendOutTimer = BetterJunimos.instance.Helper.Reflection.GetField<int>(__instance, "junimoSendOutTimer");
            if (junimoSendOutTimer.GetValue() > 0 || __instance.myJunimos.Count() >= 3 || Game1.IsWinter || Game1.farmEvent != null || !__instance.areThereMatureCropsWithinRadius())
                return;
            junimoSendOutTimer.SetValue(5000);
            BetterJunimos.instance.spawnJunimoAtHut(__instance);
        }
    }
}
