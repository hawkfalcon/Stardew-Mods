using System;
using System.Linq;
using System.Collections.Generic;
using BetterJunimos.Utils;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace BetterJunimos.Abilities {
    public class FertilizeAbility : IJunimoAbility {
        private const int ItemCategory = SObject.fertilizerCategory;

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
            var chest = Util.GetHutFromId(guid).output.Value;
            var foundItem = chest.items.FirstOrDefault(item => item is {Category: ItemCategory});
            if (foundItem == null) return false;

            Fertilize(farm, pos, foundItem.ParentSheetIndex);
            Util.RemoveItemFromChest(chest, foundItem);
            return true;
        }

        public List<int> RequiredItems() {
            return new() { ItemCategory };
        }

        private static void Fertilize(Farm farm, Vector2 pos, int index) {
            if (farm.terrainFeatures[pos] is not HoeDirt hd) return;
            hd.fertilizer.Value = index;
            CheckSpeedGro(hd, hd.crop);
            if (Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, farm)) {
                farm.playSound("dirtyHit");
            }
        }

        // taken from SDV planting code [applySpeedIncreases()], updated for 1.5
        private static void CheckSpeedGro(HoeDirt hd, Crop crop) {
            var fertilizer = hd.fertilizer.Value;
            var who = Game1.player;

            if (crop == null) {
                return;
            }
            if (!(fertilizer is 465 or 466 or 918 || who.professions.Contains(5))) {
                return;
            }
            crop.ResetPhaseDays();
            var totalDaysOfCropGrowth = 0;
            for (var j = 0; j < crop.phaseDays.Count - 1; j++) {
                totalDaysOfCropGrowth += crop.phaseDays[j];
            }
            var speedIncrease = 0f;
            switch (fertilizer)
            {
                case 465:
                    speedIncrease += 0.1f;
                    break;
                case 466:
                    speedIncrease += 0.25f;
                    break;
                case 918:
                    speedIncrease += 0.33f;
                    break;
            }
            if (who.professions.Contains(5)) {
                speedIncrease += 0.1f;
            }
            var daysToRemove = (int)Math.Ceiling(totalDaysOfCropGrowth * speedIncrease);
            var tries = 0;
            while (daysToRemove > 0 && tries < 3) {
                for (var i = 0; i < crop.phaseDays.Count; i++) {
                    if ((i > 0 || crop.phaseDays[i] > 1) && crop.phaseDays[i] != 99999) {
                        crop.phaseDays[i]--;
                        daysToRemove--;
                    }
                    if (daysToRemove <= 0) {
                        break;
                    }
                }
                tries++;
            }
        }
    }
}