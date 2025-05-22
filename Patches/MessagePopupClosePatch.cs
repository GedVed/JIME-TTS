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
        protected override List<string> TargetMethodNames => ["ClearCallbacks"];

        protected override string[] TargetGameObjectNames => ["MessagePopup_New"];
        protected override Type TargetComponentType => typeof(MessagePopup);
        protected override bool UsesDynamicPatching => true;

        public MessagePopupClosePatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
            : base(methodNameMap, patchedMethods, harmony)
        {
        }

        protected new static void Postfix(object __instance)
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
