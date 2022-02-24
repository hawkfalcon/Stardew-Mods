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

namespace BetterJunimos {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class BetterJunimos : Mod {
        internal static ModConfig Config;
        internal static IMonitor SMonitor;
        private ProgressionData _progData;
        internal static Maps CropMaps;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper) {
            // Only run if the master game
            if (!Context.IsMainPlayer) return;
            
            SMonitor = Monitor;
            
            Config = helper.ReadConfig<ModConfig>();
            
            Util.Reflection = helper.Reflection;

            Util.Abilities = new JunimoAbilities(Config.JunimoAbilities, Monitor);
            helper.WriteConfig(Config);

            Util.Payments = new JunimoPayments(Config.JunimoPayment);

            Util.Progression = new JunimoProgression(Monitor, Helper);

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

        private void ListHuts(string command, string[] args)
        {
            Util.Progression.ListHuts();
        }
        
        private void ListActions(string command, string[] args)
        {
            Util.Progression.ListAllAvailableActions();
        }
        
        private void PromptAllQuests(string command, string[] args)
        {
            Util.Progression.PromptAllQuests();
        }
        
        private void PromptQuest(string command, string[] args)
        {
            Util.Progression.SetPrompted(args[0]);
        }
        
        private void UnlockQuest(string command, string[] args)
        {
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
                MethodInfo original = replacement.Item2.GetMethods(BindingFlags.Instance | BindingFlags.Public).ToList().Find(m => m.Name == replacement.Item1);

                MethodInfo prefix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(item => item.Name == "Prefix");
                MethodInfo postfix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(item => item.Name == "Postfix");

                harmony.Patch(original, prefix == null ? null : new HarmonyMethod(prefix), postfix == null ? null : new HarmonyMethod(postfix));
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        void OnButtonPressed(object sender, ButtonPressedEventArgs e) {
            if (!Context.IsWorldReady) { return; }

            if (!Context.IsMainPlayer) return;

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

            Helper.Data.WriteSaveData("hawkfalcon.BetterJunimos.ProgressionData", _progData);
            Helper.Data.WriteSaveData("hawkfalcon.BetterJunimos.CropMaps", CropMaps);
            Helper.WriteConfig(Config);
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
            
            if (!Context.IsMainPlayer) return;
            
            if (e.OldMenu is ItemGrabMenu menu && e.NewMenu is null) {
                if (menu.context is JunimoHut || menu.context is StardewValley.Objects.Chest) {
                    CheckHutsForWagesAndProgressionItems();
                    Util.Abilities.ResetCooldowns();
                }
            }

            // opened menu
            else if (e.OldMenu == null && e.NewMenu is CarpenterMenu) {
                if (Helper.Reflection.GetField<bool>(e.NewMenu, "magicalConstruction").GetValue())
                {
                    // limit to only junimo hut
                    if (!Game1.MasterPlayer.mailReceived.Contains("hasPickedUpMagicInk"))
                    {
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
                Monitor.Log($"Better Junimos is a single-player mod. For multi-player games, only the host can have it installed.", LogLevel.Error);
            }

            // Game1.player.gainExperience(10);

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
                    Util.SendMessage(Helper.Translation.Get("junimosWillNotWorkText"));
                }
            }

            // reset for rainy days, winter, or Generic Mod Config Menu options change
            Helper.Content.InvalidateCache(@"Characters\Junimo");
        }

        private void CheckHutsForWagesAndProgressionItems() {
            var huts = Game1.getFarm().buildings.OfType<JunimoHut>();
            foreach (JunimoHut hut in huts) {
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
            if (!Context.IsMainPlayer) return;
            
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
            if (!Context.IsMainPlayer) return;

            // reload the config to pick up any changes made in Generic Mod Config Menu on the title screen
            Config = Helper.ReadConfig<ModConfig>();
            AllowJunimoHutPurchasing();
            
            if (!Context.IsMainPlayer) return;
            
            // load progression data from the save file
            _progData = Helper.Data.ReadSaveData<ProgressionData>("hawkfalcon.BetterJunimos.ProgressionData") ?? new ProgressionData();
            Util.Progression.Configure(Config, _progData);

            CropMaps = Helper.Data.ReadSaveData<Maps>("hawkfalcon.BetterJunimos.CropMaps") ?? new Maps();
        }

        private void OnLaunched(object sender, GameLaunchedEventArgs e) {
            if (!Context.IsMainPlayer) return;
            
            Config = Helper.ReadConfig<ModConfig>();
            var api = Helper.ModRegistry.GetApi<GenericModConfigMenuAPI>("spacechase0.GenericModConfigMenu");
            if (api is null) return;
            
            
            api.RegisterModConfig(ModManifest, () => Config = new ModConfig(), () => Helper.WriteConfig(Config));
            api.SetDefaultIngameOptinValue(ModManifest, true);

            api.RegisterLabel(ModManifest, "Hut Settings", "");
            api.RegisterSimpleOption(ModManifest, "Max Junimos", "", () => Config.JunimoHuts.MaxJunimos, (int val) => Config.JunimoHuts.MaxJunimos = val);
            api.RegisterSimpleOption(ModManifest, "Max Radius", "", () => Config.JunimoHuts.MaxRadius, (int val) => Config.JunimoHuts.MaxRadius = val);
            api.RegisterSimpleOption(ModManifest, "Available After CC Complete", "Available After Community Center Complete", () => Config.JunimoHuts.AvailableAfterCommunityCenterComplete, (bool val) => Config.JunimoHuts.AvailableAfterCommunityCenterComplete = val);
            api.RegisterSimpleOption(ModManifest, "Available Immediately", "", () => Config.JunimoHuts.AvailableImmediately, (bool val) => Config.JunimoHuts.AvailableImmediately = val);
            api.RegisterSimpleOption(ModManifest, "Reduced Cost To Construct", "", () => Config.JunimoHuts.ReducedCostToConstruct, (bool val) => Config.JunimoHuts.ReducedCostToConstruct = val);
            api.RegisterSimpleOption(ModManifest, "Free To Construct", "", () => Config.JunimoHuts.FreeToConstruct, (bool val) => Config.JunimoHuts.FreeToConstruct = val);
            
            api.RegisterLabel(ModManifest, "Improvements", "");
            api.RegisterSimpleOption(ModManifest, "Skills Progression", "Require each skill to be unlocked", () => Config.Progression.Enabled, (bool val) => Config.Progression.Enabled = val);
            api.RegisterSimpleOption(ModManifest, "Can Work In Rain", "", () => Config.JunimoImprovements.CanWorkInRain, (bool val) => Config.JunimoImprovements.CanWorkInRain = val);
            api.RegisterSimpleOption(ModManifest, "Can Work In Winter", "", () => Config.JunimoImprovements.CanWorkInWinter, (bool val) => Config.JunimoImprovements.CanWorkInWinter = val);
            api.RegisterSimpleOption(ModManifest, "Can Work In Evenings", "", () => Config.JunimoImprovements.CanWorkInEvenings, (bool val) => Config.JunimoImprovements.CanWorkInEvenings = val);
            api.RegisterSimpleOption(ModManifest, "Work Faster", "", () => Config.JunimoImprovements.WorkFaster, (bool val) => Config.JunimoImprovements.WorkFaster = val);
            api.RegisterSimpleOption(ModManifest, "Avoid Harvesting Flowers", "", () => Config.JunimoImprovements.AvoidHarvestingFlowers, (bool val) => Config.JunimoImprovements.AvoidHarvestingFlowers = val);
            api.RegisterSimpleOption(ModManifest, "Avoid Harvesting Giant Crops", "Don't harvest crops that could turn into giant crops", () => Config.JunimoImprovements.AvoidHarvestingGiants, (bool val) => Config.JunimoImprovements.AvoidHarvestingGiants = val);
            api.RegisterSimpleOption(ModManifest, "Avoid Planting Coffee", "", () => Config.JunimoImprovements.AvoidPlantingCoffee, (bool val) => Config.JunimoImprovements.AvoidPlantingCoffee = val);

            api.SetDefaultIngameOptinValue(ModManifest, false);

            api.RegisterLabel(ModManifest, "Payment", "");
            api.RegisterSimpleOption(ModManifest, "Work For Wages", "", () => Config.JunimoPayment.WorkForWages, (bool val) => Config.JunimoPayment.WorkForWages = val);
            api.RegisterClampedOption(ModManifest, "Foraged items", "", () => Config.JunimoPayment.DailyWage.ForagedItems, (int val) => Config.JunimoPayment.DailyWage.ForagedItems = val, 0, 20);
            api.RegisterClampedOption(ModManifest, "Flowers", "", () => Config.JunimoPayment.DailyWage.Flowers, (int val) => Config.JunimoPayment.DailyWage.Flowers = val, 0, 20);
            api.RegisterClampedOption(ModManifest, "Fruit", "", () => Config.JunimoPayment.DailyWage.Fruit, (int val) => Config.JunimoPayment.DailyWage.Fruit = val, 0, 20);
            api.RegisterClampedOption(ModManifest, "Wine", "", () => Config.JunimoPayment.DailyWage.Wine, (int val) => Config.JunimoPayment.DailyWage.Wine = val, 0, 20);

            api.RegisterLabel(ModManifest, "Other", "");
            api.RegisterClampedOption(ModManifest, "Rainy Spirit Factor", "Rainy Junimo Spirit Factor", () => Config.FunChanges.RainyJunimoSpiritFactor, (float val) => Config.FunChanges.RainyJunimoSpiritFactor = val, 0.0f, 1.0f, 0.05f);
            api.RegisterSimpleOption(ModManifest, "Always Have Umbrellas", "Junimos Always Have Leaf Umbrellas", () => Config.FunChanges.JunimosAlwaysHaveLeafUmbrellas, (bool val) => Config.FunChanges.JunimosAlwaysHaveLeafUmbrellas = val);
            api.RegisterSimpleOption(ModManifest, "More Colorful Umbrellas", "More Colorful Leaf Umbrellas", () => Config.FunChanges.MoreColorfulLeafUmbrellas, (bool val) => Config.FunChanges.MoreColorfulLeafUmbrellas = val);
            api.RegisterSimpleOption(ModManifest, "Infinite Inventory", "Infinite Junimo Inventory", () => Config.FunChanges.InfiniteJunimoInventory, (bool val) => Config.FunChanges.InfiniteJunimoInventory = val);
            api.RegisterSimpleOption(ModManifest, "Spawn Junimo Keybind", "Spawn Junimo Keybind", () => Config.Other.SpawnJunimoKeybind, (SButton val) => Config.Other.SpawnJunimoKeybind = val);
            api.RegisterSimpleOption(ModManifest, "Receive Messages", "", () => Config.Other.ReceiveMessages, (bool val) => Config.Other.ReceiveMessages = val);
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
                    Util.SendMessage(Helper.Translation.Get("spawnJunimoButNoHutsText"));
                    return;
                }
                var hut = junimoHuts.ElementAt(rand.Next(0, junimoHuts.Count));
                Util.SpawnJunimoAtPosition(Game1.player.Position, hut, rand.Next(4, 100));
            }
            else {
                Util.SendMessage(Helper.Translation.Get("spawnJunimoCommandText"));
            }
        }

        private void CheckForWages(JunimoHut hut) {
            if (!Config.JunimoPayment.WorkForWages) return;
            if (Util.Payments.WereJunimosPaidToday || !Util.Payments.ReceivePaymentItems(hut)) return;
            Util.Payments.WereJunimosPaidToday = true;
            Util.SendMessage(Helper.Translation.Get("checkForWagesText"));
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
