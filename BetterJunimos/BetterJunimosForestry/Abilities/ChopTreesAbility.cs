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
    public class ChopTreesAbility : BetterJunimos.Abilities.IJunimoAbility {

        private readonly IMonitor Monitor;
        private Axe FakeAxe = new Axe();

        internal ChopTreesAbility(IMonitor Monitor) {
            this.Monitor = Monitor;
            FakeAxe.UpgradeLevel = 1;
            FakeAxe.IsEfficient = true;
        }

        public string AbilityName() {
            return "ChopTrees";
        }

        protected bool IsHarvestableTree(TerrainFeature t) {
            if (t is Tree && (t as Tree).growthStage.Value >= 5 && !(t as Tree).tapped.Value) return true;
            return false;
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos) {
            Vector2 up = new Vector2(pos.X, pos.Y + 1);
            Vector2 right = new Vector2(pos.X + 1, pos.Y);
            Vector2 down = new Vector2(pos.X, pos.Y - 1);
            Vector2 left = new Vector2(pos.X - 1, pos.Y);

            Vector2[] positions = { up, right, down, left };
            foreach (Vector2 nextPos in positions) {
                if (farm.terrainFeatures.ContainsKey(nextPos) && IsHarvestableTree(farm.terrainFeatures[nextPos])) {
                    // Monitor.Log($"Pos {nextPos} contains tree", LogLevel.Debug);
                    return true;
                }
            }
            return false;
        }

        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Chest chest) {
            Vector2 up = new Vector2(pos.X, pos.Y + 1);
            Vector2 right = new Vector2(pos.X + 1, pos.Y);
            Vector2 down = new Vector2(pos.X, pos.Y - 1);
            Vector2 left = new Vector2(pos.X - 1, pos.Y);

            int direction = 0;
            Vector2[] positions = { up, right, down, left };
            foreach (Vector2 nextPos in positions) {
                if (farm.terrainFeatures.ContainsKey(nextPos) && IsHarvestableTree(farm.terrainFeatures[nextPos])) {
                    junimo.faceDirection(direction);

                    //farm.terrainFeatures[nextPos].performUseAction(pos, junimo.currentLocation);
                    UseToolOnTile(FakeAxe, nextPos, Game1.player, Game1.currentLocation);

                    return true;
                }
                direction++;
            }
            return false;
        }

        protected bool UseToolOnTile(Tool tool, Vector2 tile, Farmer player, GameLocation location) {
            // use tool on center of tile
            // player.lastClick = this.GetToolPixelPosition(tile);
            //             tool.DoFunction(location, (int)player.lastClick.X, (int)player.lastClick.Y, 0, player);

            Vector2 lc = GetToolPixelPosition(tile);
            tool.DoFunction(location, (int)lc.X, (int)lc.Y, 0, player);
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