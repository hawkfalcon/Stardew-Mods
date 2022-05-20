using BetterJunimos.Utils;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace BetterJunimos {
    public static class BlueprintEditor {
        private const string hutKey = "Junimo Hut";

        internal static void OnAssetRequested(object sender, AssetRequestedEventArgs e) {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Blueprints")) {
                e.Edit(asset => {
                    var data = asset.AsDictionary<string, string>().Data;

                    if (BetterJunimos.Config.JunimoHuts.FreeToConstruct) {
                        var fields = data[hutKey].Split('/');
                        fields[0] = "";
                        fields[17] = "0";
                        data[hutKey] = string.Join("/", fields);
                        BetterJunimos.SMonitor.Log(data[hutKey], LogLevel.Debug);
                    }
                    else if (Util.Progression.ReducedCostToConstruct) {
                        var fields = data[hutKey].Split('/');
                        fields[0] = "390 100 268 3 771 100";
                        fields[17] = "10000";
                        data[hutKey] = string.Join("/", fields);
                        BetterJunimos.SMonitor.Log(data[hutKey], LogLevel.Debug);
                    }
                });
            }
        }
    }
}