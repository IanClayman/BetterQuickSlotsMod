# BetterQuickSlotsMod

List of known bugs / yet-to-be-implemented features:

- BUG: Finishing a stack of consummables
	- Using the last of a stack of consummable items, then switching away and back to its assigned bar causes a bug that prevents that item and all subsequent quickslots from loading
	- Debug error message says that the operation cannot be completed - Lesson: if quickslot linked to quickSlots#[i] fails to load then all quickslots with index > i will also fail to load

- BUG:Free Slot functionality does not actually free the quick slot
	- (Quick slot will be repopulated upon switching away from, and then back to, the current quick slot bar)
	
- BUG: Using CustomKeybindings, inconsistent behaviour [SOLVED]
	- If a quick slot is bound to RT+Y and switch bars is bound to Y, pressing Y in a menu will not switch bars.  However, pressing RT+Y
		will activate the assigned Item AND switch quick slot bars
	- Similarly, if a quickslot is bound to AlphaNum4 and switching btw bars is also bound to AlphaNum4 the quickslot will be activated AND bars will be switched
	- Implemented solution: check if input used to activate any quickslot is being pressed.  If so, do not fire the quickslot bar switch

- BUG: items in quickSlots2 will overwrite items in quickSlots1 [SOLVED]
	- Seems to be caused by exiting the game with quickSlot2 active
	- Implemented solution: 1) Set BarMode to FIRST 2) Clear default behaviour quick bar 3) Load in quick slots from temp array
	
- BUG: using a consummable item assigned to a quickslot and then switch between bars causes the assigned item to disappear [SOLVED]
	- This doesn't affect the JSON arrays (reloading the game after this disappearance results in the disappeared item showing up correctly
	- Also doesn't affect non-consummable items (putting a weapon in the quickslot, using the assigned button and switching bars results in the weapon still showing up properly
		upon switching back)
	-Implemented solution: Changed quickSlots1 and quickSlots2 from Item[] -> int[].  Rewrote LoadQuickSlotArraysFromJSON() and PopulateSkillBar() to accomodate this change.
		By actually reading in items in PopulateSkillBar instead of LoadQuickSlotArraysFromJSON the assignment of a stack of consummables will always find its target, because the call to
		inventory.GetOwnedItems(quickSlots#[i]).First() will find the UID of the "next" first item as opposed to the already-used first item

