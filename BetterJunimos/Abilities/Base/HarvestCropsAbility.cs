using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using BetterJunimos.Utils;
using StardewModdingAPI;

namespace BetterJunimos.Abilities {
    public class HarvestCropsAbility : IJunimoAbility {
        // Pumpkin, Cauliflower, Melon, Powdermelon
        private List<int> giantCrops = new() { 190, 254, 276, 889 };

        internal HarvestCropsAbility() { }

        public string AbilityName() {
            return "HarvestCrops";
        }

        public bool IsActionAvailable(GameLocation location, Vector2 pos, Guid guid) {
            if (!location.terrainFeatures.ContainsKey(pos) || location.terrainFeatures[pos] is not HoeDirt hd) return false;
            if (hd.crop is null) return false;
            if (!hd.readyForHarvest()) return false;
            return !ShouldAvoidHarvesting(pos, hd);
        }

        public bool PerformAction(GameLocation location, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            // calculate the experience from this harvest
            if (BetterJunimos.Config.JunimoPayment.GiveExperience) {
                try {
                    if (location.terrainFeatures.ContainsKey(pos) && location.terrainFeatures[pos] is HoeDirt { crop: { } } hd) {
                        Game1.player.gainExperience(0, Util.ExperienceForCrop(hd.crop));
                    }
                } catch (Exception e) {
                    // modded skill frameworks can throw when given experience; don't
                    // let that break the harvest
                    BetterJunimos.SMonitor.Log($"Could not grant farming experience: {e.Message}", LogLevel.Trace);
                }
            }

            // Don't do anything, as the base junimo handles this already (see PatchTryToHarvestHere)
            return true;
        }

        public List<string> RequiredItems() {
            return new();
        }

        private bool ShouldAvoidHarvesting(Vector2 pos, HoeDirt hd) {
            var item = new StardewValley.Object(hd.crop.indexOfHarvest.Value, 1);

            // TODO: check properly if the crop will die tomorrow instead of special-casing 
            if (item.ParentSheetIndex == 421) {
                // if it's the last day of Fall, harvest sunflowers
                if (Game1.IsFall && Game1.dayOfMonth >= 28 && BetterJunimos.Config.JunimoImprovements.HarvestEverythingOn28th) return false;
            } else {
                // if it's the last day of the month, harvest whatever it is
                if (Game1.dayOfMonth >= 28 && BetterJunimos.Config.JunimoImprovements.HarvestEverythingOn28th) return false;
            }

            if (BetterJunimos.Config.JunimoImprovements.AvoidHarvestingGiants && giantCrops.Contains(item.ParentSheetIndex) && CouldStillFormGiantCrop(pos, hd)) {
                // only leave the crop when it could actually become a giant crop;
                // a lone melon/pumpkin/cauliflower must be harvested like vanilla
                return true;
            }

            if (BetterJunimos.Config.JunimoImprovements.AvoidHarvestingFlowers && item.Category == StardewValley.Object.flowersCategory) {
                return true;
            }

            return false;
        }

        /// <summary>Whether this mature crop is part of a 3x3 group of the same crop,
        /// so it could still turn into a giant crop if left unharvested.</summary>
        private static bool CouldStillFormGiantCrop(Vector2 pos, HoeDirt hd) {
            var cropId = hd.crop?.indexOfHarvest.Value;
            if (cropId is null) return false;
            var location = hd.Location;
            if (location is null) return false;

            // this tile could be any part of a 3x3 giant crop, so check every
            // 3x3 window that contains it
            for (var ox = -2; ox <= 0; ox++) {
                for (var oy = -2; oy <= 0; oy++) {
                    var allSameCrop = true;
                    for (var dx = 0; dx < 3 && allSameCrop; dx++) {
                        for (var dy = 0; dy < 3 && allSameCrop; dy++) {
                            var tile = new Vector2(pos.X + ox + dx, pos.Y + oy + dy);
                            if (!location.terrainFeatures.TryGetValue(tile, out var tf) || tf is not HoeDirt dirt ||
                                dirt.crop is null || dirt.crop.indexOfHarvest.Value != cropId || !dirt.readyForHarvest()) {
                                allSameCrop = false;
                            }
                        }
                    }

                    if (allSameCrop) return true;
                }
            }

            return false;
        }
    }
}