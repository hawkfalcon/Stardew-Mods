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
            if (greenhouse.characters.Count(npc => npc is JunimoHarvester) >= Util.Progression.MaxJunimosUnlocked - Util.Progression.BonusMaxJunimos) {
                // greenhouse already kinda full
                return false;
            }

            if (!Util.Abilities.lastKnownCropLocations.TryGetValue((hut, greenhouse), out var lkc)) {
                if (!Patches.PatchSearchAroundHut.SearchGreenhouseGrid(Util.GetHutFromId(guid), guid, greenhouse)) {
                    // no work to be done in greenhouse
                    return false;
                }
            }
            Greenhouse = greenhouse;
            return true;
        }

        public bool PerformAction(GameLocation location, Vector2 pos, JunimoHarvester junimo, Guid guid) {
            if (!IsActionAvailable(location, pos, guid)) {
                return false;
            }
            
            if (Utility.isOnScreen(Utility.Vector2ToPoint(pos), 64, location)) {
                location.playSound("junimoMeep1");
            }

            // spawn a new Junimo in greenhouse
            var hut = junimo.home;
            var greenhouse = Greenhouse;
            var door = Patches.PatchPathfindDoWork.GreenhouseDoor(junimo, greenhouse);
            var spawnAt = new Vector2(door.X, (float) door.Y - 1) * 64f + new Vector2(0.0f, 32f);

            var junimoNumber = Game1.random.Next(4, 100);
            Util.SpawnJunimoAtPosition(greenhouse, spawnAt, hut, junimoNumber);

            // schedule this Junimo for despawn
            junimo.junimoReachedHut(junimo, junimo.currentLocation);
            return true;
        }

        public List<string> RequiredItems() {
            return new();
        }
    }
}