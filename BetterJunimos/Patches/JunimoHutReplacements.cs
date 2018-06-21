using System.Linq;
using BetterJunimos.Utils;
using Harmony;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace BetterJunimos.Patches {
    // Update
    // To allow more junimos + rain working
    [HarmonyPriority(Priority.Low)]
    internal class ReplaceJunimoHutUpdate {
        // this is to prevent the update function from running, but keeps it's call to base.Update()
        public static void Prefix(JunimoHut __instance, int __state) {
            var junimoSendOutTimer = Util.Reflection.GetField<int>(__instance, "junimoSendOutTimer");
            __state = junimoSendOutTimer.GetValue();
            junimoSendOutTimer.SetValue(0);
        }

        public static void Postfix(JunimoHut __instance, GameTime time, int __state) {
            var junimoSendOutTimer = Util.Reflection.GetField<int>(__instance, "junimoSendOutTimer");
            int sendOutTimer = __state;
            junimoSendOutTimer.SetValue(__state);

            // from Update
            junimoSendOutTimer.SetValue(sendOutTimer - time.ElapsedGameTime.Milliseconds);
            if (sendOutTimer > 0 || __instance.myJunimos.Count() >= Util.Config.JunimoHuts.MaxJunimos ||
                Game1.IsWinter || !__instance.areThereMatureCropsWithinRadius() || Game1.farmEvent != null)
                return;
            // Rain
            if (Game1.isRaining && !Util.Config.JunimoImprovements.CanWorkInRain) 
                return;
            
            Util.SpawnJunimoAtHut(__instance);
            junimoSendOutTimer.SetValue(1000);
        }
    }

    // getUnusedJunimoNumber
    // For more junimos
    [HarmonyPriority(Priority.Low)]
    internal class ReplaceJunimoHutNumber {
        public static bool Prefix(JunimoHut __instance, ref int __result) {
            for (int index = 0; index < Util.Config.JunimoHuts.MaxJunimos; ++index) {
                if (index >= __instance.myJunimos.Count<JunimoHarvester>()) {
                    __result = index;
                    return false;
                }
                bool flag = false;
                foreach (JunimoHarvester junimo in __instance.myJunimos) {
                    if (junimo.whichJunimoFromThisHut == index) {
                        flag = true;
                        break;
                    }
                }
                if (!flag) {
                    __result = index;
                    return false;
                }
            }
            __result = 2;
            return false;
        }
    }
}
