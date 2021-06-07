using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.Tools;
using StardewModdingAPI;
using SObject = StardewValley.Object;

// bits of this are from Tractor Mod; https://github.com/Pathoschild/StardewMods/blob/68628a40f992288278b724984c0ade200e6e4296/TractorMod/Framework/BaseAttachment.cs#L132

namespace BetterJunimosForestry.Abilities {
    public class HarvestDebrisAbility : BetterJunimos.Abilities.IJunimoAbility {

        private readonly IMonitor Monitor;
        private Pickaxe FakePickaxe = new Pickaxe();
        private Axe FakeAxe = new Axe();
        private MeleeWeapon Scythe = new MeleeWeapon(47);

        internal HarvestDebrisAbility(IMonitor Monitor) {
            this.Monitor = Monitor;
            FakeAxe.IsEfficient = true;
            FakePickaxe.IsEfficient = true;
            Scythe.IsEfficient = true;
        }

        public string AbilityName() {
            return "HarvestDebris";
        }

        private bool IsDebris(SObject so) {
            bool debris = IsTwig(so) || IsWeed(so) || IsStone(so);
            return debris;
        }

        protected bool IsTwig(SObject obj) {
            return obj?.ParentSheetIndex == 294 || obj?.ParentSheetIndex == 295;
        }

        protected bool IsWeed(SObject obj) {
            return !(obj is Chest) && obj?.Name == "Weeds";
        }

        protected bool IsStone(SObject obj) {
            return !(obj is Chest) && obj?.Name == "Stone";
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos) {
            Vector2 up = new Vector2(pos.X, pos.Y + 1);
            Vector2 right = new Vector2(pos.X + 1, pos.Y);
            Vector2 down = new Vector2(pos.X, pos.Y - 1);
            Vector2 left = new Vector2(pos.X - 1, pos.Y);

            Vector2[] positions = { up, right, down, left };
            foreach (Vector2 nextPos in positions) {
                if (farm.objects.ContainsKey(nextPos) && IsDebris(farm.objects[nextPos])) {
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
                if (farm.objects.ContainsKey(nextPos) && IsDebris(farm.objects[nextPos])) {

                    junimo.faceDirection(direction);
                    // SetForageQuality(farm, nextPos);

                    SObject item = farm.objects[nextPos];
                    GameLocation location = Game1.currentLocation;

                    if (IsStone(item)) {
                        UseToolOnTile(FakePickaxe, nextPos, Game1.player, Game1.currentLocation);
                    }

                    if (IsTwig(item)) {
                        UseToolOnTile(FakeAxe, nextPos, Game1.player, Game1.currentLocation);
                    }

                    if (IsWeed(item)) {
                        UseToolOnTile(Scythe, nextPos, Game1.player, location);
                        item.performToolAction(Scythe, Game1.currentLocation);
                        location.removeObject(nextPos, false);
                    }
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