using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;



namespace MethodTrackerMod
{
[BepInPlugin("com.yourname.methodtrackermod", "Method Tracker Mod", "1.0.0")]
public class MethodTrackerPlugin : BaseUnityPlugin
{
    // Configuration
    private static readonly string _targetGameObjectName = "MessagePopup_New"; // Name of the GameObject
    private static readonly string _targetComponentType = "FFG.Common.MessagePopup"; // Component type containing the methods
    private static readonly List<string> _targetMethodNames = new List<string> { "ShowNormalMessage", "ShowConfirmCancelMessage","ShowNumberedChoiceMessage","ShowMultipleChoiceMessage",
    "ShowChallengeMessage","ShowAdditionalInformationMessage","ShowPlayerSelectMessage","ShowHighlightSelectMessage" }; // Methods to track

   // State
    private static readonly Dictionary<string, bool> _methodExecutionStatus = new Dictionary<string, bool>(); // Tracks if executed
    private static readonly Dictionary<string, bool> _lastLoggedStatus = new Dictionary<string, bool>(); // Tracks last logged status
    private static readonly Dictionary<string, int> _executionCount = new Dictionary<string, int>(); // Counts executions
    private static readonly Dictionary<MethodInfo, string> _methodNameMap = new Dictionary<MethodInfo, string>(); // Maps MethodInfo to methodName
    private static MethodTrackerPlugin _instance; // Static reference to plugin instance
    private static bool _isPatched = false; // Ensures patching happens only once
    private static Harmony _harmony; // Harmony instance for patching
    private static float _patchStartTime = 0f; // Tracks patching start time
    private static readonly float _patchTimeout = 600f; // Timeout after 60 seconds

    private void Awake()
    {
        _instance = this; // Store instance for Logger access
        _instance.Logger.LogInfo("Method Tracker Mod loaded.");

        // Initialize dictionaries for each method
        foreach (var methodName in _targetMethodNames)
        {
            _methodExecutionStatus[methodName] = false;
            _lastLoggedStatus[methodName] = false;
            _executionCount[methodName] = 0;
        }

        // Initialize Harmony and apply patches
        _harmony = new Harmony("com.yourname.methodtrackermod");
        _harmony.PatchAll(); // Automatically applies all [HarmonyPatch] attributes
        
        
    }

    // Patch target methods (called from LateUpdate Postfix)
    private static void PatchTargetMethods()
    {
        GameObject targetObject = GameObject.Find(_targetGameObjectName);
        // Only patch once
        if (_isPatched)
            return;

        // Log GameObject state
        _instance.Logger.LogInfo($"Found GameObject: {_targetGameObjectName}, Active: {targetObject.activeInHierarchy}, Scene: {targetObject.scene.name}");

        Component [] components = targetObject.GetComponents<Component>();

        if (components.Length == 0)
            {
                _instance.Logger.LogInfo("  No components found.");
            }
            else
            {
                foreach (Component comp in components)
                {
                    _instance.Logger.LogInfo($"  - {comp.GetType()}");
                }
            }

        Type componentType = components[1].GetType();
        if (componentType == null)
        {
            _instance.Logger.LogError($"Component type {_targetComponentType} not found on {_targetGameObjectName}.");
            _isPatched = true; // Prevent further attempts
            return;
        }

        // Verify component exists on GameObject
        Component component = targetObject.GetComponent(componentType);
        if (component == null)
        {
            _instance.Logger.LogError($"Component {componentType.Name} not found on {_targetGameObjectName}.");
            _isPatched = true;
            return;
        }

        foreach (var methodName in _targetMethodNames)
        {
            MethodInfo targetMethod = componentType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetMethod == null)
            {
                _instance.Logger.LogWarning($"Method {methodName} not found in {componentType.Name} on {_targetGameObjectName}.");
                continue;
            }

            _methodNameMap[targetMethod] = methodName;
            var prefix = new HarmonyMethod(
                typeof(MethodTrackerPlugin).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic)
            );
            _harmony.Patch(targetMethod, prefix: prefix);
            _instance.Logger.LogInfo($"Patched method {methodName} in {componentType.Name} on {_targetGameObjectName}.");
        }

        _isPatched = true; // Mark as patched
        _instance.Logger.LogInfo("All target methods patched successfully.");
    }

    // Prefix for target methods to track execution
    private static void Prefix(MethodInfo __originalMethod, object __instance)
    {
        if (_methodNameMap.TryGetValue(__originalMethod, out string methodName))
        {
            _methodExecutionStatus[methodName] = true;
            _executionCount[methodName]++;
            // Log instance details to verify GameObject context
            if (__instance is Component component && component.gameObject != null)
            {
                _instance.Logger.LogInfo($"Method {methodName} executed on {component.gameObject.name} (Count: {_executionCount[methodName]}), Active: {component.gameObject.activeInHierarchy}, Scene: {component.gameObject.scene.name}");
            }
            else
            {
                _instance.Logger.LogInfo($"Method {methodName} executed (Count: {_executionCount[methodName]}), but instance details unavailable.");
            }
        }
        else
        {
            _instance.Logger.LogWarning($"Method {__originalMethod.Name} executed but not found in methodName map.");
        }
    }

    // Postfix for UIPanel.LateUpdate to patch methods and check status
    [HarmonyPatch(typeof(UIPanel), "LateUpdate")]
    private static class UIPanelLateUpdatePatch
    {
        static void Postfix()
        {
            
            // Check for GameObject and patch if found
            PatchTargetMethods();
            

            // Check execution status (even before patching to log state)
            foreach (var kvp in _methodExecutionStatus)
            {
                string methodName = kvp.Key;
                bool isExecuted = kvp.Value;
                bool wasLogged = _lastLoggedStatus[methodName];

                if (isExecuted && !wasLogged)
                {
                    _instance.Logger.LogInfo($"Method {methodName} has been executed (Count: {_executionCount[methodName]}) (checked in UIPanel.LateUpdate).");
                    _lastLoggedStatus[methodName] = true;
                }
            }
        }
    }
}
}