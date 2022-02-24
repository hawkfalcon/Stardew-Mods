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
        public Dictionary<string, ProgressionItem> Progress { get; set; } = new Dictionary<string, ProgressionItem>();
    }

    public class JunimoProgression {
        private const int InitialJunimosLimit = 3;
        private const int MoreJunimosLimit = 6;

        private ProgressionData _progData;
        private Dictionary<string, UnlockCost> UnlockCosts { get; set; }

        private readonly IMonitor _monitor;
        private readonly IModHelper _helper;

        private readonly Dictionary<int, List<int>> _junimoPaymentsToday = new();

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

            _helper.Data.WriteJsonFile("assets/unlock_costs_recycled.json", UnlockCosts);
        }

        internal void Configure(ModConfig config, ProgressionData progData) {
            BetterJunimos.Config = config;
            _progData = progData;
            foreach (var progression in Progressions()) {
                if (! progData.Progress.ContainsKey(progression)) {
                    progData.Progress[progression] = new ProgressionItem();
                }
            }
        }

        private bool Unlocked(IJunimoAbility ability) {
            if (!_progData.Progress.ContainsKey(ability.AbilityName())) {
                return true;
            }
            return Unlocked(ability.AbilityName());
        }

        private bool Unlocked(string progression) {
            if (_progData == null) {
                _monitor.Log("Unlocked: ProgData is null", LogLevel.Warn);
                return false;
            }
            if (_progData.Progress == null) {
                _monitor.Log("Unlocked: ProgData.Progress is null", LogLevel.Warn);
                return false;
            }
            if (!_progData.Progress.ContainsKey(progression)) {
                _monitor.Log($"Unlocked: unknown progression {progression}", LogLevel.Warn);
                return true;
            }
            return _progData.Progress[progression].Unlocked;
        }

        public void SetUnlocked(string progression) {
            if (!_progData.Progress.ContainsKey(progression)) {
                _monitor.Log($"SetUnlocked: unknown progression {progression}", LogLevel.Error);
                return;
            }
            _progData.Progress[progression].Unlocked = true; 
        }

        private bool Prompted(IJunimoAbility ability) {
            if (!_progData.Progress.ContainsKey(ability.AbilityName())) {
                return false;
            }
            return Prompted(ability.AbilityName());
        }

        private bool Prompted(string progression) {
            if (!_progData.Progress.ContainsKey(progression)) {
                _monitor.Log($"Prompted: unknown progression {progression}", LogLevel.Warn);
                return false;
            }
            return _progData.Progress[progression].Prompted;
        }

        public void SetPrompted(string progression) {
            if (!_progData.Progress.ContainsKey(progression)) {
                _monitor.Log($"SetPrompted: unknown progression {progression}", LogLevel.Error);
                throw new ArgumentOutOfRangeException($"SetPrompted: unknown progression {progression}");
            }
            _progData.Progress[progression].Prompted = true;
        }

        private bool LockedAndPrompted(string progression) {
            return !Unlocked(progression) && Prompted(progression);
        }

        private bool LockedAndNotPrompted(string progression) {
            return !Unlocked(progression) && !Prompted(progression);
        }

        // is it enabled in config?
        private bool Enabled(string progression) {
            switch (progression) {
                case "MoreJunimos":
                    return BetterJunimos.Config.JunimoHuts.MaxJunimos > InitialJunimosLimit ;
                case "UnlimitedJunimos":
                    return BetterJunimos.Config.JunimoHuts.MaxJunimos > MoreJunimosLimit;
                case "WorkFaster":
                    return BetterJunimos.Config.JunimoImprovements.WorkFaster;
                case "CanWorkInWinter":
                    return BetterJunimos.Config.JunimoImprovements.CanWorkInWinter;
                case "CanWorkInRain":
                    return BetterJunimos.Config.JunimoImprovements.CanWorkInRain;
                case "CanWorkInEvenings":
                    return BetterJunimos.Config.JunimoImprovements.CanWorkInEvenings;
                case "ReducedCostToConstruct":
                    return BetterJunimos.Config.JunimoHuts.ReducedCostToConstruct;
                default:
                    return true;
            }
        }
        public int MaxJunimosUnlocked {
            get {
                if (!BetterJunimos.Config.Progression.Enabled) return BetterJunimos.Config.JunimoHuts.MaxJunimos;
                if (Unlocked("UnlimitedJunimos")) return BetterJunimos.Config.JunimoHuts.MaxJunimos;
                if (Unlocked("MoreJunimos")) return Math.Min(MoreJunimosLimit, BetterJunimos.Config.JunimoHuts.MaxJunimos);
                return Math.Min(InitialJunimosLimit, BetterJunimos.Config.JunimoHuts.MaxJunimos);
            }
        }

        public bool ReducedCostToConstruct { 
            get {
                if (!BetterJunimos.Config.JunimoHuts.ReducedCostToConstruct) return false;
                if (!BetterJunimos.Config.Progression.Enabled) return true;
                if (_progData is null) return BetterJunimos.Config.JunimoHuts.ReducedCostToConstruct;
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
            PromptForAbility(ability);
            return Unlocked(ability);
        }

        public void PromptForCanWorkInEvenings() {
            if (!BetterJunimos.Config.Progression.Enabled || Unlocked("CanWorkInEvenings") || Prompted("CanWorkInEvenings")) return;
            if (!BetterJunimos.Config.JunimoImprovements.CanWorkInEvenings) return;
            DisplayPromptFor("CanWorkInEvenings");
        }

        public void DayStartedProgressionPrompt(bool isWinter, bool isRaining) {
            if (!BetterJunimos.Config.Progression.Enabled) return;

            if (isWinter && LockedAndNotPrompted("CanWorkInWinter") && BetterJunimos.Config.JunimoImprovements.CanWorkInWinter) {
                DisplayPromptFor("CanWorkInWinter");
            }
            else if (isRaining && LockedAndNotPrompted("CanWorkInRain") && BetterJunimos.Config.JunimoImprovements.CanWorkInRain) {
                DisplayPromptFor("CanWorkInRain");
            }
            else if (BetterJunimos.Config.JunimoHuts.MaxJunimos >= MaxJunimosUnlocked && LockedAndNotPrompted("MoreJunimos")) {
                DisplayPromptFor("MoreJunimos");
            }
            else if (BetterJunimos.Config.JunimoHuts.MaxJunimos >= MaxJunimosUnlocked && Unlocked("MoreJunimos") && LockedAndNotPrompted("UnlimitedJunimos")) {
                DisplayPromptFor("UnlimitedJunimos");
            }
            else if (LockedAndNotPrompted("WorkFaster") && BetterJunimos.Config.JunimoImprovements.WorkFaster) {
                DisplayPromptFor("WorkFaster");
            }
            else if (LockedAndNotPrompted("ReducedCostToConstruct") && BetterJunimos.Config.JunimoHuts.ReducedCostToConstruct) {
                DisplayPromptFor("ReducedCostToConstruct");
            }
        }

        private void PromptForAbility(IJunimoAbility ability) {
            if (Unlocked(ability) || Prompted(ability)) return;
            var an = ability.AbilityName();
            if (! _progData.Progress.ContainsKey(an)) {
                _monitor.Log($"PromptForAbility: unknown ability {an}", LogLevel.Warn);
                return;
            }
            DisplayPromptFor(an);
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
            var textKey = $"{ability}PromptText";
            var displayName = new SObject(item, stack).DisplayName;
            var prompt = _helper.Translation.Get(textKey).ToString();
            if (prompt.Contains("no translation")) {
                prompt = $"For {{{{qty}}}} {{{{item}}}}, the Junimos can {ability.ToLower()}";
                _monitor.Log($"GetPromptText: no translation for {textKey}", LogLevel.Debug);
            }
            return prompt.Replace("{{qty}}", stack.ToString()).Replace("{{item}}", displayName);
        }

        private string GetSuccessText(string ability) {
            var textKey = $"{ability}SuccessText";
            var prompt = _helper.Translation.Get(textKey).ToString();
            if (!prompt.Contains("no translation")) return prompt;
            prompt = $"Now the Junimos can {ability.ToLower()}";
            _monitor.Log($"GetSuccessText: no translation for {textKey}", LogLevel.Debug);
            return prompt;
        }

        public void ReceiveProgressionItems(JunimoHut hut) {
            var chest = hut.output.Value;

            foreach (var ability in Progressions().Where(LockedAndPrompted).Where(ability => ReceiveItems(chest, ability)))
            {
                SetUnlocked(ability);
                Util.SendMessage(GetSuccessText(ability));
            }
        }

        private int ItemForAbility(string ability) {
            if (UnlockCosts.ContainsKey(ability)) return UnlockCosts[ability].Item;
            _monitor.Log($"ItemForAbility got a request for unknown {ability}", LogLevel.Warn);
            return 268;
        }

        private int StackForAbility(string ability) {
            return !UnlockCosts.ContainsKey(ability) ? 1 : UnlockCosts[ability].Stack;
        }

        private bool ReceiveItems(Chest chest, string ability) {
            return ReceiveItems(chest, StackForAbility(ability), ItemForAbility(ability));
        }

        private bool ReceiveItems(Chest chest, int needed, int index) {
             //Monitor.Log($"ReceiveItems wants {needed} of [{index}]", LogLevel.Debug);

            if (needed == 0) return true;
            if (!_junimoPaymentsToday.TryGetValue(index, out var items)) {
                items = new List<int>();
                _junimoPaymentsToday[index] = items;
            }
            var paidSoFar = items.Count;
            //Monitor.Log($"ReceiveItems got {paidSoFar} of [{index}] already", LogLevel.Debug);
            
            if (paidSoFar == needed) return true;

            foreach (var unused in Enumerable.Range(paidSoFar, needed)) {
                var foundItem = chest.items.FirstOrDefault(item => item != null && item.ParentSheetIndex == index);
                if (foundItem == null) continue;
                items.Add(foundItem.ParentSheetIndex);
                Util.RemoveItemFromChest(chest, foundItem);
            }

            //Monitor.Log($"ReceiveItems finished with {items.Count()} of [{index}]", LogLevel.Debug);
            return items.Count >= needed;
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
                if (!Enabled(progression)) ps += $": {Get("tracker.current")}";
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

            _monitor.Log($"{Get("debug.locked-by-progression")} [{hut.tileX} {hut.tileY}] ({id}):",
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