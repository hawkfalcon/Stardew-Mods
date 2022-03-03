using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BetterJunimos.Patches;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using BetterJunimos.Utils;
using Newtonsoft.Json;

namespace BetterJunimos {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class BetterJunimos : Mod {
        internal static ModConfig Config;
        internal static IMonitor SMonitor;
        internal static Maps CropMaps;

        internal IGenericModConfigMenuApi configMenu;


        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper) {
            SMonitor = Monitor;
            
            Config = helper.ReadConfig<ModConfig>();
            SaveConfig();

            Util.Reflection = helper.Reflection;

            Util.Abilities = new JunimoAbilities(Config.JunimoAbilities, Monitor);
            Util.Payments = new JunimoPayments(Config.JunimoPayment);
            Util.Progression = new JunimoProgression(ModManifest, Monitor, Helper);

            helper.Content.AssetEditors.Add(new JunimoEditor(helper.Content));
            helper.Content.AssetEditors.Add(new BlueprintEditor());

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.GameLoop.GameLaunched += OnLaunched;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.World.BuildingListChanged += OnBuildingListChanged;

            DoHarmonyRegistration();

            helper.ConsoleCommands.Add("bj_list_huts", "List available huts", ListHuts);
            helper.ConsoleCommands.Add("bj_list_actions", "List available actions", ListActions);
            helper.ConsoleCommands.Add("bj_prompt_all", "Prompt all quests", PromptAllQuests);
            helper.ConsoleCommands.Add("bj_prompt", "Prompt a quest", PromptQuest);
            helper.ConsoleCommands.Add("bj_unlock", "Unlock a quest", UnlockQuest);
        }

        private void ListHuts(string command, string[] args) {
            Util.Progression.ListHuts();
        }

        private void ListActions(string command, string[] args) {
            Util.Progression.ListAllAvailableActions();
        }

        private void PromptAllQuests(string command, string[] args) {
            Util.Progression.PromptAllQuests();
        }

        private void PromptQuest(string command, string[] args) {
            Util.Progression.SetPrompted(args[0]);
        }

        private void UnlockQuest(string command, string[] args) {
            Util.Progression.SetUnlocked(args[0]);
        }

        private static void DoHarmonyRegistration() {
            Harmony harmony = new Harmony("com.hawkfalcon.BetterJunimos");
            // Thank you to Cat (danvolchek) for this harmony setup implementation
            // https://github.com/danvolchek/StardewMods/blob/master/BetterGardenPots/BetterGardenPots/BetterGardenPotsMod.cs#L29
            IList<Tuple<string, Type, Type>> replacements = new List<Tuple<string, Type, Type>>();

            // Junimo Harvester patches
            Type junimoType = typeof(JunimoHarvester);
            replacements.Add("foundCropEndFunction", junimoType, typeof(PatchFindingCropEnd));
            replacements.Add("tryToHarvestHere", junimoType, typeof(PatchHarvestAttemptToCustom));
            replacements.Add("update", junimoType, typeof(PatchJunimoShake));

            // improve pathfinding
            replacements.Add("pathfindToRandomSpotAroundHut", junimoType, typeof(PatchPathfind));
            replacements.Add("pathFindToNewCrop_doWork", junimoType, typeof(PatchPathfindDoWork));

            // Junimo Hut patches
            Type junimoHutType = typeof(JunimoHut);
            replacements.Add("areThereMatureCropsWithinRadius", junimoHutType, typeof(PatchSearchAroundHut));

            // replacements for hardcoded max junimos
            replacements.Add("Update", junimoHutType, typeof(ReplaceJunimoHutUpdate));
            replacements.Add("getUnusedJunimoNumber", junimoHutType, typeof(ReplaceJunimoHutNumber));
            replacements.Add("performTenMinuteAction", junimoHutType, typeof(ReplaceJunimoTimerNumber));

            foreach (Tuple<string, Type, Type> replacement in replacements) {
                MethodInfo original = replacement.Item2.GetMethods(BindingFlags.Instance | BindingFlags.Public).ToList()
                    .Find(m => m.Name == replacement.Item1);

                MethodInfo prefix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .FirstOrDefault(item => item.Name == "Prefix");
                MethodInfo postfix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public)
                    .FirstOrDefault(item => item.Name == "Postfix");

                harmony.Patch(original, prefix == null ? null : new HarmonyMethod(prefix),
                    postfix == null ? null : new HarmonyMethod(postfix));
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
            if (!Context.IsWorldReady) {
                return;
            }
            
            if (e.Button == Config.Other.SpawnJunimoKeybind) {
                SpawnJunimoCommand();
            }

            if (e.Button == SButton.MouseLeft) {
                if (Game1.player.currentLocation is not Farm) return;
                if (Game1.activeClickableMenu != null) return;
                if (!JunimoProgression.HutOnTile(e.Cursor.Tile)) return;

                if (Helper.ModRegistry.Get("ceruleandeep.BetterJunimosForestry") != null) return;

                Util.Progression.ShowPerfectionTracker();
                Helper.Input.Suppress(SButton.MouseLeft);
            }
        }

