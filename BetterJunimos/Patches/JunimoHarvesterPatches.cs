using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.Characters;
using Harmony;

namespace BetterJunimos.Patches {
    // foundCropEndFunction
    public class PatchFindingCropEnd {
        public static void Postfix(ref PathNode currentNode, JunimoHarvester __instance, ref bool __result) {
            JunimoHut hut = Util.GetHutFromJunimo(__instance);
            __result = __result || (JunimoPlanter.IsPlantable(new Vector2(currentNode.x, currentNode.y)) && JunimoPlanter.AreThereSeeds(hut));
        }
    }

    // tryToHarvestHere
    public class PatchHarvestAttemptToCustom {
        public static void Postfix(JunimoHarvester __instance) {
            Vector2 pos = __instance.getTileLocation();
            JunimoHut hut = Util.GetHutFromJunimo(__instance);
            if (JunimoPlanter.IsPlantable(pos) && JunimoPlanter.AreThereSeeds(hut)) {
                var harvestTimer = BetterJunimos.instance.Helper.Reflection.GetField<int>(__instance, "harvestTimer");
                harvestTimer.SetValue(999);
                JunimoPlanter.UseWorkItemFromHut(hut, pos);
            }
        }
    }

    // update
    public class PatchJunimoShake {
        public static void Postfix(JunimoHarvester __instance) {
            var harvestTimer = BetterJunimos.instance.Helper.Reflection.GetField<int>(__instance, "harvestTimer").GetValue();
            if (harvestTimer > 500 && harvestTimer < 1000) {
                Util.AnimateJunimo(4, __instance);
                __instance.shake(50);
            }
        }
    }

    // pathfindToRandomSpotAroundHut
    public class PatchPathfind {
        public static void Postfix(JunimoHarvester __instance) {
            JunimoHut hut = Util.GetHutFromJunimo(__instance);
            int radius = Util.Config.JunimoImprovements.MaxRadius;
            __instance.controller = new PathFindController(__instance, __instance.currentLocation, Utility.Vector2ToPoint(
                new Vector2((float)(hut.tileX.Value + 1 + Game1.random.Next(-radius, radius + 1)), (float)(hut.tileY.Value + 1 + Game1.random.Next(-radius, radius + 1)))),
                -1, new PathFindController.endBehavior(__instance.reachFirstDestinationFromHut), 100);
        }
    }

    // pathFindToNewCrop_doWork - completely replace 
    // This exists to remove the max distance boundary
    [HarmonyPriority(Priority.Low)]
    public class PatchPathfindDoWork {

        public static bool Prefix(JunimoHarvester __instance) {
            if (Game1.timeOfDay > 1900) {
                if (__instance.controller != null)
                    return false;
                __instance.returnToJunimoHut(__instance.currentLocation);
            }
            else if (Game1.random.NextDouble() < 0.035 || Util.GetHutFromJunimo(__instance).noHarvest) {
                __instance.pathfindToRandomSpotAroundHut();
            }
            else {
                __instance.controller = new PathFindController(__instance, __instance.currentLocation, 
                    new PathFindController.isAtEnd(__instance.foundCropEndFunction), -1, false, 
                    new PathFindController.endBehavior(__instance.reachFirstDestinationFromHut), 100, Point.Zero);
                if (__instance.controller.pathToEndPoint == null) {
                    JunimoHut hut = Util.GetHutFromJunimo(__instance);
                    if (Game1.random.NextDouble() < 0.5 && !hut.lastKnownCropLocation.Equals(Point.Zero))
                        __instance.controller = new PathFindController(__instance, __instance.currentLocation, hut.lastKnownCropLocation, -1, 
                            new PathFindController.endBehavior(__instance.reachFirstDestinationFromHut), 100);
                    else if (Game1.random.NextDouble() < 0.25) {
                        Util.AnimateJunimo(0, __instance);
                        __instance.returnToJunimoHut(__instance.currentLocation);
                    }
                    else
                        __instance.pathfindToRandomSpotAroundHut();
                }
                else
                    Util.AnimateJunimo(0, __instance);
            }
            return false;
        }
    }
}
