﻿using BetterJunimos.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;

namespace BetterJunimos {
    public class JunimoEditor : IAssetEditor {
        private IContentHelper Content;

        public JunimoEditor(IContentHelper Content) {
            this.Content = Content;
        }

        public bool CanEdit<T>(IAssetInfo asset) {
            if (BetterJunimos.Config.FunChanges.JunimosAlwaysHaveLeafUmbrellas || Game1.isRaining) {
                return asset.AssetNameEquals(@"Characters\Junimo");
            }
            if (Game1.IsWinter) {
                return asset.AssetNameEquals(@"Characters\Junimo");
            }
            return false;
        }

        public void Edit<T>(IAssetData asset) {
            if (BetterJunimos.Config.FunChanges.JunimosAlwaysHaveLeafUmbrellas || Game1.isRaining) {
                Rectangle rectangle = new Rectangle(0, 0, 128, 128);
                string umbrella = BetterJunimos.Config.FunChanges.MoreColorfulLeafUmbrellas ? "JunimoUmbrellaOnly_Grayscale" : "JunimoUmbrellaOnly";
                Texture2D customTexture = Content.Load<Texture2D>("assets/" + umbrella + ".png", ContentSource.ModFolder);
                asset.AsImage().PatchImage(customTexture, rectangle, rectangle, PatchMode.Overlay);
                return;
            }
            if (Game1.IsWinter) {
                Rectangle rectangle = new Rectangle(0, 0, 128, 128);
                string beanie = "JunimoBeanie";
                Texture2D customTexture = Content.Load<Texture2D>("assets/" + beanie + ".png", ContentSource.ModFolder);
                asset.AsImage().PatchImage(customTexture, rectangle, rectangle, PatchMode.Overlay);
            }
        }
    }
}
