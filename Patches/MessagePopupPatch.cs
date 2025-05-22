using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;
using FFG.Common;

namespace ReadTextMod.Patches
{
        public class MessagePopupPatch : BasePatch
        {
            protected override List<string> TargetMethodNames => new List<string>
            {
            "ShowNormalMessage",
            "ShowConfirmCancelMessage",
            "ShowNumberedChoiceMessage",
            "ShowMultipleChoiceMessage",
            "ShowChallengeMessage",
            "ShowAdditionalInformationMessage",
            "ShowPlayerSelectMessage",
            "ShowHighlightSelectMessage",
            };

            protected override string[] TargetGameObjectNames => ["MessagePopup_New"];
            protected override Type TargetComponentType => typeof(MessagePopup);
            protected override bool UsesDynamicPatching => true;

            public MessagePopupPatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
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
                        var postfix = new HarmonyMethod(typeof(MessagePopupPatch).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic));
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

                var methodNameMap = MethodPatcher.Instance.GetMethodNameMap();
                if (methodNameMap.TryGetValue(__originalMethod, out string methodName))
                {
                    if (__instance is MessagePopup messagePopup && messagePopup.gameObject != null)
                    {
                        var gameObject = messagePopup.gameObject;
                        var isActive = gameObject.activeInHierarchy;
                        var packet = __args.OfType<LocalizationPacket>().FirstOrDefault();
                        MethodPatcher.Instance.RaiseMessagePopupMethodExecuted(methodName, gameObject, isActive, messagePopup, packet);
                        ReadText.Log.LogInfo($"Postfix executed for {methodName} on {gameObject.name} (Active: {isActive}).");
                    }
                    else
                    {
                        ReadText.Log.LogWarning($"Postfix: Instance is not MessagePopup or gameObject is null for {__originalMethod.DeclaringType.Name}.{__originalMethod.Name}.");
                    }
                }
                else
                {
                    ReadText.Log.LogWarning($"Method {__originalMethod.DeclaringType.Name}.{__originalMethod.Name} executed but not found in methodName map. Available keys: {string.Join(", ", methodNameMap.Values)}");
                }
            }
        }
    }


