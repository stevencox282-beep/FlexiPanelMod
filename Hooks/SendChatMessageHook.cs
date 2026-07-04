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
                Globals.UpdatePanels = false;
                ModMain.ClearTransformDictionaries();
                ModMain.InitialiseFlexiPanels();
                Globals.UpdatePanels = true;
                return false;
            }

            if (message.Equals("/fphide"))
            {
                // Hide all configured panels
                Globals.UpdatePanels = false;
                ModMain.HideFlexiPanels();
                return false;
            }

            // Reload the current panel configuration
            if (message.Equals($"/fpreload"))
            {
                Globals.UpdatePanels = false;
                ModMain.ClearTransformDictionaries();
                ModMain.ReadPanelConfig();
                ModMain.InitialiseFlexiPanels();
                Globals.UpdatePanels = true;
                return false;
            }
        }
        return true;
    }
}