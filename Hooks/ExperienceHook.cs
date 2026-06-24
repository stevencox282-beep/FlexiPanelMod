using HarmonyLib;
using Il2Cpp;

namespace FlexiPanelMod.Hooks;

// This Hook fires when somebody levels up for down
[HarmonyPatch(typeof(Experience.Logic), nameof(Experience.Logic.SetLevel))]
public class ExperienceHookSetLevel
{
    private static void Postfix(Experience.Logic __instance, int level, bool resetCurrentExperience, bool levelUpEvent)
    {
        if (__instance != null && Globals.PlayerIsLoaded.Equals(true))
        {
            if (__instance.Entity.NetworkId.Value.Equals(Globals.LocalPlayer.NetworkId.Value))
            {
                Globals.PlayerLevel = level;
            }
        }
        return;
    }
}
