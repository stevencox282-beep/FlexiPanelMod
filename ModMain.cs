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

        public long consolidatedEncounterUptime; // Time the debuff has been up as a % of total encounter time
        public float consolidatedEncounterUptimePercent; // Time the debuff has been up as a % of total encounter time
    }

    public class PanelConfig()
    {
        public string panelID; // Unique ID used to identify this exact panel
        public string panelTitle;
        public string displayTargetInfo;
        public List<RowConfig> rowConfig;
    }

    // Used to determine how each row should be rendered
    public class RowConfig()
    {
        public string displayText;  // The name of the buff/debuff that this row config will apply too
        public string color;
        public string persistant; // determines if the row will dissapear when timer reaches zero
        public string showUpTime; // Determines if uptime will be dispalyed for this row
        public string position; // Determines the position of the row in the panel
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
        private static string TraitSubString = "Trait: ";
        // Debuffs we want to ignore
        private static readonly string[] DebuffBlacklist = { TraitSubString, "Mana Guzzle", "Taunt Immunity", "Temporary Invulnerability", EntityStatusType.Silenced.ToString(), EntityStatusType.Stunned.ToString(), EntityStatusType.Feared.ToString(), "Taunt"};
        private static ConfigParser configParser = new ConfigParser();

        public override void OnInitializeMelon() {
            // Parse out the config to populate the panels and rows
            configParser.ParseConfig();
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

                    // Update the progress bars
                    EntityManager.EntityManager.UpdateDurationRemaining();

                    // Call the entitiy manager and get it to update the uptime timers
                    EntityManager.EntityManager.UpdateEncounterUpTime();

                    // If gCurrentTargetNetworkId is not populated there is no point updating the display
                    if (!gCurrentTargetNetworkId.Equals(""))
                    {
                        // Do not update the screen for dead enemies
                        EntityData entityData = EntityManager.EntityManager.GetEntityData(gCurrentTargetNetworkId);
                        // This will occur If the current entity despawns whilst targetted, dont try and update anything
                        if (entityData == null)
                        {
                            //MelonLogger.Error($"OnUpdate() NO ENTITY DATA IN ONUPDATE gCurrentTargetNetworkId = {gCurrentTargetNetworkId}");
                            gCurrentTargetNetworkId = "";
                            return;
                        }

                        // We do not update the screen if the entity is dead
                        if (entityData.isDead == false)
                        {
                            // If we have a valid debuff list for the current target, update the screen
                            gDebuffPanel.UpdatePanel(entityData);
                        }
                    }
                }
            }
        }

        // This function adds the new debuff panel to the UI
        public static void AddPanelsToUI()
        {
            // This is a nasty hack but is required because I am too dumb to get resizing panels working
            gDebuffPanel.PreserveRequiredTransforms();

            // Build the panel, attach it to the offensive target panel
            gDebuffPanel.DisplayPanels();
        }

        // Used to tear down all the resources allocated by the panel on logout / character change
        public static void ClearPanelLists()
        {
            gDebuffPanel.ClearPanelLists();
        }

        // Called to show the debuff panel
        public static void ShowDebuffPanel()
        {
            // Display the panel if the gloabl is set to allow it
            if (Globals.ShowDebuffPanel == true)
            {
                gDebuffPanel.ShowDebuffPanel();
            }
        }

        // Called to hide the debuff panel
        public static void HideDebuffPanel()
        {
            gDebuffPanel.HideDebuffPanel();
        }

        // This function takes the new number of rows and re-draws the panel with that number of rows
        public static void SetNumDebuffRows(string message)
        {
            string[] result = message.Split(Globals.SetNumberOfRowsCommand);
            int numRows = -1;
            // If we have something after the command name create that many rows
            if (result.Length > 1)
            {
                try
                {
                    // Minimum number of rows to display is 1
                    numRows = Int32.Parse(result[1]);
                    numRows = FlexiPanel.FlexiPanelUtils.SanitiseNumRows(numRows);
                }
                catch (Exception e)
                {
                    return;
                }

                // Clear out the user visible data
                gDebuffPanel.ClearPanel();
                // Clear out the row data from the panel
                ClearPanelLists();

                // Set the new number of rows to be drawn (dont do this earlier, it can cause problems tearing down the correct number of TextMesh and Image tranforms)
                Globals.NumDisplayableDebuffs = numRows;
                gDebuffPanel.DisplayPanels();
            } // End of IF we have a value to parse
        }

        // Determines if the target for a buff is valid for us to track
        private static bool IsValidTarget(ActiveBuff buff)
        {
            // We do not want buffs that go onto players even if those are from entitys to players
            // Monster have an access level of None so that is an easy way to detect its a entity not a player
            // Player Pets are tracked here but probably should not be
            return (buff.Target.Info.AccessLevel.Equals(AccessLevel.None)) ? true : false;
        }

        // Determines if buff type is valid for us to track
        private static bool IsValidDebuff(ActiveBuff buff)
        {
            // We do not want buffs that go onto players even if those are from entitys to players
            // Monster have an access level of None so that is an easy way to detect its a entity not a player
            // Player Pets are tracked here but probably should not be
            return (buff.BuffData.CategoryType == BuffCategoryType.Harmful) ? true : false;
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
                if (buff.BuffData == null || buff.Target == null || buff.Target.Nameplate == null || buff.Caster == null ||  buff.Caster.Nameplate == null)
                {
                    return;
                }

                // Do not process anything in the blacklist
                if (DebuffBlacklist.Contains(buff.BuffData.DisplayName.ToString()))
                {
                    return;
                }

                // Get the list for the current enemy
                EntityData entityData = EntityManager.EntityManager.GetEntityData(buff.Target.NetworkId.ToString());
                if (entityData == null)
                {
                    return;
                }

                if (entityData.entityNetworkId.IsEmpty() || entityData.entityNetworkId.ToString().Equals(""))
                {
                    MelonLogger.Error($"OnAddOrRefreshBuff() entityData.entityNetworkId is NULL");
                    return;
                }

                // Get the number of seconds since EPOCH from when the very first debuff lands
                if (entityData.encounterStartTime == 0L)
                {
                    entityData.encounterStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                }
                // If we can not find the list log a warning and exit
                if (entityData.debuffData == null)
                {
                    // update the debuff list to be empty
                    gDebuffPanel.ClearPanel();
                    return;
                }

                // If we are a refresh or a buff that was applied in the past but has since expired
                bool found = false;
                foreach (DebuffData debuff in entityData.debuffData)
                {
                    // If this is the correct debuff cast by the right person on the correct entity
                    if (debuff.debuffName == buff.BuffData.DisplayName.ToString() && 
                        buff.Caster?.NetworkId.ToString() == debuff.casterNetworkId &&
                        buff.Target?.NetworkId.ToString() == debuff.targetNetworkId)
                    {
//                        MelonLogger.Warning($"OnAddOrRefreshBuff() 1 debuff.debuffName = { debuff.debuffName}");
                        found = true;
                        debuff.debuffDurationRemaining = (int)Math.Ceiling(buff.EstimatedTotalTime);
                        // Only update the panel if we are looking at this exact entity
                        if (entityData.entityNetworkId == buff.Target.NetworkId.ToString() && entityData.entityNetworkId == gCurrentTargetNetworkId)
                        {
                            gDebuffPanel.ClearPanel();
                            gDebuffPanel.UpdatePanel(entityData);
                        }
                    }
                }

                // This is not a refresh and it is not a previously applied buff that has expired
                if (found != true)
                {
//                    MelonLogger.Warning($"OnAddOrRefreshBuff() 2");
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
                    try
                    {
                        // Bug in ILC2PP, handle it here and default if we crash
                        newDebuff.spellType = buff.CreatedByAbility.SpellType.ToString();
                    }
                    catch
                    {
                        newDebuff.spellType = SpellType.Fortification.ToString();
                    }
                    
                    
                    entityData.debuffData.Add(newDebuff);
                    EntityManager.EntityManager.AddConsolidatedUptime(buff.Target.NetworkId.ToString(), newDebuff);

                    // Update the debuff list, dont update the display if this debuf isnt for the active target
                    if (gCurrentTargetNetworkId.Equals(buff.Target?.NetworkId.ToString()))
                    {
                        //MelonLogger.Warning($"OnAddOrRefreshDebuff() 2 UpdateDebuffPanel entityData.entityNetworkId = {entityData.entityNetworkId}, buff.Target.NetworkId.ToString() = {buff.Target.NetworkId.ToString()}, gCurrentTargetNetworkId = {gCurrentTargetNetworkId.ToString()}");
                        EntityManager.EntityManager.AddEntityToUniqueDebuffs(buff.Target?.NetworkId.ToString(), newDebuff.debuffName);
                        gDebuffPanel.UpdatePanel(entityData);
                    }
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
                gDebuffPanel.ClearPanel();
                return;
            }

            // Identify the new target, make sure we have a row in the dictionary for it, this is an explicit handling of a weakness in the detect of new NPC entities
            EntityData entityData = EntityManager.EntityManager.GetEntityData(targetLogic.Offensive.NetworkId.ToString());
            if (entityData == null)
            {
                EntityManager.EntityManager.AddEntityIfMissing(targetLogic.Offensive.NetworkId.ToString());
                entityData = EntityManager.EntityManager.GetEntityData(targetLogic.Offensive.NetworkId.ToString());
            }

            // Check if we are dead, if we are dead just return, we have nothing to do
            if (entityData.isDead == true)
            {
                return;
            }

            // Reset the panel, we must do this to clear the window when somebody switches to a new target
            gDebuffPanel.ClearPanel();
            gDebuffPanel.UpdatePanel(entityData);

            // Store this for use in OnUpdate()
            gCurrentTargetNetworkId = targetLogic.Offensive.NetworkId.ToString();
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
