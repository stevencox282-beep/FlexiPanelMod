using HarmonyLib;
using Il2Cpp;
using Il2CppPantheonPersist;

namespace FlexiBuffDisplayPannel.Hooks;

// This Hook fires when a message is typed into the chat box
[HarmonyPatch(typeof(EntityClientMessaging.Logic), nameof(EntityClientMessaging.Logic.SendChatMessage), typeof(string), typeof(ChatChannelType))]
public class SendChatMessageHook
{
    private static bool Prefix(EntityClientMessaging.Logic __instance, string message, ChatChannelType channel)
    {
        if (Globals.PlayerIsLoaded == true)
        {
            if (message == "/showflexipanels")
            {
                // Sets show debuffs to true then show the debuff panel
                Globals.ShowDebuffPanel = true;
                ModMain.ShowFlexiPanels();
                return false;
            }

            if (message == "/hideflexipanels")
            {
                // Set show debuffs to false and hide the debuff panel
                Globals.ShowDebuffPanel = false;
                ModMain.HideFlexiPanels();
                return false;
            }

            // Reload the current panel configuration
            if (message.Equals($"/resetflexipanels"))
            {
                ModMain.ClearTransformDictionaries();
                ModMain.PanelConfig();
                ModMain.InitialiseFlexiPanels();

                return false;
            }

            if (message == "/pulling")
            {
                // Set show debuffs to false and hide the debuff panel
                ModMain.ShowPullMessage(__instance);
                return false;
            }

            if (message == "/pop")
            {
                // Set show debuffs to false and hide the debuff panel
                ModMain.ShowPopMessage(__instance);
                return false;
            }

            if (message == "/target")
            {
                // Set show debuffs to false and hide the debuff panel
                ModMain.ShowTargetMessage(__instance);
                return false;
            }
        }
        return true;
    }
}