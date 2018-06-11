using Netcode;
using System.Linq;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;
using StardewValley.Buildings;
using SObject = StardewValley.Object;
using System;

namespace BetterJunimos.Patches {
    public enum JunimoAbility {
        None, FertilizeCrops, PlantCrops, ClearDeadCrops
    }
    public class JunimoAbilities {
        internal ModConfig.JunimoCapability Capabilities;

        // Can the Junimo use a capability/ability here
        public bool IsActionable(JunimoHut hut, Vector2 pos) {
            return IdentifyJunimoAbility(hut, pos) == JunimoAbility.None;
        }

        public JunimoAbility IdentifyJunimoAbility(JunimoHut hut, Vector2 pos) {
            Farm farm = Game1.getFarm();
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

        public void PerformAction(JunimoAbility ability, JunimoHut hut, Vector2 pos) {
            switch (ability) {
            case JunimoAbility.FertilizeCrops:
                UseItemAbility(hut, pos, SObject.fertilizerCategory, Fertilize);
                break;
            case JunimoAbility.PlantCrops:
                UseItemAbility(hut, pos, SObject.SeedsCategory, Plant);
                break;
            case JunimoAbility.ClearDeadCrops:
                ClearDeadCrops(pos);
                break;
            }
        }

        private void ClearDeadCrops(Vector2 pos) {
            Farm farm = Game1.getFarm();
            if (farm.terrainFeatures[pos] is HoeDirt hd) {
                bool animate = Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, farm);
                hd.destroyCrop(pos, animate, farm);
            }
        }

        private void UseItemAbility(JunimoHut hut, Vector2 pos, int itemCategory, Func<Vector2, int, bool> useItem) {
            Farm farm = Game1.getFarm();
            NetObjectList<Item> chest = hut.output.Value.items;

            Item foundItem = chest.FirstOrDefault(item => item.Category == itemCategory);
            if (useItem(pos, foundItem.ParentSheetIndex)) {
                Util.ReduceItemCount(chest, foundItem);
            }
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
            if (crop.seasonsToGrowIn.Count == 0)
                return false;

            if (!crop.seasonsToGrowIn.Contains(Game1.currentSeason))
                return false;

            Farm farm = Game1.getFarm();
            if (farm.terrainFeatures[pos] is HoeDirt hd) {
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
    }
}
