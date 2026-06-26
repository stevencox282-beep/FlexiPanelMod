using Il2Cpp;
using Il2CppServiceStack;
using MelonLoader;
using UnityEngine;

// Mod meta data
[assembly: MelonInfo(typeof(FlexiPanelMod.ModMain), "FlexiPanelMod", "1.0.0", "Anonymous", null)]
[assembly: MelonGame("Visionary Realms", "Pantheon")]

namespace FlexiPanelMod
{
    // This Mod collects all buff/debuff information about all mobs and the party members that are currently in range
    public class ModMain : MelonMod
    {
        // UI Elements
        private static FlexiPanel gFlexiPanels = new FlexiPanel();
        private static string currentTargetNetworkId = string.Empty; // Only update in offensive target select and only use in OnUpdate
        private const float UpdateInterval = 1.0f; // Update interval in seconds
        private static float _timeSinceLastUpdate;
        private static ConfigParser configParser = new ConfigParser(); // Parses the mods configuration
        private static List<string> includeAllBuffsBlacklist = new List<string>(); // Holds the blacklist for IncludeAllBuffs
        private static List<string> includeAllDebuffsBlacklist = new List<string>(); // Holds the blacklist for IncludeAllDebuffs

        // Performed before character selection
        public override void OnInitializeMelon()
        {
            // Read the XML panel configuration
            ReadPanelConfig();
        }

