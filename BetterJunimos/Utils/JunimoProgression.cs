using System;
using System.Collections.Generic;
using System.Linq;
using BetterJunimos.Abilities;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Objects;
using static System.String;
using SObject = StardewValley.Object;

namespace BetterJunimos.Utils {
    public class ProgressionItem {
        public bool Prompted;
        public bool Unlocked;
    }

    public class UnlockCost {
        public int Item { get; set; }
        public int Stack { get; set; }

        public string Remarks { get; set; }
    }

    public class ProgressionData {
        
        // do not use, deprecated
        public Dictionary<string, ProgressionItem> Progress { get; set; } = new();
    }

    public class JunimoProgression {
        private const int InitialJunimosLimit = 3;
        private const int MoreJunimosLimit = 6;
        internal Dictionary<string, UnlockCost> UnlockCosts { get; set; }

        private readonly IMonitor _monitor;
        private readonly IModHelper _helper;
        
        internal JunimoProgression(IMonitor monitor, IModHelper helper) {
            _monitor = monitor;
            _helper = helper;

            UnlockCosts = _helper.Data.ReadJsonFile<Dictionary<string, UnlockCost>>("assets/unlock_costs.json");
            if (UnlockCosts is null) {
                UnlockCosts = new Dictionary<string, UnlockCost>();
                monitor.Log("JunimoProgression: did not load UnlockCosts", LogLevel.Error);
            }

            var keys = Join(", ", UnlockCosts.Keys);
            monitor.Log($"JunimoProgression: current UnlockCosts.Keys {keys}", LogLevel.Debug);
        }

        private bool Unlocked(string progression) {
            var farm = Game1.getFarm();
            if (farm == null) return false;
            var k = $"hawkfalcon.BetterJunimos.ProgressionData.{progression}.Unlocked";
            if (farm.modData.TryGetValue(k, out var v)) {
                return v == "1";
            }
            return false;
        }

        public void SetUnlocked(string progression) {
            var farm = Game1.getFarm();
            var k = $"hawkfalcon.BetterJunimos.ProgressionData.{progression}.Unlocked";
            farm.modData[k] = "1";
        }

        private bool Prompted(string progression) {
            var farm = Game1.getFarm();
            var k = $"hawkfalcon.BetterJunimos.ProgressionData.{progression}.Prompted";
            if (farm.modData.TryGetValue(k, out var v)) {
                return v == "1";
            }
            return false;
        }

        public void SetPrompted(string progression) {
            var farm = Game1.getFarm();
            var k = $"hawkfalcon.BetterJunimos.ProgressionData.{progression}.Prompted";
            farm.modData[k] = "1";
        }

        private bool LockedAndPrompted(string progression) {
            return !Unlocked(progression) && Prompted(progression);
        }

        private bool LockedAndNotPrompted(string progression) {
            return !Unlocked(progression) && !Prompted(progression);
        }

        // is it enabled in config?
        private (bool configurable, bool enabled) Enabled(string progression) {
            switch (progression) {
                case "MoreJunimos":
                    return (true, BetterJunimos.Config.JunimoHuts.MaxJunimos > InitialJunimosLimit);
                case "UnlimitedJunimos":
                    return (true, BetterJunimos.Config.JunimoHuts.MaxJunimos > MoreJunimosLimit);
                case "WorkFaster":
                    return (true, BetterJunimos.Config.JunimoImprovements.WorkFaster);
                case "CanWorkInWinter":
                    return (true, BetterJunimos.Config.JunimoImprovements.CanWorkInWinter);
                case "CanWorkInRain":
                    return (true, BetterJunimos.Config.JunimoImprovements.CanWorkInRain);
                case "CanWorkInEvenings":
                    return (true, BetterJunimos.Config.JunimoImprovements.CanWorkInEvenings);
                case "ReducedCostToConstruct":
                    return (true, BetterJunimos.Config.JunimoHuts.ReducedCostToConstruct);
                default:
                    return (false, true);
            }
        }

        public int MaxJunimosUnlocked {
            get {
                if (!BetterJunimos.Config.Progression.Enabled) return BetterJunimos.Config.JunimoHuts.MaxJunimos;
                if (Unlocked("UnlimitedJunimos")) return BetterJunimos.Config.JunimoHuts.MaxJunimos;
                if (Unlocked("MoreJunimos"))
                    return Math.Min(MoreJunimosLimit, BetterJunimos.Config.JunimoHuts.MaxJunimos);
                return Math.Min(InitialJunimosLimit, BetterJunimos.Config.JunimoHuts.MaxJunimos);
            }
        }

