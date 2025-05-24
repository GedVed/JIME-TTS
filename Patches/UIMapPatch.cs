
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
        

        public UIMapPatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
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

            
            if (__instance is UIMapScene uiMap && uiMap.gameObject != null)
            {
                var gameObject = uiMap.gameObject;
                var isActive = gameObject.activeInHierarchy;
                var packet = uiMap.Label_AdventureEpilogue;
                EventCoordinator.Instance.RaiseUIMapExecuted(gameObject, isActive, uiMap, packet);
                
            }
            else
            {
                ReadText.Log.LogWarning($"Postfix: Instance is not UIMapScene or gameObject is null.");
            }
        }
    }
}


