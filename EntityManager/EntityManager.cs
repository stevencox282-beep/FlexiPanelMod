using Il2Cpp;
using Il2CppDG.Tweening.Plugins;
using Il2CppPantheonPersist;
using Il2CppServiceStack;
using MelonLoader;
using Unity.Collections;

namespace FlexiBuffDisplayPannel.EntityManager;

public class ConsolidatedUptime()
{
    public string debuffName;
    public long totalEncounterUptime; // Time the debuff has been up as a % of total encounter time
    public float totalEncounterUptimePercent; // Time the debuff has been up as a % of total encounter time
}

public static class EntityManager
{
    private static readonly string[] Blacklist = { "Banner of Arms", "Banner of Onslaught", "Challenger's Banner", "Rallying Banner", "Shieldman's Banner", "ghostly riddler" };
    // Global to hold the list of all debuffs for an entity, it accesses a List of debuffs via a unique network id
    private static Dictionary<string, EntityData> gEntityDebuffDictionary = new Dictionary<string, EntityData>(); // NetworkId, EntityData>

    // Holds the data for calculating Uptime for each debuff
    private static Dictionary<string, List<ConsolidatedUptime>> consolidatedUptimeDictionary = new Dictionary<string, List<ConsolidatedUptime>>(); // networkId, List<debuffName, uptime>
    private static Dictionary<string, List<string>> uniqueDebuffsDictionary = new Dictionary<string, List<string>>(); // NetworkId, List<debuffName>

    // List of debuffs to ignore on the entity
    private static string[] debuffBlacklist = { "Mana Guzzle", "Taunt Immunity", "Feared", "Temporary Invulnerability", "Tainted Claws", "Ready Up!", "Dripping Fangs", "Web Spray", "Icebound Familiar", "Synthetic Toxin", "Repair" };
    private static string[] traitTargetBlacklist = { "Fading Prescence" };
    
    // This funciton returns the entity data for a given network ID
    public static EntityData GetEntityData(string targetNetworkId)
    {
        // We have no target selected
        if (targetNetworkId == null)
        {
            return null;
        }

        // EntityManager will remove entities from the Dictionary on entity death, not on entity despawn, so for now we just have to ignore all failures to find an enemy in the database
        // Not ideal as this will mask genuine problems but there is nothing we can do about it, it is how the Hook for managing NPC entities works
        if (gEntityDebuffDictionary.ContainsKey(targetNetworkId))
        {
            return gEntityDebuffDictionary[targetNetworkId];
        }
        else
        {
            return null;
        }
    }

    // Adds entry to calculate consolidated uptime
    public static void AddConsolidatedUptime(string entityNetworkId, DebuffData debuffData)
    {
        // If we do not have this debuff in our uptime dictionary, add it
        if (!consolidatedUptimeDictionary.ContainsKey(entityNetworkId))
        {
            // Add a new entry with uptime of 0
            List<ConsolidatedUptime> newConsolidatedUptimeList = new List<ConsolidatedUptime>();
            ConsolidatedUptime newConsolidatedUptime = new ConsolidatedUptime();
            newConsolidatedUptime.debuffName = debuffData.debuffName;
            newConsolidatedUptime.totalEncounterUptime = 0;
            newConsolidatedUptime.totalEncounterUptimePercent = 0f;
            newConsolidatedUptimeList.Add(newConsolidatedUptime);
            consolidatedUptimeDictionary.Add(entityNetworkId, newConsolidatedUptimeList);
        }
        else
        {
            // Update the existing row for this debuff
            List<ConsolidatedUptime> consolidatedUptimeList = consolidatedUptimeDictionary[entityNetworkId];
            foreach(var temp in consolidatedUptimeList)
            {
                if (temp.debuffName == debuffData.debuffName)
                {
                    // This debuff already exists, dont add it twice
                    return;
                }
            }
            
            // Add this additional debuff
            ConsolidatedUptime consolidatedUptime = new ConsolidatedUptime();
            consolidatedUptime.debuffName = debuffData.debuffName;
            consolidatedUptime.totalEncounterUptime = 0;
            consolidatedUptime.totalEncounterUptimePercent = 0f;
            consolidatedUptimeList.Add(consolidatedUptime);
        }
    }

    // Gets the total consolidated uptime for an entity and debuff
    public static void IncrementConsolidatedUptime(string entityNetworkId, string debuffName)
    {
        List<ConsolidatedUptime> consolidatedUptimeList = consolidatedUptimeDictionary[entityNetworkId];
        if (!entityNetworkId.IsEmpty() && consolidatedUptimeList != null && consolidatedUptimeList.Count > 0)
        {
            foreach (var uptimeItem in consolidatedUptimeList)
            {
                if (uptimeItem.debuffName == debuffName)
                {
                    uptimeItem.totalEncounterUptime++;
                }
            }
        }
    }