        // Updates the duration timers on the panel
        public override void OnUpdate()
        {
            // Only update if the player is loaded into the world
            if (Globals.PlayerIsLoaded.Equals(true))
            {
                // We only need update granularity of 1 second, save the CPU cycles
                _timeSinceLastUpdate += Time.deltaTime;
                if (_timeSinceLastUpdate >= UpdateInterval)
                {
                    // Update this immediatly so we dont flood in here
                    _timeSinceLastUpdate = 0f;

                    // Get the data needed to update the display (party and enemy)
                    EntityData partyEntityData = EntityManager.GetEntityData(Globals.PartyBuffs);
                    EntityData enemyEntityData = new EntityData();

                    // If currentTargetNetworkId is not populated we still have to process party information
                    if (!currentTargetNetworkId.IsEmpty())
                    {
                        enemyEntityData = EntityManager.GetEntityData(currentTargetNetworkId);
                        // This will occur If the current entity despawns whilst targetted, dont try and update anything
                        if (enemyEntityData == null)
                        {
                            currentTargetNetworkId = string.Empty;
                            // This is horrible as we are allocating a new entity data twice but it works
                            enemyEntityData = new EntityData();
                        }
                        else
                        {
                            // If the enemy is dead, remove all the data
                            if (enemyEntityData.isDead.Equals(true))
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

        // Parses the panel config and stores the panel configuration
        public static void ReadPanelConfig()
        {
            Dictionary<string, PanelConfig> panelConfigDictionary = new Dictionary<string, PanelConfig>(); // All panels
            try
            {
                configParser.ParseConfig(panelConfigDictionary, includeAllBuffsBlacklist, includeAllDebuffsBlacklist);
            }
            catch (Exception e)
            {
                MelonLogger.Error("ERROR: Unable to parse XML configuration");
                throw;
            }
            // Parse out the config to populate the panels and rows
            gFlexiPanels.SetPanelConfig(panelConfigDictionary);

        }

        // Preserves require templates
        public static void PreserveRequiredTransforms()
        {
            // This is a nasty hack but is required because I am too dumb to get resizing panels working
            gFlexiPanels.PreserveRequiredTransforms();
        }

        // Ttear down all the resources allocated by the panel
        public static void ClearTransformDictionaries()
        {
            gFlexiPanels.ClearTransformDictionaries();
        }

        // Create panels
        public static void InitialiseFlexiPanels()
        {
            gFlexiPanels.InitialiseFlexiPanels();
        }

        // Sshow all panels
        public static void ShowFlexiPanels()
        {
            // Display the panel if the gloabl is set to allow it
            if (Globals.ShowPanels.Equals(true))
            {
                gFlexiPanels.ShowFlexiPanels();
            }
        }

        // Hide all panels
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
            if (Globals.GroupMemberNetworkIds.Contains(activeBuff.Target.NetworkId.ToString()) || activeBuff.Target.NetworkId.ToString().Equals(Globals.PlayerNetworkId))
            {
                return true;
            }

            return false;
        }

        // Adds a new buff or refreshes a buff
        public static void OnAddOrRefreshBuff(double time, ActiveBuff activeBuff, bool inBackground, bool isRefresh, bool isItemBuff)
        {
            // Check everything major for null values, sometimes they appear and I dont know why
            if (activeBuff.Target == null || activeBuff.Caster == null)
            {
                return;
            }

            // Make sure we track only valid entities
            if (IsValidTarget(activeBuff))
            {
                // Default to player data
                EntityData entityData = EntityManager.GetEntityData(Globals.PartyBuffs);
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
                    if (enemyEntityData.encounterStartTime.Equals(0L))
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
                    EntityManager.AddEntityToUniqueDebuffs(activeBuff.Target.NetworkId.ToString(), buffData.buffName);
                    EntityManager.AddConsolidatedUptime(activeBuff.Target.NetworkId.ToString(), buffData);
                }
            }
        }

        // Creates a new buff
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
            newDebuff.buffName = activeBuff.BuffData.DisplayName;
            newDebuff.buffDuration = (int)Math.Ceiling(activeBuff.EstimatedTotalTime);
            newDebuff.buffDurationRemaining = (int)Math.Ceiling(activeBuff.EstimatedTotalTime); // This allows us to deal with buffs that have diminishign returns like Bind and Mez
            newDebuff.numStacks = activeBuff.StackCount;
            newDebuff.maxStacks = activeBuff.BuffData.MaxStacks;
            newDebuff.categoryType = activeBuff.BuffData.CategoryType.ToString();
            // Currently a bug in ILC2PP, handle it here and default if we exception
            try
            {
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
                if (buff.buffName.Equals(activeBuff.BuffData.DisplayName) &&
                    activeBuff.Caster.NetworkId.ToString().Equals(buff.casterNetworkId) &&
                    activeBuff.Target.NetworkId.ToString().Equals(buff.targetNetworkId))
                {
                    buff.buffDurationRemaining = (int)Math.Ceiling(activeBuff.EstimatedTotalTime);
                    buff.numStacks = activeBuff.StackCount;
                    return true;
                }
            }
            return false;
        }

        // On target selection / change / deselection / death
        public static void OffensiveTargetSelected(Targets.Logic targetLogic)
        {
            if (targetLogic.Offensive == null)
            {
                // Either the user has pressed ESC so they are targetting nothing or something has gone wrong somewhere
                currentTargetNetworkId = string.Empty;
                return;
            }

            // Do nothing if the same enemy has been re-targetted
            if (currentTargetNetworkId.Equals(targetLogic.Offensive.NetworkId.ToString()))
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
            if (enemyEntityData.isDead.Equals(true))
            {
                return;
            }

            // Establish the target string
            CreateTargetBaseMessage(enemyEntityData);

            EntityData partyEntityData = EntityManager.GetEntityData(Globals.PartyBuffs);
            // Reset the panel, we must do this to clear the window when somebody switches to a new target
            gFlexiPanels.ClearPanelsDisplay();
            gFlexiPanels.UpdatePanelsDisplay(enemyEntityData, partyEntityData, includeAllBuffsBlacklist, includeAllDebuffsBlacklist);

            // Store this for use in OnUpdate()
            currentTargetNetworkId = targetLogic.Offensive.NetworkId.ToString();
        }

        // For the current target, create the new base target message
        private static void CreateTargetBaseMessage(EntityData entityData)
        {
            string baseMessage = (entityData.traits.IsEmpty()) ?
                $"{entityData.targetName.ToTitleCase()}(Lv.{entityData.entityLevel}), {entityData.targetClass}, {entityData.targetKind}" :
                $"{entityData.targetName.ToTitleCase()}(Lv.{entityData.entityLevel}), {entityData.targetClass}, {entityData.targetKind}, {entityData.traits}";
            gFlexiPanels.SetTargetInformation(baseMessage);
        }

        // Removes a specific buff from an entity buff list
        public static void RemoveBuff(double time, ActiveBuff activeBuff)
        {
            // Some times these are null and it is unclear why so always check
            if (activeBuff.Target == null || activeBuff.Caster == null)
            {
                return;
            }

            MelonLogger.Warning($"activeBuff = {activeBuff.BuffData.DisplayName} activeBuff.Caster.Info.DisplayName = {activeBuff.Caster.Info.DisplayName} activeBuff.Target.Info.DisplayName = {activeBuff.Target.Info.DisplayName}");
            // Get the list for the current player
            EntityData enemyEntityData = (currentTargetNetworkId.IsEmpty()) ? new EntityData() : EntityManager.GetEntityData(currentTargetNetworkId);
            EntityData partyEntityData = EntityManager.GetEntityData(Globals.PartyBuffs);
            // Remove the mantle from the list
            for (int i = 0; i < partyEntityData.buffData.Count; i++)
            {
                BuffData buffData = partyEntityData.buffData[i];
                // If we are the correct buff and its the correct target and caster
                if (buffData.buffName.Equals(activeBuff.BuffData.DisplayName) &&
                    buffData.targetNetworkId.ToString().Equals(activeBuff.Target?.NetworkId.ToString()) &&
                    buffData.casterNetworkId.ToString().Equals(activeBuff.Caster?.NetworkId.ToString()))
                {
                    buffData.buffDurationRemaining = 0;
                    buffData.numStacks = activeBuff.StackCount;
                    EntityManager.UpdateDurationRemaining();
                }
            }
        }
        // Show curent target message
        public static void ShowTargetMessage(EntityClientMessaging.Logic __instance, string message)
        {
            gFlexiPanels.ShowTargetMessage(__instance, message);
        }
    }
}
