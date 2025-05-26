using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System;
using FFG.JIME;

namespace JIME_TTS_MOD.Patches
{
    public class TerrainNodesPatch : BasePatch
    {
        
        protected override List<string> TargetMethodNames => [ "CoroutineRevealTerrains" ];
        protected override string[] TargetGameObjectNames => [ "Adventure" ];
        protected override Type TargetComponentType => typeof(Adventure);

        protected override string PrefixMethod => null;
        public TerrainNodesPatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
            : base(methodNameMap, patchedMethods, harmony)
        {
        }

        protected new static void Postfix(GameNode[] terrainNodes)
        {

            if (EventCoordinator.Instance == null)
            {
                JIME_TTS.Log.LogWarning($"Postfix: EventCoordinator instance not found.");
                return;
            }
            EventCoordinator.Instance.RaiseTerrainNodesExecuted(terrainNodes);
        }
    }
}


