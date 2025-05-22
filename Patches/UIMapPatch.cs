
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
        private static readonly List<string> TargetMethodNames =
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


        protected override void PatchMethods(GameObject targetObject)
        {
            
            Component component = targetObject.GetComponent(TargetComponentType);
            if (component == null)
            {
                ReadText.Log.LogError($"UIMap component not found on {targetObject.name}.");
                return;
            }

            int successfulPatches = 0;
            foreach (var methodName in TargetMethodNames)
            {
                MethodInfo targetMethod = TargetComponentType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (targetMethod == null)
                {
                    ReadText.Log.LogWarning($"Method {methodName} not found in {TargetComponentType.Name} with specified BindingFlags.");
                    continue;
                }

                try
                {
                    MethodNameMap[targetMethod] = methodName;
                    var postfix = new HarmonyMethod(typeof(UIMapPatch).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic));
                    HarmonyInstance.Patch(targetMethod, postfix: postfix);

                    var patchInfo = Harmony.GetPatchInfo(targetMethod);
                    if (patchInfo?.Postfixes?.Any(p => p.owner == HarmonyInstance.Id) == true)
                    {
                        PatchedMethods.Add(methodName);
                        successfulPatches++;
                        LogPatchSuccess(methodName, successfulPatches, TargetMethodNames.Count);
                    }
                    else
                    {
                        LogPatchFailure(methodName);
                    }
                }
                catch (Exception ex)
                {
                    LogPatchFailure(methodName, ex);
                }
            }
        }

        private static void Postfix(MethodInfo __originalMethod, object __instance, object[] __args)
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


