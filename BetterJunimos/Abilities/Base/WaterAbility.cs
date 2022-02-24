using System;
using BetterJunimos.Utils;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using StardewValley.Buildings;

namespace BetterJunimos.Abilities {
    public class WaterAbility : IJunimoAbility {
        public string AbilityName() {
            return "Water";
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos, Guid guid) {
            if (!farm.terrainFeatures.ContainsKey(pos)) return false;
            if (farm.terrainFeatures[pos] is not HoeDirt hd) return false;
            if (hd.state.Value == HoeDirt.watered) return false;
            if (hd.crop == null) return false;
            return true;
        }

        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            if (farm.terrainFeatures.ContainsKey(pos) && farm.terrainFeatures[pos] is HoeDirt hd) {
                hd.state.Value = HoeDirt.watered;

                if (Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, farm)) {
                    Multiplayer multiplayer = Util.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
                    multiplayer.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite(13,
                        new Vector2(pos.X * 64f, pos.Y * 64f), Color.White, 10, Game1.random.NextDouble() < 0.5, 70f, 0, 64,
                        (float)((pos.Y * 64.0 + 32.0) / 10000.0 - 0.00999999977648258), -1, 0));
                }

                return true;
            }
            return false;
        }

        public List<int> RequiredItems() {
            return new List<int>();
        }
    }
}