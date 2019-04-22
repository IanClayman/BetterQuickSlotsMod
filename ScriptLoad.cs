using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using On;
using Rewired;

namespace BetterQuickSlots
{
    public class ScriptLoad : MonoBehaviour
    {

        // allows us to call and access methods in the betterQuickSlotsMod class (inherits from PartialityMod)
        public static BetterQuickSlotsMod betterQuickSlots;

        // Item arrays to use as temp storage for skills and items
        // These are specicically item arrays b/c Outward treats skills as Items
        // ( obviously items are also treated as Items :P )
        public int[] quickSlots1;
        public int[] quickSlots2;

        // ***UPDATE DESCRIPTION ONCE I FIGURE OUT WHAT THESE DO***
        //    (unused in current implementation of code)
        public string[] defaultIDs = new string[8];
        private bool isInited = false;

        private int numSlots = 8;
        private string charUID;

        // When dev is set to true extra debugging messages will be printed
        private bool dev = false;

        // enum used to toggle btw the two skill bars
        public enum BarMode
        {
            FIRST,
            SECOND
        }

        // default to first bar on class init
        public BarMode barMode = BarMode.FIRST;

        // Used for "neatness" ( I don't know what that means but I trust Emo#7953's judgement here :) )
        public void Initialise()
        {
            // Patch is the method that actually does all of the good stuff!
            Patch();
        }

        // The Patch method uses hooks to add new functionality to existing methods across Outward
        public void Patch()
        {
            On.PlayerSystem.Start += new On.PlayerSystem.hook_Start(PlayerSystem_StartHook);

            On.NetworkLevelLoader.LevelDoneLoading += new On.NetworkLevelLoader.hook_LevelDoneLoading(LevelDoneLoadingHook);
            On.CharacterQuickSlotManager.OnAssigningQuickSlot_1 += new On.CharacterQuickSlotManager.hook_OnAssigningQuickSlot_1(CQSManager_OnAssigningQuickSlotHook);

            On.LocalCharacterControl.UpdateInteraction += new On.LocalCharacterControl.hook_UpdateInteraction(LocalCharacterControl_UpdateInteractionHook);

            // Required to fix bug where quitting to main menu while on quickslot bar 2 and loading back in would cause improper loading of skills
            // (quickslot bar 2 skills would "overwrite" quickslot bar 1 skills)
            On.MenuManager.BackToMainMenu += new On.MenuManager.hook_BackToMainMenu(MenuManager_BackToMainMenuHook);

            // ActionHasBeenPressed is used to detect if the "Free Quick Slot" button is pressed in the quick slot menu
            On.QuickSlotContextOptions.ActionHasBeenPressed += new On.QuickSlotContextOptions.hook_ActionHasBeenPressed(QuickSlotContextOptionsActionPressedHook);
        }

        // *** Section for Hooks ***

        private void QuickSlotContextOptionsActionPressedHook(On.QuickSlotContextOptions.orig_ActionHasBeenPressed orig, QuickSlotContextOptions self, int id)
        {
            orig(self, id);

            switch(id)
            {
                case 1:
                    Type quickSlotContextOptionsType = typeof(QuickSlotContextOptions);
                    FieldInfo qsDisplayField = quickSlotContextOptionsType.GetField("m_quickSlotDisplay", BindingFlags.NonPublic | BindingFlags.Instance);
                    QuickSlotDisplay qsDisplay = (QuickSlotDisplay)qsDisplayField.GetValue(self);
                    
                    SaveEmptySkillSlot(qsDisplay.RefSlotID);

                    break;
            }
        }

        // Provides fix for bug describes above (see Patch() --> On.MenuManager.BackToMainMenu()
        // Fix provided by setting Bar Mode back to FIRST and repopulating default behaviour skill bar, then quitting to menu
        private void MenuManager_BackToMainMenuHook(On.MenuManager.orig_BackToMainMenu orig, MenuManager menuMngr)
        {
            var character = CharacterManager.Instance.GetCharacter(charUID);
            var qsManager = character.gameObject.GetComponent<CharacterQuickSlotManager>();

            SetBarMode(BarMode.FIRST);
            PopulateSkillBar(qsManager);

            orig(menuMngr);
        }

