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
                    gFlexiPanels.UpdatePanelsDisplay(enemyEntityData, partyEntityData);
                }
            }
        }

        // Parses the panel config and sets up the panels
        public static void ReadPanelConfig()
        {
            try
            {
                configParser.ParseConfig(ref panelConfigDictionary);
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

        // Adds a new buff or refreshes a buff to the entities list of all buffs
        public static void OnAddOrRefreshBuff(double time, ActiveBuff activeBuff, bool inBackground, bool isRefresh, bool isItemBuff)
        {
            //            MelonLogger.Warning($"OnAddOrRefreshBuff() 0a buff.BuffData.DisplayName.ToString() = {buff.BuffData.DisplayName.ToString()}, isRefresh = {isRefresh}, inBackground = {inBackground}, isItemBuff = {isItemBuff}");
            //            MelonLogger.Warning($"OnAddOrRefreshBuff() 0b buff.Target?.NetworkId.ToString() = {buff.Target?.NetworkId.ToString()}, buff.Target.Nameplate.nameText.text = {buff.Target?.Nameplate?.nameText.text}, gCurrentTargetNetworkId = {gCurrentTargetNetworkId}");
            //            MelonLogger.Warning($"OnAddOrRefreshBuff() 0c buff.Caster?.NetworkId.ToString() = {buff.Caster?.NetworkId.ToString()}, buff.Caster.Nameplate.nameText.text = {buff.Caster?.Nameplate?.nameText.text}, gCurrentTargetNetworkId = {gCurrentTargetNetworkId}");

            // Make sure we track only valid entities
            if (IsValidTarget(activeBuff))
            {
                // Check everything major for null values, sometimes they appear and I dont know why
                if (activeBuff.BuffData == null || activeBuff.Target == null || activeBuff.Target.Nameplate == null || activeBuff.Caster == null || activeBuff.Caster.Nameplate == null)
                {
                    return;
                }

                EntityData entityData = new EntityData();
                EntityData enemyEntityData = new EntityData();
                EntityData partyEntityData = EntityManager.GetEntityData(Globals.Party);

                // If this is a buff going onto an Enemy (we do track) or a pet (which dont track)
                if (activeBuff.Target.Info.AccessLevel.Equals(AccessLevel.None))
                {
                    // This will set enemyEntityData to NULL if it does not find a match
                    enemyEntityData = EntityManager.GetEntityData(activeBuff.Target.NetworkId.ToString());
                }

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

                // If this is a buff going onto a member of the party or the local player
                if (activeBuff.BuffData.CategoryType == BuffCategoryType.Beneficial &&
                   (Globals.GroupMemberNetworkIds.Contains(activeBuff.Caster?.NetworkId.ToString()) || activeBuff.Target.NetworkId.ToString() == Globals.PlayerNetworkId.ToString()))
                {
                    // We are a buff
                    entityData = partyEntityData;
                    if (gCurrentTargetNetworkId != "")
                    {
                        enemyEntityData = EntityManager.GetEntityData(gCurrentTargetNetworkId);
                    }
                }
                else
                {
                    // We are a buff
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
            gFlexiPanels.UpdatePanelsDisplay(enemyEntityData, partyEntityData);

            // Store this for use in OnUpdate()
            gCurrentTargetNetworkId = targetLogic.Offensive.NetworkId.ToString();
        }


        // Removes a specific buff from an entity buff list
        public static void RemoveBuff(double time, ActiveBuff activeBuff)
        {
            //            MelonLogger.Warning($"RemoveDeBuff() 0a activeBuff.BuffData.DisplayName.ToString() = {activeBuff.BuffData.DisplayName.ToString()}");
            //            MelonLogger.Warning($"RemoveDeBuff() 0b activeBuff.Target?.NetworkId.ToString() = {activeBuff.Target?.NetworkId.ToString()}, activeBuff.Target.Nameplate.nameText.text = {activeBuff.Target?.Nameplate?.nameText.text}, gCurrentTargetNetworkId = {gCurrentTargetNetworkId}");
            //            MelonLogger.Warning($"RemoveDeBuff() 0c activeBuff.Caster?.NetworkId.ToString() = {activeBuff.Caster?.NetworkId.ToString()}, activeBuff.Caster.Nameplate.nameText.text = {activeBuff.Caster?.Nameplate?.nameText.text}, gCurrentTargetNetworkId = {gCurrentTargetNetworkId}");

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
    }
}
