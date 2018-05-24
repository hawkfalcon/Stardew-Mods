using Netcode;
using System.Linq;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;
using StardewValley.Buildings;
using SObject = StardewValley.Object;

namespace BetterJunimos.Patches {
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

        public static void UseWorkItemFromHut(JunimoHut hut, Vector2 pos) {
            Farm farm = Game1.getFarm();
            NetObjectList<Item> chest = hut.output.Value.items;

            Item fertilizer = chest.FirstOrDefault(item => item.Category == SObject.fertilizerCategory);
            Item seeds = chest.FirstOrDefault(item => item.Category == SObject.SeedsCategory);

            if (farm.terrainFeatures[pos] is HoeDirt hd && Util.Config.JunimoCapabilities.FertilizeCrops) {
                if (fertilizer != null && hd.fertilizer.Value <= 0) {
                    if (Fertilize(pos, fertilizer.ParentSheetIndex)) {
                        Util.ReduceItemCount(chest, fertilizer);
                    }
                }
                else if (seeds != null && Util.Config.JunimoCapabilities.PlantCrops) {
                    if (Plant(pos, seeds.ParentSheetIndex)) {
                        Util.ReduceItemCount(chest, seeds);
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
    }
}