        private void LocalCharacterControl_UpdateInteractionHook(On.LocalCharacterControl.orig_UpdateInteraction orig, LocalCharacterControl localCharCtrl)
        {
            orig(localCharCtrl);

            // Do nothing if input is locked
            if (localCharCtrl.InputLocked)
                return;

            int playerID = localCharCtrl.Character.OwnerPlayerSys.PlayerID;
            var quickSlotMngr = localCharCtrl.Character.QuickSlotMngr;

            // AnyQuickSlotUsed checks if a quickslot is being activated
            bool quickSlotActivated = AnyQuickSlotUsed(playerID);

            // Uses the name string argument from CustomKeybindings.AddAction()
            // If a quickslot is NOT being used...
            if(!quickSlotActivated)
            {
                // ...and you actually press the button assigned to "switch quick slot bars...
                if (CustomKeybindings.m_playerInputManager[playerID].GetButtonDown("Switch Quick Slot Bars"))
                {
                    // If the current BarMode is FIRST...
                    if (barMode == BarMode.FIRST)
                    {
                        if (dev)
                            Debug.Log("Switching to SECOND skill bar");

                        // Set barMode to SECOND
                        SetBarMode(BarMode.SECOND);
                        // And repopulate the default-behaviour skill bar w/ entries from quickSlots2[]
                        PopulateSkillBar(quickSlotMngr);
                    }
                    // Else if the current BarMode is SECOND...
                    else if (barMode == BarMode.SECOND)
                    {
                        if (dev)
                            Debug.Log("Switching to FIRST skill bar");

                        // Set barMode to FIRST
                        SetBarMode(BarMode.FIRST);
                        // And repopulate the default-behaviour skill bar w/ entries from quickSlots1[]
                        PopulateSkillBar(quickSlotMngr);
                    }
                }
            }
        }

        private void PlayerSystem_StartHook(On.PlayerSystem.orig_Start orig, PlayerSystem self)
        {
            // Run default PlayerSystem.Start() code first
            orig(self);

            // call LoadConfig() method from betterQuickSlotsMod class
            betterQuickSlots.LoadConfig();

            quickSlots1 = new int[8];
            quickSlots2 = new int[8];

            betterQuickSlots.SetCurrentCharacter(self.CharUID, self.Name);

            //Set local class variable charUID to the appropriate UID from PlayerSystem
            charUID = self.CharUID;

            SetBarMode(BarMode.FIRST);
            PopulateSkillBar(self.ControlledCharacter.QuickSlotMngr);
        }

        // DEPRECATED: Now using LocalCharacterControl_UpdateInteraction instead
        private void Character_UpdateHook(On.Character.orig_Update orig, Character self)
        {
            orig(self);
            var quickSlotManager = self.GetComponent<CharacterQuickSlotManager>();

            if (quickSlotManager != null)
            {
                // First we check if the button assigned to keybind (aka the button to switch 
                // btw skill bars) has actually been pressed
                if (Input.GetKeyDown(betterQuickSlots.currentCharacter["keybind"]))
                {
                    // If the current BarMode is FIRST...
                    if (barMode == BarMode.FIRST)
                    {
                        if (dev)
                            Debug.Log("Switching to SECOND skill bar");

                        // Set barMode to SECOND
                        SetBarMode(BarMode.SECOND);
                        // And repopulate the default-behaviour skill bar w/ entries from quickSlots2[]
                        PopulateSkillBar(quickSlotManager);
                    }
                    // Else if the current BarMode is SECOND...
                    else if (barMode == BarMode.SECOND)
                    {
                        if (dev)
                            Debug.Log("Switching to FIRST skill bar");

                        // Set barMode to FIRST
                        SetBarMode(BarMode.FIRST);
                        // And repopulate the default-behaviour skill bar w/ entries from quickSlots1[]
                        PopulateSkillBar(quickSlotManager);
                    }
                }
            }
        }

