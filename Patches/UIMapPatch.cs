
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

        protected override string PrefixMethod => "OnContinueButtonClicked";
        public UIMapPatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
            : base(methodNameMap, patchedMethods, harmony)
        {
        }


        protected new static void Prefix(object __instance, object[] __args)
        {

            if (EventCoordinator.Instance == null)
            {
                JIME_TTS.Log.LogWarning($"Postfix: EventCoordinator instance not found.");
                return;
            }

            if (__instance is UIMapScene uiMap && uiMap.gameObject != null)
            {
                bool isEpilogueVisible = Traverse.Create(uiMap).Field("isEpilogueVisible").GetValue<bool>();

                if (isEpilogueVisible)
                {
                    JIME_TTS.Log.LogInfo($"Prefix: Sound will not play due to scene transistion.");
                }
                else
                {
                    EventCoordinator.Instance.RaiseUIMapExecuted(uiMap, uiMap.Label_AdventureEpilogue);
                }

            }
            else
            {
                JIME_TTS.Log.LogWarning($"Prefix: Instance is not UIMapScene or gameObject is null.");
            }
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
        
                EventCoordinator.Instance.RaiseUIMapExecuted(uiMap, uiMap.Label_AdventureEpilogue);
            
            }
            else
            {
                JIME_TTS.Log.LogWarning($"Postfix: Instance is not UIMapScene or gameObject is null.");
            }

        }
    }
}


