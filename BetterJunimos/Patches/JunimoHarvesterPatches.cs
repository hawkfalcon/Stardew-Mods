using Netcode;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace BetterJunimos.Patches {
    // JunimoHarvester - foundCropEndFunction
    public class PatchFindingCropEnd {
        public static void Postfix(ref PathNode currentNode, JunimoHarvester __instance, ref bool __result) {
            JunimoHut hut = Util.GetHutFromJunimo(__instance);
            __result = __result || (JunimoPlanter.IsPlantable(new Vector2(currentNode.x, currentNode.y)) && JunimoPlanter.AreThereSeeds(hut));
        }
    }

    // JunimoHarvester - tryToHarvestHere
    public class PatchHarvestAttemptToCustom {
        public static void Postfix(JunimoHarvester __instance) {
            Vector2 pos = __instance.getTileLocation();
            JunimoHut hut = Util.GetHutFromJunimo(__instance);
            if (JunimoPlanter.IsPlantable(pos) && JunimoPlanter.AreThereSeeds(hut)) {
                var harvestTimer = BetterJunimos.instance.Helper.Reflection.GetField<int>(__instance, "harvestTimer");
                harvestTimer.SetValue(999);
                JunimoPlanter.UseItemFromHut(hut, pos);
            }
        }
    }

    // JunimoHarvester - update
    public class PatchJunimoShake {
        public static void Postfix(JunimoHarvester __instance) {
            var harvestTimer = BetterJunimos.instance.Helper.Reflection.GetField<int>(__instance, "harvestTimer").GetValue();
            if (harvestTimer > 500 && harvestTimer < 1000) {
                var netAnimationEvent = BetterJunimos.instance.Helper.Reflection.GetField<NetEvent1Field<int, NetInt>>(__instance, "netAnimationEvent");
                netAnimationEvent.GetValue().Fire(4);
                __instance.shake(50);
            }
        }
    }
}
