﻿using System;
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

        private const int SunflowerSeeds = 431;
        
        static Dictionary<int, int> WildTreeSeeds = new() {{292, 8}, {309, 1}, {310, 2}, {311, 3}, {891, 7}};
        static Dictionary<string, Dictionary<int, bool>> cropSeasons = new();
        
        internal PlantCropsAbility(IMonitor Monitor) {
            this.Monitor = Monitor;
            var seasons = new List<string>{"spring", "summer", "fall", "winter"};
            foreach (string season in seasons) {
                cropSeasons[season] = new Dictionary<int, bool>();
            }
        }

        public string AbilityName() {
            return "PlantCrops";
        }

        public bool IsActionAvailable(GameLocation location, Vector2 pos, Guid guid) {
            var plantable = location.terrainFeatures.ContainsKey(pos) 
                            && location.terrainFeatures[pos] is HoeDirt {crop: null} 
                            && !location.objects.ContainsKey(pos);

            if (location.IsGreenhouse) {
                // BetterJunimos.SMonitor.Log($"{AbilityName()} looking in greenhouse at [{pos.X} {pos.Y}]: {plantable}", LogLevel.Debug);
            }

            if (!plantable) return false;
            
            // todo: this section is potentially slow and might be refined
            JunimoHut hut = Util.GetHutFromId(guid);
            Chest chest = hut.output.Value;
            string cropType = BetterJunimos.CropMaps.GetCropForPos(hut, pos);
            Item foundItem = PlantableSeed(location, chest, cropType);

            return foundItem is not null;

        }
        
        public bool PerformAction(GameLocation location, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            JunimoHut hut = Util.GetHutFromId(guid);
            Chest chest = hut.output.Value;
            string cropType = BetterJunimos.CropMaps.GetCropForPos(hut, pos);
            Item foundItem = PlantableSeed(location, chest, cropType);
            if (foundItem is null) {
                Monitor.Log($"No seed to plant at {location.Name} [{pos.X} {pos.Y}]", LogLevel.Warn);
                return false;
            }

            // BetterJunimos.SMonitor.Log(
            //     $"PerformAction planting {foundItem.Name} in {location.Name} at at [{pos.X} {pos.Y}]", LogLevel.Debug);
            if (Plant(location, pos, foundItem.ParentSheetIndex)) {
                Util.RemoveItemFromChest(chest, foundItem);
                // BetterJunimos.SMonitor.Log(
                    // $"PerformAction planted {foundItem.Name} in {location.Name} at at [{pos.X} {pos.Y}]", LogLevel.Debug);

                return true;
            }
            BetterJunimos.SMonitor.Log(
                $"PerformAction did not plant", LogLevel.Warn);

            return false;
        }

        /// <summary>Get an item from the chest that is a crop seed, plantable in this season</summary>
        private Item PlantableSeed(GameLocation location, Chest chest, string cropType=null) {
            var foundItems = chest.items.ToList().FindAll(item =>
                item != null 
                && item.Category == ItemCategory
                && !IsTreeSeed(item)
                && !(BetterJunimos.Config.JunimoImprovements.AvoidPlantingCoffee && item.ParentSheetIndex == Util.CoffeeId)
            );
            
            switch (cropType)
            {
                case CropTypes.Trellis:
                    foundItems = foundItems.FindAll(item => IsTrellisCrop(item));
                    break;
                case CropTypes.Ground:
                    foundItems = foundItems.FindAll(item => !IsTrellisCrop(item));
                    break;
            }
            
            if (foundItems.Count == 0) return null;
            if (location.IsGreenhouse) return foundItems.First();
            
            foreach (var foundItem in foundItems) {
                // TODO: check if item can grow to harvest before end of season
                if (foundItem.ParentSheetIndex == SunflowerSeeds && Game1.IsFall && Game1.dayOfMonth >= 25) {
                    // there is no way that a sunflower planted on Fall 25 will grow to harvest
                    continue;
                }
                
                var key = foundItem.ParentSheetIndex;
                try {
                    if (cropSeasons[Game1.currentSeason][key]) {
                        return foundItem;
                    }
                } catch (KeyNotFoundException)
                {
                    // Monitor.Log($"Cache miss: {key} {Game1.currentSeason}", LogLevel.Debug);
                    var crop = new Crop(foundItem.ParentSheetIndex, 0, 0);
                    cropSeasons[Game1.currentSeason][key] = crop.seasonsToGrowIn.Contains(Game1.currentSeason);
                    if (cropSeasons[Game1.currentSeason][key]) {
                        return foundItem;
                    }
                }
                // return foundItem;
            }

            return null;
        }
        
        // TODO: look this up properly instead of keeping a list of base-game tree seed item IDs
        protected bool IsTreeSeed(Item item) {
            return WildTreeSeeds.ContainsKey(item.ParentSheetIndex);
        }

        private bool IsTrellisCrop(Item item) {
            Crop crop = new Crop(item.ParentSheetIndex, 0, 0);
            return crop.raisedSeeds.Value;
        }

        public List<int> RequiredItems() {
            return new List<int> { ItemCategory };
        }

        private bool Plant(GameLocation location, Vector2 pos, int index) {
            SObject o = new SObject(index, 1);
            Crop crop = new Crop(index, (int)pos.X, (int)pos.Y);

            if (!location.IsGreenhouse && !crop.seasonsToGrowIn.Contains(Game1.currentSeason)) {
                Monitor.Log($"Crop {crop} ({index}) cannot be planted in {Game1.currentSeason}", LogLevel.Warn);
                return false;
            }

            if (location.terrainFeatures[pos] is not HoeDirt hd) return true;
            CheckSpeedGro(hd, crop);
            hd.crop = crop;

            if (Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, location)) {
                if (crop.raisedSeeds.Value) location.playSound("stoneStep");
                location.playSound("dirtyHit");
            }

            ++Game1.stats.SeedsSown;
            return true;
        }

        // taken from SDV planting code [applySpeedIncreases()], updated for 1.5
        private void CheckSpeedGro(HoeDirt hd, Crop crop) {
            int fertilizer = hd.fertilizer.Value;
            Farmer who = Game1.player;

            if (crop == null) {
                return;
            }
            if (!(((int)fertilizer == 465 || (int)fertilizer == 466 || (int)fertilizer == 918 || who.professions.Contains(5)))) {
                return;
            }
            crop.ResetPhaseDays();
            int totalDaysOfCropGrowth = 0;
            for (int j = 0; j < crop.phaseDays.Count - 1; j++) {
                totalDaysOfCropGrowth += crop.phaseDays[j];
            }
            float speedIncrease = 0f;
            if ((int)fertilizer == 465) {
                speedIncrease += 0.1f;
            }
            else if ((int)fertilizer == 466) {
                speedIncrease += 0.25f;
            }
            else if ((int)fertilizer == 918) {
                speedIncrease += 0.33f;
            }
            if (who.professions.Contains(5)) {
                speedIncrease += 0.1f;
            }
            int daysToRemove = (int)Math.Ceiling((float)totalDaysOfCropGrowth * speedIncrease);
            int tries = 0;
            while (daysToRemove > 0 && tries < 3) {
                for (int i = 0; i < crop.phaseDays.Count; i++) {
                    if ((i > 0 || crop.phaseDays[i] > 1) && crop.phaseDays[i] != 99999) {
                        crop.phaseDays[i]--;
                        daysToRemove--;
                    }
                    if (daysToRemove <= 0) {
                        break;
                    }
                }
                tries++;
            }
        }
        
        /* older API compat */
        public bool IsActionAvailable(Farm farm, Vector2 pos, Guid guid) {
            return IsActionAvailable((GameLocation) farm, pos, guid);
        }
        
        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            return PerformAction((GameLocation) farm, pos, junimo, guid);
        }
    }
}