        public bool ReducedCostToConstruct {
            get {
                if (!BetterJunimos.Config.JunimoHuts.ReducedCostToConstruct) return false;
                if (!BetterJunimos.Config.Progression.Enabled) return true;
                return Unlocked("ReducedCostToConstruct");
            }
        }

        public bool CanWorkInEvenings {
            get {
                if (!BetterJunimos.Config.JunimoImprovements.CanWorkInEvenings) return false;
                if (!BetterJunimos.Config.Progression.Enabled) return true;
                return Unlocked("CanWorkInEvenings");
            }
        }

        public bool CanWorkInRain {
            get {
                if (!BetterJunimos.Config.JunimoImprovements.CanWorkInRain) return false;
                if (!BetterJunimos.Config.Progression.Enabled) return true;
                return Unlocked("CanWorkInRain");
            }
        }

        public bool CanWorkInWinter {
            get {
                if (!BetterJunimos.Config.JunimoImprovements.CanWorkInWinter) return false;
                if (!BetterJunimos.Config.Progression.Enabled) return true;
                return Unlocked("CanWorkInWinter");
            }
        }

        public bool WorkFaster {
            get {
                if (!BetterJunimos.Config.JunimoImprovements.WorkFaster) return false;
                if (!BetterJunimos.Config.Progression.Enabled) return true;
                return Unlocked("WorkFaster");
            }
        }

        public bool CanUseAbility(IJunimoAbility ability) {
            if (!BetterJunimos.Config.Progression.Enabled) return true;

            var an = ability.AbilityName();
            if (Unlocked(an)) return true;
            if (Prompted(an)) return false;
            
            _monitor.Log($"CanUseAbility: prompting for {an} due CanUseAbility request", LogLevel.Debug);
            DisplayPromptFor(an);
            return false;
        }

        public void PromptForCanWorkInEvenings() {
            if (!BetterJunimos.Config.Progression.Enabled || Unlocked("CanWorkInEvenings") ||
                Prompted("CanWorkInEvenings")) return;
            if (!BetterJunimos.Config.JunimoImprovements.CanWorkInEvenings) return;
            DisplayPromptFor("CanWorkInEvenings");
        }

        public void DayStartedProgressionPrompt(bool isWinter, bool isRaining) {
            if (!BetterJunimos.Config.Progression.Enabled) return;

            if (isWinter && LockedAndNotPrompted("CanWorkInWinter") &&
                BetterJunimos.Config.JunimoImprovements.CanWorkInWinter) {
                DisplayPromptFor("CanWorkInWinter");
            }
            else if (isRaining && LockedAndNotPrompted("CanWorkInRain") &&
                     BetterJunimos.Config.JunimoImprovements.CanWorkInRain) {
                DisplayPromptFor("CanWorkInRain");
            }
            else if (BetterJunimos.Config.JunimoHuts.MaxJunimos >= MaxJunimosUnlocked &&
                     LockedAndNotPrompted("MoreJunimos")) {
                DisplayPromptFor("MoreJunimos");
            }
            else if (BetterJunimos.Config.JunimoHuts.MaxJunimos >= MaxJunimosUnlocked && Unlocked("MoreJunimos") &&
                     LockedAndNotPrompted("UnlimitedJunimos")) {
                DisplayPromptFor("UnlimitedJunimos");
            }
            else if (LockedAndNotPrompted("WorkFaster") && BetterJunimos.Config.JunimoImprovements.WorkFaster) {
                DisplayPromptFor("WorkFaster");
            }
            else if (LockedAndNotPrompted("ReducedCostToConstruct") &&
                     BetterJunimos.Config.JunimoHuts.ReducedCostToConstruct) {
                DisplayPromptFor("ReducedCostToConstruct");
            }
        }

        private void DisplayPromptFor(string progression) {
            var prompt = GetPromptText(progression);
            if (prompt.Length == 0) {
                _monitor.Log($"DisplayPromptFor: did not get progression prompt text for {progression}", LogLevel.Warn);
                return;
            }

            SetPrompted(progression);
            Util.SendMessage(prompt);
        }

