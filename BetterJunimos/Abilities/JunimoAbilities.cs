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

        /*
         * Add an IJunimoAbility to the list of possible actions
         */
        public void RegisterJunimoAbility(IJunimoAbility ability) {
            JunimoCapabilities.Add(ability);
        }

        // Can the Junimo use a capability/ability here
        public bool IsActionable(Vector2 pos, Guid id) {
            return IdentifyJunimoAbility(pos, id) != null;
        }

        public IJunimoAbility IdentifyJunimoAbility(Vector2 pos, Guid id) {
            Farm farm = Game1.getFarm();
            foreach (IJunimoAbility junimoAbility in JunimoCapabilities) {
                if (junimoAbility.IsActionAvailable(farm, pos)) {
                    Console.WriteLine("I" + junimoAbility.AbilityName());
                    int requiredItem = junimoAbility.RequiredItem();
                    Console.WriteLine("J" + requiredItem + " " + id);
                    if (requiredItem == 0 || ItemInHut(id, requiredItem)) {
                        return junimoAbility;
                    }
                }
            }
            return null;
        }

        public bool PerformAction(IJunimoAbility ability, Guid id, Vector2 pos, JunimoHarvester junimo) {
            JunimoHut hut = Util.GetHutFromId(id);
            Chest chest = hut.output.Value;
            Farm farm = Game1.getFarm();

            Console.WriteLine("P" + ability.AbilityName());


            bool success = ability.PerformAction(farm, pos, junimo, chest);
            int requiredItem = ability.RequiredItem();
            if (requiredItem != 0) {
                UpdateHutContainsItemCategory(id, chest, requiredItem);
            }

            return success;
        }

        public bool ItemInHut(Guid id, int item) {
            return ItemsInHuts[id][item];
        }

        internal void UpdateHutItems(Guid id) {
            JunimoHut hut = Util.GetHutFromId(id);
            Chest chest = hut.output.Value;

            UpdateHutContainsItemCategory(id, chest, SObject.fertilizerCategory);
            UpdateHutContainsItemCategory(id, chest, SObject.SeedsCategory);
        }

        public void UpdateHutContainsItemCategory(Guid id, Chest chest, int itemCategory) {
            if (!ItemsInHuts.ContainsKey(id)) {
                ItemsInHuts.Add(id, new Dictionary<int, bool>());
            }
            Console.WriteLine("R" + itemCategory + " " + id);
            ItemsInHuts[id][itemCategory] = chest.items.Any(item => item.Category == itemCategory);
        }
    }
}
