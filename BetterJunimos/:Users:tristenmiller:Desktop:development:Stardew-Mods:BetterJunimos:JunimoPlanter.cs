using Harmony;
using StardewValley.Characters;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;
using StardewValley.Buildings;
using Netcode;
using System;

namespace BetterJunimos {
    /* 
     * JunimoPlanter
     */
    [HarmonyPatch(typeof(JunimoHarvester))]
    [HarmonyPatch("foundCropEndFunction")]
    public class PatchFinding {
        static void Postfix(ref PathNode currentNode, ref GameLocation location, bool __result) {
            //BetterJunimos.instance.Monitor.Log("Any tilled dirt is good");
            //__result = __result || location.isTileHoeDirt(new Vector2(currentNode.x, currentNode.y));
        }
    }

    [HarmonyPatch(typeof(JunimoHarvester))]
    [HarmonyPatch("tryToHarvestHere")]
    public class PatchHarvestAttempt {
        static void Postfix(JunimoHarvester __instance) {
            BetterJunimos.instance.Monitor.Log("Any tilled dirt (set plant timer)");

        //    Vector2 pos = __instance.getTileLocation();
        //    if (__instance.currentLocation.terrainFeatures.ContainsKey(pos) && __instance.currentLocation.terrainFeatures[pos] is HoeDirt hd) {
        //        var timer = BetterJunimos.instance.Helper.Reflection.GetField<int>(__instance, "harvestTimer");
        //        //if (timer.GetValue() <= 0) {
        //        //    timer.SetValue(2000);
        //        //}
        //        NetGuid netHome = BetterJunimos.instance.Helper.Reflection.GetField<NetGuid>(__instance, "netHome").GetValue();
        //        JunimoHut hut = Game1.getFarm().buildings[netHome.Value] as JunimoHut;
        //        Item seed = hut.output.Value.items[0];
        //        BetterJunimos.instance.Monitor.Log(seed.ParentSheetIndex.ToString());
        //        hd.plant(seed.ParentSheetIndex, (int)pos.X, (int)pos.Y, Game1.player, true, Game1.getFarm());
        //        hd.plant(seed.ParentSheetIndex, (int)pos.X, (int)pos.Y, Game1.player, false, Game1.getFarm());

        //    }
        //}
    }

    [HarmonyPatch(typeof(JunimoHarvester))]
    [HarmonyPatch("update")]
    [HarmonyPatch(new Type[] { typeof(GameTime), typeof(GameLocation) })]
    public class PatchUpdateAndPlant {
        static void Postfix(JunimoHarvester __instance) {
            BetterJunimos.instance.Monitor.Log("Update: plant");
            //var timer = BetterJunimos.instance.Helper.Reflection.GetField<int>(__instance, "harvestTimer");
            //if (timer.GetValue() > 0) {
            //    BetterJunimos.instance.Monitor.Log(timer.GetValue().ToString());
            //}
        }
    }
}

/*1. Harmony postfix foundCropEndFunction to or with your condition
2. Harmony postfix tryToHarvestHere to set the timer to 2000 if junimo is in correct place
3. Harmony postfix update if harvestTimer >= 1000 && this.harvestTimer< 1000 and this.lastItemHarvested == null to  do your harvest
1. foundCropEndFunction - the tile at the location is a hoedirt
2. tryToHarvestHere - check if it's a hoedirt
3. postfix on update - do planting
*/