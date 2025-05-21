using HarmonyLib;
using FFG.JIME;
using ReadTextMod;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System;
using System.Collections.Generic;




[HarmonyPatch(typeof(Adventure), "CoroutineRevealTerrains")]
static class AdventurePatch
{

    public static bool TryPatchAdventureMethodsNow(ref bool IsAdventurePatched, Dictionary<MethodInfo, string> MethodNameMap, List<string> PatchedMethods, Harmony Harmony)
    {
        if (IsAdventurePatched)
        {
            ReadText.Log.LogInfo("TryPatchAdventureMethodsNow: Patching already completed, skipping attempt.");
            return true;
        }

        GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject targetObject = allGameObjects.FirstOrDefault(go => go.GetComponent<Adventure>() != null);
        if (targetObject != null)
        {
            ReadText.Log.LogInfo($"Found target GameObject: {targetObject.name} with Adventure component. Proceeding with patching.");
            PatchAdventureMethods(MethodNameMap, PatchedMethods, Harmony);
            IsAdventurePatched = true;
            return true;
        }
        return false;
    }

    private static void PatchAdventureMethods(Dictionary<MethodInfo, string> MethodNameMap, List<string> PatchedMethods, Harmony Harmony)
    {
        
        Type componentType = typeof(Adventure);
        MethodInfo targetMethod = componentType.GetMethod("CoroutineRevealTerrains", BindingFlags.Public | BindingFlags.Instance);
        if (targetMethod == null)
        {
            ReadText.Log.LogError("Method CoroutineRevealTerrains not found in Adventure.");
            return;
        }
        
        try
        {
            MethodNameMap[targetMethod] = "CoroutineRevealTerrains";
            var prefix = new HarmonyMethod(typeof(AdventurePatch).GetMethod(nameof(PrefixCoroutineRevealTerrains), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public));
            Harmony.Patch(targetMethod, prefix: prefix);

            var patchInfo = Harmony.GetPatchInfo(targetMethod);
            if (patchInfo?.Prefixes?.Any(p => p.owner == Harmony.Id) == true)
            {
                PatchedMethods.Add("CoroutineRevealTerrains");
                ReadText.Log.LogInfo("Successfully patched method CoroutineRevealTerrains in Adventure.");
            }
            else
            {
                ReadText.Log.LogError("Failed to verify patch for method CoroutineRevealTerrains in Adventure.");
            }
        }
        catch (Exception ex)
        {
            ReadText.Log.LogError($"Exception while patching method CoroutineRevealTerrains: {ex.Message}");
        }
    }
    private static void PrefixCoroutineRevealTerrains(GameNode[] terrainNodes)
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

