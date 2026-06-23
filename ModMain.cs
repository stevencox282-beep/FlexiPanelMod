using Il2Cpp;
using Il2CppPantheonPersist;
using Il2CppServiceStack;
using MelonLoader;
using UnityEngine;

// Mod meta data
[assembly: MelonInfo(typeof(FlexiPanelMod.ModMain), "FlexiPanelMod", "1.0.0", "Anonymous", null)]
[assembly: MelonGame("Visionary Realms", "Pantheon")]

namespace FlexiPanelMod
{
    // This Mod collects all buff information about all mobs and the party members that are currently in range
    public class ModMain : MelonMod
    {
        // UI Elements
        private static FlexiPanel gFlexiPanels = new FlexiPanel();
        private static string gCurrentTargetNetworkId = ""; // Only update in offensive target select and only use in OnUpdate
        private const float UpdateInterval = 1.0f; // Update interval in seconds
        private static float _timeSinceLastUpdate;
        private static ConfigParser configParser = new ConfigParser(); // Parses the mods configuration
        private static Dictionary<string, PanelConfig> panelConfigDictionary = new Dictionary<string, PanelConfig>(); // All panels
        private static List<string> includeAllBuffsBlacklist = new List<string>(); // Holds the blacklist for 
        private static List<string> includeAllDebuffsBlacklist = new List<string>(); // Holds the blacklist for 

