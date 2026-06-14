using FlexiBuffDisplayPanel.ConfigParser;
using Il2Cpp;
using Il2CppServiceStack;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

// Mod meta data
[assembly: MelonInfo(typeof(FlexiBuffDisplayPannel.ModMain), "FlexiBuffDisplayPannel", "1.0.0", "Anonymous", null)]
[assembly: MelonGame("Visionary Realms", "Pantheon")]

namespace FlexiBuffDisplayPannel
{
    // Holds the data for each entity
    public class EntityData()
    {
        public bool isDead; // The death status for an entity always changes to false when you move out of range, even if it is still dead, so now we track if it has ever been dead
        public List<DebuffData> debuffData = new List<DebuffData>();
        public long  encounterStartTime; // Total encounter time for this entity
        public string entityNetworkId; // network id of this entity
        public long totalEncounterTime; // Time the entity has been engaged
        public string targetName;
        public string targetKind;
        public string targetClass;
        public string traits; // Concatenated string of all traits to be displayed
        public int entityLevel; // level of the entity
    }

    // This class holds all the debuff data to display in the debuff panel
    public class DebuffData()
    {
        public string debuffName; // Base name of the debuff
        public float debuffDuration; // Debuff duration
        public float debuffDurationRemaining; // Used in the panel to keep track of remaining duration

        // Do NOT move these into EntityData, each entity can have multiple characters attacking it and casting spells on it, it has to be per debuff
        public string targetName; // Nameplate name of the target
        public string targetNetworkId; // Unique ID of the target
        public string targetKind; // Humanoid, Undead etc.
        public string targetClass; // Rogue, Wizard etc.
        public string casterName; // Nameplate name of the caster
        public string casterNetworkId; // Unique ID of the caster

        public int numStacks; // Number of stacks
        public int maxStacks; // Max stacks
        public string spellType; // Spell Type (Nature, Corruption)
        public string categoryType; // Beneficial, Detrimental, Both

        public long consolidatedEncounterUptime; // Time the debuff has been up as a % of total encounter time
        public float consolidatedEncounterUptimePercent; // Time the debuff has been up as a % of total encounter time
    }

    public class PanelConfig()
    {
        public string panelID; // Unique ID used to identify this exact panel
        public string panelTitle;
        public string displayTargetInfo;
        public bool excludeBuffs;
        public bool excludeDebuffs;
        public List<RowConfig> rowConfig;
    }

    // Used to determine how each row should be rendered
    public class RowConfig()
    {
        public string displayText;  // The name of the buff/debuff that this row config will apply too
        public string color;
        public string persistant; // determines if the row will dissapear when timer reaches zero
        public string showUpTime; // Determines if uptime will be dispalyed for this row
    };


    // This Mod collects all debuff information about all mobs that are currently in range
    // The Pantheon Client does proves multiple remove triggers but none of them provide enough information
    //   to allow this mod to reliably remove debuffs (essential information is NULL or filled with default values under various conditions)
    // So to handle the removal of debuffs (expiry / entity dies) is handled by manually reducing the time remaining by 1 through OnUpdate(), essentially ignoring all Unity remove notifications
    public class ModMain : MelonMod
    {
        // UI Elements
        private static FlexiPanel.FlexiPanel gDebuffPanel = new FlexiPanel.FlexiPanel();
        private static string gCurrentTargetNetworkId = ""; // Only update in offensive target select and only use in OnUpdate
        private const float UpdateInterval = 1.0f; // Update interval in seconds
        private static float _timeSinceLastUpdate;
        // Debuffs we want to ignore
        private static ConfigParser configParser = new ConfigParser();
        private static Dictionary<string, PanelConfig> panelConfigDictionary = new Dictionary<string, PanelConfig>();

        public override void OnInitializeMelon()
        {
            // Read the panel config
            PanelConfig();
        }

