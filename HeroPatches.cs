using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace ThisSlaps;

internal static class HeroPatches
{
    // [HarmonyPrefix]
    // [HarmonyPatch(typeof(PlayMakerFSM), "Start")]
    // private static void HookHornet(PlayMakerFSM __instance) {
    //     // ThisSlapsPlugin.Log.LogInfo($"Name: {__instance.name} | FsmName: {__instance.FsmName} | Layer: {__instance.gameObject.layer}");
    //     if (__instance.FsmName == "Silk Specials")
    //     {
    //         ThisSlapsPlugin.Log.LogInfo($"Name: {__instance.name} | Layer: {__instance.gameObject.layer}");
    //     }
    //     if (/*__instance.name == "Hero_Hornet(Clone)" &&*/ __instance.FsmName == "Silk Specials" &&
    //         __instance.gameObject.layer == LayerMask.NameToLayer("Player")) {
    //         ThisSlapsPlugin.Log.LogInfo("Trying to add component!");
    //         __instance.gameObject.AddComponent<SlapController>();
    //     }
    // }
    
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
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DamageEnemies), nameof(DamageEnemies.DoDamage), [typeof(GameObject), typeof(bool)])]
    private static void HookDoDamage(DamageEnemies __instance, GameObject target, bool isFirstHit)
    {
        if (__instance.name == "Slap_Damager")
        {
            ThisSlapsPlugin.Log.LogInfo($"Damage target: {target.name}");
        }
    }
    
    
    // [HarmonyPrefix]
    // [HarmonyPatch(typeof(HeroControllerMethods), nameof(HeroControllerMethods.OnEnter))]
    // private static void HookEnterAction(HeroControllerMethods __instance)
    // {
    //     ThisSlapsPlugin.Log.LogInfo($"Entered!!!!!!!!!!!!!!!");
    //     if (__instance.method == HeroControllerMethods.Method.RelinquishControl)
    //     {
    //         ThisSlapsPlugin.Log.LogInfo($"RelinquishControl()");
    //     }
    //     if (__instance.method == HeroControllerMethods.Method.StopAnimationControl)
    //     {
    //         ThisSlapsPlugin.Log.LogInfo($"StopAnimationControl()");
    //     }
    // }

    // [HarmonyPrefix]
    // [HarmonyPatch(typeof(HeroController), nameof(HeroController.StopAnimationControl))]
    // private static void HookStopAnim()
    // {
    //     ThisSlapsPlugin.Log.LogInfo("Animation should be stopped!");
    // }
    
    // [HarmonyPrefix]
    // [HarmonyPatch(typeof(HeroController), nameof(HeroController.StartAnimationControl))]
    // private static void HookStartAnim()
    // {
    //     ThisSlapsPlugin.Log.LogInfo("Animation should be started!");
    // }
}