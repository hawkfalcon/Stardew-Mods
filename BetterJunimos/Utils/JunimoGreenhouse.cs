using System;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;

namespace BetterJunimos.Utils {
    public class JunimoGreenhouse {
        private readonly IManifest _manifest;
        private readonly IMonitor _monitor;
        private readonly IModHelper _helper;

        internal JunimoGreenhouse(IManifest manifest, IMonitor monitor, IModHelper helper) {
            _manifest = manifest;
            _monitor = monitor;
            _helper = helper;
        }

        public bool HutHasGreenhouse(Guid id) {
            return GreenhouseBuildingNearHut(id) is not null;
        }
        
        internal static GreenhouseBuilding GreenhouseBuildingAtPos(GameLocation location, Vector2 tile) {
            if (!location.IsBuildableLocation()) return null;
            foreach (var building in location.buildings) {
                if (building.HasIndoors() && (building.GetIndoors()?.IsGreenhouse ?? false))
                {
                    if (building is not GreenhouseBuilding)
                    {
                        var ghb = building as GreenhouseBuilding;
                        if (ghb != null)
                        {
                            if (ghb.occupiesTile(tile)) {
                                return ghb;
                            }
                        }
                        BetterJunimos.SMonitor.Log($"{building.GetIndoors()?.NameOrUniqueName} {(building as GreenhouseBuilding)?.parentLocationName}", LogLevel.Debug);
                    }
                }
                if (building is not GreenhouseBuilding greenhouseBuilding) continue;
                if (greenhouseBuilding.occupiesTile(tile)) {
                    return greenhouseBuilding;
                }
            }

            return null;
        }
        
        public GreenhouseBuilding GreenhouseBuildingNearHut(Guid id) {
            var hut = Util.GetHutFromId(id);
            var radius = Util.CurrentWorkingRadius;
            var farm = hut.GetParentLocation();
        
            for (var x = hut.tileX.Value + 1 - radius; x < hut.tileX.Value + 2 + radius; ++x) {
                for (var y = hut.tileY.Value + 1 - radius; y < hut.tileY.Value + 2 + radius; ++y) {
                    var pos = new Vector2(x, y);

                    var ghb = GreenhouseBuildingAtPos(farm, pos);
                    if (ghb is not null) return ghb;
                }
            }

            return null;
        }

        internal bool IsGreenhouseAtPos(GameLocation location, Vector2 tile) {
            if (!location.IsBuildableLocation()) return false;
            return location.buildings.Any(building =>
                building.occupiesTile(tile) && building is GreenhouseBuilding);
        }
    }
}