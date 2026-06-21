using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace FlexiPanelMod.Hooks;

// This Hook fires when teh in game compass loads telling us the UI is ready to render the panels box
// This Hook does NOT fire when you change characters
[HarmonyPatch(typeof(UICompass), nameof(UICompass.Start))]
public class UICompassHooks
{
    private static void Postfix(UICompass __instance)
    {
        // Do not block this on PlayerIsLoaded, it will exception if you do as OnUpdate() gets called before the panel is made and kaboom
        ModMain.PreserveRequiredTransforms();
        ModMain.InitialiseFlexiPanels();
    }
}
