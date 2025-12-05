using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using FFG.JIME;



namespace JIME_TTS_MOD.Patches
{
    public abstract class BasePatch
    {   
        //5minutes patch timeout
        protected static readonly float PatchTimeout = 3600f; //1hour patch time 

        // Abstract properties for patch-specific configuration
        protected abstract string[] TargetGameObjectNames { get; }
        protected abstract Type TargetComponentType { get; }
        
        protected abstract string PrefixMethod { get; }
        protected abstract List<string> TargetMethodNames { get; }

        // Properties for patching status and data
        protected bool IsPatched { get; private set; }
        protected Dictionary<MethodInfo, string> MethodNameMap { get; }
        protected List<string> PatchedMethods { get; }
        protected Harmony HarmonyInstance { get; }

        protected BasePatch(Dictionary<MethodInfo, string> methodNameMap, List<string> patchedMethods, Harmony harmony)
        {
            MethodNameMap = methodNameMap ?? throw new ArgumentNullException(nameof(methodNameMap));
            PatchedMethods = patchedMethods ?? throw new ArgumentNullException(nameof(patchedMethods));
            HarmonyInstance = harmony ?? throw new ArgumentNullException(nameof(harmony));
            IsPatched = false;
        }

        
        
        public static float GetTimeout()
        {
            return PatchTimeout;
        }
        public bool TryPatchNow()
        {
            if (IsPatched)
            {
                //JIME_TTS.Log.LogInfo($"{GetType().Name} patching already completed, skipping attempt.");
                return true;
            }

            return TryPatchDynamic();
        
        }

        protected virtual bool TryPatchDynamic()
        {
            GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            bool foundAnyTarget = false;

            foreach (var gameObjectName in TargetGameObjectNames)
            {
                GameObject targetObject = allGameObjects.FirstOrDefault(go => go.GetComponent(TargetComponentType) != null);
                if (targetObject != null)
                {
                    JIME_TTS.Log.LogInfo($"Found target GameObject: {targetObject.name} (Active: {targetObject.activeInHierarchy}, Scene: {targetObject.scene.name}) for {GetType().Name}.");
                    PatchMethods(targetObject);
                    foundAnyTarget = true;
                }
            }

            if (foundAnyTarget)
            {
                IsPatched = true;
                JIME_TTS.Log.LogInfo($"{GetType().Name} patching completed.");
                return true;
            }

            //JIME_TTS.Log.LogWarning($"No target GameObjects found for {GetType().Name}.");
            return false;
        }



        protected virtual void PatchMethods(GameObject targetObject)
        {
            Component component = targetObject.GetComponent(TargetComponentType);
            if (component == null)
            {
                JIME_TTS.Log.LogError($"{TargetComponentType} component not found on {targetObject.name}.");
                return;
            }

            int successfulPatches = 0;
            foreach (var methodName in TargetMethodNames)
            {
                MethodInfo targetMethod = TargetComponentType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (targetMethod == null)
                {
                    JIME_TTS.Log.LogWarning($"Method {methodName} not found in {TargetComponentType.Name} with specified BindingFlags.");
                    continue;
                }

                try
                {
                    if (methodName != PrefixMethod)
                    {
                        MethodNameMap[targetMethod] = methodName;
                        var postfix = new HarmonyMethod(GetType().GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic));
                        HarmonyInstance.Patch(targetMethod, postfix: postfix);

                        var patchInfo = Harmony.GetPatchInfo(targetMethod);
                        if (patchInfo?.Postfixes?.Any(p => p.owner == HarmonyInstance.Id) == true)
                        {
                            PatchedMethods.Add(methodName);
                            successfulPatches++;
                            LogPatchSuccess(methodName + "_Postfix", successfulPatches, TargetMethodNames.Count);
                        }
                        else
                        {
                            LogPatchFailure(methodName);
                        }
                    }
                    else
                    {
                        MethodNameMap[targetMethod] = methodName;
                        var prefix = new HarmonyMethod(GetType().GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));
                        HarmonyInstance.Patch(targetMethod, prefix: prefix);

                        var patchInfo = Harmony.GetPatchInfo(targetMethod);
                        if (patchInfo?.Prefixes?.Any(p => p.owner == HarmonyInstance.Id) == true)
                        {
                            PatchedMethods.Add(methodName);
                            successfulPatches++;
                            LogPatchSuccess(methodName + "_Prefix", successfulPatches, TargetMethodNames.Count);
                        }
                        else
                        {
                            LogPatchFailure(methodName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogPatchFailure(methodName, ex);
                }
            }
        }

        protected static void Postfix(object __instance, object[] __args)
        {
            JIME_TTS.Log.LogWarning($"{nameof(BasePatch)}.Postfix called but not implemented.");
        }
        protected static void Postfix(GameNode[] terrainNodes )
        {
            JIME_TTS.Log.LogWarning($"{nameof(BasePatch)}.Postfix called but not implemented.");
        }
        protected static void Prefix(object __instance, object[] __args)
        {
            JIME_TTS.Log.LogWarning($"{nameof(BasePatch)}.Prefix called but not implemented.");
        }
        
        protected void LogPatchSuccess(string methodName, int successfulPatches, int totalMethods)
        {
            JIME_TTS.Log.LogInfo($"Successfully patched method {methodName} in {TargetComponentType.Name}.");
            JIME_TTS.Log.LogInfo($"{GetType().Name} patching completed: {successfulPatches}/{totalMethods} methods patched.");
        }

        protected void LogPatchFailure(string methodName, Exception ex = null)
        {
            if (ex != null)
            {
                JIME_TTS.Log.LogError($"Exception while patching method {methodName} in {TargetComponentType.Name}: {ex.Message}\n{ex.StackTrace}");
            }
            else
            {
                JIME_TTS.Log.LogError($"Failed to verify patch for method {methodName} in {TargetComponentType.Name}.");
            }
        }
    }
}