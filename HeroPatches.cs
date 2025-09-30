using HarmonyLib;
using HutongGames.PlayMaker;
namespace ThisSlaps;

internal static class HeroPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(HeroController), "Start")]
    private static void HookHornet(HeroController __instance) {
        __instance.gameObject.AddComponent<SlapController>();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Fsm), nameof(Fsm.EnterState))]
    private static void HookEnterState(Fsm __instance, FsmState state)
    {
        if (__instance.Name == "Silk Specials")
        {
            ThisSlapsPlugin.Log.LogInfo($"Next State: {state.Name}");
        }
    }
}