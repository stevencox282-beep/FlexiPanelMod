using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppViNL;

namespace FlexiBuffDisplayPannel.Hooks;

// This Hook fires when you join aparty or somebody joins / leaves a party.  It does NOT handle you leaving the party
[HarmonyPatch(typeof(Group.Logic), nameof(Group.Logic.UpdateGroupMembers))]
public class GroupUpdateGroupMembersHook
{
    private static void Postfix(Group.Logic __instance, Il2CppReferenceArray<GroupMember> members, Il2CppStructArray<NetworkId> membersPetIds)
    {
        // Clear the list, then update it with the new list of members
        Globals.GroupMembers.Clear();
        foreach (var item in members)
        {
            Globals.GroupMembers.Add(item.EntityNetworkId.ToString());
        }
    }
}


// This Hook fires when you join aparty or somebody joins / leaves a party.  It does NOT handle you leaving the party
[HarmonyPatch(typeof(Group.Logic), nameof(Group.Logic.LeftGroup))]
public class GroupLeftGroupMembersHook
{
    private static void Postfix(Group.Logic __instance, bool forceLeaveAllMembers = false)
    {
        Globals.GroupMembers.Clear();
    }
}