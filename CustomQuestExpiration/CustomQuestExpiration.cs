using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Quests;
using System.Collections.Generic;
using System.Linq;

namespace CustomQuestExpiration {
    public class CustomQuestExpiration : Mod {
        private const int InfiniteDays = 100000;
        private ModConfig Config;
        private List<Quest> dailyQuests = new List<Quest>();
       
        public override void Entry(IModHelper helper) {
            Config = Helper.ReadConfig<ModConfig>();

            if (Config.NeverExpires) {
                MenuEvents.MenuChanged += MenuEvents_MenuChanged;
            }
            MenuEvents.MenuClosed += MenuEvents_MenuClosed;
        }

        private void MenuEvents_MenuChanged(object sender, EventArgsClickableMenuChanged e) {
            if (e.NewMenu is StardewValley.Menus.QuestLog) {
                updateQuestIcons();
            }
        }
      
        private void MenuEvents_MenuClosed(object sender, EventArgsClickableMenuClosed e) {
            if (e.PriorMenu is StardewValley.Menus.Billboard) {
                updateQuest();
            }
            else if (Config.NeverExpires && e.PriorMenu is StardewValley.Menus.QuestLog) {
                resetQuestIcons();
            }
        }

        // Update quests when leaving the billboard
        private void updateQuest() {
            Quest currentDailyQuest = Game1.player.questLog.FirstOrDefault(quest =>
                quest.dailyQuest && quest.Equals(Game1.questOfTheDay)
            );
            if (currentDailyQuest != null) {
                int daysLeft = Config.NeverExpires ? InfiniteDays : Config.DaysToExpiration;
                currentDailyQuest.daysLeft.Value = daysLeft;
            }
        }

        // Make all quests not daily temporarily to set the icon to (!)
        private void updateQuestIcons() {
            foreach (Quest quest in Game1.player.questLog) {
                if (quest.dailyQuest) {
                    dailyQuests.Add(quest);
                    quest.dailyQuest.Value = false;
                }
            }
        }

        private void resetQuestIcons() {
            foreach (Quest quest in dailyQuests) {
                quest.dailyQuest.Value = true;
            }
            dailyQuests.Clear();
        }
    }
}
