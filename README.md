# BetterQuickSlotsMod

List of known bugs / yet-to-be-implemented features:

- Free Slot functionality does not actually free the quick slot
	- (Quick slot will be repopulated upon switching away from, and then back to, the current quick slot bar)
	
- BUG: Using CustomKeybindings, inconsistent behaviour [1/2 SOLVED]
	- If a quick slot is bound to RT+Y and switch bars is bound to Y, pressing Y in a menu will not switch bars.  However, pressing RT+Y
		will activate the assigned Item AND switch quick slot bars
	- requsting ControlsInput.QuickslotToggled(playerID) from LocalCharacterControls will get a bool that checks if LT or RT is pressed
	- doing so fixes the above issue on controllers only!  Problem persists on keyboard + mouse if, for instance, a quickslot is bound to AlphaNum4 and 
		switching btw bars is also bound to AlphaNum4 (quickslot will be activated AND bars will be switched)

- BUG: items in quickSlots2 will overwrite items in quickSlots1 [SOLVED]
	- Seems to be caused by exiting the game with quickSlot2 active
	- Implemented solution: 1) Set BarMode to FIRST 2) Clear default behaviour quick bar 3) Load in quick slots from temp array