        // Updates the duration timers on the panel
        public override void OnUpdate()
        {
            // Only update if the player is loaded into the world
            if (Globals.PlayerIsLoaded == true)
            {
                // We only need update granularity of 1 second, save the CPU cycles
                _timeSinceLastUpdate += Time.deltaTime;
                if (_timeSinceLastUpdate >= UpdateInterval)
                {
                    // Update this immediatly so we dont flood in here
                    _timeSinceLastUpdate = 0f;

                    EntityData partyEntityData = EntityManager.EntityManager.GetEntityData(Globals.Party);
                    EntityData enemyEntityData = new EntityData();

                    // If gCurrentTargetNetworkId is not populated there is no debuff information to update
                    if (!gCurrentTargetNetworkId.Equals(""))
                    {
                        enemyEntityData = EntityManager.EntityManager.GetEntityData(gCurrentTargetNetworkId);
                        // This will occur If the current entity despawns whilst targetted, dont try and update anything
                        if (enemyEntityData == null)
                        {
                            //MelonLogger.Error($"OnUpdate() NO ENTITY DATA IN ONUPDATE gCurrentTargetNetworkId = {gCurrentTargetNetworkId}");
                            gCurrentTargetNetworkId = "";
                            // This is horrible, we are allocated a new entity data twice
                            enemyEntityData = new EntityData();
                        }
                        else
                        {
                            // If the enemy is dead, remove all the debuffs
                            if (enemyEntityData.isDead == true)
                            {
                                enemyEntityData.debuffData.Clear();
                            }
                        }
                    }

                    // Update the progress bars
                    EntityManager.EntityManager.UpdateDurationRemaining();
                    // Call the entitiy manager and get it to update the uptime timers
                    EntityManager.EntityManager.UpdateEncounterUpTime();
                    // Update panels
                    gDebuffPanel.ClearPanelsDisplay();
                    gDebuffPanel.UpdatePanelsDisplay(enemyEntityData, partyEntityData);
                }
            }
        }

        public static void PanelConfig()
        {
            try
            {
                configParser.ParseConfig(ref panelConfigDictionary);
            }
            catch (Exception e)
            {
                MelonLogger.Error("ERROR: Unable to parse XML configuration");
                throw (e);
            }
            // Parse out the config to populate the panels and rows
            gDebuffPanel.SetPanelConfig(ref panelConfigDictionary);

        }
        // This function adds the new debuff panel to the UI
        public static void PreserveRequiredTransforms()
        {
            // This is a nasty hack but is required because I am too dumb to get resizing panels working
            gDebuffPanel.PreserveRequiredTransforms();
        }

        // Used to tear down all the resources allocated by the panel on logout / character change
        public static void ClearTransformDictionaries()
        {
            gDebuffPanel.ClearTransformDictionaries();
        }

        public static void InitialiseFlexiPanels()
        {
            gDebuffPanel.InitialiseFlexiPanels();
        }

        // Called to show the debuff panel
        public static void ShowFlexiPanels()
        {
            // Display the panel if the gloabl is set to allow it
            if (Globals.ShowDebuffPanel == true)
            {
                gDebuffPanel.ShowFlexiPanels();
            }
        }

        // Called to hide the debuff panel
        public static void HideFlexiPanels()
        {
            gDebuffPanel.HideFlexiPanels();
        }

        // This function takes the new number of rows and re-draws the panel with that number of rows
        public static void SetNumDebuffRows(string message)
        {
            // Paramter List:  numRows, panelID
            string[] result = message.Split(Globals.SetNumberOfRowsCommand);
            string panelID = string.Empty; 
            int numRows = -1;
            
            // We must have at least 1 parameters passed in, panelID is optional
            if (result.Length > 1)
            {
                try
                {
                    // Minimum number of rows to display is 1
                    numRows = Int32.Parse(result[1]);
                    numRows = FlexiPanel.FlexiPanelUtils.SanitiseNumRows(numRows);
                    if (result.Length > 2)
                    {
                        panelID = result[2];
                    }
                }
                catch (Exception e)
                {
                    return;
                }

                // Clear out the user visible data
                gDebuffPanel.ClearPanelsDisplay();
                gDebuffPanel.ClearTransformDictionaries();

                // Set the new number of rows to be drawn (dont do this earlier, it can cause problems tearing down the correct number of TextMesh and Image Tranforms)
                Globals.NumDisplayableDebuffs = numRows;
                gDebuffPanel.InitialiseFlexiPanels();
            } // End of IF we have a value to parse
        }

        // Determines if the target for a buff is valid for us to track
        private static bool IsValidTarget(ActiveBuff buff)
        {
            // If the target is a monster, its a valid thing to track
            if (buff.Target.Info.AccessLevel.Equals(AccessLevel.None))
            {
                return true;
            }

            // Track debuffs onto yourself or group members
            if (Globals.GroupMembers.Contains(buff.Target.NetworkId.ToString()) || buff.Target.NetworkId.ToString() == Globals.PlayerNetworkId.ToString())
            {
                return true;
            }

            return false;
        }

