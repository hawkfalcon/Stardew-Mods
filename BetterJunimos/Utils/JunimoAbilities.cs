using Netcode;
using System.Linq;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;
using StardewValley.Buildings;
using SObject = StardewValley.Object;
using System;
using StardewValley.Characters;

namespace BetterJunimos.Utils {
    public enum JunimoAbility {
        None, FertilizeCrops, PlantCrops, ClearDeadCrops, HarvestForageCrop
    }
    public class JunimoAbilities {
        internal ModConfig.JunimoCapability Capabilities;

        // Can the Junimo use a capability/ability here
        public bool IsActionable(JunimoHut hut, Vector2 pos) {
            return IdentifyJunimoAbility(hut, pos) != JunimoAbility.None;
        }

        public JunimoAbility IdentifyJunimoAbility(JunimoHut hut, Vector2 pos) {
            Farm farm = Game1.getFarm();
            if (Capabilities.HarvestForageCrops && IsForageCrop(farm, pos)) {
                return JunimoAbility.HarvestForageCrop;
            }
            if (farm.terrainFeatures.ContainsKey(pos) && farm.terrainFeatures[pos] is HoeDirt hd) {
                if (IsEmptyHoeDirt(farm, hd, pos)) {
                    if (Capabilities.FertilizeCrops && hd.fertilizer.Value <= 0 && HutContainsItemCategory(hut, SObject.fertilizerCategory)) {
                        return JunimoAbility.FertilizeCrops;
                    } 
                    if (Capabilities.PlantCrops && HutContainsItemCategory(hut, SObject.SeedsCategory)) {
                        return JunimoAbility.PlantCrops;
                    }
                }
                if (Capabilities.ClearDeadCrops && IsDeadCrop(farm, hd, pos)) {
                    return JunimoAbility.ClearDeadCrops;
                }
            }
            return JunimoAbility.None;
        }

        public bool IsForageCrop(Farm farm, Vector2 pos) {
            Vector2 up = new Vector2(pos.X, pos.Y + 1);
            Vector2 right = new Vector2(pos.X + 1, pos.Y);
            Vector2 down = new Vector2(pos.X, pos.Y - 1);
            Vector2 left = new Vector2(pos.X - 1, pos.Y);

            Vector2[] positions = { up, right, down, left };
            foreach (Vector2 nextPos in positions) {
                if (farm.objects.ContainsKey(nextPos) && farm.objects[nextPos].isForage(farm)) {
                    return true;
                }
            }
            return false;
        }

        public bool IsEmptyHoeDirt(Farm farm, HoeDirt hd, Vector2 pos) {
            return hd.crop == null && !farm.objects.ContainsKey(pos);
        }

        public bool IsDeadCrop(Farm farm, HoeDirt hd, Vector2 pos) {
            return farm.isCropAtTile((int)pos.X, (int)pos.Y) && hd.crop.dead.Value;
        }

        public bool HutContainsItemCategory(JunimoHut hut, int itemCategory) {
            Farm farm = Game1.getFarm();
            NetObjectList<Item> chest = hut.output.Value.items;
            return chest.Any(item => item.category == itemCategory);
        }

        public bool PerformAction(JunimoAbility ability, JunimoHut hut, Vector2 pos, JunimoHarvester junimo) {
            switch (ability) {
            case JunimoAbility.FertilizeCrops:
                return UseItemAbility(hut, pos, SObject.fertilizerCategory, Fertilize);
            case JunimoAbility.PlantCrops:
                return UseItemAbility(hut, pos, SObject.SeedsCategory, Plant);
            case JunimoAbility.HarvestForageCrop:
                return HarvestForageCrop(hut, pos, junimo);
            case JunimoAbility.ClearDeadCrops:
                return ClearDeadCrops(pos);
            }
            return false;
        }

