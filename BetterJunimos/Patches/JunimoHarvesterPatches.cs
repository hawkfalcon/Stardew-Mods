using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.Characters;
using Harmony;

namespace BetterJunimos.Patches {
    // foundCropEndFunction
    // Is there an action to perform at the end of this pathfind?
    public class PatchFindingCropEnd {
        public static void Postfix(ref PathNode currentNode, JunimoHarvester __instance, ref bool __result) {
            JunimoHut hut = Util.GetHutFromJunimo(__instance);
            __result = __result || Util.Abilities.IsActionable(hut, new Vector2(currentNode.x, currentNode.y));
        }
    }

    // tryToHarvestHere
    // Try to perform ability 
    public class PatchHarvestAttemptToCustom {
        public static void Postfix(JunimoHarvester __instance) {
            Vector2 pos = __instance.getTileLocation();
            var harvestTimer = Util.Reflection.GetField<int>(__instance, "harvestTimer");
            if (Util.ShouldAvoidHarvesting(pos)) {
                harvestTimer.SetValue(0);
                __instance.jumpWithoutSound();
                __instance.pathfindToNewCrop();
                return;
            }
            JunimoHut hut = Util.GetHutFromJunimo(__instance);
            JunimoAbility junimoAbility = Util.Abilities.IdentifyJunimoAbility(hut, pos);
            if (junimoAbility != JunimoAbility.None) {
                int time = Util.Config.JunimoImprovements.WorkFaster ? 300 : 998;
                if (!Util.Abilities.PerformAction(junimoAbility, hut, pos, __instance)) {
                    // didn't succeed, move on
                    time = 0;
                }
                harvestTimer.SetValue(time);
            }
        }
    }

    // pokeToHarvest
    // Reduce chance of doing nothing
    public class PatchPokeToHarvest {
        public static void Postfix(JunimoHarvester __instance) {
            var harvestTimer = Util.Reflection.GetField<int>(__instance, "harvestTimer");
            if (harvestTimer.GetValue() <= 0 && (Util.Config.JunimoImprovements.WorkFaster || Game1.random.NextDouble() >= 0.3)) {
                __instance.pathfindToNewCrop();
            }
        }
    }

    // update
    // Animate & handle action timer 
    public class PatchJunimoShake {
        public static void Postfix(JunimoHarvester __instance) {
            var harvestTimer = Util.Reflection.GetField<int>(__instance, "harvestTimer");
            int time = harvestTimer.GetValue();
            if (Util.Config.JunimoImprovements.WorkFaster && time == 999) {
                // skip last second of harvesting if faster
                harvestTimer.SetValue(0);
                __instance.pokeToHarvest();
            }
            else if (time > 500 && time < 1000 || (Util.Config.JunimoImprovements.WorkFaster && time > 5)) {
                __instance.shake(50);
            }
        }
    }

    // pathfindToRandomSpotAroundHut
    // Expand radius of random pathfinding
    public class PatchPathfind {
        public static void Postfix(JunimoHarvester __instance) {
            JunimoHut hut = Util.GetHutFromJunimo(__instance);
            int radius = Util.MaxRadius;
            __instance.controller = new PathFindController(__instance, __instance.currentLocation, Utility.Vector2ToPoint(
                new Vector2((float)(hut.tileX.Value + 1 + Game1.random.Next(-radius, radius + 1)), (float)(hut.tileY.Value + 1 + Game1.random.Next(-radius, radius + 1)))),
                -1, new PathFindController.endBehavior(__instance.reachFirstDestinationFromHut), 100);
        }
    }

    // pathFindToNewCrop_doWork - completely replace 
    // Remove the max distance boundary
    [HarmonyPriority(Priority.Low)]
    public class PatchPathfindDoWork {
        
        public static bool Prefix(JunimoHarvester __instance) {
            if (Game1.timeOfDay > 1900) {
                if (__instance.controller != null)
                    return false;
                __instance.returnToJunimoHut(__instance.currentLocation);
            }
            // Prevent working when not paid
            else if (Util.Config.JunimoPayment.WorkForWages && !Util.Payments.WereJunimosPaidToday) {
                if (Game1.random.NextDouble() < 0.02) {
                    __instance.pathfindToRandomSpotAroundHut();
                }
                else {
                    // go on strike
                    Util.AnimateJunimo(7, __instance);
                }
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
