using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.Tools;
using System.Collections.Generic;
using StardewModdingAPI;
using SObject = StardewValley.Object;

// bits of this are from Tractor Mod; https://github.com/Pathoschild/StardewMods/blob/68628a40f992288278b724984c0ade200e6e4296/TractorMod/Framework/BaseAttachment.cs#L132

namespace BetterJunimosForestry.Abilities {
    public class CollectDroppedObjectsAbility : BetterJunimos.Abilities.IJunimoAbility {

        private readonly IMonitor Monitor;
        private readonly Axe FakeAxe = new Axe();

        internal CollectDroppedObjectsAbility(IMonitor Monitor) {
            this.Monitor = Monitor;
            Monitor.Log($"CollectDroppedObjectsAbility ready", LogLevel.Debug);
        }

        public string AbilityName() {
            return "CollectDroppedObjects";
        }

        protected bool IsDebrisAtTile(Vector2 tile) {
            return DebrisIndexAtTile(tile) > 0;
        }

        protected int DebrisIndexAtTile(Vector2 tile) { 
            // Monitor.Log($"IsDebrisAtTile {tile}", LogLevel.Debug);
            if (Game1.currentLocation.debris is null) {
                Monitor.Log($"    Game1.currentLocation.debris is null", LogLevel.Warn);
                return -1;
            }
            foreach (Debris d in Game1.currentLocation.debris) {
                foreach (Chunk c in d.Chunks) {
                    int dx = (int)(c.position.X + Game1.tileSize / 2f) / Game1.tileSize;
                    int dy = (int)(c.position.Y + Game1.tileSize / 2f) / Game1.tileSize;
                    if (dx == tile.X && dy == tile.Y) {
                        // Monitor.Log($"        Debris chunks: {d.Chunks.Count} type: {d.debrisType} at {dx},{dy}", LogLevel.Debug);
                        if (d.item is not null) {
                            // Monitor.Log($"            {d.item.Name} [{d.item.ParentSheetIndex}]", LogLevel.Debug);
                            return d.item.ParentSheetIndex;
                        }
                        else {
                            // Monitor.Log($"            non-item debris [{c.debrisType}]", LogLevel.Debug);
                            return c.debrisType;
                        }
                    }
                }
            }
            return 0;
        }

        protected bool MoveDebrisFromTileToChest(Vector2 tile, Farm farm, Chest chest) {
            // Monitor.Log($"MoveDebrisFromTileToChest {tile}", LogLevel.Debug);
            if (Game1.currentLocation.debris is null) return false;
            List<Debris> to_remove = new List<Debris>();

            foreach (Debris d in Game1.currentLocation.debris) {
                foreach (Chunk c in d.Chunks) {
                    int dx = (int)(c.position.X + Game1.tileSize / 2f) / Game1.tileSize;
                    int dy = (int)(c.position.Y + Game1.tileSize / 2f) / Game1.tileSize;
                    if (dx == tile.X && dy == tile.Y) {
                        // Monitor.Log($"        Adding debris chunks to removal list: {d.Chunks.Count} at {dx},{dy}", LogLevel.Debug);
                        to_remove.Add(d);
                        break;
                    }
                }
            }

            foreach (Debris d in to_remove) {
                // Monitor.Log($"        removing Debris // chunks: {d.Chunks.Count} type: {d.debrisType}", LogLevel.Debug);
                MoveDebrisToChest(d, farm, chest);
                Game1.currentLocation.debris.Remove(d);
            }
            return (to_remove.Count > 0);
        }

        protected void MoveDebrisToChest(Debris d, Farm farm, Chest chest) {
            foreach (Chunk c in d.Chunks) {
                if (d.item is not null) {
                    SObject item = new SObject(d.item.ParentSheetIndex, 1);
                    Util.AddItemToChest(farm, chest, item);
                    //Monitor.Log($"            MoveDebrisToChest {d.item.Name} [{d.item.ParentSheetIndex}]", LogLevel.Debug);
                } else {
                    SObject item = new SObject(c.debrisType, 1);
                    if (item.Name != "Error Item") {
                        Util.AddItemToChest(farm, chest, item);
                        //Monitor.Log($"            MoveDebrisToChest {item.Name} [{c.debrisType}]", LogLevel.Debug);
                    }
                }
            }
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos) {
            // Monitor.Log($"CollectDroppedObjectsAbility IsActionAvailable {pos}", LogLevel.Debug);

            Vector2 up = new Vector2(pos.X, pos.Y + 1);
            Vector2 right = new Vector2(pos.X + 1, pos.Y);
            Vector2 down = new Vector2(pos.X, pos.Y - 1);
            Vector2 left = new Vector2(pos.X - 1, pos.Y);

            Vector2[] positions = { up, right, down, left };
            foreach (Vector2 nextPos in positions) {
                if (IsDebrisAtTile(nextPos)) {
                    // Monitor.Log($"Pos {nextPos} contains debris", LogLevel.Debug);
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
            int index = -1;
            Vector2[] positions = { up, right, down, left };
            foreach (Vector2 nextPos in positions) {
                index = DebrisIndexAtTile(nextPos);
                if (index > 0) {
                    junimo.faceDirection(direction);
                    Monitor.Log($"Want to grab object {nextPos}");
                    return MoveDebrisFromTileToChest(nextPos, farm, chest);
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