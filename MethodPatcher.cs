using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;
using FFG.Common;
using ReadTextMod;


    public class MethodResolver
    {



        private static readonly string[] TargetGameObjectNames = new string[] 
        { 
            "MessagePopup_New", 
            "MessagePopup_EnemyActivation", 
            "MessagePopup" 
        }; 
        private static readonly List<string> TargetMethodNames = new List<string> 
        { 
            "ShowNormalMessage", 
            "ShowConfirmCancelMessage",
            "ShowNumberedChoiceMessage",
            "ShowMultipleChoiceMessage",
            "ShowChallengeMessage",
            "ShowAdditionalInformationMessage",
            "ShowPlayerSelectMessage",
            "ShowHighlightSelectMessage" 
        }; 
        private static readonly float PatchTimeout = 30f; // Timeout after 30 seconds

        // State
        private readonly Harmony _harmony;
        private readonly MonoBehaviour _MonoBehaviour; // For coroutine
        private readonly Dictionary<string, bool> _methodExecutionStatus = new Dictionary<string, bool>(); // Tracks if executed
        private readonly Dictionary<string, bool> _lastLoggedStatus = new Dictionary<string, bool>(); // Tracks last logged status
        private readonly Dictionary<string, int> _executionCount = new Dictionary<string, int>(); // Counts executions
        private readonly Dictionary<MethodInfo, string> _methodNameMap = new Dictionary<MethodInfo, string>(); // Maps MethodInfo to methodName
        private readonly List<string> _patchedMethods = new List<string>(); // Tracks successfully patched methods
        private bool IsPatched = false; // Prevents re-patching
        private GameObject LastFoundGameObject; // Store for test invocation

        public MethodResolver(Harmony harmony, MonoBehaviour monoBehaviour)
        {
            _harmony = harmony ?? throw new ArgumentNullException(nameof(harmony));
            _MonoBehaviour = monoBehaviour ?? throw new ArgumentNullException(nameof(monoBehaviour));
            ReadText.Log.LogInfo("MethodResolver instance created.");
        }

        public void Initialize()
        {
            // Initialize dictionaries for each method
            foreach (var methodName in TargetMethodNames)
            {
                _methodExecutionStatus[methodName] = false;
                _lastLoggedStatus[methodName] = true; // Set to true to match previous behavior
                _executionCount[methodName] = 0;
            }

            // Start coroutine to try patching
            _MonoBehaviour.StartCoroutine(TryPatchMethods());
        }

        // Coroutine to retry patching until any GameObject is found or timeout
        private IEnumerator TryPatchMethods()
        {
            ReadText.Log.LogInfo($"Starting patching attempt for MessagePopup methods on GameObjects: {string.Join(", ", TargetGameObjectNames)}.");
            float startTime = Time.time;
            while (Time.time - startTime < PatchTimeout)
            {
                if (IsPatched)
                {
                    ReadText.Log.LogInfo("Patching already completed, skipping coroutine attempt.");
                    yield break;
                }

                if (TryPatchMethodsNow())
                {
                    ReadText.Log.LogInfo("Patching completed successfully via coroutine, no further attempts needed.");
                    yield break;
                }

                yield return null; // Wait for next frame
            }

            ReadText.Log.LogError($"Failed to find any of the GameObjects ({string.Join(", ", TargetGameObjectNames)}) after {PatchTimeout} seconds.");
        }

        // Attempt to patch methods now, returns true if successful
        public bool TryPatchMethodsNow()
        {
            if (IsPatched)
            {
                ReadText.Log.LogInfo("TryPatchMethodsNow: Patching already completed, skipping attempt.");
                return true;
            }

            ReadText.Log.LogInfo("Attempting to find MessagePopup GameObjects for patching...");

            // Find all GameObjects, including inactive ones
            GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var gameObjectName in TargetGameObjectNames)
            {
                GameObject targetObject = allGameObjects.FirstOrDefault(go => go.name == gameObjectName && go.GetComponent<MessagePopup>() != null);
                if (targetObject != null)
                {
                    ReadText.Log.LogInfo($"Found target GameObject: {targetObject.name}. Proceeding with patching.");
                    LastFoundGameObject = targetObject; // Store for test invocation
                    PatchTargetMethods(targetObject);
                    IsPatched = true;
                    return true;
                }
            }

            ReadText.Log.LogInfo($"No target GameObjects ({string.Join(", ", TargetGameObjectNames)}) found in this attempt.");
            return false;
        }

        // Patch target methods on the GameObject's MessagePopup component
        private void PatchTargetMethods(GameObject targetObject)
        {
            if (IsPatched)
            {
                ReadText.Log.LogWarning("PatchTargetMethods called but methods are already patched. Skipping.");
                return;
            }

            ReadText.Log.LogInfo($"Found GameObject: {targetObject.name}, Active: {targetObject.activeInHierarchy}, Scene: {targetObject.scene.name}");

            // Log components for debugging
            Component[] components = targetObject.GetComponents<Component>();
            if (components.Length == 0)
            {
                ReadText.Log.LogInfo("  No components found.");
            }
            else
            {
                foreach (Component comp in components)
                {
                    ReadText.Log.LogInfo($"  - {comp.GetType().FullName}");
                }
            }

            // Use direct type reference for MessagePopup
            Type componentType = typeof(MessagePopup);
            Component component = targetObject.GetComponent(componentType);
            if (component == null)
            {
                ReadText.Log.LogError($"MessagePopup component not found on {targetObject.name}.");
                return;
            }

            ReadText.Log.LogInfo($"Patching methods in {componentType.Name} on {targetObject.name}...");
            int successfulPatches = 0;
            foreach (var methodName in TargetMethodNames)
            {
                MethodInfo targetMethod = componentType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (targetMethod == null)
                {
                    ReadText.Log.LogWarning($"Method {methodName} not found in {componentType.Name}.");
                    continue;
                }

                try
                {
                    ReadText.Log.LogInfo($"Attempting to patch: {componentType.Name}.{methodName}({string.Join(", ", targetMethod.GetParameters().Select(p => p.ParameterType.Name))})");
                    _methodNameMap[targetMethod] = methodName;
                    var prefix = new HarmonyMethod(
                        typeof(MethodResolver).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic)
                    );
                    _harmony.Patch(targetMethod, prefix: prefix);
                    ReadText.Log.LogInfo($"Patch applied for {methodName}. Prefix method: {prefix.method.Name}");

                    // Verify patch using Harmony.GetPatchInfo
                    var patchInfo = Harmony.GetPatchInfo(targetMethod);
                    if (patchInfo?.Prefixes?.Any(p => p.owner == _harmony.Id) == true)
                    {
                        _patchedMethods.Add(methodName);
                        successfulPatches++;
                        ReadText.Log.LogInfo($"Successfully patched method {methodName} in {componentType.Name}.");
                    }
                    else
                    {
                        ReadText.Log.LogError($"Failed to verify patch for method {methodName} in {componentType.Name}.");
                    }
                }
                catch (Exception ex)
                {
                    ReadText.Log.LogError($"Exception while patching method {methodName}: {ex.Message}");
                }
            }

            ReadText.Log.LogInfo($"Patching completed: {successfulPatches}/{TargetMethodNames.Count} methods patched successfully.");
        }

        

        // Prefix for target methods to track execution
        private static void Prefix(MethodInfo __originalMethod, object __instance)
        {
            MethodResolver resolver = ReadText.MethodResolver;
            if (resolver == null)
            {
                Debug.LogWarning($"Prefix: MethodResolver instance not found for method {__originalMethod.DeclaringType.Name}.{__originalMethod.Name}.");
                return;
            }

           ReadText.Log.LogInfo($"Prefix called for method: {__originalMethod.DeclaringType.Name}.{__originalMethod.Name}");

            if (resolver._methodNameMap.TryGetValue(__originalMethod, out string methodName))
            {
                resolver._methodExecutionStatus[methodName] = true;
                resolver._executionCount[methodName]++;
                bool wasLogged = resolver._lastLoggedStatus[methodName];

                if (__instance is Component component && component.gameObject != null)
                {
                    ReadText.Log.LogInfo($"Method {methodName} executed on {component.gameObject.name} (Count: {resolver._executionCount[methodName]}), Active: {component.gameObject.activeInHierarchy}, Scene: {component.gameObject.scene.name}");
                }
                else
                {
                    ReadText.Log.LogInfo($"Method {methodName} executed (Count: {resolver._executionCount[methodName]}), but instance details unavailable. Instance type: {__instance?.GetType().Name ?? "null"}");
                }

                if (!wasLogged)
                {
                    ReadText.Log.LogInfo($"Method {methodName} has been executed for the first time (Count: {resolver._executionCount[methodName]}).");
                    resolver._lastLoggedStatus[methodName] = true;
                }
            }
            else
            {
                ReadText.Log.LogInfo($"Method {__originalMethod.DeclaringType.Name}.{__originalMethod.Name} executed but not found in methodName map.");
            }
        }
}


