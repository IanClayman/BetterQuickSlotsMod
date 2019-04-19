# BetterQuickSlotsMod

List of known bugs / yet-to-be-implemented features:

- Free Slot functionality does not actually free the quick slot
	- (Quick slot will be repopulated upon switching away from, and then back to, the current quick slot bar)
	
- Using CustomKeybindings, inconsistent behaviour
	- (If a quick slot is bound to RT+Y and switch bars is bound to Y, pressing Y in a menu will not switch bars.  However, pressing RT+Y
		will activate the assigned Item AND switch quick slot bars

- Bug where items in quickSlots2 will overwrite items in quickSlots1
	- Seems to be caused by exiting the game with quickSlot2 active
	- Implemented solution: 1) Set BarMode to FIRST 2) Clear default behaviour quick bar 3) Load in quick slots from temp array

