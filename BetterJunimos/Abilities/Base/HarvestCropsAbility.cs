using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;

namespace BetterJunimos.Abilities {
    public class HarvestCropsAbility : IJunimoAbility {
        private List<int> giantCrops = new() {190, 254, 276};

        internal HarvestCropsAbility() {
        }
        
        public string AbilityName() {
            return "HarvestCrops";
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos, Guid guid) {
            if (!farm.terrainFeatures.ContainsKey(pos) || farm.terrainFeatures[pos] is not HoeDirt hd) return false;
            if (hd.crop is null) return false;
            if (!hd.readyForHarvest()) return false;
            return !ShouldAvoidHarvesting(pos, hd);
        }

        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            // Don't do anything, as the base junimo handles this already (see PatchHarvestAttemptToCustom)
            return true;
        }

        public List<int> RequiredItems() {
            return new();
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