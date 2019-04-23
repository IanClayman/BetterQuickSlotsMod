#Solved Bugs

- SOLVED: Finishing a stack of consummables
	- Using the last of a stack of consummable items, then switching away and back to its assigned bar causes a bug that prevents that item and all subsequent quickslots from loading
	- Debug error message says that the operation cannot be completed - Lesson: if quickslot linked to quickSlots#[i] fails to load then all quickslots with index > i will also fail to load
	- Implemented solution: Check if inventory.GetOwnedItems(quickSlots#[i]).Count() == 0, and if so set entry to ResourcesPrefabManager.Instance.GetItemPrefab(quickSlots#[i]) instead of
		inventory.GetOwnedItems(quickSlots#[i]).First()

- SOLVED:Free Slot functionality does not actually free the quick slot
	- Quick slot will be repopulated upon switching away from, and then back to, the current quick slot bar
	- Implemented Solution: Modified QuickSlotContextOptions.ActionHasBeenPressed(int _id).  When _id == 1 the "action pressed" is "Free Quick Slot" (_id == 0 is equivalent to "Assign Quick Slot")
	
- SOLVED: Using CustomKeybindings, inconsistent behaviour
	- If a quick slot is bound to RT+Y and switch bars is bound to Y, pressing Y in a menu will not switch bars.  However, pressing RT+Y
		will activate the assigned Item AND switch quick slot bars
	- Similarly, if a quickslot is bound to AlphaNum4 and switching btw bars is also bound to AlphaNum4 the quickslot will be activated AND bars will be switched
	- Implemented solution: check if input used to activate any quickslot is being pressed.  If so, do not fire the quickslot bar switch

- SOLVED: items in quickSlots2 will overwrite items in quickSlots1
	- Seems to be caused by exiting the game with quickSlot2 active
	- Implemented solution: 1) Set BarMode to FIRST 2) Clear default behaviour quick bar 3) Load in quick slots from temp array
	
- SOLVED: using a consummable item assigned to a quickslot and then switch between bars causes the assigned item to disappear
	- This doesn't affect the JSON arrays (reloading the game after this disappearance results in the disappeared item showing up correctly
	- Also doesn't affect non-consummable items (putting a weapon in the quickslot, using the assigned button and switching bars results in the weapon still showing up properly
		upon switching back)
	-Implemented solution: Changed quickSlots1 and quickSlots2 from Item[] -> int[].  Rewrote LoadQuickSlotArraysFromJSON() and PopulateSkillBar() to accomodate this change.
		By actually reading in items in PopulateSkillBar instead of LoadQuickSlotArraysFromJSON the assignment of a stack of consummables will always find its target, because the call to
		inventory.GetOwnedItems(quickSlots#[i]).First() will find the UID of the "next" first item as opposed to the already-used first item
		
#Unsolved Bugs

- Fix BetterQuickSlotsMod.cs line 109 (AND line 47)
	- by declaring the string path of the config file to be Mods\BetterQuickSlots_Config.json it ignores the possibility that the name of the folder where mods exist is not "Mods\"
	- for example, BepInEx stores mods in the folder ...\Outward\BepInEx\plugins\

- Multiplayer issue
	- As reported by MrPig#2647: "when a friend joins and you tab to your other bar it deletes both of ya bars and ya gotta reset em"
	- "once there in the game and you reset em they dont go away"
	- Current code seems to detect any time there's a "new" character in the lobby (aka character who doesn't have an entry in the config file).  If it does, it resets the quickslots of all players with the mod, 
		regardless of whether how many people have the mod installed and if the "new" player has the mod or not

- Local multiplayer bug
	- Reported by Shoe#6966: "So when I assign a hotkey everything will look normal to her, but if she cycles her hotkey bars suddenly the one she was just looking at will update to reflect 
		the change I just made to my characters quickslots"
	- Theory: has to do with where charUID is set in PlayerSystem.Start().  The quickslots for that player will be assigned when any player goes to switch between bars
		
		
{"characters":[{"characterUID":"NJsukNThOk6LRDUzLe9BVg","characterName":"Garruk","FirstBarIDs":[8100070,8100360,8100071,8100350,8200120,0,0,5100000],"SecondBarIDs":[4400010,4300110,4200040,4300010,0,0,0,0]}]}