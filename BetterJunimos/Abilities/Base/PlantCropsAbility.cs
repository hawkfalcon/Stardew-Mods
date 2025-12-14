using System;
using System.Linq;
using BetterJunimos.Utils;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley.Buildings;
using SObject = StardewValley.Object;

namespace BetterJunimos.Abilities {
    public class PlantCropsAbility : IJunimoAbility {
        int ItemCategory = SObject.SeedsCategory;
        private readonly IMonitor Monitor;

        private const string SunflowerSeeds = "431";
        private const string SpeedGro = "465";
        private const string DeluxeSpeedGro = "466";
        private const string HyperSpeedGro = "918";
        
        static Dictionary<string, Dictionary<string, bool>> cropSeasons = new();
        
        internal PlantCropsAbility(IMonitor Monitor) {
            this.Monitor = Monitor;
            var seasons = new List<string>{"spring", "summer", "fall", "winter"};
            foreach (string season in seasons) {
                cropSeasons[season] = new Dictionary<string, bool>();
            }
        }

        public string AbilityName() {
            return "PlantCrops";
        }

        public bool IsActionAvailable(GameLocation location, Vector2 pos, Guid guid) {
            var plantable = location.terrainFeatures.ContainsKey(pos) 
                            && location.terrainFeatures[pos] is HoeDirt {crop: null} 
                            && !location.objects.ContainsKey(pos);
            if (!plantable) return false;
            
            // todo: this section is potentially slow and might be refined
            JunimoHut hut = Util.GetHutFromId(guid);
            Chest chest = hut.GetOutputChest();
            string cropType = BetterJunimos.CropMaps.GetCropForPos(hut, pos);
            Item foundItem = PlantableSeed(location, chest, cropType);

            return foundItem is not null;

        }
        
        public bool PerformAction(GameLocation location, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            JunimoHut hut = Util.GetHutFromId(guid);
            Chest chest = hut.GetOutputChest();
            string cropType = BetterJunimos.CropMaps.GetCropForPos(hut, pos);
            Item foundItem = PlantableSeed(location, chest, cropType);
            if (foundItem is null) {
                Monitor.Log($"No seed to plant at {location.Name} [{pos.X} {pos.Y}]", LogLevel.Warn);
                return false;
            }

            if (Plant(location, pos, foundItem.ItemId)) {
                Util.RemoveItemFromChest(chest, foundItem);
                return true;
            }
            BetterJunimos.SMonitor.Log($"PerformAction did not plant", LogLevel.Warn);
            return false;
        }

        /// <summary>Get an item from the chest that is a crop seed, plantable in this season</summary>
        private Item PlantableSeed(GameLocation location, Chest chest, string cropType=null) {
            var foundItems = chest.Items.ToList().FindAll(item =>
                item != null
                //&& new StardewValley.Object(item.ItemId, 1).Type == "Seeds"
                && !(BetterJunimos.Config.JunimoImprovements.AvoidPlantingCoffee && item.ParentSheetIndex == Util.CoffeeId)
            );
            foundItems = foundItems.FindAll(item => IsCrop(item, location));
            switch (cropType)
            {
                case CropTypes.Trellis:
                    foundItems = foundItems.FindAll(item => IsTrellisCrop(item, location));
                    break;
                case CropTypes.Ground:
                    foundItems = foundItems.FindAll(item => !IsTrellisCrop(item, location));
                    break;
            }
            
            if (foundItems.Count == 0) return null;
            if (location.IsGreenhouse) return foundItems.First();
            if (!BetterJunimos.Config.JunimoImprovements.AvoidPlantingOutOfSeason) return foundItems.First();
            
            foreach (var foundItem in foundItems) {
                // TODO: check if item can grow to harvest before end of season
                if (foundItem.ItemId == SunflowerSeeds && Game1.IsFall && Game1.dayOfMonth >= 25) {
                    // there is no way that a sunflower planted on Fall 25 will grow to harvest
                    continue;
                }
                
                var key = foundItem.ItemId;
                try {
                    if (cropSeasons[Game1.currentSeason][key]) {
                        return foundItem;
                    }
                } catch (KeyNotFoundException)
                {
                    var crop = new Crop(key, 0, 0, location);
                    cropSeasons[Game1.currentSeason][key] = crop.IsInSeason(location);
                    if (cropSeasons[Game1.currentSeason][key]) {
                        return foundItem;
                    }
                }
            }

            return null;
        }

