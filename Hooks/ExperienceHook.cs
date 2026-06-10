using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace FlexiBuffDisplayPannel.Hooks;

[HarmonyPatch(typeof(Experience.Logic), nameof(Experience.Logic.SetLevel))]
public class ExperienceHookSetLevel
{
    private static void Postfix(Experience.Logic __instance, int level, bool resetCurrentExperience, bool levelUpEvent)
    {
        if (__instance != null && Globals.PlayerIsLoaded == true)
        {
            if (__instance.Entity.NetworkId.Value == Globals.LocalPlayer.NetworkId.Value)
            {
                Globals.PlayerLevel = level;
            }
        }
        return;
    }
}
