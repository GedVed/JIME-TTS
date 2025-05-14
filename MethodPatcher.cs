using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;
using ReadTextMod;
using FFG.Common;
using UnityEngine.Bindings;


public class MethodResolver
    {
        public event EventHandler<MessagePopupMethodExecutedEventArgs> MessagePopupMethodExecuted;
        public event EventHandler<MessagePopupCloseExecutedEventArgs> MessagePopupCloseExecuted;
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
            "ShowHighlightSelectMessage",
            "ClearCallbacks"
        }; 
        private static readonly float PatchTimeout = 30f; // Timeout after 30 seconds

        // State
        private readonly Harmony Harmony;
        private readonly MonoBehaviour MonoBehaviour; 
        private readonly Dictionary<MethodInfo, string> MethodNameMap = new Dictionary<MethodInfo, string>(); // Maps MethodInfo to methodName
        private readonly List<string> PatchedMethods = new List<string>(); // Tracks successfully patched methods
        private bool IsPatched = false; 
        private GameObject LastFoundGameObject; 

        public MethodResolver(Harmony harmony, MonoBehaviour monoBehaviour)
        {
            Harmony = harmony ?? throw new ArgumentNullException(nameof(harmony));
            MonoBehaviour = monoBehaviour ?? throw new ArgumentNullException(nameof(monoBehaviour));
            ReadText.Log.LogInfo("MethodResolver instance created.");
        }

        public void Initialize()
        {
            // Start coroutine to try patching
            MonoBehaviour.StartCoroutine(TryPatchMethods());
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

            int successfulPatches = 0;
            foreach (var methodName in TargetMethodNames)
            {
                MethodInfo targetMethod = componentType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (targetMethod == null)
                {
                    ReadText.Log.LogWarning($"Method {methodName} not found in {componentType.Name}.");
                    continue;
                }
                if(methodName == "ClearCallbacks"){
                    try
                    {
                        
                        MethodNameMap[targetMethod] = methodName;
                        var prefix = new HarmonyMethod(
                            typeof(MethodResolver).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic)
                        );
                        Harmony.Patch(targetMethod, prefix: prefix);
                
                        // Verify patch using Harmony.GetPatchInfo
                        var patchInfo = Harmony.GetPatchInfo(targetMethod);
                        if (patchInfo?.Prefixes?.Any(p => p.owner == Harmony.Id) == true)
                        {
                            PatchedMethods.Add(methodName);
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
                }else
                {
                    try
                        {
            
                        MethodNameMap[targetMethod] = methodName;
                        var postfix = new HarmonyMethod(
                            typeof(MethodResolver).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic)
                        );
                        Harmony.Patch(targetMethod, postfix: postfix);
                
                        // Verify patch using Harmony.GetPatchInfo
                        var patchInfo = Harmony.GetPatchInfo(targetMethod);
                        if (patchInfo?.Postfixes?.Any(p => p.owner == Harmony.Id) == true)
                        {
                            PatchedMethods.Add(methodName);
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
            }
            
            ReadText.Log.LogInfo($"Patching completed: {successfulPatches}/{TargetMethodNames.Count} methods patched successfully.");
        }

        

        // Postfix for target methods to track execution
        private static void Postfix(MethodInfo __originalMethod, object __instance)
        {
            MethodResolver resolver = ReadText.MethodResolver;
            if (resolver == null)
            {
                Debug.LogWarning($"Postfix: MethodResolver instance not found for method {__originalMethod.DeclaringType.Name}.{__originalMethod.Name}.");
                return;
            }
            
            if (resolver.MethodNameMap.TryGetValue(__originalMethod, out string methodName))
            {
                GameObject gameObject = null;
                bool isActive = false;
                MessagePopup instance = null;

                if (__instance is MessagePopup messagePopup && messagePopup.gameObject != null)
                {
                    gameObject = messagePopup.gameObject;
                    isActive = gameObject.activeInHierarchy;
                    instance = messagePopup;
                }

                // Raise the event
                resolver.MessagePopupMethodExecuted?.Invoke(resolver, new MessagePopupMethodExecutedEventArgs(
                    methodName,
                    gameObject,
                    isActive,
                    instance
                ));

                ReadText.Log.LogInfo($"Event invoked for {methodName}. Handlers: {resolver.MessagePopupMethodExecuted?.GetInvocationList().Length ?? 0}");
            }
            else
            {
                ReadText.Log.LogWarning($"Method {__originalMethod.DeclaringType.Name}.{__originalMethod.Name} executed but not found in methodName map.");
            }
        }

        private static void Prefix(MethodInfo __originalMethod, object __instance){
            MethodResolver resolver = ReadText.MethodResolver;
            if (resolver == null)
            {
                Debug.LogWarning($"Postfix: MethodResolver instance not found for method {__originalMethod.DeclaringType.Name}.{__originalMethod.Name}.");
                return;
            }
            if (resolver.MethodNameMap.TryGetValue(__originalMethod, out string methodName))
            {
                GameObject gameObject = null;
                bool isActive = false;
                MessagePopup instance = null;

                if (__instance is MessagePopup messagePopup && messagePopup.gameObject != null)
                {
                    gameObject = messagePopup.gameObject;
                    isActive = gameObject.activeInHierarchy;
                    instance = messagePopup;
                }

                // Raise the event
                resolver.MessagePopupCloseExecuted?.Invoke(resolver, new MessagePopupCloseExecutedEventArgs(
                    isActive,
                    instance
                ));

                ReadText.Log.LogInfo($"Event invoked for {methodName}. Handlers: {resolver.MessagePopupCloseExecuted?.GetInvocationList().Length ?? 0}");
            }
            else
            {
                ReadText.Log.LogWarning($"Method {__originalMethod.DeclaringType.Name}.{__originalMethod.Name} executed but not found in methodName map.");
            }
        }
}


    public class MessagePopupMethodExecutedEventArgs : EventArgs
    {
        public string MethodName { get; }
        public GameObject GameObject { get; }
        public bool IsActive { get; }
        public MessagePopup Instance { get; }
        
        public MessagePopupMethodExecutedEventArgs(string methodName, GameObject gameObject, bool isActive, MessagePopup instance)
        {
            MethodName = methodName;
            GameObject = gameObject;
            IsActive = isActive;
            Instance = instance;
        }
    }
    public class MessagePopupCloseExecutedEventArgs : EventArgs
    {
        public bool IsActive { get; }
        public MessagePopup Instance { get; }

        public MessagePopupCloseExecutedEventArgs(bool isActive, MessagePopup instance)
        {
            IsActive = isActive;
            Instance = instance;
        }
    }
