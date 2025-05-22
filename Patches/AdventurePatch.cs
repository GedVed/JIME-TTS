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
        
        private static readonly List<string> TargetMethodNames =[ "CoroutineRevealTerrains" ];

        protected override string[] TargetGameObjectNames => [ "Adventure" ];
        protected override Type TargetComponentType => typeof(Adventure);
        protected override bool UsesDynamicPatching => true;

        public AdventurePatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
            : base(methodNameMap, patchedMethods, harmony)
        {
        }

        protected override void PatchMethods(GameObject targetObject)
        {
            
            Component component = targetObject.GetComponent(TargetComponentType);
            if (component == null)
            {
                ReadText.Log.LogError($"Adventure component not found on {targetObject.name}.");
                return;
            }

            int successfulPatches = 0;
            foreach (var methodName in TargetMethodNames)
            {
                MethodInfo targetMethod = TargetComponentType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (targetMethod == null)
                {
                    ReadText.Log.LogWarning($"Method {methodName} not found in {TargetComponentType.Name} with specified BindingFlags.");
                    var methods = TargetComponentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                   // ReadText.Log.LogInfo($"Available methods in {TargetComponentType.Name}: {string.Join(", ", methods.Select(m => m.Name))}");
                    continue;
                }

                try
                {
                    MethodNameMap[targetMethod] = methodName;
                    var postfix = new HarmonyMethod(typeof(AdventurePatch).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic));
                    HarmonyInstance.Patch(targetMethod, postfix: postfix);

                    var patchInfo = Harmony.GetPatchInfo(targetMethod);
                    if (patchInfo?.Postfixes?.Any(p => p.owner == HarmonyInstance.Id) == true)
                    {
                        PatchedMethods.Add(methodName);
                        successfulPatches++;
                        LogPatchSuccess(methodName, successfulPatches, TargetMethodNames.Count);
                    }
                    else
                    {
                        LogPatchFailure(methodName);
                    }
                }
                catch (Exception ex)
                {
                    LogPatchFailure(methodName, ex);
                }
            }
            ReadText.Log.LogInfo($"AdventurePatch completed for {targetObject.name}. methodNameMap contains: {string.Join(", ", MethodNameMap.Values)}");
        }

        private static void Postfix(GameNode[] terrainNodes)
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


