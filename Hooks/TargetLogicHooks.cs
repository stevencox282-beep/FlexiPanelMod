using HarmonyLib;
using Il2Cpp;

namespace FlexiBuffDisplayPannel.Hooks;

// This Hook fires when an offensive target is selected / reselected
[HarmonyPatch(typeof(Targets.Logic), nameof(Targets.Logic.SetOffensive))]
public class TargetSetOffensiveHook
{
    private static void Postfix(Targets.Logic __instance)
    {
        ModMain.OffensiveTargetSelected(__instance);
    }
}