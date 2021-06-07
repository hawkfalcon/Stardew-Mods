using System;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewModdingAPI;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace BetterJunimosForestry.Abilities {
    public class PlantFruitTreesAbility : BetterJunimos.Abilities.IJunimoAbility {
        static Dictionary<int, string> FruitTreeSeeds = new Dictionary<int, string>
         {
            {69, "Banana"},
            {835, "Mango"},
            {628, ""},
            {629, ""},
            {630, ""},
            {631, ""},
            {632, ""},
            {633, ""}
         };
        private string Pattern = "tight";

        private readonly IMonitor Monitor;

        internal PlantFruitTreesAbility(IMonitor Monitor) {
            this.Monitor = Monitor;
        }

        public string AbilityName() {
            return "PlantFruitTrees";
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos) {
            Vector2 up = new Vector2(pos.X, pos.Y + 1);
            Vector2 right = new Vector2(pos.X + 1, pos.Y);
            Vector2 down = new Vector2(pos.X, pos.Y - 1);
            Vector2 left = new Vector2(pos.X - 1, pos.Y);

            Vector2[] positions = { up, right, down, left };
            foreach (Vector2 nextPos in positions) {
                if (BorderClear(farm, nextPos)) return true;
            }
            return false;
        }

        // is this tile and every tile around it plantable?
        private bool BorderClear(Farm farm, Vector2 pos) {
            // is this tile in the planting pattern?
            if (!IsTileInPattern(pos)) return false;

            if (FruitTree.IsGrowthBlocked(pos, farm)) {
                //Monitor.Log($"BorderClear [{pos.X}, {pos.Y}]: growth blocked", LogLevel.Warn);
                return false;
            }

            if (!Plantable(farm, pos)) {
                //Monitor.Log($"BorderClear [{pos.X}, {pos.Y}]: growth possible but tile not plantable", LogLevel.Warn);
                return false;
            }

            for (int x = -1; x < 2; x++) {
                for (int y = -1; y < 2; y++) {
                    Vector2 v = new Vector2(pos.X + x, pos.Y + y);
                    if (!Plantable(farm, v)) {
                        //Monitor.Log($"BorderClear [{pos.X}, {pos.Y}]: neighbour tile [{v.X}, {v.Y}] not plantable", LogLevel.Warn);
                        return false;
                    }
                }
            }
            return true;
        }

        private bool IsTileInPattern(Vector2 pos) {

            if (Pattern == "rows") {
                return pos.X % 3 == 0 && pos.Y % 3 == 0;
            }

            if (Pattern == "diagonals") {
                if (pos.X % 4 == 2) return pos.Y % 4 == 2;
                if (pos.X % 4 == 0) return pos.Y % 4 == 0;
                return false;
            }

            if (Pattern == "tight") {
                if (pos.Y % 2 == 0) return pos.X % 4 == 0;
                if (pos.Y % 2 == 1) return pos.X % 4 == 2;
                return false;
            }

            throw new ArgumentOutOfRangeException($"Pattern '{Pattern}' not recognized");
        }

        // is this tile plantable?
        private bool Plantable(Farm farm, Vector2 pos) {
            string noSpawn = farm.doesTileHaveProperty((int)pos.X, (int)pos.Y, "NoSpawn", "Back");
            bool cantSpawnHere = noSpawn != null && (noSpawn.Equals("Tree") || noSpawn.Equals("All") || noSpawn.Equals("True"));
            bool isBlocked = farm.objects.ContainsKey(pos) || farm.terrainFeatures.ContainsKey(pos);
            bool building_on_tile = Util.BuildingOnTile(pos);

            //bool avail = farm.doesTileHavePropertyNoNull((int)pos.X, (int)pos.Y, "Type", "Back") == "Dirt";
            //if (growth_blocked) Monitor.Log($"Plantable: growth_blocked tile [{pos.X}, {pos.Y}]", LogLevel.Warn);

            bool plantable = !cantSpawnHere && !isBlocked && !building_on_tile && FruitTreePlantable(farm, pos);

            //Monitor.Log($"Plantable [{pos.X}, {pos.Y}]: {plantable} ({noSpawn} {prop} {!cantSpawnHere} {!isBlocked})", LogLevel.Debug);
            return plantable;
        }

        private bool FruitTreePlantable(Farm farm, Vector2 pos) {
            int x = (int)pos.X;
            int y = (int)pos.Y;
            return (farm is Farm && (farm.doesTileHaveProperty(x, y, "Diggable", "Back") != null || farm.doesTileHavePropertyNoNull(x, y, "Type", "Back").Equals("Grass") || farm.doesTileHavePropertyNoNull(x, y, "Type", "Back").Equals("Dirt")) && !farm.doesTileHavePropertyNoNull(x, y, "NoSpawn", "Back").Equals("Tree")) || (farm.CanPlantTreesHere(628, x, y) && (farm.doesTileHaveProperty(x, y, "Diggable", "Back") != null || farm.doesTileHavePropertyNoNull(x, y, "Type", "Back").Equals("Stone")));
        }



        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Chest chest) {

            Item foundItem;
            foundItem = chest.items.FirstOrDefault(item => item != null && FruitTreeSeeds.Keys.Contains(item.ParentSheetIndex));
            if (foundItem == null) return false;

            Vector2 up = new Vector2(pos.X, pos.Y + 1);
            Vector2 right = new Vector2(pos.X + 1, pos.Y);
            Vector2 down = new Vector2(pos.X, pos.Y - 1);
            Vector2 left = new Vector2(pos.X - 1, pos.Y);

            Vector2[] positions = { up, right, down, left };
            foreach (Vector2 nextPos in positions) {
                if (BorderClear(farm, nextPos)) {
                    bool success = Plant(farm, nextPos, foundItem.ParentSheetIndex);
                    if (success) {
                        //Monitor.Log($"PerformAction planted {foundItem.Name} at {nextPos.X} {nextPos.Y}", LogLevel.Info);
                        Util.RemoveItemFromChest(chest, foundItem);
                        return true;
                    }
                    else {
                        Monitor.Log($"PerformAction could not plant {foundItem.Name} at {nextPos.X} {nextPos.Y}", LogLevel.Warn);
                    }
                }
            }
            return false;
        }

        private bool Plant(Farm farm, Vector2 pos, int index) {
            if (farm.terrainFeatures.Keys.Contains(pos)) {
                Monitor.Log($"Plant: {pos.X} {pos.Y} occupied by {farm.terrainFeatures[pos]}", LogLevel.Error);
                return false;
            }

            FruitTree tree = new FruitTree(index, 0);
            farm.terrainFeatures.Add(pos, tree);

            if (Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, farm)) {
                farm.playSound("stoneStep");
                farm.playSound("dirtyHit");
            }

            return true;
        }


        public List<int> RequiredItems() {
            return FruitTreeSeeds.Keys.ToList<int>();
        }
    }
}