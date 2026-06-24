using HarmonyLib;
using Il2Cpp;

namespace FlexiPanelMod.Hooks;

// This hook fires when any entity in range receives a buff/debuff
[HarmonyPatch(typeof(Buffs.Logic), nameof(Buffs.Logic.Add), typeof(double), typeof(ActiveBuff), typeof(bool), typeof(bool), typeof(bool))]
public class BuffLogicAdd
{
    private static void Prefix(double time, ActiveBuff buff, bool putInBackground = false, bool isRefresh = false, bool isItemBuff = false)
    {
        ModMain.OnAddOrRefreshBuff(time, buff, putInBackground, isRefresh, isItemBuff);
    }
}

// This hook fires when any entity in range looses a buff/debuff
[HarmonyPatch(typeof(Buffs.Logic), nameof(Buffs.Logic.RemoveMyActiveBuff), typeof(double), typeof(ActiveBuff))]
public class BuffLogicRemove
{
    private static void Prefix(double time, ActiveBuff buff)
    {
        ModMain.RemoveBuff(time, buff);
    }
}