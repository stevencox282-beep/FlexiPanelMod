using HarmonyLib;
using Il2Cpp;

namespace FlexiBuffDisplayPannel.Hooks;

// This hook fires when any entity in range receives any buff / debuff
[HarmonyPatch(typeof(Buffs.Logic), nameof(Buffs.Logic.Add), typeof(double), typeof(ActiveBuff), typeof(bool), typeof(bool), typeof(bool))]
public class BuffLogicAdd
{
    private static void Prefix(double time, ActiveBuff buff, bool putInBackground = false, bool isRefresh = false, bool isItemBuff = false)
    {
        ModMain.OnAddOrRefreshBuff(time, buff, putInBackground, isRefresh, isItemBuff);
    }
}
