using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewModdingAPI;
using SObject = StardewValley.Object;

namespace BetterJunimosForestry.Abilities {
    public class HarvestFruitTreesAbility : BetterJunimos.Abilities.IJunimoAbility {
        private readonly IMonitor Monitor;

        internal HarvestFruitTreesAbility(IMonitor Monitor) {
            this.Monitor = Monitor;
        }

        public string AbilityName() {
            return "HarvestFruitTrees";
        }

        private bool IsHarvestableFruitTree(TerrainFeature tf) {
            return tf is FruitTree tree && tree.fruitsOnTree.Value > 0;
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos) {
            Vector2 up = new Vector2(pos.X, pos.Y + 1);
            Vector2 right = new Vector2(pos.X + 1, pos.Y);
            Vector2 down = new Vector2(pos.X, pos.Y - 1);
            Vector2 left = new Vector2(pos.X - 1, pos.Y);

            Vector2[] positions = { up, right, down, left };
            foreach (Vector2 nextPos in positions) {
                if (farm.terrainFeatures.ContainsKey(nextPos) && IsHarvestableFruitTree(farm.terrainFeatures[nextPos])) {
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
                if (farm.terrainFeatures.ContainsKey(nextPos) && IsHarvestableFruitTree(farm.terrainFeatures[nextPos])) {
                    FruitTree tree = farm.terrainFeatures[nextPos] as FruitTree;

                    junimo.faceDirection(direction);

                    return HarvestFromTree(farm, pos, junimo, chest, tree);
                }
                direction++;
            }

            return false;
        }

        /// <summary>Harvest fruit from a FruitTree and update the tree accordingly.</summary>
        internal static SObject GetFruitFromTree(FruitTree tree) {
            if (tree.fruitsOnTree.Value == 0)
                return null;

            int quality = 0;
            if (tree.daysUntilMature.Value <= -112)
                quality = 1;
            if (tree.daysUntilMature.Value <= -224)
                quality = 2;
            if (tree.daysUntilMature.Value <= -336)
                quality = 4;
            if (tree.struckByLightningCountdown.Value > 0)
                quality = 0;

            tree.fruitsOnTree.Value --;

            SObject result = new SObject(Vector2.Zero, tree.struckByLightningCountdown.Value > 0 ? 382 : tree.indexOfFruit.Value, 1) { Quality = quality };
            return result;
        }

        protected bool HarvestFromTree(Farm farm, Vector2 pos, JunimoHarvester junimo, Chest chest, FruitTree tree) {
            //shake the tree without it releasing any fruit
            int fruitsOnTree = tree.fruitsOnTree.Value;
            tree.fruitsOnTree.Value = 0;
            tree.performUseAction(pos, junimo.currentLocation);
            tree.fruitsOnTree.Value = fruitsOnTree;
            SObject result = GetFruitFromTree(tree);
            if (result != null) {
                junimo.tryToAddItemToHut(result);
                return true;
            }
            return false;
        }

        public List<int> RequiredItems() {
            return new List<int>();
        }
    }
}