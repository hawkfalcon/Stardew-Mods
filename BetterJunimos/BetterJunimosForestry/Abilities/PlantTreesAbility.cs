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
    public class PlantTreesAbility : BetterJunimos.Abilities.IJunimoAbility {
        static Dictionary<int, int> WildTreeSeeds = new Dictionary<int, int>
         {
            {292, 8},
            {309, 1},
            {310, 2},
            {311, 3},
            {891, 7}
         };

        private string Pattern = "tight";
        private readonly IMonitor Monitor;

        internal PlantTreesAbility(IMonitor Monitor) {
            this.Monitor = Monitor;
        }

        public string AbilityName() {
            return "PlantTrees";
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

            //return farm.terrainFeatures.ContainsKey(pos) && farm.terrainFeatures[pos] is HoeDirt hd && hd.crop == null &&
            //    !farm.objects.ContainsKey(pos);
        }

        // is this tile and every tile around it plantable?
        private bool BorderClear(Farm farm, Vector2 pos) {
            // is this tile in the planting pattern?
            if (!IsTileInPattern(pos)) return false;

            if (!Plantable(farm, pos)) return false;

            if (Pattern == "tight" || Pattern == "impassable") return true;

            for (int x = -1; x < 2; x++) {
                for (int y = -1; y < 2; y++) {
                    Vector2 v = new Vector2(pos.X + x, pos.Y + y);
                    if (!Plantable(farm, v)) return false;
                }
            }
            return true;
        }

        private bool IsTileInPattern(Vector2 pos) {
            if (Pattern == "impassable") {
                return true;
            }

            if (Pattern == "tight") {
                return pos.X % 2 == 0;
            }

            if (Pattern == "loose") {
                return pos.X % 2 == 0 && pos.Y % 2 == 0;
            }

            if (Pattern == "fruity-tight") {
                return pos.X % 3 == 0 && pos.Y % 3 == 0;
            }

            if (Pattern == "fruity-loose") {
                if (pos.X % 4 == 2) return pos.Y % 2 == 0;
                if (pos.X % 4 == 0) return pos.Y % 2 == 0;
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

            bool plantable = !cantSpawnHere && !isBlocked && !building_on_tile;

            //Monitor.Log($"Plantable [{pos.X}, {pos.Y}]: {plantable} ({noSpawn} {prop} {!cantSpawnHere} {!isBlocked})", LogLevel.Debug);
            return plantable;
        }



        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Chest chest) {

            Item foundItem;
            foundItem = chest.items.FirstOrDefault(item => item != null && WildTreeSeeds.Keys.Contains(item.ParentSheetIndex));
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
                    } else {
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

            Tree tree = new Tree(WildTreeSeeds[index], 0);
            farm.terrainFeatures.Add(pos, tree);

            if (Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, farm)) {
                farm.playSound("stoneStep");
                farm.playSound("dirtyHit");
            }

            ++Game1.stats.SeedsSown;
            return true;
        }


        public List<int> RequiredItems() {
            return WildTreeSeeds.Keys.ToList<int>();
        }
    }
}