using System.Linq;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using SObject = StardewValley.Object;
using System;
using StardewValley.Characters;
using System.Collections.Generic;
using BetterJunimos.Abilities;
using StardewValley.Objects;

namespace BetterJunimos.Utils {
    public class JunimoAbilities {
        internal Dictionary<String, bool> EnabledAbilities;

        private List<IJunimoAbility> JunimoCapabilities = new List<IJunimoAbility>();

        public static Dictionary<Guid, Dictionary<int, bool>> ItemsInHuts = new Dictionary<Guid, Dictionary<int, bool>>();
        private HashSet<int> RequiredItems = new HashSet<int> { SObject.fertilizerCategory, SObject.SeedsCategory };

        public JunimoAbilities(Dictionary<string, bool> EnabledAbilities) {
            this.EnabledAbilities = EnabledAbilities;
            RegisterDefaultAbilites();
        }

        // register built in abilities, in order
        private void RegisterDefaultAbilites() {
            List<IJunimoAbility> DefaultAbilities = new List<IJunimoAbility> {
                new FertilizeAbility(),
                new WaterAbility(),
                new PlantCropsAbility(),
                new HarvestCropsAbility(),
                new HarvestBushesAbility(),
                new HarvestForageCropsAbility(), 
                new ClearDeadCropsAbility()
            };
            foreach(IJunimoAbility junimoAbility in DefaultAbilities) {
                RegisterJunimoAbility(junimoAbility);
            }
        }

        /*
         * Add an IJunimoAbility to the list of possible actions if allowed
         */
        public void RegisterJunimoAbility(IJunimoAbility junimoAbility) {
            string name = junimoAbility.AbilityName();
            if (!EnabledAbilities.ContainsKey(name)) {
                EnabledAbilities.Add(name, true);
            }
            if (EnabledAbilities[name]) {
                JunimoCapabilities.Add(junimoAbility);
                RequiredItems.UnionWith(junimoAbility.RequiredItems());
            }
        }

        // Can the Junimo use a capability/ability here
        public bool IsActionable(Vector2 pos, Guid id) {
            return IdentifyJunimoAbility(pos, id) != null;
        }

        public IJunimoAbility IdentifyJunimoAbility(Vector2 pos, Guid id) {
            Farm farm = Game1.getFarm();
            foreach (IJunimoAbility junimoAbility in JunimoCapabilities) {
                if (junimoAbility.IsActionAvailable(farm, pos)) {
                    List<int> requiredItems = junimoAbility.RequiredItems();
                    if (requiredItems.Count == 0 || ItemInHut(id, requiredItems)) {

                        // is this ability available in current progression?
                        // CanUseAbility also has side-effect of prompting user to unlock needed abilities
                        if (Util.Progression.CanUseAbility(junimoAbility)) {
                            return junimoAbility;
                        }
                    }
                }
            }
            return null;
        }

        public bool PerformAction(IJunimoAbility ability, Guid id, Vector2 pos, JunimoHarvester junimo) {
            JunimoHut hut = Util.GetHutFromId(id);
            Chest chest = hut.output.Value;
            Farm farm = Game1.getFarm();

            bool success = ability.PerformAction(farm, pos, junimo, chest);
            List<int> requiredItems = ability.RequiredItems();
            if (requiredItems.Count > 0) {
                UpdateHutContainsItems(id, chest, requiredItems);
            }

            return success;
        }

        public bool ItemInHut(Guid id, int item) {
            return ItemsInHuts[id][item];
        }

        public bool ItemInHut(Guid id, List<int> items) {
            foreach (int item in items) {
                if (ItemInHut(id, item)) return true;
            }
            return false;
        }

        internal void UpdateHutItems(Guid id) {
            JunimoHut hut = Util.GetHutFromId(id);
            Chest chest = hut.output.Value;
            UpdateHutContainsItems(id, chest, RequiredItems.ToList<int>());
        }

        public void UpdateHutContainsItems(Guid id, Chest chest, List<int> items) {
            foreach (int item_id in items) {
                if (!ItemsInHuts.ContainsKey(id)) {
                    ItemsInHuts.Add(id, new Dictionary<int, bool>());
                }

                if (item_id > 0) {
                    ItemsInHuts[id][item_id] = chest.items.Any(item =>
                        item != null && item.ParentSheetIndex == item_id &&
                        !(Util.Config.JunimoImprovements.AvoidPlantingCoffee && item.ParentSheetIndex == Util.CoffeeId)
                    );
                }
                else {
                    UpdateHutContainsItemCategory(id, chest, item_id);
                }
            }
        }

        public void UpdateHutContainsItemCategory(Guid id, Chest chest, int itemCategory) {
            if (!ItemsInHuts.ContainsKey(id)) {
                ItemsInHuts.Add(id, new Dictionary<int, bool>());
            }
            ItemsInHuts[id][itemCategory] = chest.items.Any(item =>
                item != null && item.Category == itemCategory && 
                !(Util.Config.JunimoImprovements.AvoidPlantingCoffee && item.ParentSheetIndex == Util.CoffeeId)
            );
        }
    }
}