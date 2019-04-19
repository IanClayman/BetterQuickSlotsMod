/* * * * *
 * BetterQuickSlots
 * --------------------
 * "It Does What It Says"
 * 
 * Better Quick Slots is an improvement on the mod "Second Quick Slot Mod" (https://www.nexusmods.com/outward/mods/18) created by Emo#7953.
 * It eliminates the stutter that occurred in that version when switching between the two quick slot bars and improves overall performance
 * 
 * Created by IAmTheClayman (Discord: IAmTheClayman#1280) with major credit to Emo#7953 for the original concept and tech support along the way.
 * Would never have been able to make this without your help!
 * 
 * WHAT EXACTLY DOES IT DO
 * Better Quick Slots allows the player to set two separate quick slot bars as opposed to Outward's default single bar.  Users can specify a custom keybinding
 * to switch between the two skill bars in Mods/BetterQuickSlots_Config.json.
 * 
 * Please not that in current implementation the first time loading up the mod on a character will cause all previously-assigned  quick slots to be cleared.  Make sure
 * to switch between the quick slot bars twice to fully clear skills before attempting to set new ones.  THIS MOD IS A PRE-1.0 RELEASE, so please be understanding as we make
 * quality of life improvements and add features down the line.
 * 
 * PLANNED FEATURES
 * - Right now the assigned keybinding will switch quick slot bars even while in inventory menus, during trading with merchants, during general NPC dialogue, etc.  Plans are to 
 *      alter functionality to be more in line with general gameplay input (i.e. attacks getting locked out while in menus)
 * - Ability to set the keybinding for switching quick slot bars in the default keyboard and controller keybinding menus, including the option to set up to 2 bindings
 *      per control scheme as with every other input in Outward
 * - UI elements to allow players to tell which of the two quick slot bars they are using at a glance, possibly with custom naming for each set
 * 
 * If you are reading this and have any questions feel free to contact me via Discord.  I am still learning my way around Outward modding (and modding in general) so I
 * will do my best to assist you but please be patient and know that your programming skills may far exceed my own :p
 * 
 * * * * */
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Partiality.Modloader;
using SimpleJSON;

namespace BetterQuickSlots
{

    public class BetterQuickSlotsMod : PartialityMod
    {
        // betterQuickSlotsPatch:Does the actual work related to loading into temp arrays, saving to/reading from JSON, etc.
        public static ScriptLoad betterQuickSlotsPatch;

        // modConfigFile: string declaring the location and name of the JSON file that stores each character's settings
        public static string modConfigFile = "Mods/BetterQuickSlots_Config.json";

        // modConfig: JSON object that allows us to actually work with the data saved in the modConfigFile
        public SimpleJSON.JSONNode modConfig;

        // currentCharacter: JSONNode that stores all data for the current character (name, keybind, skillbars, etc.)
        public JSONNode currentCharacter;

        // Constructor: sets the fields of the PartialityMod class
        public BetterQuickSlotsMod()
        {
            this.ModID = "\"Better Quick Slots Mod\"";
            this.Version = "0.1";
            //this.loadPriority = 0;
            this.author = "IAmTheClayman";
        }

        // ***Default/Required PartialityMod Methods***

        // Init(): called when the mod is loaded
        // Use to set properties or load configs
        public override void Init()
        {
            base.Init();
        }

        // OnLoad(): called after ALL mods have loaded
        // First-time load code goes here (creating objs, getting info from other mods, etc.)
        public override void OnLoad()
        {
            base.OnLoad();
        }

        // OnDisable(): called when a mod is disabled
        public override void OnDisable()
        {
            base.OnDisable();
        }

        // OnEnable(): called when a mod is enabled
        // Also called when a mod is loaded
        public override void OnEnable()
        {
            // REQUIRED
            base.OnEnable();

            CustomKeybindings.AddAction("Switch Quick Slot Bars", CustomKeybindings.KeybindingsCategory.Actions, true, 5, CustomKeybindings.InputActionType.Button);

            ScriptLoad.betterQuickSlots = this;

            LoadConfig();

            GameObject obj = new GameObject();
            betterQuickSlotsPatch = obj.AddComponent<ScriptLoad>();
            betterQuickSlotsPatch.Initialise();
        }