        // Determines if buff type is valid for us to track
        private static bool IsValidDebuff(ActiveBuff buff)
        {
            if (buff.BuffData.CategoryType == BuffCategoryType.Harmful)
            {
                return true;
            }

            if (buff.Caster?.NetworkId.ToString() == Globals.PlayerNetworkId && buff.BuffData.CategoryType == BuffCategoryType.Beneficial)
            {
                return true;
            }

            return false;
        }

        // Adds a new buff or refreshes a buff to the list of all buffs and updates the UI only if the entity reciveing the debuff is the active offensive target entity
        public static void OnAddOrRefreshBuff(double time, ActiveBuff buff, bool inBackground, bool isRefresh, bool isItemBuff)
        {
            //MelonLogger.Warning($"OnAddOrRefreshBuff() 0a buff.BuffData.DisplayName.ToString() = {buff.BuffData.DisplayName.ToString()}, isRefresh = {isRefresh}, inBackground = {inBackground}, isItemBuff = {isItemBuff}");
            //MelonLogger.Warning($"OnAddOrRefreshBuff() 0b buff.Target?.NetworkId.ToString() = {buff.Target?.NetworkId.ToString()}, buff.Target.Nameplate.nameText.text = {buff.Target?.Nameplate?.nameText.text}, gCurrentTargetNetworkId = {gCurrentTargetNetworkId}");
            //MelonLogger.Warning($"OnAddOrRefreshBuff() 0c buff.Caster?.NetworkId.ToString() = {buff.Caster?.NetworkId.ToString()}, buff.Caster.Nameplate.nameText.text = {buff.Caster?.Nameplate?.nameText.text}, gCurrentTargetNetworkId = {gCurrentTargetNetworkId}");
            //MelonLogger.Warning($"OnAddOrRefreshBuff buff.Target.Experience.Level = {buff.Target.Experience.Level}, Globals.PlayerLevel = {Globals.PlayerLevel}");

            // Make sure we track only debuffs on only entitys
            if (IsValidTarget(buff) && IsValidDebuff(buff))
            {
             //   MelonLogger.Warning($"OnAddOrRefreshBuff() 1");
                if (buff.BuffData == null || buff.Target == null || buff.Target.Nameplate == null || buff.Caster == null ||  buff.Caster.Nameplate == null)
                {
                    return;
                }

//                MelonLogger.Warning($"OnAddOrRefreshBuff() 2");
                // If the target is an enemy
                EntityData entityData = new EntityData();
                EntityData enemyEntityData = new EntityData();
                EntityData partyEntityData = EntityManager.EntityManager.GetEntityData(Globals.Party);
                
                // If this is a debuff going onto an Enemy
                if (buff.Target.Info.AccessLevel.Equals(AccessLevel.None))
                {
  //                  MelonLogger.Warning($"OnAddOrRefreshBuff() 3");
                    enemyEntityData = EntityManager.EntityManager.GetEntityData(buff.Target.NetworkId.ToString());
                }

//                MelonLogger.Warning($"OnAddOrRefreshBuff() 8");
                // Get the number of seconds since EPOCH from when the very first debuff lands
                if (enemyEntityData.encounterStartTime == 0L)
                {
                    enemyEntityData.encounterStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
  //              MelonLogger.Warning($"OnAddOrRefreshBuff() 9");
                // If we can not find the list log a warning and exit
                if (enemyEntityData.debuffData == null)
                {
                    // update the debuff list to be empty
                    gDebuffPanel.ClearPanelsDisplay();
                    return;
                }

                if (buff.Caster?.NetworkId.ToString() == Globals.PlayerNetworkId &&
                         buff.BuffData.CategoryType == BuffCategoryType.Beneficial &&
                         (Globals.GroupMembers.Contains(buff.Caster?.NetworkId.ToString()) || buff.Target.NetworkId.ToString() == Globals.PlayerNetworkId.ToString()))
                {
//                    MelonLogger.Warning($"OnAddOrRefreshBuff() PARTY");
                    entityData = partyEntityData;
                    if (gCurrentTargetNetworkId != "")
                    {
                        enemyEntityData = EntityManager.EntityManager.GetEntityData(gCurrentTargetNetworkId);
                    }
                }
                else
                {
//                    MelonLogger.Warning($"OnAddOrRefreshBuff() ENEMY");
                    entityData = enemyEntityData;
                }

//                MelonLogger.Warning($"OnAddOrRefreshBuff() 10");
                // If we are a refresh of a buff/debuff that was applied in the past
                bool found = false;
                foreach (DebuffData debuff in entityData.debuffData)
                {
                    // If this is the correct debuff cast by the right person on the correct entity
                    if (debuff.debuffName == buff.BuffData.DisplayName.ToString() && 
                        buff.Caster?.NetworkId.ToString() == debuff.casterNetworkId &&
                        buff.Target?.NetworkId.ToString() == debuff.targetNetworkId)
                    {
//                        MelonLogger.Warning($"OnAddOrRefreshBuff() 11 debuff.debuffName = { debuff.debuffName}");
                        found = true;
                        debuff.debuffDurationRemaining = (int)Math.Ceiling(buff.EstimatedTotalTime);
                        // Only update the panel if we are looking at this exact entity
                        if (entityData.entityNetworkId == buff.Target.NetworkId.ToString() && entityData.entityNetworkId == gCurrentTargetNetworkId)
                        {
                            gDebuffPanel.ClearPanelsDisplay();
                            gDebuffPanel.UpdatePanelsDisplay(enemyEntityData, partyEntityData);
                        }
                    }
                }

                // This is not a refresh and it is not a previously applied buff that has expired
                if (found != true)
                {
//                    MelonLogger.Warning($"OnAddOrRefreshBuff() 12 buff.BuffData.DisplayName.ToString() = {buff.BuffData.DisplayName.ToString()}");
                    // We do not have a debuff of this type in the list, make a new one
                    DebuffData newDebuff = new DebuffData();

                    // Sometimes Caster is null when it should not be, maybe they have logged out? Maybe its a bug in the game code?
                    if (buff.Caster != null && buff.Caster.Nameplate != null)
                    {
                        newDebuff.casterName = buff.Caster.Nameplate.nameText.text;
                        newDebuff.casterNetworkId = buff.Caster.NetworkId.ToString();
                    }
                    newDebuff.targetName = buff.Target.Nameplate.nameText.text;
                    newDebuff.targetNetworkId = buff.Target.NetworkId.ToString();
                    newDebuff.targetClass = buff.Target.Info.Class.ToString();
                    newDebuff.targetKind = buff.Target.Info.Kind.ToString();
                    // Il2Cpp has a missing entry for EntityKind, manually deal with it here
                    if (newDebuff.targetKind.Equals("262144"))
                    {
                        newDebuff.targetKind = "Construct";
                    }
                    newDebuff.debuffName = buff.BuffData.DisplayName.ToString();
                    newDebuff.debuffDuration = (int)Math.Ceiling(buff.EstimatedTotalTime);
                    newDebuff.debuffDurationRemaining = (int)Math.Ceiling(buff.EstimatedTotalTime); // This allows us to deal with debuffs that have diminishign returns like Bind and Mez
                    newDebuff.numStacks = buff.StackCount;
                    newDebuff.maxStacks = buff.BuffData.MaxStacks;
                    newDebuff.categoryType = buff.BuffData.CategoryType.ToString();
                    try
                    {
                        // Bug in ILC2PP, handle it here and default if we crash
                        newDebuff.spellType = buff.CreatedByAbility.SpellType.ToString();
                    }
                    catch
                    {
                        newDebuff.spellType = SpellType.Fortification.ToString();
                    }

//                    MelonLogger.Warning($"OnAddOrRefreshBuff() 13 newDebuff.debuffName = {newDebuff.debuffName}");
                    entityData.debuffData.Add(newDebuff);
                    EntityManager.EntityManager.AddConsolidatedUptime(buff.Target.NetworkId.ToString(), newDebuff);

                    // Update the panel only if the debuff is for the current targets monster
                    if (gCurrentTargetNetworkId.Equals(buff.Target?.NetworkId.ToString()))
                    {
                        EntityManager.EntityManager.AddEntityToUniqueDebuffs(buff.Target?.NetworkId.ToString(), newDebuff.debuffName);
                    }
                    gDebuffPanel.ClearPanelsDisplay();
                    gDebuffPanel.UpdatePanelsDisplay (enemyEntityData, partyEntityData);
                    //                  MelonLogger.Warning($"OnAddOrRefreshBuff() 16");
                }
            }
        }

        // This fires on at least the following conditions:
        // User selects a new target/reselects the existng target
        // Current selected moster despawns 
        public static void OffensiveTargetSelected(Targets.Logic targetLogic)
        {
            if (targetLogic.Offensive == null)
            {
                // Either the user has pressed ESC so they are targetting nothing or something has gone wrong somewhere
                gCurrentTargetNetworkId = "";
                gDebuffPanel.ClearPanelsDisplay();
                return;
            }

            // Do nothing if the same enemy has been re-targetted
            if (gCurrentTargetNetworkId == targetLogic.Offensive.NetworkId.ToString())
            {
                return;
            }

            // Identify the new target, make sure we have a row in the dictionary for it, this is an explicit handling of a weakness in the detect of new NPC entities
            EntityData enemyEntityData = EntityManager.EntityManager.GetEntityData(targetLogic.Offensive.NetworkId.ToString());
            if (enemyEntityData == null)
            {
                EntityManager.EntityManager.AddEntityIfMissing(targetLogic.Offensive.NetworkId.ToString());
                enemyEntityData = EntityManager.EntityManager.GetEntityData(targetLogic.Offensive.NetworkId.ToString());
            }

            // Check if we are dead, if we are dead just return, we have nothing to do
            if (enemyEntityData.isDead == true)
            {
                return;
            }

            EntityData partyEntityData = EntityManager.EntityManager.GetEntityData(Globals.Party);
            // Reset the panel, we must do this to clear the window when somebody switches to a new target
            gDebuffPanel.ClearPanelsDisplay();
            gDebuffPanel.UpdatePanelsDisplay(enemyEntityData, partyEntityData);

            // Store this for use in OnUpdate()
            gCurrentTargetNetworkId = targetLogic.Offensive.NetworkId.ToString();
        }

        // We only process removal of my buffs, specifically for handling of Hurry The Past which causes our mantle to finish early
        public static void RemoveBuff(double time, ActiveBuff buff)
        {
            //            MelonLogger.Warning($"RemoveDeBuff() 0a buff.BuffData.DisplayName.ToString() = {buff.BuffData.DisplayName.ToString()}");
            //            MelonLogger.Warning($"RemoveDeBuff() 0b buff.Target?.NetworkId.ToString() = {buff.Target?.NetworkId.ToString()}, buff.Target.Nameplate.nameText.text = {buff.Target?.Nameplate?.nameText.text}, gCurrentTargetNetworkId = {gCurrentTargetNetworkId}");
            //            MelonLogger.Warning($"RemoveDeBuff() 0c buff.Caster?.NetworkId.ToString() = {buff.Caster?.NetworkId.ToString()}, buff.Caster.Nameplate.nameText.text = {buff.Caster?.Nameplate?.nameText.text}, gCurrentTargetNetworkId = {gCurrentTargetNetworkId}");

            // We only handle the consequences of Hurry The Past which causes your mantle to expire, there is no buff "Hurry The Past" to track, the mantle simply expires
            string buffname = buff.BuffData.DisplayName.ToString();
            // Get the list for the current player
            EntityData enemyEntityData = (gCurrentTargetNetworkId.IsEmpty()) ? new EntityData() : EntityManager.EntityManager.GetEntityData(gCurrentTargetNetworkId);
            EntityData partyEntityData = EntityManager.EntityManager.GetEntityData(Globals.Party);
            // Remove the mantle from the list
            for (int i = 0; i < partyEntityData.debuffData.Count; i++)
            {
                DebuffData buffData = partyEntityData.debuffData[i];
                // If we are the correct debuff and its the correct target and it is cast by us, set its duration remaining to zero
                if (buffData.debuffName.ToString() == buff.BuffData.DisplayName.ToString() && buffData.targetNetworkId.ToString() == buff.Target.NetworkId.ToString() && Globals.PlayerNetworkId == buff.Caster.NetworkId.ToString())
                {
                    buffData.debuffDurationRemaining = 0;
                    buffData.numStacks = buff.StackCount;
                    EntityManager.EntityManager.UpdateDurationRemaining();
                    gDebuffPanel.ClearPanelsDisplay();
                    gDebuffPanel.UpdatePanelsDisplay(enemyEntityData, partyEntityData);
                    return;
                }
            }
        }

        public static void ShowPullMessage(EntityClientMessaging.Logic __instance)
        {
            gDebuffPanel.ShowPullMessage(__instance);
        }

        public static void ShowPopMessage(EntityClientMessaging.Logic __instance)
        {
            gDebuffPanel.ShowPopMessage(__instance);
        }
    }
}
