using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace BetterJunimosForestry {
    public class Util {
        internal const int DefaultRadius = 8;
        internal const int UnpaidRadius = 3;
        public const int CoffeeId = 433;

        public const int GemCategory = -2;
        public const int MineralCategory = -12;

        public const int ForageCategory = -81;
        public const int FlowerCategory = -80;
        public const int FruitCategory = -79;
        public const int WineCategory = -26; 

        public static int MaxRadius;

        internal static ModConfig Config;
        
        public static bool BuildingOnTile(Vector2 pos) {
            foreach (Building b in Game1.getFarm().buildings) {
                if (b.occupiesTile(pos)) {
                    return true;
                }
            }
            return false;
        }

        public static Guid GetHutIdFromHut(JunimoHut hut) {
            return Game1.getFarm().buildings.GuidOf(hut);
        }

        public static JunimoHut GetHutFromId(Guid id) {
            return Game1.getFarm().buildings[id] as JunimoHut;
        }

        public static void AddItemToChest(Farm farm, Chest chest, SObject item) {
            Item obj = chest.addItem(item);
            if (obj == null) return;
            Vector2 pos = chest.TileLocation;
            for (int index = 0; index < obj.Stack; ++index)
                Game1.createObjectDebris(item.ParentSheetIndex, (int)pos.X + 1, (int)pos.Y + 1, -1, item.Quality, 1f, farm);
        }

        public static void RemoveItemFromChest(Chest chest, Item item) {
            if (Config.FunChanges.InfiniteJunimoInventory) { return; }
            item.Stack--;
            if (item.Stack == 0) {
                chest.items.Remove(item);
            }
        }
    }
}
