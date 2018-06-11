using Harmony;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Buildings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BetterJunimos.Patches;
using static BetterJunimos.Patches.ListExtensions;

namespace BetterJunimos {
    public class BetterJunimos : Mod {
        internal ModConfig Config;

        public override void Entry(IModHelper helper) {
            Config = Helper.ReadConfig<ModConfig>();

            JunimoAbilities junimoAbilities = new JunimoAbilities();
            junimoAbilities.Capabilities = Config.JunimoCapabilities;

            Util.Config = Config;
            Util.Reflection = Helper.Reflection;
            Util.Abilities = junimoAbilities;
            Util.MaxRadius = Config.JunimoPayment.WorkForWages ? 3 : Config.JunimoImprovements.MaxRadius;

            Helper.Content.AssetEditors.Add(new JunimoEditor(Helper.Content));

            InputEvents.ButtonPressed += InputEvents_ButtonPressed;
            MenuEvents.MenuClosed += MenuEvents_MenuClosed;
            TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;

            DoHarmonyRegistration();
        }

        private void DoHarmonyRegistration() {
            HarmonyInstance harmony = HarmonyInstance.Create("com.hawkfalcon.BetterJunimos");
            // Thank you to Cat (danvolchek) for this harmony setup implementation
            // https://github.com/danvolchek/StardewMods/blob/master/BetterGardenPots/BetterGardenPots/BetterGardenPotsMod.cs#L29
            IList<Tuple<string, Type, Type>> replacements = new List<Tuple<string, Type, Type>>();

            // Junimo Harvester patches
            Type junimoType = Util.GetSDVType("Characters.JunimoHarvester");
            replacements.Add("foundCropEndFunction", junimoType, typeof(PatchFindingCropEnd));
            replacements.Add("tryToHarvestHere", junimoType, typeof(PatchHarvestAttemptToCustom));
            replacements.Add("update", junimoType, typeof(PatchJunimoShake));
            if (Config.JunimoImprovements.MaxRadius > Util.DefaultRadius || Config.JunimoPayment.WorkForWages) {
                replacements.Add("pathfindToRandomSpotAroundHut", junimoType, typeof(PatchPathfind));
                replacements.Add("pathFindToNewCrop_doWork", junimoType, typeof(PatchPathfindDoWork));
            }

            // Junimo Hut patches
            Type junimoHutType = Util.GetSDVType("Buildings.JunimoHut");
            replacements.Add("areThereMatureCropsWithinRadius", junimoHutType, typeof(PatchSearchAroundHut));

            // replacements for hardcoded max junimos
            replacements.Add("Update", junimoHutType, typeof(ReplaceJunimoHutUpdate));
            replacements.Add("getUnusedJunimoNumber", junimoHutType, typeof(ReplaceJunimoHutNumber));

            // fix stupid bugs in SDV 
            Type chestType = Util.GetSDVType("Objects.Chest");
            replacements.Add("grabItemFromChest", chestType, typeof(ChestPatchFrom));
            replacements.Add("grabItemFromInventory", chestType, typeof(ChestPatchTo));

            foreach (Tuple<string, Type, Type> replacement in replacements) {
                MethodInfo original = replacement.Item2.GetMethods(BindingFlags.Instance | BindingFlags.Public).ToList().Find(m => m.Name == replacement.Item1);

                MethodInfo prefix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(item => item.Name == "Prefix");
                MethodInfo postfix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(item => item.Name == "Postfix");

                harmony.Patch(original, prefix == null ? null : new HarmonyMethod(prefix), postfix == null ? null : new HarmonyMethod(postfix));
            }
        }

        void InputEvents_ButtonPressed(object sender, EventArgsInput e) {
            if (!Context.IsWorldReady) { return; }

            if (e.Button == SButton.O) {
                Util.spawnJunimo();
            }
        }

        // Closed Junimo Hut menu
        void MenuEvents_MenuClosed(object sender, EventArgsClickableMenuClosed e) {
            if (e.PriorMenu is StardewValley.Menus.ItemGrabMenu menu) {
                if (menu.specialObject != null && menu.specialObject is JunimoHut hut) {
                    if (Config.JunimoPayment.WorkForWages && !Util.WereJunimosPaidToday &&
                        Util.JunimoPaymentReceiveItems(hut)) {
                        Util.WereJunimosPaidToday = true;
                        Util.MaxRadius = Config.JunimoImprovements.MaxRadius;
                    }
                }
            }
        }

        void TimeEvents_AfterDayStarted(object sender, EventArgs e) {
            Util.JunimoPaymentsToday.Clear();
            Util.WereJunimosPaidToday = false;
            
            if (!Config.FunChanges.JunimosAlwaysHaveLeafUmbrellas) {
                // reset for rainy days
                Helper.Content.InvalidateCache(@"Characters\Junimo");
            }
        }
    }
}
