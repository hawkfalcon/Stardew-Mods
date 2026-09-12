using System;
using System.Linq;
using System.Collections.Generic;
using BetterJunimos.Utils;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

// ReSharper disable InconsistentNaming

namespace BetterJunimos.Patches {
    /* areThereMatureCropsWithinRadius **OVERWRITES PREFIX**
     *
     * Search for actionable tiles
     * Completely rewrite original function.
     */
    internal class PatchSearchAroundHut {
        /*
         * The full search (radius grid x every ability x chest contents) is expensive,
         * especially with large radii, and was previously re-run every frame, causing
         * severe lag. Cache the result per hut and re-scan at most once per second.
         * The cache is also invalidated on day start / menu close / building changes
         * (see BetterJunimos, which calls PatchSearchAroundHut.InvalidateCache).
         */
        private const int ScanCooldownTicks = 60;

        // keyed by hut + its tile position, so a moved hut doesn't reuse a stale scan
        private static readonly Dictionary<JunimoHut, (bool foundWork, int radius, int scannedTick, int tileX, int tileY)> _scanCache = new();

        public static bool Prefix(JunimoHut __instance, ref bool __result) {
            if (!Context.IsMainPlayer) return true;
            // Prevent unnecessary searching when unpaid
            if (BetterJunimos.Config.JunimoPayment.WorkForWages && !Util.Payments.WereJunimosPaidToday) {
                __instance.lastKnownCropLocation = Point.Zero;
                return false;
            }

            __result = SearchAroundHut(__instance);
            return false;
        }

        internal static void InvalidateCache() {
            _scanCache.Clear();
        }

        // search for crops + open plantable spots
        private static bool SearchAroundHut(JunimoHut hut) {
            var id = Util.GetHutIdFromHut(hut);
            var radius = Util.CurrentWorkingRadius;
            GameLocation farm = hut.GetParentLocation();

            if (_scanCache.TryGetValue(hut, out var cached) && cached.radius == radius && cached.tileX == hut.tileX.Value &&
                cached.tileY == hut.tileY.Value && Game1.ticks - cached.scannedTick < ScanCooldownTicks) {
                return cached.foundWork;
            }

            // SearchHutGrid manages hut.lastKnownCropLocation and Util.Abilities.lastKnownCropLocations
            var foundWork = SearchHutGrid(hut, radius, farm, id);

            if (BetterJunimos.Config.JunimoImprovements.CanWorkInGreenhouse) {
                var ghb = Util.Greenhouse.GreenhouseBuildingNearHut(id);
                var gh = Game1.getLocationFromName("Greenhouse");
                if (ghb != null) {
                    gh = ghb;
                }

                if (Util.Greenhouse.HutHasGreenhouse(id)) {
                    // SearchGreenhouseGrid manages hut.lastKnownCropLocation (a hack!) and Util.Abilities.lastKnownCropLocations
                    foundWork |= SearchGreenhouseGrid(hut, id, gh);
                }
            }

            _scanCache[hut] = (foundWork, radius, Game1.ticks, hut.tileX.Value, hut.tileY.Value);
            return foundWork;
        }

        /// <summary>
        /// Search the Greenhouse for work to do, and update
        /// hut.lastKnownCropLocation and
        /// Util.Abilities.lastKnownCropLocations
        /// with the location of any work found
        /// </summary>
        /// <param name="hut">JunimoHut to search</param>
        /// <param name="hut_guid">GUID of hut to search</param>
        /// <returns>True if there's any work to do</returns>
        internal static bool SearchGreenhouseGrid(JunimoHut hut, Guid hut_guid, GameLocation gl = null) {
            var gh = Game1.getLocationFromName("Greenhouse");
            if (gl != null) {
                gh = gl;
            }

            for (var x = 0; x < gh.map.Layers[0].LayerWidth; x++) {
                for (var y = 0; y < gh.map.Layers[0].LayerHeight; y++) {
                    var pos = new Vector2(x, y);
                    var ability = Util.Abilities.IdentifyJunimoAbility(gh, pos, hut_guid);
                    if (ability == null) continue;
                    hut.lastKnownCropLocation = new Point(x, y);
                    Util.Abilities.lastKnownCropLocations[(hut, gh)] = new Point(x, y);
                    return true;
                }
            }

            Util.Abilities.lastKnownCropLocations[(hut, gh)] = Point.Zero;
            return false;
        }