        // ***Helper functions for this specific mod***

        public void LoadConfig()
        {
            // use System.IO to read the mod config file into a string
            string json = File.ReadAllText(modConfigFile);

            // convert the raw string into usable JSON object
            modConfig = JSON.Parse(json);
        }

        public void SetCurrentCharacter(string charUID, string charName)
        {
            // 1st check if the modConfigFile has any entries.  If it does...
            if (modConfig["characters"] != null)
            {
                // ...next check if the specific character requested has an entry, and if it does...
                if (DoesCharacterExist(charUID))
                {
                    // Get the index of the desireed question in the array of characters, then
                    // use that index to get the specific JSON object for that character
                    int charSaveIndex = GetCharacterSaveIndex(charUID);
                    var charSaveObject = modConfig["characters"][charSaveIndex];

                    currentCharacter = charSaveObject;
                }
                // In the event that the desired character does not have an entry in the modConfigFile...
                else
                {
                    currentCharacter = CreateNewCharacterSave(charUID, charName);
                }
            }

            SaveConfig();

        }

        // Helper function used to check if a character has keybindings stored in the modConfigFile
        public bool DoesCharacterExist(string charUID)
        {
            for (int i = 0; i < modConfig["characters"].AsArray.Count; i++)
            {
                string thisCharUID = modConfig["characters"][i]["characterUID"];

                // if paramater charUID already exists, break out and return true
                if (thisCharUID == charUID)
                    return true;
            }

            // return false if no match is found
            return false;
        }

        public int GetCharacterSaveIndex(string charUID)
        {
            for (int i = 0; i < modConfig["characters"].AsArray.Count; i++)
            {
                string thisCharUID = modConfig["characters"][i]["characterUID"];

                if (thisCharUID == charUID)
                    return i;
            }

            // Return 0 in event character cannot be found
            return 0;
        }

        public JSONObject CreateNewCharacterSave(string charUID, string charName)
        {
            var newCharOBJ = new JSONObject();
            modConfig["characters"][-1] = newCharOBJ;

            // Start adding default values to new character's JSONObject
            var jsonCharUID = new JSONString(charUID);
            var jsonCharName = new JSONString(charName);
            var jsonDefaultKeybind = new JSONString("tab");
            var jsonFirstBarUIDs = new JSONArray();

            jsonFirstBarUIDs[0] = 0;
            jsonFirstBarUIDs[1] = 0;
            jsonFirstBarUIDs[2] = 0;
            jsonFirstBarUIDs[3] = 0;
            jsonFirstBarUIDs[4] = 0;
            jsonFirstBarUIDs[5] = 0;
            jsonFirstBarUIDs[6] = 0;
            jsonFirstBarUIDs[7] = 0;

            var jsonSecondBarUIDs = new JSONArray();

            jsonSecondBarUIDs[0] = 0;
            jsonSecondBarUIDs[1] = 0;
            jsonSecondBarUIDs[2] = 0;
            jsonSecondBarUIDs[3] = 0;
            jsonSecondBarUIDs[4] = 0;
            jsonSecondBarUIDs[5] = 0;
            jsonSecondBarUIDs[6] = 0;
            jsonSecondBarUIDs[7] = 0;

            // Now we begin assigning these values to the newly created JSONObject
            newCharOBJ.Add("characterUID", jsonCharUID);
            newCharOBJ.Add("characterName", jsonCharName);
            newCharOBJ.Add("keybind", jsonDefaultKeybind);
            newCharOBJ.Add("FirstBarUIDs", jsonFirstBarUIDs);
            newCharOBJ.Add("SecondBarUIDs", jsonSecondBarUIDs);

            return newCharOBJ;
        }

        public void SaveConfig()
        {
            File.WriteAllText(modConfigFile, modConfig.ToString());
        }

    }

    [Serializable]
    public class ModConfig
    {
        public string SwitchKeybinding;
        public int[] firstBarUIDs;
        public int[] secondBarUIDs;
    }
}
