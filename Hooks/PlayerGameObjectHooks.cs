using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace FlexiBuffDisplayPannel.Hooks;

// This Hook fires when your character enters in the world (after character selection) or change of zone
[HarmonyPatch(typeof(EntityPlayerGameObject), nameof(EntityPlayerGameObject.NetworkStart))]
public class PlayerNetworkStart
{
    private static void Postfix(EntityPlayerGameObject __instance)
    {
        // Fired in character select
        if (__instance.NetworkId.Value == 1)
        {
            Globals.PlayerIsLoaded = false;
            return;
        }

        if (__instance.NetworkId.Value == EntityPlayerGameObject.LocalPlayerId.Value)
        {
            Globals.LocalPlayer = __instance;
            Globals.PlayerIsLoaded = true;
            if (Globals.PlayerNetworkId.Equals(string.Empty))
            {
                Globals.PlayerNetworkId = EntityPlayerGameObject.LocalPlayerId.ToString();
            }

            try
            {
                Globals.PlayerLevel = Int32.Parse(__instance.Nameplate.levelText.text); // Updates level on character load / zone
            }
            catch
            {
                MelonLogger.Error("Could not convert Local Player level to int, defaulting to player level 0");
                Globals.PlayerLevel = 0;
            }

            
            return;
        }
    }
}

// This Hook fires when you character exits the game or exits a zone
[HarmonyPatch(typeof(EntityPlayerGameObject), nameof(EntityPlayerGameObject.NetworkStop))]
public class PlayerNetworkStop
{
    private static void Prefix(EntityPlayerGameObject __instance)
    {
        // Fired in character select
        if (__instance.NetworkId.Value == 1)
        {
            Globals.PlayerIsLoaded = false;
            return;
        }

        if (__instance.NetworkId.Value == EntityPlayerGameObject.LocalPlayerId.Value)
        {
            Globals.LocalPlayer = null;
            Globals.PlayerIsLoaded = false;
            Globals.PlayerNetworkId = string.Empty;
            Globals.PlayerLevel = 0;
            return;
        }
    }
}