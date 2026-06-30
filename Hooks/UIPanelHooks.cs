using HarmonyLib;
using Il2Cpp;

namespace FlexiPanelMod.Hooks;

// This Hook fires when the in game compass loads telling us the UI is ready to render the panels box
// This Hook does NOT fire when you change characters
[HarmonyPatch(typeof(UICompass), nameof(UICompass.Start))]
public class UICompassHooks
{
    private static void Postfix(UICompass __instance)
    {
        // Do not block this on PlayerIsLoaded, it will exception if you do as OnUpdate() gets called before the panel is made and kaboom
        FlexiPanelBuilder.PreserveRequiredTransforms();
        ModMain.InitialiseFlexiPanels();
        Globals.UpdatePanels = true;
    }
}