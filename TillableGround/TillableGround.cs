using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Quests;
using System.Linq;
using xTile.Layers;

namespace TillableGround {
	public class TillableGround : Mod {

        public override void Entry(IModHelper helper) {
            InputEvents.ButtonPressed += InputEvents_ButtonPressed;
        }

        void InputEvents_ButtonPressed( object sender, EventArgsInput e ) {
            if (!Context.IsWorldReady) { return; }
            if (e.Button == SButton.P) {
				int x = Game1.player.getTileX();
				int y = Game1.player.getTileY();
				this.Monitor.Log("Made tile tillable at (" + x + " " + y + ")");
                Game1.currentLocation.setTileProperty(x, y, "Back", "Diggable", "T");
            }
        }
    }
}
