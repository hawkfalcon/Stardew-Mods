using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Quests;
using System.Linq;

namespace CustomQuestExpiration {
    public class CustomQuestExpiration : Mod {
        private ModConfig Config;
       
        public override void Entry(IModHelper helper) {
            this.Config = Helper.ReadConfig<ModConfig>();
            
            MenuEvents.MenuClosed += MenuEvents_MenuClosed;
        }
      
        private void MenuEvents_MenuClosed(object sender, EventArgsClickableMenuClosed e) {
            // Only update days left when leaving the billboard
            if (e.PriorMenu is StardewValley.Menus.Billboard) {
                updateQuest();
            }
        }

        private void updateQuest() {
			Quest currentDailyQuest = Game1.player.questLog.FirstOrDefault(quest =>
                quest.dailyQuest && quest.Equals(Game1.questOfTheDay));
			if (currentDailyQuest != null) {             
				if (Config.NeverExpires) {
					currentDailyQuest.dailyQuest = false;
				}
				else {
					currentDailyQuest.daysLeft = Config.DaysToExpiration;
				}
			}
        }
    }
}
