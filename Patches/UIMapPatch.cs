
using HarmonyLib;
using FFG.JIME;
using System.Reflection;
using System;
using System.Collections.Generic;


namespace JIME_TTS_MOD.Patches
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
            
            if (EventCoordinator.Instance == null)
            {
                JIME_TTS.Log.LogWarning($"Postfix: EventCoordinator instance not found.");
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
                JIME_TTS.Log.LogWarning($"Postfix: Instance is not UIMapScene or gameObject is null.");
            }
        }
    }
}


