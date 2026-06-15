using HarmonyLib;
using Il2Cpp;
using Il2CppViNL;
using MelonLoader;

namespace FlexiBuffDisplayPannel.Hooks;

// This Hook is fired on login (after character selection) and change of zone
[HarmonyPatch(typeof(EntityNpcGameObject))]
[HarmonyPatch(nameof(EntityNpcGameObject.NetworkStart))]
public class NetworkStart
{
    private static void Postfix(EntityNpcGameObject __instance, NetworkObject networkObject)
    {
        EntityManager.EntityManager.OnNpcAdded(__instance);
    }
}

// This Hook is fired on log out change of zone
[HarmonyPatch(typeof(EntityNpcGameObject))]
[HarmonyPatch(nameof(EntityNpcGameObject.NetworkStop))]
public class NetworkStop
{
    private static void Prefix(EntityNpcGameObject __instance)
    {
        EntityManager.EntityManager.OnNpcRemoved(__instance);
    }
}