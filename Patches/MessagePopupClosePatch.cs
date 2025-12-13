using FFG.Common;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace JIME_TTS_MOD.Patches
{
    public class MessagePopupClosePatch : BasePatch
    {
        protected override List<string> TargetMethodNames => ["ClearCallbacks"];

        protected override string[] TargetGameObjectNames => ["MessagePopup_New"];

        protected override Type TargetComponentType => typeof(MessagePopup);

        protected override List<string> PrefixMethods => [];

        public MessagePopupClosePatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
            : base(methodNameMap, patchedMethods, harmony)
        {
        }

        protected new static void Postfix(object __instance, object[] __args)
        {
            
            if (EventCoordinator.Instance == null)
            {
                JIME_TTS.Log.LogWarning($"Postfix: EventCoordinator instance not found.");
                return;
            }

            if (__instance is MessagePopup messagePopup && messagePopup.gameObject != null)
            {
                EventCoordinator.Instance.RaiseMessagePopupCloseExecuted(messagePopup.gameObject.activeInHierarchy);
            }
            else
            {
                JIME_TTS.Log.LogWarning($"Postfix: Instance is not MessagePopup or gameObject is null for MessagePopup.Close.");
            }
        }
    }
}
