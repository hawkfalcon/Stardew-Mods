using Harmony;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BetterJunimos {
    public class BetterJunimos : Mod {
        public static Mod instance;

		public override void Entry(IModHelper helper) {
            instance = this;
            var harmony = HarmonyInstance.Create("com.hawkfalcon.BetterJunimos");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            InputEvents.ButtonPressed += InputEvents_ButtonPressed;
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
