using System;
using System.Linq;
using System.Collections.Generic;
using BetterJunimos.Utils;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace BetterJunimos.Abilities {
    public class FertilizeAbility : IJunimoAbility {
        int ItemCategory = SObject.fertilizerCategory;

        public string AbilityName() {
            return "Fertilize";
        }

        public bool IsActionAvailable(Farm farm, Vector2 pos, Guid guid) {
            if (!farm.terrainFeatures.ContainsKey(pos)) return false;
            if (farm.terrainFeatures[pos] is not HoeDirt hd) return false;
            if (hd.fertilizer.Value > 0) return false;
            if (hd.crop is null) return true;

            // now we allow fertilizing just-planted crops
            if (hd.crop.currentPhase.Value > 1) return false;
            return true;
        }

        public bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            Chest chest = Util.GetHutFromId(guid).output.Value;
            Item foundItem = chest.items.FirstOrDefault(item => item != null && item.Category == ItemCategory);
            if (foundItem == null) return false;

            Fertilize(farm, pos, foundItem.ParentSheetIndex);
            Util.RemoveItemFromChest(chest, foundItem);
            return true;
        }

        public List<int> RequiredItems() {
            return new List<int> { ItemCategory };
        }

        private void Fertilize(Farm farm, Vector2 pos, int index) {
            if (farm.terrainFeatures[pos] is HoeDirt hd) {
                hd.fertilizer.Value = index;
                if (Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, farm)) {
                    farm.playSound("dirtyHit");
                }
            }
        }
    }
}