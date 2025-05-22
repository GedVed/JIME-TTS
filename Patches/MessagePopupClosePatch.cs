using FFG.Common;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;

namespace ReadTextMod.Patches
{
    public class MessagePopupClosePatch : BasePatch
    {
        private static readonly List<string> TargetMethodNames =
        [
            "ClearCallbacks"
        ];

        protected override string[] TargetGameObjectNames => ["MessagePopup_New"];
        protected override Type TargetComponentType => typeof(MessagePopup);
        protected override bool UsesDynamicPatching => true;

        public MessagePopupClosePatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
            : base(methodNameMap, patchedMethods, harmony)
        {
        }

        protected override void PatchMethods(GameObject targetObject)
        {
            Component component = targetObject.GetComponent(TargetComponentType);
            if (component == null)
            {
                ReadText.Log.LogError($"MessagePopup component not found on {targetObject.name}.");
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
                    var postfix = new HarmonyMethod(typeof(MessagePopupClosePatch).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic));
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
            ReadText.Log.LogInfo($"MessagePopupClosePatch completed for {targetObject.name}. methodNameMap contains: {string.Join(", ", MethodNameMap.Values)}");
        }

        private static void Postfix(object __instance)
        {
            if (__instance == null)
            {
                ReadText.Log.LogWarning($"Postfix: Invalid instance for MessagePopup.Close.");
                return;
            }

            if (MethodPatcher.Instance == null)
            {
                ReadText.Log.LogWarning($"Postfix: MethodPatcher instance not found.");
                return;
            }

            if (__instance is MessagePopup messagePopup && messagePopup.gameObject != null)
            {
                var isActive = messagePopup.gameObject.activeInHierarchy;
                MethodPatcher.Instance.RaiseMessagePopupCloseExecuted(isActive, messagePopup);
                ReadText.Log.LogInfo($"Postfix executed for CloseCallbacks on {messagePopup.gameObject.name} (Active: {isActive}).");
            }
            else
            {
                ReadText.Log.LogWarning($"Postfix: Instance is not MessagePopup or gameObject is null for MessagePopup.Close.");
            }
        }
    }
}
