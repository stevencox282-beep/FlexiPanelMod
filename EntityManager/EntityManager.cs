using Il2Cpp;
using Il2CppServiceStack;
using MelonLoader;

namespace FlexiPanelMod;

public static class EntityManager
{
    // Do not track information about these specific NPCs
    private static readonly string[] NPCBlacklist = { "Banner of Arms", "Banner of Onslaught", "Challenger's Banner", "Rallying Banner", "Shieldman's Banner", "ghostly riddler" };
    // Global to hold the list of all Entities
    private static Dictionary<string, EntityData> allEntitiesDictionary = new Dictionary<string, EntityData>();

    // Holds the data for calculating Uptime for each buff
    private static Dictionary<string, List<ConsolidatedUptime>> consolidatedUptimeDictionary = new Dictionary<string, List<ConsolidatedUptime>>();
    private static Dictionary<string, List<string>> allDebuffsPerEntityDictionary = new Dictionary<string, List<string>>();
    private static string traitString = "Trait: ";

    static EntityManager()
    {
        // We add an entity that will contain all party buffs/debuffs
        AddEntityIfMissing(Globals.PartyBuffs);
    }

    public static void ClearEntityDatabase()
    {
        allEntitiesDictionary.Clear();
        consolidatedUptimeDictionary.Clear();
        allDebuffsPerEntityDictionary.Clear();
    }

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
        if (allEntitiesDictionary.ContainsKey(targetNetworkId))
        {
            return allEntitiesDictionary[targetNetworkId];
        }
        else
        {
            return null;
        }
    }

    // Adds entry to calculate consolidated uptime
    public static void AddConsolidatedUptime(string entityNetworkId, BuffData buffData)
    {
        // If we do not have this buff in our uptime dictionary, add it
        if (!consolidatedUptimeDictionary.ContainsKey(entityNetworkId))
        {
            // Add a new entry with uptime of 0
            List<ConsolidatedUptime> newConsolidatedUptimeList = new List<ConsolidatedUptime>();
            ConsolidatedUptime newConsolidatedUptime = new ConsolidatedUptime();
            newConsolidatedUptime.buffName = buffData.buffName;
            newConsolidatedUptime.totalEncounterUptime = 0;
            newConsolidatedUptime.totalEncounterUptimePercent = 0f;
            newConsolidatedUptimeList.Add(newConsolidatedUptime);
            consolidatedUptimeDictionary.Add(entityNetworkId, newConsolidatedUptimeList);
        }
        else
        {
            // Update the existing row for this buff
            List<ConsolidatedUptime> consolidatedUptimeList = consolidatedUptimeDictionary[entityNetworkId];
            foreach (var temp in consolidatedUptimeList)
            {
                if (temp.buffName.Equals(buffData.buffName))
                {
                    // This buff already exists, dont add it twice
                    return;
                }
            }

            // Add this additional buff
            ConsolidatedUptime consolidatedUptime = new ConsolidatedUptime();
            consolidatedUptime.buffName = buffData.buffName;
            consolidatedUptime.totalEncounterUptime = 0;
            consolidatedUptime.totalEncounterUptimePercent = 0f;
            consolidatedUptimeList.Add(consolidatedUptime);
        }
    }

    // Gets the total consolidated uptime for an entity and buff
    public static void IncrementConsolidatedUptime(string entityNetworkId, string buffName)
    {
        List<ConsolidatedUptime> consolidatedUptimeList = consolidatedUptimeDictionary[entityNetworkId];
        if (!entityNetworkId.IsEmpty() && consolidatedUptimeList != null && consolidatedUptimeList.Count > 0)
        {
            foreach (var uptimeItem in consolidatedUptimeList)
            {
                if (uptimeItem.buffName.Equals(buffName))
                {
                    uptimeItem.totalEncounterUptime++;
                }
            }
        }
    }

    // Gets the total consolidated uptime for a entity and buff
    public static long GetConsolidatedUptime(string entityNetworkId, string buffName)
    {
        List<ConsolidatedUptime> ConsolidatedUptimeList = consolidatedUptimeDictionary[entityNetworkId];
        foreach (var uptimeItem in ConsolidatedUptimeList)
        {
            if (uptimeItem.buffName.Equals(buffName))
            {
                return uptimeItem.totalEncounterUptime;
            }
        }

        return 0;
    }


    // Adds a buff to the list of unique entity buffs, creates a new entity row if needed
    public static void AddEntityToUniqueDebuffs(string entityNetworkId, string buffName)
    {
        // Add a new entity to the list if this is the first time we are putting buffs on it
        if (!allDebuffsPerEntityDictionary.ContainsKey(entityNetworkId))
        {
            allDebuffsPerEntityDictionary.Add(entityNetworkId, new List<string>());
        }

        // Add a new buff to the list of buffs if it does not already exist
        List<string> uniqueDebuffs = allDebuffsPerEntityDictionary[entityNetworkId];
        if (!uniqueDebuffs.Contains(buffName))
        {
            uniqueDebuffs.Add(buffName);
        }
    }

    // This removes a entity from the list of entities with unique debuffs
    public static void RemoveEntityFromUniqueBuffs(string entityNetworkId)
    {
        if (allDebuffsPerEntityDictionary.ContainsKey(entityNetworkId))
        {
            allDebuffsPerEntityDictionary.Remove(entityNetworkId);
        }
    }

    // This function updates the duration remaining for all the progress bars
    public static void UpdateDurationRemaining(bool removal = true)
    {
        for (int i = 0; i < allEntitiesDictionary.Count; i++)
        {
            EntityData entityData = allEntitiesDictionary.ElementAt(i).Value;
            List<BuffData> buffData = entityData.buffData;

            for (int j = buffData.Count - 1; j >= 0; j--)
            {
                BuffData buff = buffData.ElementAt(j);
                // Update the time remaining and the size of the progress bar, stop at zero seconds
                buff.buffDurationRemaining = (buff.buffDurationRemaining.Equals(0)) ? 0 : buff.buffDurationRemaining - 1;
                if (buff.buffDurationRemaining <= 0 && removal.Equals(true))
                {
                    buffData.RemoveAt(j);
                }
            } // End of FOR all debuffs
        } // End of FOR all entities
    }

    // This function updates the uptime for all active debuffs for all entities
    public static void UpdateEncounterUpTime()
    {
        // We need to handle the folllowing scenarios
        // 1) Update the uptime value of an active buff
        // 2) Update the uptime value of a buff that has dropped off the list of active debuffs but might be reapplied later on

        List<string> allEntityIds = new List<string>(allEntitiesDictionary.Keys);
        List<string> allDebuffEntityIds = new List<string>(allDebuffsPerEntityDictionary.Keys);

        // For any debbuff we have ever had for this entity
        foreach (string targetEntityNetworkId in allDebuffEntityIds)
        {
            // If we have a match process further
            if (allEntityIds.Contains(targetEntityNetworkId))
            {
                EntityData entity = allEntitiesDictionary[targetEntityNetworkId];
                entity.totalEncounterTime++;
                entity.entityNetworkId = targetEntityNetworkId;

                var uniqueEntityDebuffList = allDebuffsPerEntityDictionary[targetEntityNetworkId];
                for (int i = 0; i < uniqueEntityDebuffList.Count; i++)
                {
                    // Find every entity buff that matches this historic buff name
                    string currentHistoricDebuffName = uniqueEntityDebuffList[i];

                    // For every buff on a n entity
                    foreach (BuffData buff in entity.buffData)
                    {
                        // If the buff on the entity is the buff we are looking for
                        if (buff.buffName.Equals(currentHistoricDebuffName))
                        {
                            // Match found, increase the encounter uptime only if the current duration remaining on the buff is > 0
                            if (buff.buffDurationRemaining > 0)
                            {
                                EntityManager.IncrementConsolidatedUptime(entity.entityNetworkId, buff.buffName);
                                buff.consolidatedEncounterUptime = EntityManager.GetConsolidatedUptime(entity.entityNetworkId, buff.buffName);
                            }

                            // OnUpdate will certainly run before we can target and engage an entity in range, prevent a possible DIV0
                            if (entity.encounterStartTime.Equals(0L))
                            {
                                buff.consolidatedEncounterUptimePercent = 0L;
                            }
                            else
                            {
                                // Get the time in seconds the encounter has been running
                                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                float currentEncounterDurationS = (float)(currentTime - entity.encounterStartTime);
                                buff.consolidatedEncounterUptimePercent = (float)(buff.consolidatedEncounterUptime / (float)(currentTime - entity.encounterStartTime)) * 100;
                                // Cap at 100 and 0, this handles the case when the combat start time and current time are the same
                                if (buff.consolidatedEncounterUptimePercent > 100)
                                {
                                    buff.consolidatedEncounterUptimePercent = 100;
                                }
                                else if (buff.consolidatedEncounterUptimePercent < 0)
                                {
                                    buff.consolidatedEncounterUptimePercent = 0;
                                }
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
            newMonster.buffData = new List<BuffData>();
            newMonster.targetName = (targetNetworkId.Equals(Globals.PartyBuffs)) ? Globals.PartyBuffs : string.Empty; // Set to PARTY if we are creating the party entity
            allEntitiesDictionary.Add(targetNetworkId, newMonster);
        }
    }

    // Updates the isDead status for a entity
    public static void UpdateEnemyDeadStatus(EntityStatus.Logic entityStatusLogic)
    {
        if (entityStatusLogic == null)
            return;

        string networkId = entityStatusLogic.Entity.NetworkId.ToString();
        bool isDead = entityStatusLogic.Entity.Nameplate.isDead;

        if (allEntitiesDictionary.ContainsKey(networkId))
        {
            // The API used reports dead enemies as alive when you move out of range, never go back from dead to not dead
            if (isDead.Equals(true) && allEntitiesDictionary[networkId].isDead.Equals(false))
            {
                allEntitiesDictionary[networkId].isDead = true; // Once set to true can NEVER be set to false
            }
        }
    }

    // Add a entity that has come into render range, including when changing zones and login
    public static void OnEntityAdded(EntityNpcGameObject entityNpcGameObject)
    {
        var npcName = entityNpcGameObject.Nameplate.nameText.text;

        if (entityNpcGameObject.Profession.Equals(NpcProfession.None))
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

            if (NPCBlacklist.Contains(npcName))
            {
                return;
            }

            // Add this entity to the list of all entites
            string targetNetworkId = entityNpcGameObject.NetworkId.ToString();
            if (allEntitiesDictionary.ContainsKey(entityNpcGameObject.NetworkId.ToString()))
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
                string activeBuffName = activeBuff.BuffData.DisplayName;
                // Find all traits, ignore any traits we dont want to display in the Target bar
                if (activeBuffName.Contains(traitString))
                {
                    string[] result = activeBuffName.Split(traitString);
                    if (result.Length > 1)
                    {
                        // We have a trait.  if this is the first trait, we dont want the leading comma
                        if (isFirst.Equals(true))
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
            }
            // Set the remaining common data
            newEntity.targetClass = entityNpcGameObject.Info.Class.ToString();
            newEntity.targetKind = entityNpcGameObject.Info.Kind.ToString();
            newEntity.targetName = entityNpcGameObject.Nameplate.nameText.text;
            newEntity.isDead = entityNpcGameObject.Status.IsDead();
        }
    }

    // Removes an emey from the list, on zone, moving out of range or logging out
    public static void OnEntityRemoved(EntityNpcGameObject entityNpcGameObject)
    {
        //  Remove an entry from the dictionary based on the network id
        try
        {
            RemoveEntityFromUniqueBuffs(entityNpcGameObject.NetworkId.ToString());
            allEntitiesDictionary.Remove(entityNpcGameObject.NetworkId.ToString());
        }
        catch (Exception e)
        {
            MelonLogger.Error($"OnNpcRemoved() Entry {entityNpcGameObject.NetworkId.ToString()} does not exist");
        }
    }
}