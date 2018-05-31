using Harmony;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BetterJunimos.Patches;
using static BetterJunimos.Patches.ListExtensions;

namespace BetterJunimos {
    public class BetterJunimos : Mod {
        internal static BetterJunimos instance;
        internal ModConfig Config;
        JunimoEditor editor;
        bool addedEditor = false;

        public override void Entry(IModHelper helper) {
            instance = this;
            Config = Helper.ReadConfig<ModConfig>();
            Util.Config = Config;
            Util.MaxRadius = Config.JunimoPayment.WorkForWages ? 3 : Config.JunimoImprovements.MaxRadius;
            editor = new JunimoEditor();

            if (Config.FunChanges.JunimosAlwaysHaveLeafUmbrellas) {
                Helper.Content.AssetEditors.Add(editor);
                addedEditor = true;
            }

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
                spawnJunimo();
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
            if (!Config.FunChanges.JunimosAlwaysHaveLeafUmbrellas) {
                if (!addedEditor && Game1.isRaining) {
                    Helper.Content.AssetEditors.Add(editor);
                    addedEditor = true;
                }
                else if (addedEditor) {
                    Helper.Content.AssetEditors.Remove(editor);
                    addedEditor = false;
                }
            }
        }

        public void spawnJunimoAtHut(JunimoHut hut) {
            if (hut == null) return;
            Farm farm = Game1.getFarm();
            JunimoHarvester junimoHarvester = new JunimoHarvester(new Vector2((float)hut.tileX.Value + 1, (float)hut.tileY.Value + 1) * 64f + new Vector2(0.0f, 32f), hut, hut.myJunimos.Count + 1);
            farm.characters.Add((NPC)junimoHarvester);
            hut.myJunimos.Add(junimoHarvester);

            if (Game1.isRaining) {
                var alpha = this.Helper.Reflection.GetField<float>(junimoHarvester, "alpha");
                alpha.SetValue(Config.FunChanges.RainyJunimoSpiritFactor);
            }
            //if (!Utility.isOnScreen(Utility.Vector2ToPoint(new Vector2((float)hut.tileX.Value + 1, (float)hut.tileY.Value + 1)), 64, farm))
            //    return;
            farm.playSound("junimoMeep1");
        }

        public void spawnJunimo() {
            Farm farm = Game1.getFarm();
            JunimoHut hut = farm.buildings.FirstOrDefault(building => building is JunimoHut) as JunimoHut;
            spawnJunimoAtHut(hut);
        }
    }
}
