using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using FFG.JIME;
using FFG.JIME.Actions;

namespace ReadTextMod.Patches
{
    public abstract class BasePatch
    {
        protected static readonly float PatchTimeout = 60f;

        // Abstract properties for patch-specific configuration
        protected abstract string[] TargetGameObjectNames { get; }
        protected abstract Type TargetComponentType { get; }
        

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
               // ReadText.Log.LogInfo($"{GetType().Name} patching already completed, skipping attempt.");
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
                    ReadText.Log.LogInfo($"Found target GameObject: {targetObject.name} (Active: {targetObject.activeInHierarchy}, Scene: {targetObject.scene.name}) for {GetType().Name}.");
                    PatchMethods(targetObject);
                    foundAnyTarget = true;
                }
                else
                {
                    //ReadText.Log.LogInfo($"GameObject {gameObjectName} not found or lacks {TargetComponentType.Name} component for {GetType().Name}.");
                }
            }

            if (foundAnyTarget)
            {
                IsPatched = true;
                ReadText.Log.LogInfo($"{GetType().Name} patching completed.");
                return true;
            }

           // ReadText.Log.LogWarning($"No target GameObjects found for {GetType().Name}.");
            return false;
        }



        protected virtual void PatchMethods(GameObject targetObject)
        {
            Component component = targetObject.GetComponent(TargetComponentType);
            if (component == null)
            {
                ReadText.Log.LogError($"{TargetComponentType} component not found on {targetObject.name}.");
                return;
            }

            int successfulPatches = 0;
            foreach (var methodName in TargetMethodNames)
            {
                MethodInfo targetMethod = TargetComponentType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (targetMethod == null)
                {
                    ReadText.Log.LogWarning($"Method {methodName} not found in {TargetComponentType.Name} with specified BindingFlags.");
                    continue;
                }

                try
                {
                    MethodNameMap[targetMethod] = methodName;
                    var postfix = new HarmonyMethod(GetType().GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic));
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
        }

        protected static void Postfix(object __instance, object[] __args)
        {
            ReadText.Log.LogWarning($"{nameof(BasePatch)}.Postfix called but not implemented.");
        }
        protected static void Postfix(GameNode[] terrainNodes )
        {
            ReadText.Log.LogWarning($"{nameof(BasePatch)}.Postfix called but not implemented.");
        }


        protected GameObject FindTargetGameObject()
        {
            return Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(go => TargetGameObjectNames.Contains(go.name) && go.GetComponent(TargetComponentType) != null);
        }

        protected void LogPatchSuccess(string methodName, int successfulPatches, int totalMethods)
        {
            ReadText.Log.LogInfo($"Successfully patched method {methodName} in {TargetComponentType.Name}.");
            ReadText.Log.LogInfo($"{GetType().Name} patching completed: {successfulPatches}/{totalMethods} methods patched.");
        }

        protected void LogPatchFailure(string methodName, Exception ex = null)
        {
            if (ex != null)
            {
                ReadText.Log.LogError($"Exception while patching method {methodName} in {TargetComponentType.Name}: {ex.Message}\n{ex.StackTrace}");
            }
            else
            {
                ReadText.Log.LogError($"Failed to verify patch for method {methodName} in {TargetComponentType.Name}.");
            }
        }
    }
}