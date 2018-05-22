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

        public static bool AreThereSeeds(JunimoHut hut) {
            Farm farm = Game1.getFarm();
            NetObjectList<Item> chest = hut.output.Value.items;
            return chest.Any(item => item.category == SObject.SeedsCategory);
        }

        public static JunimoHut GetHutFromJunimo(JunimoHarvester junimo) {
            NetGuid netHome = BetterJunimos.instance.Helper.Reflection.GetField<NetGuid>(junimo, "netHome").GetValue();
            return Game1.getFarm().buildings[netHome.Value] as JunimoHut;
        }

        public static void UseItemFromHut(JunimoHut hut, Vector2 pos) {
            Farm farm = Game1.getFarm();
            NetObjectList<Item> chest = hut.output.Value.items;

            Item fertilizer = chest.FirstOrDefault(item => item.Category == SObject.fertilizerCategory);
            Item seeds = chest.FirstOrDefault(item => item.Category == SObject.SeedsCategory);

            if (farm.terrainFeatures[pos] is HoeDirt hd && BetterJunimos.instance.Config.FertilizeCrops) {
                if (fertilizer != null && hd.fertilizer.Value <= 0) {
                    if (Fertilize(pos, fertilizer.ParentSheetIndex)) {
                        ReduceItemCount(chest, fertilizer);
                    }
                }
                else if (seeds != null && BetterJunimos.instance.Config.PlantCrops) {
                    if (Plant(pos, seeds.ParentSheetIndex)) {
                        ReduceItemCount(chest, seeds);
                    }
                }
            }
        }

        private static bool Fertilize(Vector2 pos, int index) {
            Farm farm = Game1.getFarm();
            if (farm.terrainFeatures[pos] is HoeDirt hd) {
                if (hd.fertilizer.Value != 0)
                    return false;
                
                hd.fertilizer.Value = index;
                if (Utility.isOnScreen(pos.ToPoint(), 64, farm)) {
                    farm.playSound("dirtyHit");
                }
            }
            return true;
        }

        private static bool Plant(Vector2 pos, int index) {
            Crop crop = new Crop(index, (int)pos.X, (int)pos.Y);
            if (crop.seasonsToGrowIn.Count == 0)
                return false;

            if (!crop.seasonsToGrowIn.Contains(Game1.currentSeason))
                return false;

            Farm farm = Game1.getFarm();
            if (farm.terrainFeatures[pos] is HoeDirt hd) {
                hd.crop = crop;
                if (Utility.isOnScreen(pos.ToPoint(), 64, farm)) {
                    if (crop.raisedSeeds)
                        farm.playSound("stoneStep");
                    farm.playSound("dirtyHit");
                }

                ++Game1.stats.SeedsSown;
            }
            return true;

        }

        private static void ReduceItemCount(NetObjectList<Item> chest, Item item) {
            if (!BetterJunimos.instance.Config.ConsumeItemsFromJunimoHut) { return; }
            item.Stack--;
            if (item.Stack == 0) {
                chest.Remove(item);
            }
        }
    }

    // JunimoHarvester - foundCropEndFunction
    public class PatchFindingCropEnd {
        public static void Postfix(ref PathNode currentNode, JunimoHarvester __instance, ref bool __result) {
            JunimoHut hut = JunimoPlanter.GetHutFromJunimo(__instance);
            __result = __result || (JunimoPlanter.IsPlantable(new Vector2(currentNode.x, currentNode.y)) && JunimoPlanter.AreThereSeeds(hut));
        }
    }

    // JunimoHarvester - tryToHarvestHere
    public class PatchHarvestAttemptToCustom {
        public static void Postfix(JunimoHarvester __instance) {
            Vector2 pos = __instance.getTileLocation();
            JunimoHut hut = JunimoPlanter.GetHutFromJunimo(__instance);
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

    // JunimoHut - areThereMatureCropsWithinRadius
    public class PatchPathfindHut {
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

    public class PatchJunimosInRain {
        public static void Postfix(JunimoHut __instance) {
            var junimoSendOutTimer = BetterJunimos.instance.Helper.Reflection.GetField<int>(__instance, "junimoSendOutTimer");
            if (junimoSendOutTimer.GetValue() > 0 || __instance.myJunimos.Count<JunimoHarvester>() >= 3 || Game1.IsWinter || (!__instance.areThereMatureCropsWithinRadius() || Game1.farmEvent != null)) 
                return;
            junimoSendOutTimer.SetValue(5000);
            BetterJunimos.instance.spawnJunimoAtHut(__instance);
        }
    }
}
