using Netcode;
using System.Linq;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;
using StardewValley.Buildings;
using StardewValley.Characters;
using SObject = StardewValley.Object;

namespace BetterJunimos {
    public class JunimoPlanter {
        public static bool IsPlantable(Vector2 pos) {
            Farm farm = Game1.getFarm();
            return farm.terrainFeatures.ContainsKey(pos) && farm.terrainFeatures[pos] is HoeDirt hd &&
                hd.crop == null && !farm.objects.ContainsKey(pos);
        }

        public static void UseItemFromHut(JunimoHut hut, Vector2 pos) {
            Farm farm = Game1.getFarm();
            NetObjectList<Item> chest = hut.output.Value.items;

            Item fertilizer = chest.FirstOrDefault(item => item.Category == SObject.fertilizerCategory);
            Item seeds = chest.FirstOrDefault(item => item.Category == SObject.SeedsCategory);

            if (farm.terrainFeatures[pos] is HoeDirt hd) {
                if (fertilizer != null && hd.fertilizer.Value <= 0) {
                    hd.plant(fertilizer.ParentSheetIndex, (int)pos.X, (int)pos.Y, Game1.player, true, farm);
                    ReduceItemCount(chest, fertilizer);
                }
                else if (seeds != null) {
                    hd.plant(seeds.ParentSheetIndex, (int)pos.X, (int)pos.Y, Game1.player, false, farm);
                    ReduceItemCount(chest, seeds);
                }
            }
            //BetterJunimos.instance.Monitor.Log();
        }

        private static void ReduceItemCount(NetObjectList<Item> chest, Item item) {
            item.Stack--;
            if (item.Stack == 0) {
                chest.Remove(item);
            }
        }
    }

    // JunimoHarvester - foundCropEndFunction
    public class PatchFindingCropEnd {
        static void Postfix(ref PathNode currentNode, ref GameLocation location, ref bool __result) {
            __result = __result || JunimoPlanter.IsPlantable(new Vector2(currentNode.x, currentNode.y));
        }
    }

    // JunimoHarvester - tryToHarvestHere
    public class PatchHarvestAttemptToCustom {
        static void Postfix(JunimoHarvester __instance) {
            //BetterJunimos.instance.Monitor.Log("Any tilled dirt (set plant timer)");
            Vector2 pos = __instance.getTileLocation();
            if (JunimoPlanter.IsPlantable(pos)) {
                var harvestTimer = BetterJunimos.instance.Helper.Reflection.GetField<int>(__instance, "harvestTimer");
                harvestTimer.SetValue(2000);

                NetGuid netHome = BetterJunimos.instance.Helper.Reflection.GetField<NetGuid>(__instance, "netHome").GetValue();
                JunimoHut hut = Game1.getFarm().buildings[netHome.Value] as JunimoHut;
                JunimoPlanter.UseItemFromHut(hut, pos);
            }
        }
    }

    /* JunimoHarvester - update
    [HarmonyPatch(new Type[] { typeof(GameTime), typeof(GameLocation) })]
    public class PatchUpdateAndPlant {
        static void Postfix(JunimoHarvester __instance) {
        }
    }*/

    // JunimoHut - areThereMatureCropsWithinRadius
    public class PatchPathfindHut {
        static void Postfix(JunimoHut __instance, ref bool __result) {
            if (__instance.lastKnownCropLocation.Equals(Point.Zero)) {
                __result = pathFindTilledDirt(__instance);
            }
        }

        private static bool pathFindTilledDirt(JunimoHut hut) {
            for (int index1 = (int)((NetFieldBase<int, NetInt>)hut.tileX) + 1 - 8; index1 < (int)((NetFieldBase<int, NetInt>)hut.tileX) + 2 + 8; ++index1) {
                for (int index2 = (int)((NetFieldBase<int, NetInt>)hut.tileY) - 8 + 1; index2 < (int)((NetFieldBase<int, NetInt>)hut.tileY) + 2 + 8; ++index2) {
                    Vector2 pos = new Vector2((float)index1, (float)index2);
                    if (JunimoPlanter.IsPlantable(pos)) {
                        hut.lastKnownCropLocation = new Point(index1, index2);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
