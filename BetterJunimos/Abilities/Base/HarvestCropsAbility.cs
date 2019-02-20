using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace BetterJunimos.Abilities {
    public class HarvestCropsAbility : IJunimoAbility {
        public string AbilityName() {
            return "HarvestCrops";
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos) {
            return farm.terrainFeatures.ContainsKey(pos) && farm.terrainFeatures[pos] is HoeDirt hd && 
                hd.crop != null && hd.readyForHarvest();
        }

        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Chest chest) {
            // Don't do anything, as the base junimo handles this already
            return true;
        }

        public int RequiredItem() {
            return 0;
        }
    }
}
