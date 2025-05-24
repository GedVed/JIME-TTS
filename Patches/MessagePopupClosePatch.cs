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
        

        public MessagePopupClosePatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
            : base(methodNameMap, patchedMethods, harmony)
        {
        }

        protected new static void Postfix(object __instance, object[] __args)
        {
            
            if (MethodPatcher.Instance == null)
            {
                ReadText.Log.LogWarning($"Postfix: MethodPatcher instance not found.");
                return;
            }

            if (__instance is MessagePopup messagePopup && messagePopup.gameObject != null)
            {
                var isActive = messagePopup.gameObject.activeInHierarchy;
                EventCoordinator.Instance.RaiseMessagePopupCloseExecuted(isActive, messagePopup);
            }
            else
            {
                ReadText.Log.LogWarning($"Postfix: Instance is not MessagePopup or gameObject is null for MessagePopup.Close.");
            }
        }
    }
}
