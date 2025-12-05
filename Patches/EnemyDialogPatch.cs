using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System;
using FFG.JIME;

namespace JIME_TTS_MOD.Patches
{
    public class EnemyDialogPatch : BasePatch
    {
        protected override List<string> TargetMethodNames => ["OnButtonConfirmClicked"];

        protected override string[] TargetGameObjectNames => ["Dialog_EnemyInfo/Combat"];
        
        protected override Type TargetComponentType => typeof(EnemyInfoDialog);

        protected override string PrefixMethod => null;

        public EnemyDialogPatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
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

            if (__instance is EnemyInfoDialog EnemyDialog && EnemyDialog.gameObject != null)
            {
                var gameObject = EnemyDialog.gameObject;
                //Reflection to get specific private property
                var packet = typeof(EnemyInfoDialog).GetField("_labelConfirmationDialog", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(EnemyDialog) as UILocalizationPacket;

                if(packet == null)
                {
                    JIME_TTS.Log.LogInfo("EnemyInfoDialog unable to find UiLocalization packet");
                }
                else
                {
                    EventCoordinator.Instance.RaiseEnemyDialogExecuted(gameObject, EnemyDialog, packet);
                }

                
                
            }
            else
            {
                JIME_TTS.Log.LogWarning($"Postfix: Instance is not EnemyInfoDialog or gameObject is null.");
            } 
        }
    }
}
