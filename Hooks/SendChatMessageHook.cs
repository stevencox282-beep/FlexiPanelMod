using HarmonyLib;
using Il2Cpp;
using Il2CppPantheonPersist;

namespace FlexiPanelMod.Hooks;

// This Hook fires when a message is typed into the chat box
[HarmonyPatch(typeof(EntityClientMessaging.Logic), nameof(EntityClientMessaging.Logic.SendChatMessage), typeof(string), typeof(ChatChannelType))]
public class SendChatMessageHook
{
    private static bool Prefix(EntityClientMessaging.Logic __instance, string message, ChatChannelType channel)
    {
        if (Globals.PlayerIsLoaded.Equals(true))
        {
            if (message.Equals("/fpshow"))
            {
                // Shows all configured panels
                ModMain.ClearTransformDictionaries();
                ModMain.InitialiseFlexiPanels();
                Globals.ShowPanels = true;
                return false;
            }

            if (message.Equals("/fphide"))
            {
                // Hide all configured panels
                Globals.ShowPanels = false;
                ModMain.HideFlexiPanels();
                return false;
            }

            // Reload the current panel configuration
            if (message.Equals($"/fpreload"))
            {
                ModMain.ClearTransformDictionaries();
                ModMain.ReadPanelConfig();
                ModMain.InitialiseFlexiPanels();
                return false;
            }

            // Shows the current target information in Group chat
            if (message.Contains("/fptarget"))
            {
                ModMain.ShowTargetMessage(__instance, message);
                return false;
            }
        }
        return true;
    }
}