        private bool IsTrellisCrop(Item item, GameLocation location) {
            Crop crop = new Crop(item.ItemId, 0, 0, location);
            return crop.raisedSeeds.Value;
        }

        //Verify if the item is a crop seed
        private bool IsCrop(Item item, GameLocation location) {
            var objCrop = new StardewValley.Object(item.ItemId, 1);
            return objCrop.Category == -74 && item.ItemId != "770" && item.ItemId != "MixedFlowerSeeds" && !Tree.GetWildTreeSeedLookup().Keys.Contains(item.ItemId) && !Game1.fruitTreeData.Keys.Contains(item.ItemId);
        }

        public List<string> RequiredItems() {
            return new List<string> { ItemCategory.ToString()  };
        }

        private bool Plant(GameLocation location, Vector2 pos, string index) {
            Crop crop = new Crop(index, (int)pos.X, (int)pos.Y, location);

            if (!location.IsGreenhouse && !crop.IsInSeason(location) && BetterJunimos.Config.JunimoImprovements.AvoidPlantingOutOfSeason) {
                Monitor.Log($"Crop {crop} ({index}) cannot be planted in {Game1.currentSeason}", LogLevel.Warn);
                return false;
            }

            if (location.terrainFeatures[pos] is not HoeDirt hd) return true;
            hd.crop = crop;
            applySpeedIncreases(hd);
            ApplyPaddy(hd, location);
   
            if (Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, location)) {
                if (crop.raisedSeeds.Value) location.playSound("stoneStep");
                location.playSound("dirtyHit");
            }

            ++Game1.stats.SeedsSown;
            return true;
        }

        private void ApplyPaddy(HoeDirt hd, GameLocation location) {
            hd.nearWaterForPaddy.Value = -1;
            if (!hd.hasPaddyCrop()) return;
            if (!hd.paddyWaterCheck()) return;
            hd.state.Value = 1;
            hd.updateNeighbors();
        }

        private void applySpeedIncreases(HoeDirt hd)
        {
            var who = Game1.player;

            if (hd.crop == null)
                return;
            
            var paddyWaterCheck = hd.paddyWaterCheck();
            var fertilizer = hd.fertilizer.Value is SpeedGro or DeluxeSpeedGro or HyperSpeedGro;
            var agriculturalist = who.professions.Contains(5);
            
            if (! (fertilizer || agriculturalist || paddyWaterCheck)) return;
            
            hd.crop.ResetPhaseDays();
            var num1 = 0;
            for (var index = 0; index < hd.crop.phaseDays.Count - 1; ++index)
                num1 += hd.crop.phaseDays[index];
            var num2 = 0.0f;
            switch (hd.fertilizer.Value)
            {
                case "465":
                    num2 += 0.1f;
                    break;
                case "466":
                    num2 += 0.25f;
                    break;
                case "918":
                    num2 += 0.33f;
                    break;
            }
            if (paddyWaterCheck)
                num2 += 0.25f;
            if (who.professions.Contains(5))
                num2 += 0.1f;
            var num3 = (int) Math.Ceiling((double) num1 * num2);
            for (var index1 = 0; num3 > 0 && index1 < 3; ++index1)
            {
                for (var index2 = 0; index2 < hd.crop.phaseDays.Count; ++index2)
                {
                    if ((index2 > 0 || hd.crop.phaseDays[index2] > 1) && hd.crop.phaseDays[index2] != 99999)
                    {
                        hd.crop.phaseDays[index2]--;
                        --num3;
                    }
                    if (num3 <= 0)
                        break;
                }
            }
        }
    }
}