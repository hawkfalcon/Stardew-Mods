using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Objects;
using System.Collections.Generic;

namespace BetterJunimos.Abilities {
    /* 
     * Provides abilities for Junimos 
     */    
    public interface IJunimoAbility {
        /*
         * What is the name of this ability 
         */
        String AbilityName();

        /*
         * Is the action available at the position? E.g. is the crop ready to harvest
         */
        bool IsActionAvailable(Farm farm, Vector2 pos, Guid guid);

        /*
         * Action to take if it is available, return false if action failed
         */
        bool PerformAction(Farm farm, Vector2 pos, JunimoHarvester junimo, Guid guid);

        /*
         * Does this action require specific items (or SObject.SeedsCategory, etc)?
         * Return empty list if no item needed
         */
        List<int> RequiredItems();
    }
}
