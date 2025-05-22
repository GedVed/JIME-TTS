
using HarmonyLib;
using FFG.JIME;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System;
using System.Collections.Generic;


namespace ReadTextMod.Patches
{

    public class UIMapPatch : BasePatch
    {
        protected override List<string> TargetMethodNames =>
        [
        "OnLeftButtonDetailedClick",
        "OnRightButtonDetailedClick",
        "OnContinueButtonClicked"
        ];
        protected override string[] TargetGameObjectNames => ["UI_Map"];
        protected override Type TargetComponentType => typeof(UIMapScene);
        protected override bool UsesDynamicPatching => true;

        public UIMapPatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
            : base(methodNameMap, patchedMethods, harmony)
        {
        }

        
        protected new static void Postfix(MethodInfo __originalMethod, object __instance, object[] __args)
        {
            if (__originalMethod == null || __instance == null)
            {
                ReadText.Log.LogWarning($"Postfix: Invalid method or instance for {__originalMethod?.DeclaringType.Name}.{__originalMethod?.Name}.");
                return;
            }

            if (MethodPatcher.Instance == null)
            {
                ReadText.Log.LogWarning($"Postfix: MethodPatcher instance not found.");
                return;
            }

            string methodName = __originalMethod.Name;
            if (__instance is UIMapScene uiMap && uiMap.gameObject != null)
            {
                var gameObject = uiMap.gameObject;
                var isActive = gameObject.activeInHierarchy;
                var packet = uiMap.Label_AdventureEpilogue;
                MethodPatcher.Instance.RaiseUIMapExecuted(methodName, gameObject, isActive, uiMap, packet);
                ReadText.Log.LogInfo($"Postfix executed for {methodName} on {gameObject.name} (Active: {isActive}).");
            }
            else
            {
                ReadText.Log.LogWarning($"Postfix: Instance is not UIMapScene or gameObject is null for {__originalMethod.DeclaringType.Name}.{__originalMethod.Name}.");
            }
        }
    }
}


