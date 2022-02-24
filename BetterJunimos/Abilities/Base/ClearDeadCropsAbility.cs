using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace BetterJunimos.Abilities {
    public class ClearDeadCropsAbility : IJunimoAbility {
    public string AbilityName() {
            return "ClearDeadCrops";
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos, Guid guid) {
            return farm.terrainFeatures.ContainsKey(pos) 
                   && farm.terrainFeatures[pos] is HoeDirt hd 
                   && hd.crop != null 
                   && hd.crop.dead.Value;
        }

        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            if (farm.terrainFeatures.ContainsKey(pos) && farm.terrainFeatures[pos] is HoeDirt hd) {
                bool animate = Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, farm);
                hd.destroyCrop(pos, animate, farm);
                return true;
            }
            return false;
        }

        public List<int> RequiredItems() {
            return new List<int>();
        }
    }
}