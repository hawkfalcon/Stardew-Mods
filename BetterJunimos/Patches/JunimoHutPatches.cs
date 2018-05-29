using System.Linq;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;

namespace BetterJunimos.Patches {
    // areThereMatureCropsWithinRadius
    internal class PatchSearchAroundHut {
        public static void Postfix(JunimoHut __instance, ref bool __result) {
            // Prevent unnecessary searching when unpaid
            if (Util.Config.JunimoPayment.WorkForWages && !Util.WereJunimosPaidToday) {
                __result = true;
                __instance.lastKnownCropLocation = Point.Zero;
            }
            
            if (__result)
                return;
            
            __result = searchAroundHut(__instance, Util.MaxRadius);
        }

        // search for crops + open plantable spots
        internal static bool searchAroundHut(JunimoHut hut, int radius) {
            Farm farm = Game1.getFarm();
            for (int x = hut.tileX.Value + 1 - radius; x < hut.tileX.Value + 2 + radius; ++x) {
                for (int y = hut.tileY.Value + 1 - radius; y < hut.tileY.Value + 2 + radius; ++y) {
                    Vector2 pos = new Vector2((float)x, (float)y);
                    if ((radius > Util.DefaultRadius && farm.isCropAtTile(x, y) && (farm.terrainFeatures[new Vector2((float)x, (float)y)] as HoeDirt).readyForHarvest()) ||
                        (Util.Config.JunimoCapabilities.PlantCrops && JunimoPlanter.IsPlantable(pos) && JunimoPlanter.AreThereSeeds(hut))) {
                        hut.lastKnownCropLocation = new Point(x, y);
                        return true;
                    }
                }
            }
            return false;
        }
    }

    // performTenMinuteAction
    internal class PatchJunimosSpawning {
        public static void Postfix(JunimoHut __instance) {
            //if (Util.Config.JunimoPayment.WorkForWages && !Util.WereJunimosPaidToday) {
            //    Util.MaxRadius = 3;
            //}
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
