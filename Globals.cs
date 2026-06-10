using Il2Cpp;
using UnityEngine;

namespace FlexiBuffDisplayPannel;

public static class Globals
{
    public static bool PanelsInitilisized = false;
    public static bool PlayerIsLoaded = false;
    public static int PlayerLevel = 0;
    public static bool ShowDebuffPanel = true;
    public static EntityPlayerGameObject? LocalPlayer = null;
    
    public static string SetNumberOfRowsCommand = "setdebuffrows";
    public static int NumDisplayableDebuffs = 10;
    
    // Panel / TextMesh Constants
    public static float RowLeftMargin = 0.05f;
    public static int DefaultPanelHeight = 540; // y-axis
    public static int DefaultPanelWidth = 300; // x-axis

    public static int NameMeshWidth = 250;
    public static int NameMeshHeight = 12;
    public static float TimeLeftMargin = 0.75f; // The Time mesh must start after the name text mesh ends and the progress bars end
    public static int   TimeMeshHeight = NameMeshHeight;
    public static int TimeMeshWidth = 75;
    public static int FontSize = 10;
    public static int PixelsToAdd = 6; // Number of pixels to add create enough height for a row to be separate from the one above and below

    // Progress Bar Display Co-ordinates
    public static float TopMargin         = 0.04f;
    public static float InterBarOffset    = 0.028f;
}

