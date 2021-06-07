using System;
using System.Collections.Generic;
using StardewModdingAPI;

namespace BetterJunimosForestry {
    internal class ModConfig {
        public Dictionary<string, bool> JunimoAbilites { get; set; } = new Dictionary<string, bool>();

        public FunSettings FunChanges { get; set; } = new FunSettings();
        internal class FunSettings {
            public bool InfiniteJunimoInventory { get; set; } = false;
            public bool PlantWildTreesFullSize { get; set; } = false;
            public bool PlantFruitTreesFullSize { get; set; } = false;
        }

        public JunimoProgression Progression { get; set; } = new JunimoProgression();
        internal class JunimoProgression {
            public bool Enabled { get; set; } = true;

            public ChopTreesPT ChopTrees { get; set; } = new ChopTreesPT();
            internal class ChopTreesPT {
                public int Item { get; set; } = 268; // starfruit
                public int Stack { get; set; } = 3;
            }
            public CollectDroppedObjectsPT CollectDroppedObjects { get; set; } = new CollectDroppedObjectsPT();
            internal class CollectDroppedObjectsPT {
                public int Item { get; set; } = 268; // starfruit
                public int Stack { get; set; } = 5;
            }
            public WorkInRainPT CollectFromTappersTrees { get; set; } = new WorkInRainPT();
            internal class WorkInRainPT {
                public int Item { get; set; } = 771; // fiber
                public int Stack { get; set; } = 40;
            }
            public WorkInWinterPT HarvestDebris { get; set; } = new WorkInWinterPT();
            internal class WorkInWinterPT {
                public int Item { get; set; } = 440; // wool
                public int Stack { get; set; } = 6;
            }
            public WorkInEveningsPT HarvestFruitTrees { get; set; } = new WorkInEveningsPT();
            internal class WorkInEveningsPT {
                public int Item { get; set; } = 768; // solar essence
                public int Stack { get; set; } = 2;
            }
            public HarvestGrassPT HarvestGrass { get; set; } = new HarvestGrassPT();
            internal class HarvestGrassPT {
                public int Item { get; set; } = 771; // fiber
                public int Stack { get; set; } = 40;
            }
            public PlantFruitTreesPT PlantFruitTrees { get; set; } = new PlantFruitTreesPT();
            internal class PlantFruitTreesPT {
                public int Item { get; set; } = 336; // gold bar
                public int Stack { get; set; } = 5;
            }
            public PlantPT PlantWildTrees { get; set; } = new PlantPT();
            internal class PlantPT {
                public int Item { get; set; } = 335; // iron bar
                public int Stack { get; set; } = 5;
            }
            public InstallTappersPT InstallTappers { get; set; } = new InstallTappersPT();
            internal class InstallTappersPT {
                public int Item { get; set; } = 88; // coconut
                public int Stack { get; set; } = 10;
            }
            public CollectFromTappersPT CollectFromTappers { get; set; } = new CollectFromTappersPT();
            internal class CollectFromTappersPT {
                public int Item { get; set; } = 330; // clay
                public int Stack { get; set; } = 20;
            }
            public HarvestSeedsFromWildTreesPT HarvestSeedsFromWildTrees { get; set; } = new HarvestSeedsFromWildTreesPT();
            internal class HarvestSeedsFromWildTreesPT {
                public int Item { get; set; } = 372; // clam
                public int Stack { get; set; } = 6;
            }      
        }
    }
}
