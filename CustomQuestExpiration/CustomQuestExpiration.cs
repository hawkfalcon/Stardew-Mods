using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Quests;
using System.Linq;

namespace CustomQuestExpiration {
    public class CustomQuestExpiration : Mod {
        private ModConfig Config;
        private bool acceptedDailyQuest = false;
       
        public override void Entry(IModHelper helper) {
            this.Config = Helper.ReadConfig<ModConfig>();

            MenuEvents.MenuClosed += MenuEvents_MenuClosed;
            TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
        }

        void TimeEvents_AfterDayStarted(object sender, System.EventArgs e) {
            acceptedDailyQuest = false;
        }

        private void MenuEvents_MenuClosed(object sender, EventArgsClickableMenuClosed e) {
            if (acceptedDailyQuest || !Context.IsMainPlayer) { return; }
            // Only update days left when leaving the billboard
            if (e.PriorMenu.GetType() != typeof(StardewValley.Menus.Billboard)) {
                return;
            }

            updateQuest();
        }

        private void updateQuest() {
            Quest currentDailyQuest = Game1.player.questLog.FirstOrDefault(quest =>
                quest.dailyQuest && quest.Equals(Game1.questOfTheDay)
            );

            if (currentDailyQuest != null)
            {
                if (Config.NeverExpires)
                {
                    currentDailyQuest.dailyQuest = false;
                }
                else
                {
                    currentDailyQuest.daysLeft = Config.DaysToExpiration;
                }
                acceptedDailyQuest = true;
            }
        }
    }
}
