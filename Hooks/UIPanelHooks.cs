using HarmonyLib;
using Il2Cpp;

namespace FlexiBuffDisplayPannel.Hooks;

// This Hook fires when teh in game compass loads telling us the UI is ready to render the panels box
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
