using Harmony;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BetterJunimos {
    public class BetterJunimos : Mod {
        public static Mod instance;

		public override void Entry(IModHelper helper) {
            instance = this;
            InputEvents.ButtonPressed += InputEvents_ButtonPressed;

            var harmony = HarmonyInstance.Create("com.hawkfalcon.BetterJunimos");

            // Thank you to Cat (danvolchek) for this harmony setup implementation
            // https://github.com/danvolchek/StardewMods/blob/master/BetterGardenPots/BetterGardenPots/BetterGardenPotsMod.cs#L29
            Type junimoType = GetSDVType("Characters.JunimoHarvester");
            IList<Tuple<string, Type, Type>> replacements = new List<Tuple<string, Type, Type>>();

            Add(replacements, "foundCropEndFunction", junimoType, typeof(PatchFindingCropEnd));
            Add(replacements, "tryToHarvestHere", junimoType, typeof(PatchHarvestAttemptToCustom));
            Add(replacements, "areThereMatureCropsWithinRadius", GetSDVType("Buildings.JunimoHut"), typeof(PatchPathfindHut));

            foreach (Tuple<string, Type, Type> replacement in replacements) {
                MethodInfo original = replacement.Item2.GetMethods(BindingFlags.Instance | BindingFlags.Public).ToList().Find(m => m.Name == replacement.Item1);

                MethodInfo prefix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(item => item.Name == "Prefix");
                MethodInfo postfix = replacement.Item3.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(item => item.Name == "Postfix");

                harmony.Patch(original, prefix == null ? null : new HarmonyMethod(prefix), postfix == null ? null : new HarmonyMethod(postfix));
            }
        }

        public void Add<T1, T2, T3>(IList<Tuple<T1, T2, T3>> list, T1 item1, T2 item2, T3 item3) {
            list.Add(new Tuple<T1, T2, T3>(item1, item2, item3));
        }

        //Big thanks to Routine for this workaround for mac users.
        //https://github.com/Platonymous/Stardew-Valley-Mods/blob/master/PyTK/PyUtils.cs#L117
        /// <summary>Gets the correct type of the object, handling different assembly names for mac/linux users.</summary>
        private static Type GetSDVType( string type ) {
            const string prefix = "StardewValley.";
            Type defaultSDV = Type.GetType(prefix + type + ", Stardew Valley");

            return defaultSDV ?? Type.GetType(prefix + type + ", StardewValley");
        }

        void InputEvents_ButtonPressed( object sender, EventArgsInput e ) {
            if (!Context.IsWorldReady) { return; }

            if (e.Button == SButton.O) {
                spawnJunimo();
            }
        }

        void spawnJunimo() {
            Farm farm = Game1.getFarm();
            JunimoHut hut = farm.buildings.FirstOrDefault(building => building is JunimoHut) as JunimoHut;
            if (hut == null) return;
            JunimoHarvester junimoHarvester = new JunimoHarvester(new Vector2((float)((int)((NetFieldBase<int, NetInt>)hut.tileX) + 1), (float)((int)((NetFieldBase<int, NetInt>)hut.tileY) + 1)) * 64f + new Vector2(0.0f, 32f), hut, hut.myJunimos.Count + 1);
            hut.myJunimos.Add(junimoHarvester);
            farm.characters.Add((NPC)junimoHarvester);
            farm.playSound("junimoMeep1");
        }
	}
}
