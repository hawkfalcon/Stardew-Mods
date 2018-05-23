namespace BetterJunimos {
    internal class ModConfig {
        public JunimoCapability JunimoCapabilities { get; set; } = new JunimoCapability();
        internal class JunimoCapability {
            public bool PlantCrops { get; set; } = true;
            public bool FertilizeCrops { get; set; } = true;
        }

        public JunimoImprovement JunimoImprovements { get; set; } = new JunimoImprovement();
        internal class JunimoImprovement {
            public bool CanWorkInRain { get; set; } = true;
            public int WorkRangeRadius { get; set; } = 8;   
            public bool ConsumeItemsFromChest { get; set; } = true;
        }

        public JunimoPayments JunimoPayment { get; set; } = new JunimoPayments();
        internal class JunimoPayments {
            public bool WorkForWages { get; set; } = false;
            public int ForagePaymentAmount { get; set; } = 1;
        }
    }
}
