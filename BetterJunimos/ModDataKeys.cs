namespace BetterJunimos {
    /// <summary>
    /// Constants used throughout the BetterJunimos mod
    /// </summary>
    internal static class ModDataKeys {
        // Mod unique ID prefix
        private const string ModPrefix = "hawkfalcon.BetterJunimos";

        // Chest identification
        public const string JunimoChestMarker = "JunimoChest";

        // Save data keys
        public const string CropMapsSaveKey = ModPrefix + ".CropMaps";
        public const string ProgressionDataSaveKey = ModPrefix + ".ProgressionData";

        // ModData keys for progression
        public const string HarvestCropsUnlockedKey = ModPrefix + ".ProgressionData.HarvestCrops.Unlocked";

        /// <summary>
        /// Gets the prompted key for a progression item
        /// </summary>
        public static string GetPromptedKey(string itemName) => $"{ModPrefix}.ProgressionData.{itemName}.Prompted";

        /// <summary>
        /// Gets the unlocked key for a progression item
        /// </summary>
        public static string GetUnlockedKey(string itemName) => $"{ModPrefix}.ProgressionData.{itemName}.Unlocked";

        /// <summary>
        /// Gets the Junimo chest marker key with mod manifest unique ID
        /// </summary>
        public static string GetJunimoChestKey(string manifestUniqueId) => $"{manifestUniqueId}/{JunimoChestMarker}";
    }
}