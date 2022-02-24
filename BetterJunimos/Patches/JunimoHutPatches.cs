using System.Linq;
using BetterJunimos.Utils;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
// ReSharper disable InconsistentNaming

namespace BetterJunimos.Patches {
    /* areThereMatureCropsWithinRadius **OVERWRITES PREFIX**
     *
     * Search for actionable tiles
     * Completely rewrite original function.
    */
    internal class PatchSearchAroundHut {
        public static bool Prefix(JunimoHut __instance, ref bool __result) {
            if (!Context.IsMainPlayer) return true;

            // Prevent unnecessary searching when unpaid
            if (BetterJunimos.Config.JunimoPayment.WorkForWages && !Util.Payments.WereJunimosPaidToday) {
                __instance.lastKnownCropLocation = Point.Zero;
                return false;
            }

            __result = SearchAroundHut(__instance);
            return false;
        }

        // search for crops + open plantable spots
        private static bool SearchAroundHut(JunimoHut hut) {
            var id = Util.GetHutIdFromHut(hut);
            var radius = Util.CurrentWorkingRadius;
            for (var x = hut.tileX.Value + 1 - radius; x < hut.tileX.Value + 2 + radius; ++x) {
                for (var y = hut.tileY.Value + 1 - radius; y < hut.tileY.Value + 2 + radius; ++y) {
                    var pos = new Vector2(x, y);
                    var ability = Util.Abilities.IdentifyJunimoAbility(pos, id);
                    if (ability == null) continue;
                    hut.lastKnownCropLocation = new Point(x, y);
                    return true;
                }
            }

            hut.lastKnownCropLocation = Point.Zero;
            return false;
        }
    }

    /* Update
     * 
     * To allow more junimos, allow working in rain
     */
    [HarmonyPriority(Priority.Low)]
    internal class ReplaceJunimoHutUpdate {
        // This is to prevent the update function from running, other than base.Update()
        // Capture sendOutTimer and use to stop execution
        public static void Prefix(JunimoHut __instance, ref int ___junimoSendOutTimer, out int __state) {
            __state = ___junimoSendOutTimer;
            ___junimoSendOutTimer = 0;
        }

        public static void Postfix(JunimoHut __instance, GameTime time, ref int ___junimoSendOutTimer, int __state) {
            if (__state <= 0) return;
            if (!Context.IsMainPlayer) return;

            ___junimoSendOutTimer = __state - time.ElapsedGameTime.Milliseconds;
            
            // Don't work on farmEvent days
            if (Game1.farmEvent != null)
                return;
            // Winter
            if (Game1.IsWinter && !Util.Progression.CanWorkInWinter) {
                return;
            }
            // Rain
            if (Game1.isRaining && !Util.Progression.CanWorkInRain){
                return;
            }
            // Currently sending out a junimo
            if (___junimoSendOutTimer > 0) {
                return;
            }
            // Already enough junimos
            if (__instance.myJunimos.Count() >= Util.Progression.MaxJunimosUnlocked){
                // FileLog.Log($"Already {__instance.myJunimos.Count} Junimos, limit is {Util.Progression.MaxJunimosUnlocked}");
                return;
            }
            // Nothing to do
            if (!__instance.areThereMatureCropsWithinRadius()) {
                // FileLog.Log("No work for Junimos to do, not spawning another");
                return;
            }
            Util.SpawnJunimoAtHut(__instance);
            ___junimoSendOutTimer = 1000;
        }
    }

    /*
     * performTenMinuteAction
     * 
     * Add the end to trigger more than 3 junimos to spawn
     */
    [HarmonyPriority(Priority.Low)]
    internal class ReplaceJunimoTimerNumber {
        public static void Postfix(JunimoHut __instance, ref int ___junimoSendOutTimer) {
            if (!Context.IsMainPlayer) return;
            var time = Util.Progression.CanWorkInEvenings ? 2400 : 1900;
            if (Game1.timeOfDay > time) return;

            if (__instance.myJunimos.Count < Util.Progression.MaxJunimosUnlocked) {
                ___junimoSendOutTimer = 1;
            }
        }
    }

    /* getUnusedJunimoNumber
     * 
     * Completely rewrite method to support more than 3 junimos
     * The only difference is the use of MaxJunimos
    */
    [HarmonyPriority(Priority.Low)]
    internal class ReplaceJunimoHutNumber {
        public static bool Prefix(JunimoHut __instance, ref int __result) {
            if (!Context.IsMainPlayer) return true;

            for (var index = 0; index < Util.Progression.MaxJunimosUnlocked; ++index) {
                if (index >= __instance.myJunimos.Count) {
                    __result = index;
                    return false;
                }

                var flag = __instance.myJunimos.Any(junimo => junimo.whichJunimoFromThisHut == index);

                if (flag) continue;
                __result = index;
                return false;
            }

            __result = 2;
            return false;
        }
    }
}