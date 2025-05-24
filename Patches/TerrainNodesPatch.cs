using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System;
using FFG.JIME;

namespace ReadTextMod.Patches
{
    public class TerrainNodesPatch : BasePatch
    {
        
        protected override List<string> TargetMethodNames => [ "CoroutineRevealTerrains" ];
        protected override string[] TargetGameObjectNames => [ "Adventure" ];
        protected override Type TargetComponentType => typeof(Adventure);
        

        public TerrainNodesPatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
            : base(methodNameMap, patchedMethods, harmony)
        {
        }

        protected new static void Postfix(GameNode[] terrainNodes)
        {

            if (EventCoordinator.Instance == null)
            {
                ReadText.Log.LogWarning($"Postfix: EventCoordinator instance not found.");
                return;
            }
            EventCoordinator.Instance.RaiseTerrainNodesExecuted(terrainNodes);
        }
    }
}