        private void LevelDoneLoadingHook(On.NetworkLevelLoader.orig_LevelDoneLoading orig, NetworkLevelLoader self)
        {
            orig(self);

            Character character = CharacterManager.Instance.GetCharacter(charUID);
            var quickSlotManager = character.gameObject.GetComponent<CharacterQuickSlotManager>();
            LoadQuickSlotArraysFromJSON(quickSlotManager);

            //ClearSkillBar(quickSlotManager);
            SetBarMode(BarMode.FIRST);
            PopulateSkillBar(quickSlotManager);
        }

        private void CQSManager_OnAssigningQuickSlotHook(On.CharacterQuickSlotManager.orig_OnAssigningQuickSlot_1 orig, CharacterQuickSlotManager qsManager, Item _itemToQuickSlot)
        {
            orig(qsManager, _itemToQuickSlot);

            if (dev)
                Debug.Log("ASSIGNING " + _itemToQuickSlot + " to slot index " + _itemToQuickSlot.QuickSlotIndex + " on " + barMode.ToString() + " skill bar");

            if (_itemToQuickSlot.QuickSlotIndex == -1)
            {
                Debug.Log("Found the code where _itemToQuickSlot.QuickSlotIndex == -1");
            }
            else
            {
                SaveSkillSlotByIndex(_itemToQuickSlot.QuickSlotIndex, _itemToQuickSlot);
            }
        }

        // *** Section for Helper Functions ***

        // LoadBarEntriesFromJSON populates quickSlots1[] and quickSlots2[] using the data from
        // the modConfig file saved in betterQuickSlots
        private void LoadQuickSlotArraysFromJSON(CharacterQuickSlotManager qsManager)
        {
            CharacterInventory inventory = qsManager.gameObject.GetComponent<CharacterInventory>();
            CharacterSkillKnowledge knownSkills = inventory.SkillKnowledge;
            ItemManager itemManager = ItemManager.Instance;

            // Load items and skills into quickSlots1[]
            for (int i = 0; i < numSlots; i++)
            {
                if (dev)
                    Debug.Log("LOADING SKILLBAR SLOT " + i + "FOR 1ST BAR");

                quickSlots1[i] = betterQuickSlots.currentCharacter["FirstBarUIDs"][i];
            }

            // Load items and skills into quickSlots2[]
            for (int i = 0; i < numSlots; i++)
            {
                if (dev)
                    Debug.Log("LOADING SKILLBAR SLOT " + i + "FOR 2ND BAR");

                quickSlots2[i] = betterQuickSlots.currentCharacter["SecondBarUIDs"][i];
            }
        }

