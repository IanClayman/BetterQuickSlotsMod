# BetterQuickSlotsMod

List of known bugs / yet-to-be-implemented features:

- Free Slot functionality does not actually free the quick slot
	- (Quick slot will be repopulated upon switching away from, and then back to, the current quick slot bar)
	
- BUG: Using CustomKeybindings, inconsistent behaviour [SOLVED]
	- If a quick slot is bound to RT+Y and switch bars is bound to Y, pressing Y in a menu will not switch bars.  However, pressing RT+Y
		will activate the assigned Item AND switch quick slot bars
	- Similarly, if a quickslot is bound to AlphaNum4 and switching btw bars is also bound to AlphaNum4 the quickslot will be activated AND bars will be switched
	- Implemented solution: check if input used to activate any quickslot is being pressed.  If so, do not fire the quickslot bar switch

- BUG: items in quickSlots2 will overwrite items in quickSlots1 [SOLVED]
	- Seems to be caused by exiting the game with quickSlot2 active
	- Implemented solution: 1) Set BarMode to FIRST 2) Clear default behaviour quick bar 3) Load in quick slots from temp array
	
- BUG: using a consummable item assigned to a quickslot and then switch between bars causes the assigned item to disappear
	- This doesn't affect the JSON arrays (reloading the game after this disappearance results in the disappeared item showing up correctly
	- Also doesn't affect non-consummable items (putting a weapon in the quickslot, using the assigned button and switching bars results in the weapon still showing up properly
		upon switching back)

