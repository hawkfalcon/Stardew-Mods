using System.Linq;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Buildings;
using System;
using StardewValley.Characters;
using System.Collections.Generic;
using BetterJunimos.Abilities;
using StardewValley.Objects;
using SObject = StardewValley.Object;
using StardewModdingAPI;

namespace BetterJunimos.Utils {
    public class JunimoAbilities {
        private Dictionary<String, bool> _enabledAbilities;
        private readonly IMonitor _monitor;
        
        /* ACTION FAILURE COOLDOWNS:
         Intent: don't retry actions that failed recently (in the last game hour)
         
         Rationale: if IsActionAvailable says yes but PerformAction says no, the Junimo will retry that action all day
         and appear to be stuck on a tile. 
         While these two functions should be as consistent as possible, it's impossible for IsActionAvailable to 
         predict all the ways that PerformAction could fail. 

         Registering a failed action:
         Call ActionFailed. Currently called in the Harmony patch PatchHarvestAttemptToCustom
         
         Resetting cooldowns:
         Call ResetCooldowns. Currently called by the start-of-day event handler and the hut-menu-closed handler
         (player may have added seeds etc so that failed actions will now succeed)           
         */
        private static readonly Dictionary<Tuple<IJunimoAbility, Vector2>, int> FailureCooldowns = new();

        private readonly List<IJunimoAbility> _junimoCapabilities = new();

        private static readonly Dictionary<Guid, Dictionary<int, bool>> ItemsInHuts = new();
        private readonly HashSet<int> _requiredItems = new() { SObject.fertilizerCategory, SObject.SeedsCategory };

        public JunimoAbilities(Dictionary<string, bool> enabledAbilities, IMonitor monitor) {
            _enabledAbilities = enabledAbilities;
            _monitor = monitor;
            RegisterDefaultAbilities();
        }

        // register built in abilities, in order
        private void RegisterDefaultAbilities() {
            var defaultAbilities = new List<IJunimoAbility> {
                new WaterAbility(),
                new FertilizeAbility(),
                new PlantCropsAbility(_monitor),
                new HarvestCropsAbility(_monitor),
                new HarvestBushesAbility(),
                new HarvestForageCropsAbility(), 
                new ClearDeadCropsAbility()
            };
            foreach(var junimoAbility in defaultAbilities) {
                RegisterJunimoAbility(junimoAbility);
            }
        }

        /*
         * Add an IJunimoAbility to the list of possible actions if allowed
         */
        public void RegisterJunimoAbility(IJunimoAbility junimoAbility) {
            var name = junimoAbility.AbilityName();
            if (!BetterJunimos.Config.JunimoAbilities.ContainsKey(name)) {
                BetterJunimos.Config.JunimoAbilities.Add(name, true);
            }

            if (!BetterJunimos.Config.JunimoAbilities[name]) return;
            _junimoCapabilities.Add(junimoAbility);
            _requiredItems.UnionWith(junimoAbility.RequiredItems());
        }

        // Can the Junimo use a capability/ability here
        public bool IsActionable(Vector2 pos, Guid id) {
            return IdentifyJunimoAbility(pos, id) != null;
        }

        public IJunimoAbility IdentifyJunimoAbility(Vector2 pos, Guid id) {
            // Monitor.Log($"IdentifyJunimoAbility [for {caller}] at [{pos.X} {pos.Y}] for {id}", LogLevel.Info);
            var farm = Game1.getFarm();

            foreach (var ability in _junimoCapabilities) {
                if (ActionCoolingDown(ability, pos)) continue;
                if (!ItemInHut(id, ability.RequiredItems())) continue;
                if (!ability.IsActionAvailable(farm, pos, id)) continue;
                if (!Util.Progression.CanUseAbility(ability)) continue;
                return ability;
            }

            return null;
        }