    // Gets the total consolidated uptime for a entity and debuff
    public static long GetConsolidatedUptime(string entityNetworkId, string debuffName)
    {
        List<ConsolidatedUptime> ConsolidatedUptimeList = consolidatedUptimeDictionary[entityNetworkId];
        foreach(var uptimeItem in ConsolidatedUptimeList)
        {
            if (uptimeItem.debuffName == debuffName)
            {
                return uptimeItem.totalEncounterUptime;
            }
        }

        return 0;
    }


    // Adds a debuff to the list of unique entity debuffs, creates a new entity row if needed
    public static void AddEntityToUniqueDebuffs(string entityNetworkId, string debuffName)
    {
        // Add a new entity to the list if this is the first time we are putting debuffs on it
        if (!uniqueDebuffsDictionary.ContainsKey(entityNetworkId))
        {
            uniqueDebuffsDictionary.Add(entityNetworkId, new List<string>());
        }

        // Add a new debuff to the list of debuffs if it does not already exist
        List<string> uniqueDebuffs = uniqueDebuffsDictionary[entityNetworkId];
        if (!uniqueDebuffs.Contains(debuffName))
        {
            uniqueDebuffs.Add(debuffName);
        }
    }

    // This removes a entity from the list of entities with unique debuffs
    public static void RemoveEntityFromUniqueBuffs(string entityNetworkId)
    {
        if (uniqueDebuffsDictionary.ContainsKey(entityNetworkId)) {
            uniqueDebuffsDictionary.Remove(entityNetworkId);
        }
    }

    // This function updates the duration remaining for all the progress bars
    public static void UpdateDurationRemaining()
    {
        for (int i = 0; i < gEntityDebuffDictionary.Count; i++)
        {
            EntityData entityData = gEntityDebuffDictionary.ElementAt(i).Value;
            List<DebuffData> debuffData = entityData.debuffData;

            // For all debuffs for this entity
            for (int j = 0; j < debuffData.Count; j++)
            {
                DebuffData debuff = debuffData.ElementAt(j);
                // Update the time remaining and the size of the progress bar, stop at zero seconds
                debuff.debuffDurationRemaining = (debuff.debuffDurationRemaining == 0) ? 0 : debuff.debuffDurationRemaining - 1;
            } // End of FOR all debuffs for a entity
        } // End of FOR all entities
    }

    // This function updates the uptime for all active debuffs for all entities
    public static void UpdateEncounterUpTime()
    {
        // We need to handle the folllowing scenarios
        // 1) Update the uptime value of an active debuff 
        // 2) Update the uptime value of a debuff that has dropped off the list of active debuffs but might be reapplied later on

        // For any debuff we have ever had for this entity
        var allEntityNetworkIds = uniqueDebuffsDictionary.Keys;
        foreach (var entityNetworkId in allEntityNetworkIds)
        {
            // Incremement this
            gEntityDebuffDictionary[entityNetworkId].totalEncounterTime++;

            var uniqueEntityDebuffList = uniqueDebuffsDictionary[entityNetworkId];
            for (int i = 0; i < uniqueEntityDebuffList.Count; i++)
            {
                // Find every entity debuff that matches this historic debuff name
                string currentHistoricDebuffName = uniqueEntityDebuffList[i];

                // Update encounter uptime for this specific debuff in the list of all entity debuffs
                EntityData entity = gEntityDebuffDictionary[entityNetworkId];
                entity.entityNetworkId = entityNetworkId;

                // For every debuff on a n entity
                foreach (DebuffData debuff in entity.debuffData)
                {
                    // If the debuff on the entity is the debuff we are looking for
                    if (debuff.debuffName == currentHistoricDebuffName)
                    {
                        // Match found, increase the encounter uptime only if the current duration remaining on the buff is > 0
                        if (debuff.debuffDurationRemaining > 0)
                        {
                            EntityManager.IncrementConsolidatedUptime(entity.entityNetworkId, debuff.debuffName);
                            debuff.consolidatedEncounterUptime = EntityManager.GetConsolidatedUptime(entity.entityNetworkId, debuff.debuffName);
                        }
                        
                        // OnUpdate will certainly run before we can target and engage an entity in range, prevent a possible DIV0
                        if (entity.encounterStartTime == 0L)
                        {
                            debuff.consolidatedEncounterUptimePercent = 0L;
                        }
                        else
                        {
                            // Get the time in seconds the encounter has been running
                            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            float currentEncounterDurationS = (float)(currentTime - entity.encounterStartTime);
                            debuff.consolidatedEncounterUptimePercent = (float)(debuff.consolidatedEncounterUptime / (float)(currentTime - entity.encounterStartTime)) * 100;
                            // Cap at 100 and 0, this handles the case when the combat start time and current time are the same
                            if (debuff.consolidatedEncounterUptimePercent > 100)
                            {
                                debuff.consolidatedEncounterUptimePercent = 100;
                            }
                            else if (debuff.consolidatedEncounterUptimePercent < 0)
                            {
                                debuff.consolidatedEncounterUptimePercent = 0;
                            }
                        }
                    }
                }
            }
        }
    }

