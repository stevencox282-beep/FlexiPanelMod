using HarmonyLib;
using Il2Cpp;

namespace FlexiPanelMod.Hooks;

public static class EntityStatusLogicHook
{
    private const float UpdateInterval = 0.75f; // Update interval in seconds
    private static float _timeSinceLastUpdate;

    // This HarmonyPatch fires every frame which is too frequent for our use so we throttle it.
    // This Hook has a unwanted behaviour, it has a range that is smaller than the render range so when you kill a entity
    //   and move away from it, before it is un-rendered this Hook starts reporting the entity is alive, even though it is very clearly dead on the screen still.
    // We can not use Hook isDead which ALWAYS returns false, Hook isDeadOrNearDead NEVER fires
    [HarmonyPatch(typeof(EntityStatus.Logic), nameof(EntityStatus.Logic.IsAlive))]
    public class EntityStausIsDeadLogicHook
    {
        private static void Prefix(EntityStatus.Logic __instance)
        {
            if (__instance == null)
                return;

            // Throttle this so it doesnt fire every frame
            _timeSinceLastUpdate += UnityEngine.Time.deltaTime;
            if (_timeSinceLastUpdate >= UpdateInterval)
            {
                // Update this immediatly so we dont flood in here
                _timeSinceLastUpdate = 0f;

                // We must use TRY here, on zone change/exiting this API continues to fire despite everything being torn down around it, it probably should not.
                // Perhaps I should not be using this API call but this is the best way to handle death notifications I can find
                try
                {
                    EntityManager.UpdateEnemyDeadStatus(__instance);
                }
                catch (Exception e) { }
            }
            return;
        }
    }
}
