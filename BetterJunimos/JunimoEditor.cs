using BetterJunimos.Patches;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace BetterJunimos {
    public class JunimoEditor : IAssetEditor {
        
        public bool CanEdit<T>(IAssetInfo asset) {
            if (Util.Config.FunChanges.JunimosAlwaysHaveLeafUmbrellas || Game1.isRaining) {
                return asset.AssetNameEquals(@"Characters\Junimo");
            }
            return false;
        }

        public void Edit<T>(IAssetData asset) {
            if (Util.Config.FunChanges.JunimosAlwaysHaveLeafUmbrellas || Game1.isRaining) {
                Rectangle rectangle = new Rectangle(0, 0, 128, 128);
                Texture2D customTexture = BetterJunimos.instance.Helper.Content.Load<Texture2D>("assets/JunimoUmbrellaOnly.png", ContentSource.ModFolder);
                asset.AsImage().PatchImage(customTexture, rectangle, rectangle, PatchMode.Overlay);
            }
        }
    }
}