        public IJunimoAbility AvailableJunimoAbility(Vector2 pos, Guid id, string caller) {
            var farm = Game1.getFarm();
            if (caller == "ListAvailableActions") {
                _monitor.Log($"      AvailableJunimoAbility [for {caller}] at [{pos.X} {pos.Y}]", LogLevel.Debug);
            }
            foreach (var ability in _junimoCapabilities) {
                bool available = ability.IsActionAvailable(farm, pos, id);
                if (caller == "ListAvailableActions") {
                    // Monitor.Log($"    AvailableJunimoAbility [for {caller}] considering {ability.AbilityName()} at [{pos.X} {pos.Y}]", LogLevel.Debug);
                    if (available || ability.AbilityName() == "HoeAroundTrees") {
                        _monitor.Log($"            AvailableJunimoAbility [for {caller}]: {ability.AbilityName()} at [{pos.X} {pos.Y}] available {available}", LogLevel.Debug);
                    }
                }
                if (available) return ability;
            }
            return null;
        }

        public bool PerformAction(IJunimoAbility ability, Guid id, Vector2 pos, JunimoHarvester junimo) {
            JunimoHut hut = Util.GetHutFromId(id);
            Chest chest = hut.output.Value;
            Farm farm = Game1.getFarm();

            bool success = ability.PerformAction(farm, pos, junimo, id);
            List<int> requiredItems = ability.RequiredItems();
            if (requiredItems.Count > 0) {
                UpdateHutContainsItems(id, chest, requiredItems);
            }

            // Monitor.Log($"Performed {ability.AbilityName()} at [{pos.X} {pos.Y}]: {success}", LogLevel.Debug);
            //
            // if (!success) {
            //     Monitor.Log($"Failed to do {ability.AbilityName()} at [{pos.X} {pos.Y}] {pos}");
            // }
            return success;
        }

        public void ActionFailed(IJunimoAbility ability, Vector2 pos) {
            _monitor.Log($"Action {ability.AbilityName()} at [{pos.X} {pos.Y}] failed", LogLevel.Debug);
            var cd = new Tuple<IJunimoAbility, Vector2>(ability, pos);
            FailureCooldowns[cd] = Game1.timeOfDay;
        }

        public void ResetCooldowns() {
            FailureCooldowns.Clear();
        }

        public static bool ActionCoolingDown(IJunimoAbility ability, Vector2 pos) {
            Tuple<IJunimoAbility, Vector2> cd = new Tuple<IJunimoAbility, Vector2>(ability, pos);
            if (FailureCooldowns.TryGetValue(cd, out int failureTime)) {
                if (failureTime > Game1.timeOfDay - 1000) {
                    BetterJunimos.SMonitor.Log($"Action {ability.AbilityName()} at [{pos.X} {pos.Y}] is in cooldown");
                    return true;
                }
            }
            return false;
        }
        
        public static bool ItemInHut(Guid id, int item) {
            return ItemsInHuts[id][item];
        }

        public static bool ItemInHut(Guid id, List<int> items) {
            if (items.Count == 0) return true;
            foreach (var item in items) {
                if (ItemInHut(id, item)) return true;
            }
            return false;
        }

        internal void UpdateHutItems(Guid id) {
            var hut = Util.GetHutFromId(id);
            var chest = hut.output.Value;
            UpdateHutContainsItems(id, chest, _requiredItems.ToList<int>());
        }

        private void UpdateHutContainsItems(Guid id, Chest chest, List<int> items) {
            foreach (int itemId in items) {
                if (!ItemsInHuts.ContainsKey(id)) {
                    ItemsInHuts.Add(id, new Dictionary<int, bool>());
                }

                if (itemId > 0) {
                    ItemsInHuts[id][itemId] = chest.items.Any(item =>
                        item != null && item.ParentSheetIndex == itemId &&
                        !(BetterJunimos.Config.JunimoImprovements.AvoidPlantingCoffee && item.ParentSheetIndex == Util.CoffeeId)
                    );
                }
                else {
                    UpdateHutContainsItemCategory(id, chest, itemId);
                }
            }
        }

        public void UpdateHutContainsItemCategory(Guid id, Chest chest, int itemCategory) {
            if (!ItemsInHuts.ContainsKey(id)) {
                ItemsInHuts.Add(id, new Dictionary<int, bool>());
            }
            ItemsInHuts[id][itemCategory] = chest.items.Any(item =>
                item != null && item.Category == itemCategory && 
                !(BetterJunimos.Config.JunimoImprovements.AvoidPlantingCoffee && item.ParentSheetIndex == Util.CoffeeId)
            );
        }
    }
}