        // PopulateSkillBar populates the default-behaviour Outward skill bar with enties from either
        // quickSlots1[] or quickSlots2[] based on the value of barMode
        private void PopulateSkillBar(CharacterQuickSlotManager qsManager)
        {
            if (qsManager.isActiveAndEnabled)
            {
                CharacterInventory inventory = qsManager.gameObject.GetComponent<CharacterInventory>();
                CharacterSkillKnowledge knownSkills = inventory.SkillKnowledge;
                ItemManager itemManager = ItemManager.Instance;

                if (dev)
                    Debug.Log("Populating Skill Bar");

                // The entries we are going to use to populate Outward's default-behaviour skill bar is dependent on
                // whether we want the first skill bar or the second skill bar
                switch (barMode)
                {
                    // In the case where we want entries from the first skill bar...
                    case BarMode.FIRST:
                        ClearSkillBar(qsManager);

                        for (int i = 0; i < numSlots; i++)
                        {
                            if (quickSlots1[i] != 0)
                            {
                                // This line loads a skill into the variable called "entry"...
                                var entry = knownSkills.GetItemFromItemID(quickSlots1[i]);

                                // ...But if "entry" isn't a skill, load an item instead
                                if (entry == null)
                                {
                                    if (inventory.GetOwnedItems(quickSlots1[i]).Count() == 0)
                                        entry = ResourcesPrefabManager.Instance.GetItemPrefab(quickSlots1[i]);
                                    else
                                        entry = inventory.GetOwnedItems(quickSlots1[i]).First();
                                }

                                qsManager.SetQuickSlot(i, entry, false);
                            }
                            else
                            {
                                qsManager.ClearQuickSlot(i);
                            }
                        }
                        // We are done doing stuff in the case of the first skill bar, so BREAK!
                        break;
                    // In the case where we want to add entries from the second skill bar...
                    case BarMode.SECOND:
                        ClearSkillBar(qsManager);

                        for (int i = 0; i < numSlots; i++)
                        {
                            if (quickSlots2[i] != 0)
                            {
                                // This line loads a skill into the variable called "entry"...
                                var entry = knownSkills.GetItemFromItemID(quickSlots2[i]);

                                // ...But if "entry" isn't a skill, load an item instead
                                if (entry == null)
                                {
                                    if (inventory.GetOwnedItems(quickSlots2[i]).Count() == 0)
                                        entry = ResourcesPrefabManager.Instance.GetItemPrefab(quickSlots2[i]);
                                    else
                                        entry = inventory.GetOwnedItems(quickSlots2[i]).First();
                                }

                                qsManager.SetQuickSlot(i, entry, false);
                            }
                            else
                            {
                                qsManager.ClearQuickSlot(i);
                            }
                        }
                        // We are done doing stuff in the case of the second skill bar, so BREAK!
                        break;
                }

            }

        }

        private void ClearSkillBar(CharacterQuickSlotManager qsManager)
        {
            for (int i = 0; i < numSlots; i++)
            {
                qsManager.ClearQuickSlot(i);
            }
        }

        private void SaveSkillSlotByIndex(int index, Item item)
        {
            // What we do here depends on which skill bar is currently active
            switch (barMode)
            {
                case BarMode.FIRST:
                    if (dev)
                        Debug.Log("Setting quickSlots1[" + index + "] to " + item);

                    quickSlots1[index] = item.ItemID;
                    betterQuickSlots.currentCharacter["FirstBarUIDs"][index] = item.ItemID;

                    break;

                case BarMode.SECOND:
                    if (dev)
                        Debug.Log("Setting quickSlots2[" + index + "] to " + item);

                    quickSlots2[index] = item.ItemID;
                    betterQuickSlots.currentCharacter["SecondBarUIDs"][index] = item.ItemID;

                    break;
            }

            betterQuickSlots.SaveConfig();
        }

        private void SaveEmptySkillSlot(int index)
        {
            switch (barMode) {
                case BarMode.FIRST:
                    quickSlots1[index] = 0;
                    betterQuickSlots.currentCharacter["FirstBarUIDs"][index] = 0;

                    break;

                case BarMode.SECOND:
                    quickSlots2[index] = 0;
                    betterQuickSlots.currentCharacter["SecondBarUIDs"][index] = 0;

                    break;
            }

            betterQuickSlots.SaveConfig();
        }

        private bool AnyQuickSlotUsed(int playerID)
        {
            return ( ControlsInput.QuickSlotInstant1(playerID) || ControlsInput.QuickSlotInstant2(playerID) || ControlsInput.QuickSlotInstant3(playerID) ||
                ControlsInput.QuickSlotInstant4(playerID) || ControlsInput.QuickSlotInstant5(playerID) || ControlsInput.QuickSlotInstant6(playerID) ||
                ControlsInput.QuickSlotInstant7(playerID) || ControlsInput.QuickSlotInstant8(playerID) );
        }

        private void SetBarMode(BarMode bm)
        {
            barMode = bm;
        }
    }
}