        private static bool SearchHutGrid(JunimoHut hut, int radius, GameLocation farm, Guid id) {
            for (var x = hut.tileX.Value + 1 - radius; x < hut.tileX.Value + 2 + radius; ++x) {
                for (var y = hut.tileY.Value + 1 - radius; y < hut.tileY.Value + 2 + radius; ++y) {
                    var pos = new Vector2(x, y);
                    var ability = Util.Abilities.IdentifyJunimoAbility(farm, pos, id);
                    if (ability == null) continue;

                    hut.lastKnownCropLocation = new Point(x, y);
                    Util.Abilities.lastKnownCropLocations[(hut, farm)] = new Point(x, y);
                    return true;
                }
            }

            hut.lastKnownCropLocation = Point.Zero;
            Util.Abilities.lastKnownCropLocations[(hut, farm)] = Point.Zero;
            return false;
        }
    }

    /*
     * Update
     *
     * Drive junimo spawning. JunimoHut.Update only runs while the hut's location is
     * the current location; updateWhenFarmNotCurrentLocation runs for every other
     * location, so exactly one of the two drivers runs for a given hut each frame.
     *
     * Unlike the old timer-based logic, spawning here does not depend on
     * junimoSendOutTimer having been primed by performTenMinuteAction or on any
     * save-load state, which fixes huts that would never send junimos out after
     * loading a save (or until the player used the manual spawn key).
     *
     * The hut count limit is applied strictly and orphaned/despawned junimos are
     * pruned so they can never permanently block a hut from spawning.
     */
    [HarmonyPriority(Priority.Low)]
    internal class ReplaceJunimoHutUpdate {
        public static void Postfix(JunimoHut __instance) {
            JunimoSpawnHelper.TrySpawnJunimo(__instance);
        }
    }

    /*
     * updateWhenFarmNotCurrentLocation
     *
     * See ReplaceJunimoHutUpdate.
     */
    [HarmonyPriority(Priority.Low)]
    internal class ReplaceJunimoHutupdateWhenFarmNotCurrentLocation {
        // Vanilla's spawn driver in this method adds junimos until myJunimos.Count
        // reaches 3, regardless of the configured cap, and only spawns on the frame
        // the timer crosses zero. Keeping the timer high means the vanilla driver
        // never fires; spawning is driven entirely by the postfix, which respects
        // the configured cap. (The vanilla body still runs: base update + chest
        // mutex handling.)
        public static void Prefix(ref int ___junimoSendOutTimer) {
            if (Context.IsMainPlayer && ___junimoSendOutTimer < 1000) {
                ___junimoSendOutTimer = 1000;
            }
        }

        public static void Postfix(JunimoHut __instance) {
            JunimoSpawnHelper.TrySpawnJunimo(__instance);
        }
    }

    internal static class JunimoSpawnHelper {
        // pace spawns like vanilla (at most one junimo per hut per ~second);
        // Game1.ticks is a per-frame counter, so 60 ticks ≈ 1 second at ~60fps
        private const int SpawnCooldownTicks = 60;
        private static readonly Dictionary<JunimoHut, int> _lastSpawnTick = new();

        // forget pacing state (e.g. for huts that were demolished overnight)
        internal static void ResetPacing() {
            _lastSpawnTick.Clear();
        }

        internal static void TrySpawnJunimo(JunimoHut hut) {
            if (!Context.IsMainPlayer) return;
            var maxJunimos = Util.Progression.MaxJunimosUnlocked;
            if (maxJunimos < 0) maxJunimos = 0;

            // prune junimos that can no longer work (orphaned when their hut
            // vanished, or no longer in the world) so a lost junimo can never
            // permanently block this hut, and despawn any excess above the cap
            // (e.g. from a save made while the cap was higher)
            if (hut.myJunimos.Count >= maxJunimos) {
                hut.myJunimos.RemoveAll(junimo => junimo is null || !IsJunimoAliveAndHomed(junimo));
                while (hut.myJunimos.Count > maxJunimos) {
                    var excess = hut.myJunimos[hut.myJunimos.Count - 1];
                    excess.junimoReachedHut(excess, excess.currentLocation);
                    hut.myJunimos.RemoveAt(hut.myJunimos.Count - 1);
                }
            }
            if (hut.myJunimos.Count >= maxJunimos) return;

            // space out spawns (one junimo per ~second)
            if (_lastSpawnTick.TryGetValue(hut, out var lastSpawn) && Game1.ticks - lastSpawn < SpawnCooldownTicks) return;

            // Don't start new work after quitting time
            var quittingTime = Util.Progression.CanWorkInEvenings ? 2400 : 1900;
            if (Game1.timeOfDay > quittingTime) return;

            // Winter
            if (hut.GetParentLocation().IsWinterHere() && !Util.Progression.CanWorkInWinter) return;

            // Rain
            if (hut.GetParentLocation().IsRainingHere() && !Util.Progression.CanWorkInRain) return;

            // Don't spawn during a farm event on the farm (vanilla behaviour)
            if (hut.GetParentLocation().NameOrUniqueName == "Farm" && Game1.farmEvent != null) return;

            // Nothing to do
            if (!hut.areThereMatureCropsWithinRadius()) return;

            Util.SpawnJunimoAtHut(hut);
            _lastSpawnTick[hut] = Game1.ticks;
        }

