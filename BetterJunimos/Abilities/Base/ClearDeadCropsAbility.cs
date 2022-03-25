using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;

namespace BetterJunimos.Abilities {
    public class ClearDeadCropsAbility : IJunimoAbility {
        public string AbilityName() {
            return "ClearDeadCrops";
        }

        public bool IsActionAvailable(GameLocation location, Vector2 pos, Guid guid) {
            return location.terrainFeatures.ContainsKey(pos)
                   && location.terrainFeatures[pos] is HoeDirt hd
                   && hd.crop != null
                   && hd.crop.dead.Value;
        }

        public bool PerformAction(GameLocation location, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            if (!location.terrainFeatures.ContainsKey(pos) || location.terrainFeatures[pos] is not HoeDirt hd)
                return false;
            var animate = Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, location);
            hd.destroyCrop(pos, animate, location);
            return true;
        }

        public List<int> RequiredItems() {
            return new List<int>();
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