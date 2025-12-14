using System;
using System.Collections.Generic;
using System.Linq;
using BetterJunimos.Utils;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;

namespace BetterJunimos.Abilities {
    public class VisitGreenhouseAbility : IJunimoAbility {
        public string AbilityName() {
            return "VisitGreenhouse";
        }

        public GameLocation Greenhouse { get; set; }

        public bool IsActionAvailable(GameLocation location, Vector2 pos, Guid guid) {
            if (!BetterJunimos.Config.JunimoImprovements.CanWorkInGreenhouse) return false;
            //if (!location.IsFarm) return false;
            var hut = Util.GetHutFromId(guid);
            var (x, y) = pos;
            var up = new Vector2(x, y + 1);
            var right = new Vector2(x + 1, y);
            var down = new Vector2(x, y - 1);
            var left = new Vector2(x - 1, y);

            Vector2[] positions = {up, right, down, left};
            var greenhouses = positions.Select(nextPos => JunimoGreenhouse.GreenhouseBuildingAtPos(location, nextPos));
            if (!greenhouses.Any(greenhouseBuilding => greenhouseBuilding is not null)) return false;
            var greenhouse = greenhouses.FirstOrDefault(greenhouseBuilding => greenhouseBuilding is not null);
            //BetterJunimos.SMonitor.Log("Greenhouse found", LogLevel.Debug);
            if (greenhouse.characters.Count(npc => npc is JunimoHarvester) >= Util.Progression.MaxJunimosUnlocked - Util.Progression.BonusMaxJunimos) {
                // greenhouse already kinda full
                // BetterJunimos.SMonitor.Log("Greenhouse full", LogLevel.Debug);
                return false;
            }

            if (!Util.Abilities.lastKnownCropLocations.TryGetValue((hut, greenhouse), out var lkc)) {
                if (!Patches.PatchSearchAroundHut.SearchGreenhouseGrid(Util.GetHutFromId(guid), guid, greenhouse)) {
                    // no work to be done in greenhouse
                    //BetterJunimos.SMonitor.Log("VisitGreenhouse IsActionAvailable: no work", LogLevel.Debug);
                    return false;
                }
            }
            
            Greenhouse = greenhouse;
            
            //BetterJunimos.SMonitor.Log("VisitGreenhouse IsActionAvailable: available", LogLevel.Debug);
            return true;
        }

        public bool PerformAction(GameLocation location, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            // BetterJunimos.SMonitor.Log($"VisitGreenhouse #{junimo.whichJunimoFromThisHut}: PerformAction: begins",
            //     LogLevel.Debug);
            if (!IsActionAvailable(location, pos, guid)) {
                // BetterJunimos.SMonitor.Log($"VisitGreenhouse #{junimo.whichJunimoFromThisHut}: PerformAction: unavail",
                //     LogLevel.Debug);
                return false;
            }

            // BetterJunimos.SMonitor.Log($"VisitGreenhouse #{junimo.whichJunimoFromThisHut}: PerformAction: doing",
            //     LogLevel.Trace);

            if (Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, location)) {
                location.playSound("junimoMeep1");
            }

            // spawn a new Junimo in greenhouse
            //BetterJunimos.SMonitor.Log($"VisitGreenhouse #{junimo.home is null} {guid}: PerformAction: doing", LogLevel.Debug);
            var hut = junimo.home;
            var greenhouse = Greenhouse;
            var door = Patches.PatchPathfindDoWork.GreenhouseDoor(junimo, greenhouse);
            //BetterJunimos.SMonitor.Log($"Door at {door.X}, {door.Y}", LogLevel.Debug);
            var spawnAt = new Vector2(door.X, (float) door.Y - 1) * 64f + new Vector2(0.0f, 32f);

            var junimoNumber = Game1.random.Next(4, 100);
            Util.SpawnJunimoAtPosition(greenhouse, spawnAt, hut, junimoNumber);
            // BetterJunimos.SMonitor.Log(
            //     $"VisitGreenhouse PerformAction: #{junimoNumber} spawned in {greenhouse.Name} at {spawnAt.X} {spawnAt.Y}",
            //     LogLevel.Trace);

            // schedule this Junimo for despawn
            junimo.junimoReachedHut(junimo, junimo.currentLocation);
            return true;
        }

        public List<string> RequiredItems() {
            return new();
        }
    }
}