        private string GetPromptText(string ability) {
            return GetPromptText(ability, ItemForAbility(ability), StackForAbility(ability));
        }

        private string GetPromptText(string ability, int item, int stack) {
            var textKey = $"prompt.{ability}";
            var displayName = new SObject(item, stack).DisplayName;
            var prompt = Get(textKey);
            if (prompt.Contains("no translation")) {
                prompt = $"For {{{{qty}}}} {{{{item}}}}, the Junimos can {ability.SplitCamelCase().ToLower()}";
                _monitor.Log($"GetPromptText: no translation for {textKey}", LogLevel.Debug);
            }

            return prompt.Replace("{{qty}}", stack.ToString()).Replace("{{item}}", displayName);
        }

        private string GetSuccessText(string ability) {
            var textKey = $"success.{ability}";
            var prompt = Get(textKey);
            if (!prompt.Contains("no translation")) return prompt;
            prompt = $"Now the Junimos can {ability.SplitCamelCase().ToLower()}";
            _monitor.Log($"GetSuccessText: no translation for {textKey}", LogLevel.Debug);
            return prompt;
        }

        public void ReceiveProgressionItems(JunimoHut hut) {
            var chest = hut.output.Value;

            foreach (var ability in Progressions().Where(LockedAndPrompted)) {
                if (!ReceiveItems(chest, ability)) continue;
                SetUnlocked(ability);
                Util.SendMessage(GetSuccessText(ability));
            }
        }

        private int ItemForAbility(string ability) {
            if (UnlockCosts.ContainsKey(ability)) return UnlockCosts[ability].Item;
            _monitor.Log($"ItemForAbility got a request for unknown {ability}", LogLevel.Warn);
            UnlockCosts[ability] = new UnlockCost {Item = 268, Stack = 1, Remarks = "Starfruit"};
            return UnlockCosts[ability].Item;
        }

        private int StackForAbility(string ability) {
            if (UnlockCosts.ContainsKey(ability)) return UnlockCosts[ability].Stack;
            _monitor.Log($"StackForAbility got a request for unknown {ability}", LogLevel.Warn);
            UnlockCosts[ability] = new UnlockCost {Item = 268, Stack = 1, Remarks = "Starfruit"};
            return UnlockCosts[ability].Stack;
        }

        private bool ReceiveItems(Chest chest, string ability) {
            return ReceiveItems(chest, StackForAbility(ability), ItemForAbility(ability));
        }

        private bool ReceiveItems(Chest chest, int needed, int index) {
            // BetterJunimos.SMonitor.Log($"ReceiveItems wants {needed} of [{index}]", LogLevel.Debug);
            if (needed <= 0) return true;

            var inChest = chest.items.Where(item => item != null && item.ParentSheetIndex == index).ToList();

            foreach (var itemStack in inChest) {
                if (itemStack.Stack >= needed) {
                    // BetterJunimos.SMonitor.Log($"    At least {itemStack.Stack} in stack, removing some and unlocking", LogLevel.Debug);
                    Util.RemoveItemFromChest(chest, itemStack, needed);
                    return true;
                } 
                // BetterJunimos.SMonitor.Log($"    Only {itemStack.Stack} in stack, not unlocking", LogLevel.Debug);
            }

            return false;
        }
        
        private int UnlockedCount() {
            var unlocked = 0.0f;
            foreach (var unused in Progressions().Where(Unlocked)) {
                unlocked++;
            }

            var pc = unlocked / Progressions().Count * 100;
            return (int) Math.Round(pc);
        }

        private string ActiveQuests() {
            var quests = (from progression in Progressions()
                where Prompted(progression) && !Unlocked(progression)
                select $"= {GetPromptText(progression)}").ToList();
            return Join("^", quests);
        }

        private string PaymentsDetail() {
            var detail = $"{Get("tracker.payments")}: ";
            if (!BetterJunimos.Config.JunimoPayment.WorkForWages) detail += Get("tracker.working-for-free");
            else if (Util.Payments.WereJunimosPaidToday) detail += Get("tracker.paid-today");
            else detail += Get("tracker.unpaid-not-working");
            return detail;
        }