        private static bool IsJunimoAliveAndHomed(JunimoHarvester junimo) {
            // a junimo with no current location can't be working; treat as dead
            if (junimo.currentLocation is null) return false;

            if (junimo.home is null) {
                // orphaned: no hut to return to, despawn it
                junimo.junimoReachedHut(junimo, junimo.currentLocation);
                return false;
            }

            return Game1.locations.Any(location => location.characters.Contains(junimo));
        }
    }

    /* dayUpdate
     *
     * To allow more junimos, allow working
     */
    [HarmonyPriority(Priority.VeryHigh)]
    internal class ReplaceJunimoHutdayUpdate {
        public static void Postfix(JunimoHut __instance, int dayOfMonth) {
            __instance.shouldSendOutJunimos.Value = true;
            __instance.cropHarvestRadius = Util.CurrentWorkingRadius;
            PatchSearchAroundHut.InvalidateCache();
            JunimoSpawnHelper.ResetPacing();
        }
    }

    /*
     * performTenMinuteAction
     *
     * Keep vanilla behaviour of poking junimos back to work (and the hut light,
     * which the vanilla body handles), but understand that junimos may be working
     * in other locations (greenhouse) and that more than 3 junimos can be in
     * service.
     */
    [HarmonyPriority(Priority.Low)]
    internal class ReplaceJunimoTimerNumber {
        public static void Postfix(JunimoHut __instance, int timeElapsed) {
            if (!Context.IsMainPlayer) return;

            // remove junimos that are no longer in the world (in any location)
            __instance.myJunimos.RemoveAll(junimo => junimo is null || !Game1.locations.Any(loc => loc.characters.Contains(junimo)));

            // despawn junimos whose hut no longer exists anywhere (e.g. the building
            // was moved or demolished), so they don't linger until the next day
            foreach (var loc in Game1.locations) {
                foreach (var npc in loc.characters.OfType<JunimoHarvester>().ToList()) {
                    if (npc.currentLocation != null && npc.home is null) {
                        npc.junimoReachedHut(npc, npc.currentLocation);
                    }
                }
            }

            // make sure junimos working in a greenhouse are still accounted for
            foreach (var loc in Game1.locations) {
                if (!loc.IsGreenhouse) continue;
                foreach (var npc in loc.characters) {
                    if (npc is JunimoHarvester jh && jh.home == __instance) {
                        if (!__instance.myJunimos.Contains(jh)) {
                            __instance.myJunimos.Add(jh);
                            jh.pokeToHarvest();
                        }
                    }
                }
            }

            // send the junimos back to work (vanilla already pokes the ones in this
            // hut's location; this also covers greenhouse junimos it couldn't see)
            for (var index = __instance.myJunimos.Count - 1; index >= 0; --index) {
                __instance.myJunimos[index].pokeToHarvest();
            }
            // note: the hut light (wasLit) is handled by the vanilla body of
            // performTenMinuteAction, so it is not duplicated here
        }
    }

    /* getUnusedJunimoNumber
     *
     * Completely rewrite method to support more than 3 junimos
     * The only difference is the use of MaxJunimos
     */
    [HarmonyPriority(Priority.Low)]
    internal class ReplaceJunimoHutNumber {
        public static bool Prefix(JunimoHut __instance, ref int __result) {
            if (!Context.IsMainPlayer) return true;
            for (var index = 0; index < Util.Progression.MaxJunimosUnlocked; ++index) {
                if (index >= __instance.myJunimos.Count) {
                    __result = index;
                    return false;
                }

                var flag = __instance.myJunimos.Any(junimo => junimo.whichJunimoFromThisHut == index);

                if (flag) continue;
                __result = index;
                return false;
            }

            __result = 2;
            return false;
        }
    }
}
