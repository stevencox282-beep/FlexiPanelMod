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
            if (message == "/showpanels")
            {
                // Sets show debuffs to true then show the debuff panel
                Globals.ShowDebuffPanel = true;
                ModMain.ShowDebuffPanel();
                return false;
            }

            if (message == "/hidepanels")
            {
                // Set show debuffs to false and hide the debuff panel
                Globals.ShowDebuffPanel = false;
                ModMain.HideDebuffPanel();
                return false;
            }

            // Change the number of rows for a given panel.  /setpanelrows <numRows> <panelID>
            if (message.Contains($"/{Globals.SetNumberOfRowsCommand}"))
            {
                ModMain.SetNumDebuffRows(message);
                return false;
            }

            // Reload the panel configuration
            if (message.Equals($"/reloadflexipanels"))
            {
                ModMain.ClearPanelLists();
                ModMain.InitPanels();
                ModMain.DisplayPanels();
                
                return false;
            }


            //            if (message == "/pulling")
            //            {
            //                // Set show debuffs to false and hide the debuff panel
            //                Globals.ShowDebuffPanel = false;
            //                ModMain.ShowPullMessage(__instance);
            //                return false;
            //            }

            //            if (message == "/popping")
            //            {
            //                // Set show debuffs to false and hide the debuff panel
            //                Globals.ShowDebuffPanel = false;
            //                ModMain.ShowPopMessage(__instance);
            //                return false;
            //            }
        }
        return true;
    }
}