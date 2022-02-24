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
            var item = new StardewValley.Object(pos, hd.crop.indexOfHarvest.Value, 0);

            // TODO: check properly if the crop will die tomorrow instead of special-casing 
            if (item.ParentSheetIndex == 421) {
                // if it's the last day of Fall, harvest sunflowers
                if (Game1.IsFall && Game1.dayOfMonth >= 28) return false;
            }
            else {
                // if it's the last day of the month, harvest whatever it is
                if (Game1.dayOfMonth >= 28) return false;
            }

            if (BetterJunimos.Config.JunimoImprovements.AvoidHarvestingGiants && giantCrops.Contains(item.ParentSheetIndex)) {
                return true;
            }

            if (BetterJunimos.Config.JunimoImprovements.AvoidHarvestingFlowers &&
                item.Category == StardewValley.Object.flowersCategory) {
                return true;
            }

            return false;
        }
    }
}