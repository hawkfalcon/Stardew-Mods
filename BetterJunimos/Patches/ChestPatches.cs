using Harmony;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace BetterJunimos.Patches {
    // grabItemFromChest
    [HarmonyPriority(Priority.Low)]
    public class ChestPatchFrom {
        public static bool Prefix(Chest __instance, Item item, Farmer who) {
            if (!who.couldInventoryAcceptThisItem(item))
                return false;
            
            object special = (object)null;
            int specialNum = -1;
            if (Game1.activeClickableMenu is ItemGrabMenu menu) {
                special = (object)menu.specialObject;
                specialNum = menu.whichSpecialButton;
            }
            __instance.items.Remove(item);
            __instance.clearNulls();
            Game1.activeClickableMenu = new ItemGrabMenu(__instance.items, false, true, 
                new InventoryMenu.highlightThisItem(InventoryMenu.highlightAllItems), new ItemGrabMenu.behaviorOnItemSelect(__instance.grabItemFromInventory), 
                (string)null, new ItemGrabMenu.behaviorOnItemSelect(__instance.grabItemFromChest), false, true, true, true, true, 1, (Item)__instance, specialNum, special);
            return false;
        }
    }

    // grabItemFromInventory
    [HarmonyPriority(Priority.Low)]
    public class ChestPatchTo {
        public static bool Prefix(Chest __instance, Item item, Farmer who) {
            if (item.Stack == 0)
                item.Stack = 1;
            Item obj = __instance.addItem(item);
            if (obj == null)
                who.removeItemFromInventory(item);
            else
                obj = who.addItemToInventory(obj);
            __instance.clearNulls();
            int id = Game1.activeClickableMenu.currentlySnappedComponent != null ? Game1.activeClickableMenu.currentlySnappedComponent.myID : -1;

            object special = (object)null;
            int specialNum = -1;
            if (Game1.activeClickableMenu is ItemGrabMenu menu) {
                special = (object)menu.specialObject;
                specialNum = menu.whichSpecialButton;
            }
            Game1.activeClickableMenu = (IClickableMenu)new ItemGrabMenu(__instance.items, false, true, new InventoryMenu.highlightThisItem(InventoryMenu.highlightAllItems), 
                new ItemGrabMenu.behaviorOnItemSelect(__instance.grabItemFromInventory), (string)null, new ItemGrabMenu.behaviorOnItemSelect(__instance.grabItemFromChest), 
                false, true, true, true, true, 1, (Item)__instance, specialNum, special);
            (Game1.activeClickableMenu as ItemGrabMenu).heldItem = obj;
            if (id == -1)
                return false;
            Game1.activeClickableMenu.currentlySnappedComponent = Game1.activeClickableMenu.getComponentWithID(id);
            Game1.activeClickableMenu.snapCursorToCurrentSnappedComponent();

            return false;
        }
    }
}
