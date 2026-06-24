using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace FlexiPanelMod.Hooks;

// This Hook fires when your character enters in the world (after character selection) or change of zone
[HarmonyPatch(typeof(EntityPlayerGameObject), nameof(EntityPlayerGameObject.NetworkStart))]
public class PlayerNetworkStart
{
    private static void Postfix(EntityPlayerGameObject __instance)
    {
        // Fired in character select
        if (__instance.NetworkId.Value.Equals(1))
        {
            Globals.PlayerIsLoaded = false;
            return;
        }

        if (__instance.NetworkId.Value.Equals(EntityPlayerGameObject.LocalPlayerId.Value))
        {
            Globals.LocalPlayer = __instance;
            if (Globals.PlayerNetworkId.Equals(string.Empty))
            {
                Globals.PlayerNetworkId = EntityPlayerGameObject.LocalPlayerId.ToString();
                // We add an entity that will contain all party buffs/debuffs
                EntityManager.AddEntityIfMissing(Globals.Party);
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
            Globals.PlayerIsLoaded = true;
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
        if (__instance.NetworkId.Value.Equals(1))
        {
            Globals.PlayerIsLoaded = false;
            return;
        }

        if (__instance.NetworkId.Value.Equals(EntityPlayerGameObject.LocalPlayerId.Value))
        {
            // We have logged out / changed zones.  Clear the screen as we cant reliably tell what buffs have / have not been preserved during zone transition or between logout and login
            Globals.PlayerIsLoaded = false;
            EntityManager.ClearEntityDatabase();
            Globals.LocalPlayer = null;
            Globals.PlayerNetworkId = string.Empty;
            Globals.PlayerLevel = 0;
            return;
        }
    }
}