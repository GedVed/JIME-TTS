using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;
using ReadTextMod;
using FFG.Common;
using FFG.JIME;



public class MethodResolver
{
    public event EventHandler<MessagePopupMethodExecutedEventArgs> MessagePopupMethodExecuted;
    public event EventHandler<MessagePopupCloseExecutedEventArgs> MessagePopupCloseExecuted;
    public event EventHandler<UIMapExecuted> UIMapExecuted;
    private static readonly string[] TargetGameObjectMessageNames = new string[]
    {
        "MessagePopup_New",
        "MessagePopup_EnemyActivation",
        "MessagePopup",
    }; 
    private static readonly List<string> TargetMethodMessageNames = new List<string>
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

    private static readonly string UIMapTargetObject = "UI_Map";
    private static readonly List<string> TargetMethodUIMapNames = new List<string>
    {
        "OnLeftButtonDetailedClick",
        "OnRightButtonDetailedClick",
        "OnContinueButtonClicked"
    }; 

    private static readonly float PatchTimeout = 60f; // Timeout after 30 seconds

    // State
    private readonly Harmony Harmony;
    private readonly MonoBehaviour MonoBehaviour; 
    private readonly Dictionary<MethodInfo, string> MethodNameMap = new Dictionary<MethodInfo, string>(); // Maps MethodInfo to methodName
    private readonly List<string> PatchedMethods = new List<string>(); 
    private bool IsMessagePatched = false;
    private bool IsAdventurePatched = false;
    private bool IsUIMapPatched = false;
    
    public MethodResolver(Harmony harmony, MonoBehaviour monoBehaviour)
    {
        Harmony = harmony ?? throw new ArgumentNullException(nameof(harmony));
        MonoBehaviour = monoBehaviour ?? throw new ArgumentNullException(nameof(monoBehaviour));
        ReadText.Log.LogInfo("MethodResolver instance created.");
    }

    public void Initialize()
    {
        MonoBehaviour.StartCoroutine(TryPatchMethods());
    }

    // Coroutine to retry patching until any GameObject is found or timeout
    private IEnumerator TryPatchMethods()
{
    ReadText.Log.LogInfo($"Starting patching attempt for methods on GameObjects: {string.Join(", ", TargetGameObjectMessageNames)}, UI_Map, and Adventure.");
    float startTime = Time.time;
    while (Time.time - startTime < PatchTimeout)
    {
        if (IsMessagePatched && IsUIMapPatched && IsAdventurePatched)
        {
            ReadText.Log.LogInfo("Patching already completed, skipping coroutine attempt.");
            yield break;
        }

        bool messagePatchedThisFrame = TryPatchMessageMethodsNow();
        bool uiMapPatchedThisFrame = TryPatchUIMapMethodsNow();
        bool adventurePatchedThisFrame = AdventurePatch.TryPatchAdventureMethodsNow(ref IsAdventurePatched, MethodNameMap, PatchedMethods, Harmony);

        if (messagePatchedThisFrame)
        {
            IsMessagePatched = true;
            ReadText.Log.LogInfo("MessagePopup methods patched successfully.");
        }

        if (uiMapPatchedThisFrame)
        {
            IsUIMapPatched = true;
            ReadText.Log.LogInfo("UIMapScene methods patched successfully.");
        }

        if (adventurePatchedThisFrame)
        {
            IsAdventurePatched = true;
            ReadText.Log.LogInfo("Adventure methods patched successfully.");
        }

        if (IsMessagePatched && IsUIMapPatched && IsAdventurePatched)
        {
            ReadText.Log.LogInfo("Patching completed successfully for MessagePopup, UIMapScene, and Adventure via coroutine, no further attempts needed.");
            yield break;
        }
        
        yield return null; // Wait for next frame
    }

    ReadText.Log.LogError($"Failed to complete patching after {PatchTimeout} seconds. Status: MessagePatched={IsMessagePatched}, UIMapPatched={IsUIMapPatched}, AdventurePatched={IsAdventurePatched}.");
}

    
    public bool TryPatchMessageMethodsNow()
    {
        if (IsMessagePatched)
        {
            ReadText.Log.LogInfo("TryPatchMethodsNow: Patching already completed, skipping attempt.");
            return true;
        }


        GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var gameObjectName in TargetGameObjectMessageNames)
        {
            GameObject targetObject = allGameObjects.FirstOrDefault(go => go.name == gameObjectName && go.GetComponent<MessagePopup>() != null);
            if (targetObject != null)
            {
                ReadText.Log.LogInfo($"Found target GameObject: {targetObject.name}. Proceeding with patching.");
                PatchTargetMessageMethods(targetObject);
                IsMessagePatched = true;
                return true;
            }
        }