        private bool HarvestForageCrop(JunimoHut hut, Vector2 pos, JunimoHarvester junimo) {
            Farm farm = Game1.getFarm();

            Vector2 up = new Vector2(pos.X, pos.Y + 1);
            Vector2 right = new Vector2(pos.X + 1, pos.Y);
            Vector2 down = new Vector2(pos.X, pos.Y - 1);
            Vector2 left = new Vector2(pos.X - 1, pos.Y);

            int direction = 0;
            Vector2[] positions = { up, right, down, left };
            foreach (Vector2 nextPos in positions) {
                if (farm.objects.ContainsKey(nextPos) && farm.objects[nextPos].isForage(farm)) {
                    junimo.faceDirection(direction);
                    SetForageQuality(farm, nextPos);

                    SObject item = farm.objects[nextPos];
                    Util.AddItemToHut(farm, hut, item);

                    Util.SpawnParticles(nextPos);
                    farm.objects.Remove(nextPos);
                    return true;
                }
                direction++;
            }

            return false;
        }

        // adapted from GameLocation.checkAction
        private void SetForageQuality(Farm farm, Vector2 pos) {
            int quality = farm.objects[pos].Quality;
            Random random = new Random((int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed + (int)pos.X + (int)pos.Y * 777);

            foreach (Farmer farmer in Game1.getOnlineFarmers()) {
                int maxQuality = quality;
                if (farmer.professions.Contains(16))
                    maxQuality = 4;
                else if (random.NextDouble() < farmer.ForagingLevel / 30.0)
                    maxQuality = 2;
                else if (random.NextDouble() < farmer.ForagingLevel / 15.0)
                    maxQuality = 1;
                if (maxQuality > quality)
                    quality = maxQuality;
            }
            
            farm.objects[pos].Quality = quality;
        }

        private bool ClearDeadCrops(Vector2 pos) {
            Farm farm = Game1.getFarm();
            if (farm.terrainFeatures[pos] is HoeDirt hd) {
                bool animate = Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, farm);
                hd.destroyCrop(pos, animate, farm);
                return true;
            }
            return false;
        }

        private bool UseItemAbility(JunimoHut hut, Vector2 pos, int itemCategory, Func<Vector2, int, bool> useItem) {
            Farm farm = Game1.getFarm();
            NetObjectList<Item> chest = hut.output.Value.items;

            Item foundItem = chest.FirstOrDefault(item => item.Category == itemCategory);
            bool success = useItem(pos, foundItem.ParentSheetIndex);
            if (success) {
                Util.ReduceItemCount(chest, foundItem);
            }
            return success;
        }

        private bool Fertilize(Vector2 pos, int index) {
            Farm farm = Game1.getFarm();
            if (farm.terrainFeatures[pos] is HoeDirt hd) {
                hd.fertilizer.Value = index;
                if (Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, farm)) {
                    farm.playSound("dirtyHit");
                }
            }
            return true;
        }

        private bool Plant(Vector2 pos, int index) {
            Crop crop = new Crop(index, (int)pos.X, (int)pos.Y);

            if (!crop.seasonsToGrowIn.Contains(Game1.currentSeason))
                return false;

            Farm farm = Game1.getFarm();
            if (farm.terrainFeatures[pos] is HoeDirt hd) {
                CheckSpeedGro(hd, crop);
                hd.crop = crop;

                if (Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, farm)) {
                    if (crop.raisedSeeds)
                        farm.playSound("stoneStep");
                    farm.playSound("dirtyHit");
                }

                ++Game1.stats.SeedsSown;
            }
            return true;
        }

        private void CheckSpeedGro(HoeDirt hd, Crop crop) {
            if (hd.fertilizer.Value == 465 || hd.fertilizer.Value == 466 || Game1.player.professions.Contains(5)) {
                int num1 = 0;
                for (int index1 = 0; index1 < crop.phaseDays.Count - 1; ++index1)
                    num1 += crop.phaseDays[index1];
                float num2 = hd.fertilizer.Value == 465 ? 0.1f : (hd.fertilizer.Value == 466 ? 0.25f : 0.0f);
                if (Game1.player.professions.Contains(5))
                    num2 += 0.1f;
                int num3 = (int)Math.Ceiling((double)num1 * (double)num2);
                for (int index1 = 0; num3 > 0 && index1 < 3; ++index1) {
                    for (int index2 = 0; index2 < crop.phaseDays.Count; ++index2) {
                        if (index2 > 0 || crop.phaseDays[index2] > 1) {
                            crop.phaseDays[index2]--;
                            --num3;
                        }
                        if (num3 <= 0)
                            break;
                    }
                }
            }
        }
    }
}
