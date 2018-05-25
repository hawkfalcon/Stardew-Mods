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
            //public int MaxJunimos { get; set; } = 3;
            public int MaxRadius { get; set; } = 8;
        }

        public JunimoPayments JunimoPayment { get; set; } = new JunimoPayments();
        internal class JunimoPayments {
            public bool WorkForWages { get; set; } = false;
            public PaymentAmount DailyWage { get; set; } = new PaymentAmount();
            internal class PaymentAmount {
                public int ForagedItems { get; set; } = 1;
                public int Flowers { get; set; } = 0;
                public int Fruit { get; set; } = 0;
                public int Wine { get; set; } = 0;
            }
        }

        public OtherSettings FunChanges { get; set; } = new OtherSettings();
        internal class OtherSettings {
            public bool JunimosAlwaysHaveLeafUmbrellas { get; set; } = false;
            public bool InfiniteJunimoInventory { get; set; } = false;
        }

    }
}
