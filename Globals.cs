using Il2Cpp;

namespace FlexiPanelMod;

public static class Globals
{
    // Local Player
    public static bool PlayerIsLoaded = false; // Set true when a character loads/unloads from the game world / changes zones
    public static int PlayerLevel = 0; // Holds the current player level
    public static string PlayerNetworkId = string.Empty;
    public static EntityPlayerGameObject? LocalPlayer = null;

    // Group Members
    public static List<string> GroupMemberNetworkIds = new List<string>(); // List of all group members (does not include LocalPlayer)
    public static List<string> GroupMemberNames = new List<string>(); // List of all group members (does not include LocalPlayer)
    public static string Party = "Party Buffs"; // Used to create an EntityData that holds buff/debuff information for the party and local player

    // Panel/TextMeshs
    public static bool ShowPanels = true; // Controls wether or not the panels will be displayed
    public static int DefaultPanelHeight = 540; // y-axis
    public static int DefaultPanelWidth = 300; // x-axis
    public static int DefaultNameMeshWidth = 250;
    public static int MinimumRowWidth = 200; // Anything less than this just makes no sense, you wont be able to see anything at all
    public static int PixelsNeededForUptime = 75; // Number of pixel required to display the uptime part
    public static float RowLeftMargin = 0.05f;
    public static int NameMeshHeight = 12;
    public static float TimeLeftMargin = 0.75f; // The Time mesh must start after the name text mesh ends and the progress bars end
    public static int TimeMeshHeight = NameMeshHeight;
    public static int TimeMeshWidth = 75;
    public static int FontSize = 10;
    public static int PixelsToAdd = 6; // Number of pixels to add create enough height for a row to be separate from the one above and below
    // Progress Bar Display Co-ordinates
    public static float TopMargin = 0.04f;
    public static float InterBarOffset = 0.028f;
}

// Holds the data for each entity
public class EntityData()
{
    // Some of these are duplicated in the buffs but putting them here as well makes accessing the information much easier
    public long encounterStartTime; // Total encounter time for this entity
    public string entityNetworkId; // network id of this entity
    public long totalEncounterTime; // Time the entity has been engaged
    public string targetName; // Nameplate name of the target
    public string targetKind; // Humanoid, Undead etc.
    public string targetClass; // Rogue, Wizard etc.
    public string traits; // Concatenated string of all traits to be displayed
    public int entityLevel; // level of the entity
    public bool isDead; // The death status for an entity always changes to false when you move out of range, even if it is still dead, so now we track if it has ever been dead
    public List<BuffData> buffData = new List<BuffData>();
}

// Holds all the buff data to display in the panel
public class BuffData()
{
    public string buffName; // Base name of the buff
    public float buffDuration; // Buff duration
    public float buffDurationRemaining; // Used in the panel to keep track of remaining duration

    // Do NOT move these into EntityData, each entity can have multiple characters attacking it and casting spells on it, it has to be per buff
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

    public long consolidatedEncounterUptime; // Time the buff has been up as a % of total encounter time
    public float consolidatedEncounterUptimePercent; // Time the buff has been up as a % of total encounter time
}

// Holds the data for the panesl to be displayed
public class PanelConfig()
{
    public string panelID; // Unique ID used to identify this exact panel
    public string panelTitle; // Hold the panel title to be displayed
    public string targetOrTitle; // Determines if we display the Panel title or Target information
    public int panelWidth; // Width of the panel in pixels
    public int rowNameWidth; // Width of the name portion of a row in pixels
    public float panelOpacity; // Integer from 0 to 100 to be converted to a float 0.0 to 1.0f for setting alpha value of background image
    public bool excludeAllBuffs; // Indicates if the panel ignores all buffs (has priority over includeAllBuffs)
    public bool excludeAllDebuffs; // Indicates if the panel ignores all debuffs (has priority over includeAllBuffs)
    public bool includeAllBuffs; // Indicates if the panel ignores all buffs
    public bool includeAllDebuffs; // Indicates if the panel ignores all debuffs
    public int rowsToDisplay; // Number of rows for the panel
    public List<RowConfig> rowConfig;
}

// Holds the data for a row in the panel
public class RowConfig()
{
    public string displayText;  // The name of the buff/debuff that this row config will apply too
    public string color; // Colour of the bar
    public string include; // Include who we track this buff/debuff for.  Valid includes are:  [Me]/[Party]/David,Sharon,Peter
};