        public override void OnInitializeMelon()
        {
            // Read the panel config
            ReadPanelConfig();
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

                    EntityData partyEntityData = EntityManager.GetEntityData(Globals.Party);
                    EntityData enemyEntityData = new EntityData();

                    // If gCurrentTargetNetworkId is not populated we still have to process party information
                    if (!gCurrentTargetNetworkId.Equals(""))
                    {
                        enemyEntityData = EntityManager.GetEntityData(gCurrentTargetNetworkId);
                        // This will occur If the current entity despawns whilst targetted, dont try and update anything
                        if (enemyEntityData == null)
                        {
                            gCurrentTargetNetworkId = "";
                            // This is horrible as we are allocating a new entity data twice but it works
                            enemyEntityData = new EntityData();
                        }
                        else
                        {
                            // If the enemy is dead, remove all the data
                            if (enemyEntityData.isDead == true)
                            {
                                enemyEntityData.buffData.Clear();
                            }
                        }
                    }

                    // Update the progress bars
                    EntityManager.UpdateDurationRemaining();
                    // Call the entitiy manager and get it to update the uptime timers
                    EntityManager.UpdateEncounterUpTime();
                    // Update panels
                    gFlexiPanels.ClearPanelsDisplay();
                    gFlexiPanels.UpdatePanelsDisplay(enemyEntityData, partyEntityData, includeAllBuffsBlacklist, includeAllDebuffsBlacklist);
                }
            }
        }

        // Parses the panel config and sets up the panels
        public static void ReadPanelConfig()
        {
            try
            {
                configParser.ParseConfig(ref panelConfigDictionary, ref includeAllBuffsBlacklist, ref includeAllDebuffsBlacklist);
            }
            catch (Exception e)
            {
                MelonLogger.Error("ERROR: Unable to parse XML configuration");
                throw;
            }
            // Parse out the config to populate the panels and rows
            gFlexiPanels.SetPanelConfig(ref panelConfigDictionary);

        }

        // This function adds the new buff panel to the UI
        public static void PreserveRequiredTransforms()
        {
            // This is a nasty hack but is required because I am too dumb to get resizing panels working
            gFlexiPanels.PreserveRequiredTransforms();
        }

        // Used to tear down all the resources allocated by the panel on logout / character change
        public static void ClearTransformDictionaries()
        {
            gFlexiPanels.ClearTransformDictionaries();
        }

        // Create panels
        public static void InitialiseFlexiPanels()
        {
            gFlexiPanels.InitialiseFlexiPanels();
        }

        // Called to show the buff panel
        public static void ShowFlexiPanels()
        {
            // Display the panel if the gloabl is set to allow it
            if (Globals.ShowPanels == true)
            {
                gFlexiPanels.ShowFlexiPanels();
            }
        }

        // Called to hide the buff panel
        public static void HideFlexiPanels()
        {
            gFlexiPanels.HideFlexiPanels();
        }

        // Determines if the target for a buff is valid for us to track
        private static bool IsValidTarget(ActiveBuff activeBuff)
        {
            // If the target is a monster, its a valid thing to track
            if (activeBuff.Target.Info.AccessLevel.Equals(AccessLevel.None))
            {
                return true;
            }

            // Track buffs onto yourself or group members
            if (Globals.GroupMemberNetworkIds.Contains(activeBuff.Target.NetworkId.ToString()) || activeBuff.Target.NetworkId.ToString() == Globals.PlayerNetworkId.ToString())
            {
                return true;
            }

            return false;
        }

        // Adds a new buff or refreshes a buff
        public static void OnAddOrRefreshBuff(double time, ActiveBuff activeBuff, bool inBackground, bool isRefresh, bool isItemBuff)
        {
//            if (activeBuff.Target?.NetworkId.ToString() == Globals.PlayerNetworkId)
//            {
//                MelonLogger.Warning($"OnAddOrRefreshBuff() 0a activeBuff.BuffData.DisplayName.ToString() = {activeBuff.BuffData.DisplayName.ToString()} activeBuff.Target?.NetworkId.ToString() = {activeBuff.Target?.NetworkId.ToString()}");
//            }

            // Make sure we track only valid entities
            if (IsValidTarget(activeBuff))
            {
                // Check everything major for null values, sometimes they appear and I dont know why
                if (activeBuff.BuffData == null || activeBuff.Target == null || activeBuff.Target.Nameplate == null || activeBuff.Caster == null || activeBuff.Caster.Nameplate == null)
                {
                    return;
                }

                // Default to player data
                EntityData entityData = EntityManager.GetEntityData(Globals.Party);
                EntityData enemyEntityData = new EntityData();

                // If this is a buff going onto an Enemy (we do track) or a pet (which dont track)
                if (activeBuff.Target.Info.AccessLevel.Equals(AccessLevel.None))
                {
                    // This will set enemyEntityData to NULL if it does not find a match
                    enemyEntityData = EntityManager.GetEntityData(activeBuff.Target.NetworkId.ToString());

                    // HACK - We do not track pets or this is a missing entity (something went wrong somewhere else)
                    if (enemyEntityData == null || enemyEntityData.buffData == null)
                    {
                        return;
                    }

                    // Get the number of seconds since EPOCH from when the very first buff lands
                    if (enemyEntityData.encounterStartTime == 0L)
                    {
                        enemyEntityData.encounterStartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    }
                    entityData = enemyEntityData;
                }

                // If we are a refresh of a buff/debuff
                bool found = HandleRefresh(entityData, activeBuff);

                // This is not a refresh and it is not a previously applied buff that has expired
                if (found != true)
                {
                    // We do not have a buff of this type in the list, make a new one
                    BuffData buffData = new BuffData();
                    CreateNewBuff(entityData, activeBuff, ref buffData);
                    entityData.buffData.Add(buffData);
                    // Update the buff list and uptimes
                    EntityManager.AddEntityToUniqueDebuffs(activeBuff.Target?.NetworkId.ToString(), buffData.buffName);
                    EntityManager.AddConsolidatedUptime(activeBuff.Target.NetworkId.ToString(), buffData);                    
                }
            }
        }

        // Creates a new buff type
        private static BuffData CreateNewBuff(EntityData entityData, ActiveBuff activeBuff, ref BuffData newDebuff)
        {
            // Sometimes Caster is null when it should not be, maybe they have logged out? Maybe its a bug in the game code?
            if (activeBuff.Caster != null && activeBuff.Caster.Nameplate != null)
            {
                newDebuff.casterName = activeBuff.Caster.Nameplate.nameText.text;
                newDebuff.casterNetworkId = activeBuff.Caster.NetworkId.ToString();
            }
            newDebuff.targetName = activeBuff.Target.Nameplate.nameText.text;
            newDebuff.targetNetworkId = activeBuff.Target.NetworkId.ToString();
            newDebuff.targetClass = activeBuff.Target.Info.Class.ToString();
            newDebuff.targetKind = activeBuff.Target.Info.Kind.ToString();
            newDebuff.buffName = activeBuff.BuffData.DisplayName.ToString();
            newDebuff.buffDuration = (int)Math.Ceiling(activeBuff.EstimatedTotalTime);
            newDebuff.buffDurationRemaining = (int)Math.Ceiling(activeBuff.EstimatedTotalTime); // This allows us to deal with buffs that have diminishign returns like Bind and Mez
            newDebuff.numStacks = activeBuff.StackCount;
            newDebuff.maxStacks = activeBuff.BuffData.MaxStacks;
            newDebuff.categoryType = activeBuff.BuffData.CategoryType.ToString();
            try
            {
                // Bug in ILC2PP, handle it here and default if we exception
                newDebuff.spellType = activeBuff.CreatedByAbility.SpellType.ToString();
            }
            catch
            {
                newDebuff.spellType = SpellType.Fortification.ToString();
            }
            return newDebuff;
        }

        // Handle buff refresh
        private static bool HandleRefresh(EntityData entityData, ActiveBuff activeBuff)
        {
            // Check for the buff in the list of all existing buffs
            foreach (BuffData buff in entityData.buffData)
            {
                // If this is the correct buff/debuff cast by the right person on the correct entity return true
                if (buff.buffName == activeBuff.BuffData.DisplayName.ToString() &&
                    activeBuff.Caster?.NetworkId.ToString() == buff.casterNetworkId &&
                    activeBuff.Target?.NetworkId.ToString() == buff.targetNetworkId)
                {
                    buff.buffDurationRemaining = (int)Math.Ceiling(activeBuff.EstimatedTotalTime);
                    buff.numStacks = activeBuff.StackCount;
                    return true;
                }
            }
            return false;
        }

        // On target sleection / change / deselection / death
        public static void OffensiveTargetSelected(Targets.Logic targetLogic)
        {
            if (targetLogic.Offensive == null)
            {
                // Either the user has pressed ESC so they are targetting nothing or something has gone wrong somewhere
                gCurrentTargetNetworkId = "";
                return;
            }

            // Do nothing if the same enemy has been re-targetted
            if (gCurrentTargetNetworkId == targetLogic.Offensive.NetworkId.ToString())
            {
                return;
            }

            // Identify the new target, make sure we have a row in the dictionary for it, this is an explicit handling of a weakness in the detect of new NPC entities
            EntityData enemyEntityData = EntityManager.GetEntityData(targetLogic.Offensive.NetworkId.ToString());
            if (enemyEntityData == null)
            {
                EntityManager.AddEntityIfMissing(targetLogic.Offensive.NetworkId.ToString());
                enemyEntityData = EntityManager.GetEntityData(targetLogic.Offensive.NetworkId.ToString());
            }

            // Return if entity is dead
            if (enemyEntityData.isDead == true)
            {
                return;
            }

            EntityData partyEntityData = EntityManager.GetEntityData(Globals.Party);
            // Reset the panel, we must do this to clear the window when somebody switches to a new target
            gFlexiPanels.ClearPanelsDisplay();
            gFlexiPanels.UpdatePanelsDisplay(enemyEntityData, partyEntityData, includeAllBuffsBlacklist, includeAllDebuffsBlacklist);

            // Store this for use in OnUpdate()
            gCurrentTargetNetworkId = targetLogic.Offensive.NetworkId.ToString();
        }


        // Removes a specific buff from an entity buff list
        public static void RemoveBuff(double time, ActiveBuff activeBuff)
        {
            //MelonLogger.Warning($"RemoveDeBuff() 0a activeBuff.BuffData.DisplayName.ToString() = {activeBuff.BuffData.DisplayName.ToString()}");

            // Get the list for the current player
            EntityData enemyEntityData = (gCurrentTargetNetworkId.IsEmpty()) ? new EntityData() : EntityManager.GetEntityData(gCurrentTargetNetworkId);
            EntityData partyEntityData = EntityManager.GetEntityData(Globals.Party);
            // Remove the mantle from the list
            for (int i = 0; i < partyEntityData.buffData.Count; i++)
            {
                BuffData buffData = partyEntityData.buffData[i];
                // If we are the correct buff and its the correct target and caster
                if (buffData.buffName.ToString() == activeBuff.BuffData.DisplayName.ToString() &&
                    buffData.targetNetworkId.ToString() == activeBuff.Target.NetworkId.ToString() &&
                    buffData.casterNetworkId.ToString() == activeBuff.Caster.NetworkId.ToString())
                {
                    buffData.buffDurationRemaining = 0;
                    buffData.numStacks = activeBuff.StackCount;
                    EntityManager.UpdateDurationRemaining();
                }
            }
        }

        // Show the pull message
        public static void ShowPullMessage(EntityClientMessaging.Logic __instance)
        {
            gFlexiPanels.ShowPullMessage(__instance);
        }

        // Show the pop message
        public static void ShowPopMessage(EntityClientMessaging.Logic __instance)
        {
            gFlexiPanels.ShowPopMessage(__instance);
        }

        // Show curent target information
        public static void ShowTargetMessage(EntityClientMessaging.Logic __instance)
        {
            gFlexiPanels.ShowTargetMessage(__instance);
        }

        public static void ShowAddMessage(EntityClientMessaging.Logic __instance)
        {
            gFlexiPanels.ShowAddMessage(__instance);
        }
    }
}
