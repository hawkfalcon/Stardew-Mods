using System;
using BetterJunimos.Utils;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley.Buildings;

namespace BetterJunimos.Abilities {
    public class HarvestCropsAbility : IJunimoAbility {
        private List<int> giantCrops = new List<int> {190, 254, 276};
        private readonly IMonitor Monitor;

        internal HarvestCropsAbility(IMonitor Monitor) {
            this.Monitor = Monitor;
        }
        
        public string AbilityName() {
            return "HarvestCrops";
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos, Guid guid) {
            if (farm.terrainFeatures.ContainsKey(pos) && farm.terrainFeatures[pos] is HoeDirt hd) {
                if (hd.crop is null) return false;
                if (!hd.readyForHarvest()) return false;
                
                // var item = new StardewValley.Object(pos, hd.crop.indexOfHarvest.Value, 0);
                // if (item.ParentSheetIndex == 190) {
                //     Monitor.Log($"    Crop at [{pos.X} {pos.Y}] is 190 {item.displayName}; AHG: {BetterJunimos.Config.JunimoImprovements.AvoidHarvestingGiants}", LogLevel.Warn);
                // }
                
                if (ShouldAvoidHarvesting(pos, hd))
                {
                    // Monitor.Log($"    Avoiding harvest of crop {hd.crop} at [{pos.X} {pos.Y}]", LogLevel.Warn);
                    return false;
                }

                return true;
            }

            return false;
        }

        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            // Don't do anything, as the base junimo handles this already (see PatchHarvestAttemptToCustom)
            return true;
        }

        public List<int> RequiredItems() {
            return new List<int>();
        }

        private bool ShouldAvoidHarvesting(Vector2 pos, HoeDirt hd) {
            return Util.Config.JunimoImprovements.AvoidHarvestingFlowers && new StardewValley.Object(pos, hd.crop.indexOfHarvest.Value, 0).Category == StardewValley.Object.flowersCategory;
        }
    }
}