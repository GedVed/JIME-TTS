using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Collections;
using ReadTextMod;
using ReadTextMod.Patches;


public class MethodPatcher
{
    public static MethodPatcher Instance { get; private set; }

    // State
    private readonly Harmony Harmony;
    private readonly Dictionary<MethodInfo, string> MethodNameMap = []; // Maps MethodInfo to methodName
    private readonly List<string> PatchedMethods = [];
    private readonly List<BasePatch> Patches;

    public MethodPatcher(Harmony harmony)
    {
        Instance = this;
        Harmony = harmony ?? throw new ArgumentNullException(nameof(harmony));
        ReadText.Log.LogInfo("MethodPatcher instance created.");
        Patches = new List<BasePatch>
        {
            new TerrainNodes(MethodNameMap, PatchedMethods, Harmony),
            new MessagePopupPatch(MethodNameMap, PatchedMethods, Harmony),
            new MessagePopupClosePatch(MethodNameMap, PatchedMethods, Harmony),
            new UIMapPatch(MethodNameMap, PatchedMethods, Harmony),

        };
    }

    public static void Initialize(Harmony harmony, MonoBehaviour monoBehaviour)
    {
        var resolver = new MethodPatcher(harmony);
        monoBehaviour.StartCoroutine(resolver.TryPatchMethods());
    }

    // Coroutine to retry patching until any GameObject is found or timeout
    private IEnumerator TryPatchMethods()
    {
        ReadText.Log.LogInfo("Starting patching attempt for MessagePopup, UIMapScene, and Adventure.");
        float startTime = Time.time;
        while (Time.time - startTime < BasePatch.GetTimeout())
        {
            bool allPatched = true;
            foreach (var patch in Patches)
            {
                if (!patch.TryPatchNow())
                {
                    allPatched = false;
                }
            }

            if (allPatched)
            {
                ReadText.Log.LogInfo($"Patching completed for all patches. methodNameMap contains: {string.Join(", ", MethodNameMap.Values)}");
                yield break;
            }

            yield return null;
        }

        ReadText.Log.LogError($"Failed to complete patching after {BasePatch.GetTimeout()} seconds. methodNameMap contains: {string.Join(", ", MethodNameMap.Values)}");
    }

    public Dictionary<MethodInfo, string> GetMethodNameMap()
    {
        return MethodNameMap;
    }

}
    

