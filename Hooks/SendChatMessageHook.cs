using HarmonyLib;
using Il2Cpp;
using Il2CppPantheonPersist;
using Il2CppViNL;
using MelonLoader;

namespace FlexiPanelMod.Hooks;

// This Hook fires when a message is typed into the chat box
[HarmonyPatch(typeof(EntityClientMessaging.Logic), nameof(EntityClientMessaging.Logic.SendChatMessage), typeof(string), typeof(ChatChannelType))]
public class SendChatMessageHook
{
    private static bool Prefix(EntityClientMessaging.Logic __instance, string message, ChatChannelType channel)
    {
        if (Globals.PlayerIsLoaded == true)
        {
            if (message == "/showfp")
            {
                // Shows all configured panels
                Globals.ShowPanels = true;
                ModMain.ShowFlexiPanels();
                return false;
            }

            if (message == "/hidefp")
            {
                // Hide all configured panels
                Globals.ShowPanels = false;
                ModMain.HideFlexiPanels();
                return false;
            }

            // Reload the current panel configuration
            if (message.Equals($"/configfp"))
            {
                ModMain.ClearTransformDictionaries();
                ModMain.ReadPanelConfig();
                ModMain.InitialiseFlexiPanels();
                return false;
            }

            // Shows the pulling message in Group chat
            if (message == "/pull")
            {
                ModMain.ShowPullMessage(__instance);
                return false;
            }

            // Shows the pop message in Group chat
            if (message == "/pop")
            {
                ModMain.ShowPopMessage(__instance);
                return false;
            }

            // Shows the current target information in Group chat
            if (message == "/target")
            {
                ModMain.ShowTargetMessage(__instance);
                return false;
            }

            // Shows the current target information in Group chat
            if (message == "/add")
            {
                ModMain.ShowAddMessage(__instance);
                return false;
            }
        }
        return true;
    }
}