    // This function checks is there is an entry in the dictionary for casterNetworkId and if not makes one
    public static void AddEntityIfMissing(string targetNetworkId)
    {
        EntityData entityData = EntityManager.GetEntityData(targetNetworkId);        
        // Make a new entity if one does not exist
        if (entityData == null)
        {
            EntityData newMonster = new EntityData();
            newMonster.entityNetworkId = targetNetworkId;
            newMonster.isDead = false;
            newMonster.debuffData = new List<DebuffData>();
            gEntityDebuffDictionary.Add(targetNetworkId, newMonster);
        }
    }

    // Updates the isDead status for a entity
    public static void UpdateEnemyDeadStatus(EntityStatus.Logic entityStatusLogic)
    {
        if (entityStatusLogic == null)
            return;

        string networkId = entityStatusLogic.Entity.NetworkId.ToString();
        bool isDead = entityStatusLogic.Entity.Nameplate.isDead;

        if (gEntityDebuffDictionary.ContainsKey(networkId.ToString()))
        {
            // The API used reports dead enemies as alive when you move out of range, never go back from dead to not dead
            if (isDead == true && gEntityDebuffDictionary[networkId].isDead == false)
            {
                gEntityDebuffDictionary[networkId].isDead = true; // Once set to true can NEVER be set to false
            }
        }
    }

    // Add a entity that has come into render range, including when changing zones and login
    public static void OnNpcAdded(EntityNpcGameObject entityNpcGameObject)
    {
        var npcName = entityNpcGameObject.Nameplate.nameText.text;

        if (entityNpcGameObject.Profession == NpcProfession.None)
        {
            if (entityNpcGameObject.Status.IsDead())
            {
                return;
            }

            // Weird behaviour in game, all NPCs have subname text set to Soandso's Minion, I guess as placeholder, but it
            // never displays this, so we'll rely on it I guess... sometimes minions are bugged and display as attackable
            // NPCs even if they're a player's summon. So we can't just rely on petmaster, as that's set to null in these cases.
            // I bet it's because the Summon enters the player's loadable area before the owner.
            if (entityNpcGameObject.PetMaster != null)
            {
                return;
            }

            if (Blacklist.Contains(npcName))
            {
                return;
            }

            // Add this entity to the list of all entites
            string targetNetworkId = entityNpcGameObject.NetworkId.ToString();
            if (gEntityDebuffDictionary.ContainsKey(entityNpcGameObject.NetworkId.ToString()))
            {
                // We can't do anything about this, but we should log it anyway and return, we do not want dupliicate entries in our dictionary
                MelonLogger.Error($"OnNpcAdded() Entry {entityNpcGameObject.NetworkId.ToString()} already exists in the dictionary, this should never happen");
                return;
            }

            // We do not have this entity in our list, add it
            AddEntityIfMissing(targetNetworkId);
            EntityData newEntity = GetEntityData(targetNetworkId);
            newEntity.entityLevel = entityNpcGameObject.Experience.Level;

            // Pick up any traits if they exist
            bool isFirst = true;
            foreach (ActiveBuff activeBuff in entityNpcGameObject.Buffs.myActiveBuffs)
            {
                string activeBuffName = activeBuff.BuffData.DisplayName.ToString();
                // Find all traits, ignor any traits we dont want to display in the Target bar
                if (activeBuffName.Contains("Trait: ") && !traitTargetBlacklist.Contains(activeBuffName))
                {
                    string[] result = activeBuffName.Split("Trait: ");
                    if (result.Length > 1)
                    {
                        // We have a trait.  if this is the first trait, we dont want the leading comma
                        if (isFirst == true)
                        {
                            newEntity.traits = result[1];
                            isFirst = false;
                        }
                        else
                        {
                            newEntity.traits = newEntity.traits + ", " + result[1];
                        }
                    }
                }
                else
                {
                    // Do not process anything in the blacklist.  TODO - we do not need this, can be removed, just for debuggin purposes
                    if (!debuffBlacklist.Contains(activeBuffName.ToString()))
                    {
                        // Do we do anything about this?
                        MelonLogger.Error($"OnNpcAdded() MONSTER WITH DEBUFF {activeBuffName} FOUND, DO WE WANT TO TRACK THIS ?");
                    }
                }
            }
            // Set the remaining common data
            newEntity.targetClass = entityNpcGameObject.Info.Class.ToString();
            newEntity.targetKind = entityNpcGameObject.Info.Kind.ToString();
            newEntity.targetName = entityNpcGameObject.Nameplate.nameText.text.ToString();
            newEntity.isDead = entityNpcGameObject.Status.IsDead();
        }
    }

    // Removes an emey from the list, on zone, moving out of range or logging out
    public static void OnNpcRemoved(EntityNpcGameObject entityNpcGameObject)
    {
        //  Remove an entry from the dictionary based on the network id
        try
        {
            RemoveEntityFromUniqueBuffs(entityNpcGameObject.NetworkId.ToString());
            gEntityDebuffDictionary.Remove(entityNpcGameObject.NetworkId.ToString());
        }
        catch (Exception e)
        {
            MelonLogger.Error($"OnNpcRemoved() Entry {entityNpcGameObject.NetworkId.ToString()} does not exist");
        }
        
    }
}