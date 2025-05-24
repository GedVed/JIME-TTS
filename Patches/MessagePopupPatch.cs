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
            

            public MessagePopupPatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
                : base(methodNameMap, patchedMethods, harmony)
            {
            }

            protected new static void Postfix(object __instance, object[] __args)
            {
    
                if (EventCoordinator.Instance == null)
                {
                    ReadText.Log.LogWarning($"Postfix: EventCoordinator instance not found.");
                    return;
                }

                if (__instance is MessagePopup messagePopup && messagePopup.gameObject != null)
                {
                    var gameObject = messagePopup.gameObject;
                    var isActive = gameObject.activeInHierarchy;
                    var packet = __args.OfType<LocalizationPacket>().FirstOrDefault();
                    EventCoordinator.Instance.RaiseMessagePopupMethodExecuted(gameObject, isActive, messagePopup, packet);
                    
                }
                else
                {
                    ReadText.Log.LogWarning($"Postfix: Instance is not MessagePopup or gameObject is null.");
                }
                
                
            }
        }
    }