        return false;
    }


    public bool TryPatchUIMapMethodsNow()
    {
        if (IsUIMapPatched)
        {
            ReadText.Log.LogInfo("TryPatchUIMapMethodsNow: Patching already completed, skipping attempt.");
            return true;
        }

        GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        
            GameObject targetObject = allGameObjects.FirstOrDefault(go => go.name == UIMapTargetObject && go.GetComponent<UIMapScene>() != null);
            if (targetObject != null)
            {
                ReadText.Log.LogInfo($"Found target GameObject: {targetObject.name}. Proceeding with patching.");
                PatchTargetUIMapMethods(targetObject);
                IsUIMapPatched = true;
                return true;
            }
        return false;

    }
    


    private void PatchTargetUIMapMethods(GameObject targetObject)
    {
        if (IsUIMapPatched)
        {
            ReadText.Log.LogWarning("PatchTargetUIMapMethods called but methods are already patched. Skipping.");
            return;
        }
        ReadText.Log.LogInfo($"Found GameObject: {targetObject.name}, Active: {targetObject.activeInHierarchy}, Scene: {targetObject.scene.name}");

        LogComponents(targetObject);

        Type componentType = typeof(UIMapScene);
        Component component = targetObject.GetComponent(componentType);
        if (component == null)
        {
            ReadText.Log.LogError($"UIMapScene component not found on {targetObject.name}.");
            return;
        }

        int successfulPatches = 0;
        foreach (var methodName in TargetMethodUIMapNames)
        {
            MethodInfo targetMethod = componentType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetMethod == null)
            {
                ReadText.Log.LogWarning($"Method {methodName} not found in {componentType.Name}.");
                continue;
            }
            PatchPostfix(MethodNameMap, targetMethod, methodName, Harmony, PatchedMethods, ref successfulPatches, componentType);
        }

        ReadText.Log.LogInfo($"Patching completed: {successfulPatches}/{TargetMethodUIMapNames.Count} methods patched successfully.");

    }

    private void PatchTargetMessageMethods(GameObject targetObject)
    {
        if (IsMessagePatched)
        {
            ReadText.Log.LogWarning("PatchTargetMethods called but methods are already patched. Skipping.");
            return;
        }

        ReadText.Log.LogInfo($"Found GameObject: {targetObject.name}, Active: {targetObject.activeInHierarchy}, Scene: {targetObject.scene.name}");


        LogComponents(targetObject);


        Type componentType = typeof(MessagePopup);
        Component component = targetObject.GetComponent(componentType);
        if (component == null)
        {
            ReadText.Log.LogError($"MessagePopup component not found on {targetObject.name}.");
            return;
        }

        int successfulPatches = 0;
        foreach (var methodName in TargetMethodMessageNames)
        {
            MethodInfo targetMethod = componentType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetMethod == null)
            {
                ReadText.Log.LogWarning($"Method {methodName} not found in {componentType.Name}.");
                continue;
            }
            if (methodName == "ClearCallbacks")
            {
                PatchPrefix(MethodNameMap, targetMethod, methodName, Harmony, PatchedMethods, ref successfulPatches, componentType);
            }
            else
            {
                PatchPostfix(MethodNameMap, targetMethod, methodName, Harmony, PatchedMethods, ref successfulPatches, componentType);
            }


        }

        ReadText.Log.LogInfo($"Patching completed: {successfulPatches}/{TargetMethodMessageNames.Count} methods patched successfully.");
    }

    private static void PatchPrefix(Dictionary<MethodInfo, string> MethodNameMap, MethodInfo targetMethod, string methodName, Harmony Harmony, List<string> PatchedMethods, ref int successfulPatches, Type componentType)
    {
        
        try
        {

            MethodNameMap[targetMethod] = methodName;
            var prefix = new HarmonyMethod(
                typeof(MethodResolver).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic)
            );
            Harmony.Patch(targetMethod, prefix: prefix);
    
            
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

    }

    private static void PatchPostfix(Dictionary<MethodInfo, string> MethodNameMap, MethodInfo targetMethod, string methodName, Harmony Harmony, List<string> PatchedMethods, ref int successfulPatches, Type componentType)
    {

        try
        {

            MethodNameMap[targetMethod] = methodName;
            var postfix = new HarmonyMethod(
                typeof(MethodResolver).GetMethod(nameof(Postfix), BindingFlags.Static | BindingFlags.NonPublic)
            );
            Harmony.Patch(targetMethod, postfix: postfix);


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


    private static void LogComponents(GameObject targetObject)
    {
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
    }

    private static void Postfix(MethodInfo __originalMethod, object __instance, object[] __args)
    {
        MethodResolver resolver = ReadText.MethodResolver;
        if (resolver == null)
        {
            Debug.LogWarning($"Postfix: MethodResolver instance not found for method {__originalMethod.DeclaringType.Name}.{__originalMethod.Name}.");
            return;
        }

        if (resolver.MethodNameMap.TryGetValue(__originalMethod, out string methodName))
        {
            if (methodName == "OnContinueButtonClicked" || methodName == "OnLeftButtonDetailedClick" || methodName == "OnRightButtonDetailedClick")
            {
                GameObject gameObject = null;
                bool isActive = false;
                UIMapScene instance = null;
                UILocalizationPacket localizationPacket = null;

                if (__instance is UIMapScene UIMap && UIMap.gameObject != null)
                {
                    gameObject = UIMap.gameObject;
                    isActive = gameObject.activeInHierarchy;
                    instance = UIMap;
                    localizationPacket = instance.Label_AdventureEpilogue;

                }


                resolver.UIMapExecuted?.Invoke(resolver, new UIMapExecuted(
                    methodName,
                    gameObject,
                    isActive,
                    instance,
                    localizationPacket

                ));
            }
            else
            {
                GameObject gameObject = null;
                bool isActive = false;
                MessagePopup instance = null;
                LocalizationPacket localizationPacket = null;

                if (__instance is MessagePopup messagePopup && messagePopup.gameObject != null)
                {
                    gameObject = messagePopup.gameObject;
                    isActive = gameObject.activeInHierarchy;
                    instance = messagePopup;
                    localizationPacket = __args.OfType<LocalizationPacket>().FirstOrDefault();

                }


                resolver.MessagePopupMethodExecuted?.Invoke(resolver, new MessagePopupMethodExecutedEventArgs(
                    methodName,
                    gameObject,
                    isActive,
                    instance,
                    localizationPacket

                ));
            }


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
[HarmonyPatch(typeof(Adventure), "CoroutineRevealTerrains")]
static class Adventure_Patch
{
    public static void PrefixCoroutineRevealTerrains(GameNode[] terrainNodes)
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

public class MessagePopupMethodExecutedEventArgs : EventArgs
{
    public string MethodName { get; }
    public GameObject GameObject { get; }
    public bool IsActive { get; }
    public MessagePopup Instance { get; }
    public LocalizationPacket LocalizationPacket { get; }

    public MessagePopupMethodExecutedEventArgs(string methodName, GameObject gameObject, bool isActive, MessagePopup instance, LocalizationPacket packet)
    {
        MethodName = methodName;
        GameObject = gameObject;
        IsActive = isActive;
        Instance = instance;
        LocalizationPacket = packet;

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
public class UIMapExecuted : EventArgs
{
    public string MethodName { get; }
    public GameObject GameObject { get; }
    public bool IsActive { get; }
    public UIMapScene Instance { get; }
    public UILocalizationPacket LocalizationPacket{ get; }

    public UIMapExecuted(string methodName, GameObject gameObject, bool isActive, UIMapScene instance, UILocalizationPacket packet)
    {
        MethodName = methodName;
        GameObject = gameObject;
        IsActive = isActive;
        Instance = instance;
        LocalizationPacket = packet;

    }
}
