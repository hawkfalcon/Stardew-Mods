using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace BetterJunimosForestry {
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod {

        internal ModConfig Config;

        private void OnLaunched(object sender, GameLaunchedEventArgs e) {
            Config = Helper.ReadConfig<ModConfig>();
            Util.Config = Config;

            var gmcm_api = Helper.ModRegistry.GetApi<GenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");
            if (gmcm_api is not null) {
                gmcm_api.RegisterModConfig(ModManifest, () => Config = new ModConfig(), () => Helper.WriteConfig(Config));
                gmcm_api.SetDefaultIngameOptinValue(ModManifest, true);
            }

            var bj_api = Helper.ModRegistry.GetApi("hawkfalcon.BetterJunimos");
            if (bj_api is null) {
                Monitor.Log($"Could not load Better Junimos API", LogLevel.Error);
                return;
            }

            Helper.Reflection.GetMethod(bj_api, "RegisterJunimoAbility").Invoke(new Abilities.HarvestGrassAbility());
            Helper.Reflection.GetMethod(bj_api, "RegisterJunimoAbility").Invoke(new Abilities.HarvestDebrisAbility(Monitor));
            Helper.Reflection.GetMethod(bj_api, "RegisterJunimoAbility").Invoke(new Abilities.CollectDroppedObjectsAbility(Monitor));
            Helper.Reflection.GetMethod(bj_api, "RegisterJunimoAbility").Invoke(new Abilities.ChopTreesAbility(Monitor));
            Helper.Reflection.GetMethod(bj_api, "RegisterJunimoAbility").Invoke(new Abilities.FertilizeTreesAbility());
            Helper.Reflection.GetMethod(bj_api, "RegisterJunimoAbility").Invoke(new Abilities.PlantTreesAbility(Monitor));
            Helper.Reflection.GetMethod(bj_api, "RegisterJunimoAbility").Invoke(new Abilities.PlantFruitTreesAbility(Monitor));
            Helper.Reflection.GetMethod(bj_api, "RegisterJunimoAbility").Invoke(new Abilities.HarvestFruitTreesAbility(Monitor));
            Helper.Reflection.GetMethod(bj_api, "RegisterJunimoAbility").Invoke(new Abilities.HoeAroundTreesAbility(Monitor));
        }

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper) {
            Helper.Events.GameLoop.GameLaunched += OnLaunched;
        }
    }
}
