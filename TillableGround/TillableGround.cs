using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;

namespace TillableGround {
	public class TillableGround : Mod {

        public override void Entry(IModHelper helper) {
            InputEvents.ButtonPressed += InputEvents_ButtonPressed;
        }

        void InputEvents_ButtonPressed( object sender, EventArgsInput e ) {
            if (!Context.IsWorldReady) { return; }
            if (e.Button == SButton.H) {
				GameLocation loc = Game1.currentLocation;
				int x = Game1.player.getTileX();
				int y = Game1.player.getTileY();

				//this.Monitor.Log("Made tile tillable at (" + x + ", " + y + ")");
				loc.setTileProperty(x, y, "Back", "Diggable", "T");
                // Add the hoe animation without hoeing
				loc.temporarySprites.Add(
					new TemporaryAnimatedSprite(12, new Vector2(x * 64f, y * 64f), Color.White, 8, 
					    Game1.random.NextDouble() < 0.5, 50f, 0, -1, -1f, -1, 0)
				);

				Game1.addHUDMessage(new HUDMessage("Made tile tillable", 3) {
					noIcon = true, timeLeft = HUDMessage.defaultTime / 4 
				});
            }
        }
    }
}
