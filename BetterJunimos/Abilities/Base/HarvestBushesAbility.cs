using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;

namespace BetterJunimos.Abilities {
    public class HarvestBushesAbility : IJunimoAbility {
        public string AbilityName() {
            return "HarvestBushes";
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos, Guid guid) {
            if (farm.terrainFeatures.ContainsKey(pos) && farm.terrainFeatures[pos] is Bush bush) {
                return bush.tileSheetOffset.Value == 1;
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
    }
}