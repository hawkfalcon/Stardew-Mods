using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.Tools;
using StardewValley.TerrainFeatures;
using StardewModdingAPI;
using SObject = StardewValley.Object;

// bits of this are from Tractor Mod; https://github.com/Pathoschild/StardewMods/blob/68628a40f992288278b724984c0ade200e6e4296/TractorMod/Framework/BaseAttachment.cs#L132

namespace BetterJunimosForestry.Abilities {
    public class HoeAroundTreesAbility : BetterJunimos.Abilities.IJunimoAbility {

        private readonly IMonitor Monitor;
        private Hoe FakeHoe = new Hoe();

        internal HoeAroundTreesAbility(IMonitor Monitor) {
            this.Monitor = Monitor;
            FakeHoe.IsEfficient = true;
        }

        public string AbilityName() {
            return "HoeAroundTrees";
        }

        private bool IsMatureFruitTree(TerrainFeature tf) {
            return tf is FruitTree tree && tree.growthStage.Value >= 4;
        }

        private bool IsFruitTreeSapling(TerrainFeature tf) {
            return tf is FruitTree tree && tree.growthStage.Value < 4;
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos) {
            Vector2 up = new Vector2(pos.X, pos.Y + 1);
            Vector2 right = new Vector2(pos.X + 1, pos.Y);
            Vector2 down = new Vector2(pos.X, pos.Y - 1);
            Vector2 left = new Vector2(pos.X - 1, pos.Y);

            Vector2[] positions = { up, right, down, left };
            foreach (Vector2 nextPos in positions) {
                if (IsFruitTreeEdge(farm, nextPos)) return true;
            }
            return false;
        }

        private bool IsFruitTreeEdge(Farm farm, Vector2 pos) {
            // is this tile plain dirt?
            if (farm.objects.ContainsKey(pos)) return false;
            if (farm.terrainFeatures.ContainsKey(pos)) return false;

            // is it next to a mature fruit tree?
            // is it not next to any immature fruit trees?

            bool next_to_tree = false;
            for (int x = -1; x < 2; x++) {
                for (int y = -1; y < 2; y++) {
                    Vector2 v = new Vector2(pos.X + x, pos.Y + y);
                    if (farm.terrainFeatures.ContainsKey(v) && IsFruitTreeSapling(farm.terrainFeatures[v])) {
                        //Monitor.Log($"    sapling at [{v.X} {v.Y}]", LogLevel.Debug);
                        return false;
                    }
                    else if (farm.terrainFeatures.ContainsKey(v) && IsMatureFruitTree(farm.terrainFeatures[v])) next_to_tree = true;
                }
            }
            //Monitor.Log($"IsFruitTreeEdge [{pos.X} {pos.Y}]: {next_to_tree}", LogLevel.Debug);

            return next_to_tree;
        }

        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Chest chest) {
            Vector2 up = new Vector2(pos.X, pos.Y + 1);
            Vector2 right = new Vector2(pos.X + 1, pos.Y);
            Vector2 down = new Vector2(pos.X, pos.Y - 1);
            Vector2 left = new Vector2(pos.X - 1, pos.Y);

            int direction = 0;
            Vector2[] positions = { up, right, down, left };
            foreach (Vector2 nextPos in positions) {
                if (IsFruitTreeEdge(farm, nextPos)) {
                    junimo.faceDirection(direction);
                    UseToolOnTile(FakeHoe, nextPos, Game1.player, Game1.currentLocation);
                    Monitor.Log($"    PerformAction hoed the crap out of [{nextPos.X} {nextPos.Y}]", LogLevel.Debug);
                    return true;
                }
                direction++;
            }

            return false;
        }

        protected bool UseToolOnTile(Tool tool, Vector2 tile, Farmer player, GameLocation location) {
            // use tool on center of tile
            player.lastClick = this.GetToolPixelPosition(tile);
            tool.DoFunction(location, (int)player.lastClick.X, (int)player.lastClick.Y, 0, player);
            return true;
        }

        protected Vector2 GetToolPixelPosition(Vector2 tile) {
            return (tile * Game1.tileSize) + new Vector2(Game1.tileSize / 2f);
        }

        public List<int> RequiredItems() {
            return new List<int>();
        }
    }
}