        private string QuestsDetail() {
            var quests = new List<string> {
                PaymentsDetail(),
                $"{Get("tracker.max-junimos-per-hut")}: {Util.Progression.MaxJunimosUnlocked} {Get("tracker.current")}, {BetterJunimos.Config.JunimoHuts.MaxJunimos} {Get("tracker.configured")}",
                $"{Get("tracker.working-radius")}: {Util.CurrentWorkingRadius} {Get("tracker.current")}, {BetterJunimos.Config.JunimoHuts.MaxRadius} {Get("tracker.configured")}"
            };

            foreach (var progression in Progressions()) {
                var ps = progression;
                var (configurable, enabled) = Enabled(progression);
                
                if (configurable && !enabled) ps += $": {Get("tracker.disabled")}";  // user has disabled
                else if (!BetterJunimos.Config.Progression.Enabled) ps += $": {Get("tracker.enabled")}";
                else if (Unlocked(progression)) ps += $": {Get("tracker.unlocked")}";
                else if (Prompted(progression)) ps += $": {Get("tracker.prompted")}";
                else ps += $": {Get("tracker.not-triggered")}";
                quests.Add(ps);
            }

            return Join("^", quests);
        }

        internal void PromptAllQuests() {
            _monitor.Log(Get("debug.prompting-all-quests"), LogLevel.Info);
            foreach (var progression in Progressions()) {
                SetPrompted(progression);
                _monitor.Log($"    {progression}", LogLevel.Debug);
            }
        }

        public void ShowPerfectionTracker() {
            var quests = ActiveQuests();
            var percentage = UnlockedCount().ToString();

            var prompt = Get("tracker.tracker-title");

            if (quests.Length == 0) quests = Get("tracker.no-abilities-to-unlock");

            var message = prompt
                .Replace("{{percentage}}", percentage)
                .Replace("{{quests}}", quests)
                .Replace("{{details}}", QuestsDetail());
            Game1.drawLetterMessage(message);
        }

        private List<string> Progressions() {
            return UnlockCosts.Keys.ToList();
        }

        internal void ListHuts() {
            _monitor.Log("Huts:", LogLevel.Debug);

            foreach (JunimoHut hut in Util.GetAllHuts()) {
                _monitor.Log($"    [{hut.tileX} {hut.tileY}] {Util.GetHutIdFromHut(hut)}", LogLevel.Debug);
            }
        }

        internal void ListAllAvailableActions() {
            foreach (JunimoHut hut in Util.GetAllHuts()) {
                ListAvailableActions(Util.GetHutIdFromHut(hut));
            }
        }

        // search for crops + open plantable spots
        internal void ListAvailableActions(Guid id) {
            JunimoHut hut = Util.GetHutFromId(id);
            int radius = Util.CurrentWorkingRadius;

            _monitor.Log($"{Get("debug.available-actions-for-hut-at")} [{hut.tileX} {hut.tileY}] ({id}):",
                LogLevel.Debug);
            for (var x = hut.tileX.Value + 1 - radius; x < hut.tileX.Value + 2 + radius; ++x) {
                for (var y = hut.tileY.Value + 1 - radius; y < hut.tileY.Value + 2 + radius; ++y) {
                    var pos = new Vector2(x, y);

                    var ability = Util.Abilities.IdentifyJunimoAbility(pos, id);
                    if (ability == null) {
                        continue;
                    }

                    // these 3 statements don't run unless you remove the `continue` above
                    var cooldown = JunimoAbilities.ActionCoolingDown(ability, pos)
                        ? Get("debug.in-cooldown")
                        : "";
                    var itemsAvail = JunimoAbilities.ItemInHut(id, ability.RequiredItems())
                        ? ""
                        : Get("debug.required-item-unavailable");
                    var progLocked = Util.Progression.CanUseAbility(ability)
                        ? ""
                        : Get("debug.locked-by-progression");

                    _monitor.Log($"    [{pos.X} {pos.Y}] {ability.AbilityName()} {cooldown} {itemsAvail} {progLocked}",
                        LogLevel.Debug);
                }
            }

            Util.SendMessage(Get("debug.actions-logged"));
        }

        public static bool HutOnTile(Vector2 pos) {
            return Game1.getFarm().buildings.Any(b => b is JunimoHut && b.occupiesTile(pos));
        }

        private string Get(string key) {
            return _helper.Translation.Get(key);
        }
    }
}