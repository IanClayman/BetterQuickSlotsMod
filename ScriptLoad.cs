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
        public Item[] quickSlots1;
        public Item[] quickSlots2;

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
        }

        // *** Section for Hooks ***

        private void LocalCharacterControl_UpdateInteractionHook(On.LocalCharacterControl.orig_UpdateInteraction orig, LocalCharacterControl localCharCtrl)
        {
            orig(localCharCtrl);

            // Do nothing if input is locked
            if (localCharCtrl.InputLocked)
                return;

            int playerID = localCharCtrl.Character.OwnerPlayerSys.PlayerID;
            var quickSlotMngr = localCharCtrl.Character.QuickSlotMngr;

            // LINE ADDED TO CHECK IF HOLDING LT or RT
            bool quickslotToggledFlag = ControlsInput.QuickSlotToggled(playerID);

            // Uses the name string argument from CustomKeybindings.AddAction()
            if(!quickslotToggledFlag && CustomKeybindings.m_playerInputManager[playerID].GetButtonDown("Switch Quick Slot Bars"))
            {
                //Debug.Log("Hello World!");

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

        private void PlayerSystem_StartHook(On.PlayerSystem.orig_Start orig, PlayerSystem self)
        {
            // Run default PlayerSystem.Start() code first
            orig(self);

            // call LoadConfig() method from betterQuickSlotsMod class
            betterQuickSlots.LoadConfig();

            quickSlots1 = new Item[8];
            quickSlots2 = new Item[8];

            betterQuickSlots.SetCurrentCharacter(self.CharUID, self.Name);

            //Set local class variable charUID to the appropriate UID from PlayerSystem
            charUID = self.CharUID;

            SetBarMode(BarMode.FIRST);

            // The following line fixes a bug where items in quickSlots2[] would get loaded in on top of
            // items loaded from quickSlots1[].  For this reason DO NOT REMOVE the following line without 
            // rigorously testing the impact on mod perfomance!
            ClearSkillBar(self.ControlledCharacter.QuickSlotMngr);
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

            //Debug.Log(System.DateTime.Now.ToShortTimeString() + " || Running LevelDoneLoadingHook");
            //Character character = CharacterManager.Instance.GetCharacter(CharacterManager.Instance.PlayerCharacters.Values[0]);
            //Debug.Log("Character Name: " + character.Name);

            Character character = CharacterManager.Instance.GetCharacter(charUID);
            var quickSlotManager = character.gameObject.GetComponent<CharacterQuickSlotManager>();
            LoadQuickSlotArraysFromJSON(quickSlotManager);

            SetBarMode(BarMode.FIRST);

            ClearSkillBar(quickSlotManager);
            PopulateSkillBar(quickSlotManager);
        }

        private void CQSManager_OnAssigningQuickSlotHook(On.CharacterQuickSlotManager.orig_OnAssigningQuickSlot_1 orig, CharacterQuickSlotManager qsManager, Item _itemToQuickSlot)
        {
            orig(qsManager, _itemToQuickSlot);

            if (dev)
                Debug.Log("ASSINGING " + _itemToQuickSlot + " to slot index " + _itemToQuickSlot.QuickSlotIndex + " on " + barMode.ToString() + " skill bar");

            if (_itemToQuickSlot.QuickSlotIndex == -1)
            {
                // Nothing to do here
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

                var entryUID = betterQuickSlots.currentCharacter["FirstBarUIDs"][i];

                // If there is actually a skill/item stored at the index being looked at in FirstBarUIDs[]...
                if (entryUID != 0)
                {
                    if (dev)
                        Debug.Log("UID of skill/item is : " + entryUID);

                    // This line loads a skill into the variable called "entry"...
                    var entry = knownSkills.GetItemFromItemID(entryUID.AsInt);

                    // ...But if "entry" isn't a skill, load an item instead
                    if (entry == null)
                        entry = inventory.GetOwnedItems(entryUID).First();

                    quickSlots1[i] = entry;
                }
                // If there isn't a skill/item stored at the index being looked at in FirstBarUIDs[]...
                else
                {
                    if (dev)
                        Debug.Log("Skill bar slot " + i + " has no assigned skill/item");
                }
            }

            // Load items and skills into quickSlots2[]
            for (int i = 0; i < numSlots; i++)
            {
                if (dev)
                    Debug.Log("LOADING SKILLBAR SLOT " + i + "FOR 2ND BAR");

                var entryUID = betterQuickSlots.currentCharacter["SecondBarUIDs"][i];

                // If there is actually a skill/item stored at the index being looked at in SecondBarUIDs[]...
                if (entryUID != 0)
                {
                    if (dev)
                        Debug.Log("UID of skill/item is : " + entryUID);

                    // This line loads a skill into the variable called "entry"...
                    var entry = knownSkills.GetItemFromItemID(entryUID.AsInt);

                    // ...But if "entry" isn't a skill, load an item instead
                    if (entry == null)
                        entry = inventory.GetOwnedItems(entryUID).First();

                    quickSlots2[i] = entry;
                }
                // If there isn't a skill/item stored at the index being looked at in SecondBarUIDs[]...
                else
                {
                    if (dev)
                        Debug.Log("Skill bar slot " + i + " has no assigned skill/item");
                }
            }
        }

        // PopulateSkillBar populates the default-behaviour Outward skill bar with enties from either
        // quickSlots1[] or quickSlots2[] based on the value of barMode
        private void PopulateSkillBar(CharacterQuickSlotManager qsManager)
        {
            if (qsManager.isActiveAndEnabled)
            {
                if (dev)
                    Debug.Log("Populating Skill Bar");

                // The entries we are going to use to populate Outward's default-behaviour skill bar is dependent on
                // whether we want the first skill bar or the second skill bar
                switch (barMode)
                {
                    // In the case where we want entries from the first skill bar...
                    case BarMode.FIRST:
                        for (int i = 0; i < numSlots; i++)
                        {
                            // If the entry we want to add to the skill bar is actually exists...
                            if (quickSlots1[i] != null)
                            {
                                if (dev)
                                    Debug.Log("Populating skillbar slot " + i + " with entry " + quickSlots1[i]);
                                // Actually assign the entry to the corresponding slot
                                qsManager.SetQuickSlot(i, quickSlots1[i], false);
                            }
                            // If the entry to add to the skill bar DOESN'T exist...
                            else
                            {
                                if (dev)
                                    Debug.Log("No assignable entry.  Clearing slot");
                                // Clear the slot
                                qsManager.ClearQuickSlot(i);
                            }
                        }
                        // We are done doing stuff in the case of the first skill bar, so BREAK!
                        break;
                    // In the case where we want to add entries from the second skill bar...
                    case BarMode.SECOND:
                        for (int i = 0; i < numSlots; i++)
                        {
                            // If the entry we want to add to the skill bar is actually exists...
                            if (quickSlots2[i] != null)
                            {
                                if (dev)
                                    Debug.Log("Populating skillbar slot " + i + " with entry " + quickSlots2[i]);
                                // Actually assign the entry to the corresponding slot
                                qsManager.SetQuickSlot(i, quickSlots2[i], false);
                            }
                            // If the entry to add to the skill bar DOESN'T exist...
                            else
                            {
                                if (dev)
                                    Debug.Log("No assignable entry.  Clearing slot");
                                // Clear the slot
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

                    quickSlots1[index] = item;
                    betterQuickSlots.currentCharacter["FirstBarUIDs"][index] = item.ItemID;

                    break;

                case BarMode.SECOND:
                    if (dev)
                        Debug.Log("Setting quickSlots2[" + index + "] to " + item);

                    quickSlots2[index] = item;
                    betterQuickSlots.currentCharacter["SecondBarUIDs"][index] = item.ItemID;

                    break;
            }

            betterQuickSlots.SaveConfig();
        }

        private void SetBarMode(BarMode bm)
        {
            barMode = bm;
        }
    }
}
