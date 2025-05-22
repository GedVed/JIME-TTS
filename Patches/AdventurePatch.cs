using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;
using FFG.JIME;

namespace ReadTextMod.Patches
{
    public class AdventurePatch : BasePatch
    {
        
        protected override List<string> TargetMethodNames => [ "CoroutineRevealTerrains" ];
        protected override string[] TargetGameObjectNames => [ "Adventure" ];
        protected override Type TargetComponentType => typeof(Adventure);
        

        public AdventurePatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
            : base(methodNameMap, patchedMethods, harmony)
        {
        }

        protected new static void Postfix(GameNode[] terrainNodes)
        {
           if (terrainNodes == null) { ReadText.Log.LogInfo("terrainNodes is null in CoroutineRevealTerrains."); return; }
                ReadText.Log.LogInfo($"CoroutineRevealTerrains completed with {terrainNodes.Length} terrain nodes:");
                for (int i = 0; i < terrainNodes.Length; i++)
                {
                    var terrainNode = terrainNodes[i];
                    if (terrainNode == null)
                    {
                        ReadText.Log.LogInfo($"terrainNodes[{i}] is null");
                        continue;
                    }
                    if (terrainNode.TerrainModel == null)
                    {
                        ReadText.Log.LogInfo($"terrainNodes[{i}].TerrainModel is null");
                        continue;
                    }
                    ReadText.Log.LogInfo($"terrainNodes[{i}].TerrainModel.Id = {terrainNode.TerrainModel.Id}");
                }
        }
    }
}


