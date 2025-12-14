using BetterJunimos.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Buildings;
using System.Linq;

namespace BetterJunimos {
    public static class BuildingEditor {
        private const string hutKey = "Junimo Hut";

        internal static void OnAssetRequested(object sender, AssetRequestedEventArgs e) {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Buildings")) {
                e.Edit(asset => {
                    var data = asset.AsDictionary<string, BuildingData>().Data;
                    var junimoHut = data[hutKey];

                    if (BetterJunimos.Config.JunimoHuts.FreeToConstruct) {
                        junimoHut.BuildCost = 0;
                        junimoHut.BuildMaterials.Clear();
                        BetterJunimos.SMonitor.Log($"Edited Junimo Huts to cost ${junimoHut.BuildCost} and no materials", LogLevel.Debug);
                    } else if (Util.Progression.ReducedCostToConstruct) {
                        junimoHut.BuildCost = 10000;
                        junimoHut.BuildMaterials.Clear();

                        junimoHut.BuildMaterials.Add(new BuildingMaterial {
                            ItemId = "390",
                            Amount = 100
                        });
                        junimoHut.BuildMaterials.Add(new BuildingMaterial {
                            ItemId = "268",
                            Amount = 3
                        });
                        junimoHut.BuildMaterials.Add(new BuildingMaterial {
                            ItemId = "771",
                            Amount = 100
                        });
                        var materials = string.Join(" / ", from material in junimoHut.BuildMaterials select $"{material.ItemId} {material.Amount}");
                        BetterJunimos.SMonitor.Log($"Edited Junimo Huts to cost ${junimoHut.BuildCost}, with materials {materials}", LogLevel.Debug);
                    }
                });
            }
        }
    }
}