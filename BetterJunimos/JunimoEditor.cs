using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace BetterJunimos {
    public class JunimoEditor : IAssetEditor {
        
        public bool CanEdit<T>(IAssetInfo asset) {
            return asset.AssetNameEquals(@"Characters\Junimo");
        }

        public void Edit<T>(IAssetData asset) {
            Rectangle rectangle = new Rectangle(0, 0, 128, 128);
            Texture2D customTexture = BetterJunimos.instance.Helper.Content.Load<Texture2D>("assets/JunimoUmbrellaOnly.png", ContentSource.ModFolder);
            asset.AsImage().PatchImage(customTexture, rectangle, rectangle, PatchMode.Overlay);
        }
    }
}