        /// <summary>Raised after a the game is saved</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        void OnSaving(object sender, SavingEventArgs e) {
            if (!Context.IsMainPlayer) return;
            Helper.Data.WriteSaveData("hawkfalcon.BetterJunimos.CropMaps", CropMaps);
            SaveConfig();
        }

        // BUG: player warps back to wizard hut after use
        private void OpenJunimoHutMenu() {
            var menu = new CarpenterMenu(true);
            var blueprints = Helper.Reflection.GetField<List<BluePrint>>(menu, "blueprints");
            var newBluePrints = new List<BluePrint> {new("Junimo Hut")};
            blueprints.SetValue(newBluePrints);
            Game1.activeClickableMenu = menu;
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        void OnMenuChanged(object sender, MenuChangedEventArgs e) {
            // closed Junimo Hut menu
            //
            // check that e.NewMenu is null because this event also fires when items are added to the chest
            // caution: this runs after any chest is closed, not just Junimo huts
            
            if (e.OldMenu is ItemGrabMenu menu && e.NewMenu is null) {
                if (menu.context is JunimoHut or StardewValley.Objects.Chest) {
                    CheckHutsForWagesAndProgressionItems();
                    Util.Abilities.ResetCooldowns();
                }
            }

            // opened menu
            else if (e.OldMenu == null && e.NewMenu is CarpenterMenu) {
                if (Helper.Reflection.GetField<bool>(e.NewMenu, "magicalConstruction").GetValue()) {
                    // limit to only junimo hut
                    if (!Game1.MasterPlayer.mailReceived.Contains("hasPickedUpMagicInk")) {
                        OpenJunimoHutMenu();
                    }
                }
            }
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        void OnDayStarted(object sender, DayStartedEventArgs e) {
            if (!Context.IsMainPlayer) {
                Monitor.Log(
                    $"Better Junimos is a single-player mod. It is not well-tested in multiplayer",
                    LogLevel.Warn);
            }

            if (Config.JunimoPayment.WorkForWages) {
                Util.Payments.JunimoPaymentsToday.Clear();
                Util.Payments.WereJunimosPaidToday = false;
            }

            var huts = Game1.getFarm().buildings.OfType<JunimoHut>();
            if (huts.Any()) {
                CheckHutsForWagesAndProgressionItems();
                Util.Progression.DayStartedProgressionPrompt(Game1.IsWinter, Game1.isRaining);
                Util.Abilities.ResetCooldowns();

                if (Config.JunimoPayment.WorkForWages && !Util.Payments.WereJunimosPaidToday) {
                    Util.SendMessage(Helper.Translation.Get("msg.no-work-until-paid"));
                }
            }

            // reset for rainy days, winter, or Generic Mod Config Menu options change
            Helper.Content.InvalidateCache(@"Characters\Junimo");
        }

        private void CheckHutsForWagesAndProgressionItems() {
            var huts = Game1.getFarm().buildings.OfType<JunimoHut>();
            // Monitor.Log("Updating hut items", LogLevel.Debug);
            foreach (var hut in huts) {
                // this might be getting called a bit too much
                // but since OnMenuChanged doesn't tell us reliably which hut has changed
                // it's safer to update items from all huts here
                Util.Abilities.UpdateHutItems(Util.GetHutIdFromHut(hut));

                if (Config.JunimoPayment.WorkForWages) {
                    CheckForWages(hut);
                }

                if (Config.Progression.Enabled) {
                    CheckForProgressionItems(hut);
                }

                Util.Abilities.UpdateHutItems(Util.GetHutIdFromHut(hut));
            }
        }

        /// <summary>Raised after a building is added</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnBuildingListChanged(object sender, BuildingListChangedEventArgs e) {
            foreach (Building building in e.Added) {
                if (building is JunimoHut hut) {
                    Util.Abilities.UpdateHutItems(Util.GetHutIdFromHut(hut));
                }
            }
        }

        /// <summary>Raised after the player loads a save slot and the world is initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, EventArgs e) {
            // reload the config to pick up any changes made in Generic Mod Config Menu on the title screen
            Config = Helper.ReadConfig<ModConfig>();
            AllowJunimoHutPurchasing();
            
            // make sure crop harvesting is on for everyone
            var farm = Game1.getFarm();
            var k = $"hawkfalcon.BetterJunimos.ProgressionData.HarvestCrops.Unlocked";
            farm.modData[k] = "1";
            
            if (!Context.IsMainPlayer) return;
            
            // check for old progression data stored in the save, migrate it
            MigrateProgData();
            
            // load crop maps from save (TODO: not MP-safe)
            CropMaps = Helper.Data.ReadSaveData<Maps>("hawkfalcon.BetterJunimos.CropMaps") ?? new Maps();
        }

        // copy any progress made from the SaveData into modData
        private void MigrateProgData() {
            var pd = Helper.Data.ReadSaveData<ProgressionData>("hawkfalcon.BetterJunimos.ProgressionData");
            if (pd?.Progress is null) return;
            var farm = Game1.getFarm();
            foreach (var (key, value) in pd.Progress) {
                var k = $"hawkfalcon.BetterJunimos.ProgressionData.{key}.Prompted";
                if (value.Prompted) farm.modData[k] = "1";
                k = $"hawkfalcon.BetterJunimos.ProgressionData.{key}.Unlocked";
                if (value.Unlocked) farm.modData[k] = "1";
            }
        }

        private void OnLaunched(object sender, GameLaunchedEventArgs e) {
            // only register after the game is launched so we can query the object registry
            Util.Abilities.RegisterDefaultAbilities();

            Config = Helper.ReadConfig<ModConfig>();
            setupGMCM();
        }

        private void setupGMCM()
        {
            configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null) return;

            configMenu.Register(ModManifest, () => Config = new ModConfig(), SaveConfig);
            configMenu.SetTitleScreenOnlyForNextOptions(ModManifest, false);

            configMenu.AddSectionTitle(ModManifest,
                () => Helper.Translation.Get("cfg.hut-settings"));
            configMenu.AddNumberOption(ModManifest,
                () => Config.JunimoHuts.MaxJunimos,
                val => Config.JunimoHuts.MaxJunimos = val,
                () => Helper.Translation.Get("cfg.max-junimos"),
                () => "",
                1,
                40
            );
            configMenu.AddNumberOption(ModManifest,
                () => Config.JunimoHuts.MaxRadius,
                val => Config.JunimoHuts.MaxRadius = val,
                () => Helper.Translation.Get("cfg.max-radius"),
                () => "",
                1,
                40
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoHuts.AvailableAfterCommunityCenterComplete,
                val => Config.JunimoHuts.AvailableAfterCommunityCenterComplete = val,
                () => Helper.Translation.Get("cfg.avail-after-cc"),
                () => Helper.Translation.Get("cfg.avail-after-cc.tooltip")
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoHuts.AvailableImmediately,
                val => Config.JunimoHuts.AvailableImmediately = val,
                () => Helper.Translation.Get("cfg.avail-immediately"),
                () => ""
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoHuts.ReducedCostToConstruct,
                val => Config.JunimoHuts.ReducedCostToConstruct = val,
                () => Helper.Translation.Get("cfg.reduced-cost"),
                () => ""
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoHuts.FreeToConstruct,
                val => Config.JunimoHuts.FreeToConstruct = val,
                () => Helper.Translation.Get("cfg.free"),
                () => ""
            );

            configMenu.AddSectionTitle(ModManifest,
                () => Helper.Translation.Get("cfg.improvements"),
                () => ""
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.Progression.Enabled,
                val => Config.Progression.Enabled = val,
                () => Helper.Translation.Get("cfg.skills-progression"),
                () => Helper.Translation.Get("cfg.skills-progression.tooltip"));
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoImprovements.CanWorkInRain,
                val => Config.JunimoImprovements.CanWorkInRain = val,
                () => Helper.Translation.Get("cfg.can-work-in-rain"),
                () => ""
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoImprovements.CanWorkInWinter,
                val => Config.JunimoImprovements.CanWorkInWinter = val,
                () => Helper.Translation.Get("cfg.can-work-in-winter"),
                () => ""
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoImprovements.CanWorkInEvenings,
                val => Config.JunimoImprovements.CanWorkInEvenings = val,
                () => Helper.Translation.Get("cfg.can-work-in-evenings"),
                () => ""
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoImprovements.WorkFaster,
                val => Config.JunimoImprovements.WorkFaster = val,
                () => Helper.Translation.Get("cfg.work-faster"),
                () => ""
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoImprovements.AvoidHarvestingFlowers,
                val => Config.JunimoImprovements.AvoidHarvestingFlowers = val,
                () => Helper.Translation.Get("cfg.avoid-harvesting-flowers"),
                () => ""
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoImprovements.AvoidHarvestingGiants,
                val => Config.JunimoImprovements.AvoidHarvestingGiants = val,
                () => Helper.Translation.Get("cfg.avoid-harvesting-giant-crops"),
                () => Helper.Translation.Get("cfg.avoid-harvesting-giant-crops.tooltip")
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoImprovements.AvoidPlantingCoffee,
                val => Config.JunimoImprovements.AvoidPlantingCoffee = val,
                () => Helper.Translation.Get("cfg.avoid-planting-coffee"),
                () => ""
            );

            configMenu.AddSectionTitle(ModManifest,
                () => Helper.Translation.Get("cfg.payment"),
                () => "");
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoPayment.WorkForWages,
                val => Config.JunimoPayment.WorkForWages = val,
                () => Helper.Translation.Get("cfg.work-for-wages"),
                () => ""
            );
            configMenu.AddNumberOption(ModManifest,
                () => Config.JunimoPayment.DailyWage.ForagedItems,
                val => Config.JunimoPayment.DailyWage.ForagedItems = val,
                () => Helper.Translation.Get("cfg.foraged-items"),
                () => "",
                0,
                20
            );
            configMenu.AddNumberOption(ModManifest,
                () => Config.JunimoPayment.DailyWage.Flowers,
                val => Config.JunimoPayment.DailyWage.Flowers = val,
                () => Helper.Translation.Get("cfg.flowers"),
                () => "",
                0,
                20
            );
            configMenu.AddNumberOption(ModManifest,
                () => Config.JunimoPayment.DailyWage.Fruit,
                val => Config.JunimoPayment.DailyWage.Fruit = val,
                () => Helper.Translation.Get("cfg.fruit"),
                () => "",
                0,
                20
            );
            configMenu.AddNumberOption(ModManifest,
                () => Config.JunimoPayment.DailyWage.Wine,
                val => Config.JunimoPayment.DailyWage.Wine = val,
                () => Helper.Translation.Get("cfg.wine"),
                () => "",
                0,
                20
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.JunimoPayment.GiveExperience,
                val => Config.JunimoPayment.GiveExperience = val,
                () => Helper.Translation.Get("cfg.give-experience"),
                () => Helper.Translation.Get("cfg.give-experience.tooltip")
            );

            configMenu.AddSectionTitle(ModManifest,
                () => Helper.Translation.Get("cfg.other"),
                () => "");
            configMenu.AddNumberOption(ModManifest,
                () => Config.FunChanges.RainyJunimoSpiritFactor,
                val => Config.FunChanges.RainyJunimoSpiritFactor = val,
                () => Helper.Translation.Get("cfg.rainy-spirit-factor"),
                () => Helper.Translation.Get("cfg.rainy-spirit-factor.tooltip"),
                0.0f,
                1.0f,
                0.05f);
            configMenu.AddBoolOption(ModManifest,
                () => Config.FunChanges.JunimosAlwaysHaveLeafUmbrellas,
                val => Config.FunChanges.JunimosAlwaysHaveLeafUmbrellas = val,
                () => Helper.Translation.Get("cfg.always-have-umbrellas"),
                () => Helper.Translation.Get("cfg.always-have-umbrellas.tooltip")
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.FunChanges.MoreColorfulLeafUmbrellas,
                val => Config.FunChanges.MoreColorfulLeafUmbrellas = val,
                () => Helper.Translation.Get("cfg.more-colorful-umbrellas"),
                () => Helper.Translation.Get("cfg.more-colorful-umbrellas.tooltip")
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.FunChanges.InfiniteJunimoInventory,
                val => Config.FunChanges.InfiniteJunimoInventory = val,
                () => Helper.Translation.Get("cfg.infinite-inventory"),
                () => Helper.Translation.Get("cfg.infinite-inventory.tooltip")
            );
            configMenu.AddKeybind(ModManifest,
                () => Config.Other.SpawnJunimoKeybind,
                val => Config.Other.SpawnJunimoKeybind = val,
                () => Helper.Translation.Get("cfg.spawn-junimo-keybind"),
                () => ""
            );
            configMenu.AddBoolOption(ModManifest,
                () => Config.Other.ReceiveMessages,
                val => Config.Other.ReceiveMessages = val,
                () => Helper.Translation.Get("cfg.receive-messages"),
                () => ""
            );
        }

        // save config.json and invalidate caches
        private void SaveConfig()
        {
            Helper.WriteConfig(Config);
            Helper.Content.InvalidateCache(@"Characters\Junimo");
            Helper.Content.InvalidateCache(@"Data/Blueprints");
        }

        private static void AllowJunimoHutPurchasing() {
            if (Config.JunimoHuts.AvailableImmediately ||
                (Config.JunimoHuts.AvailableAfterCommunityCenterComplete &&
                 Game1.MasterPlayer.mailReceived.Contains("ccIsComplete"))) {
                Game1.player.hasMagicInk = true;
            }
        }

        private void SpawnJunimoCommand() {
            if (Game1.player.currentLocation.IsFarm) {
                var farm = Game1.getFarm();
                var rand = new Random();

                var huts = farm.buildings.OfType<JunimoHut>();
                var junimoHuts = huts.ToList();
                if (!junimoHuts.Any()) {
                    Util.SendMessage(Helper.Translation.Get("msg.cannot-spawn-without-hut"));
                    return;
                }

                var hut = junimoHuts.ElementAt(rand.Next(0, junimoHuts.Count));
                Util.SpawnJunimoAtPosition(Game1.player.Position, hut, rand.Next(4, 100));
            }
            else {
                Util.SendMessage(Helper.Translation.Get("msg.cannot-spawn-here"));
            }
        }

        private void CheckForWages(JunimoHut hut) {
            if (!Config.JunimoPayment.WorkForWages) return;
            if (Util.Payments.WereJunimosPaidToday || !Util.Payments.ReceivePaymentItems(hut)) return;
            Util.Payments.WereJunimosPaidToday = true;
            Util.SendMessage(Helper.Translation.Get("msg.happy-with-payment"));
        }

        private void CheckForProgressionItems(JunimoHut hut) {
            if (!Config.Progression.Enabled) return;
            Util.Progression.ReceiveProgressionItems(hut);
        }

        public override object GetApi() {
            return new BetterJunimosApi();
        }
    }
}