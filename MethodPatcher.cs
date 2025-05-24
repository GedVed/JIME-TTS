using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Collections;
using ReadTextMod;
using FFG.Common;
using FFG.JIME;
using ReadTextMod.Patches;





public class MethodPatcher
{
    public static MethodPatcher Instance { get; private set; }
    
    // State
    private readonly Harmony Harmony;
    private readonly Dictionary<MethodInfo, string> MethodNameMap = []; // Maps MethodInfo to methodName
    private readonly List<string> PatchedMethods = [];
    private readonly List<BasePatch> Patches;
    

    //Events
    public event EventHandler<MessagePopupMethodExecutedEventArgs> MessagePopupMethodExecuted;
    public event EventHandler<MessagePopupCloseExecutedEventArgs> MessagePopupCloseExecuted;
    public event EventHandler<UIMapExecutedEventArgs> UIMapExecuted;
    public event EventHandler<TerrainNodesEventArgs> TerrainNodesExecuted;


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
    
    public void RaiseMessagePopupMethodExecuted(GameObject gameObject, bool isActive, MessagePopup instance, LocalizationPacket packet, GameNode[] gameNodes = null)
    {
        MessagePopupMethodExecuted?.Invoke(this, new MessagePopupMethodExecutedEventArgs( gameObject, isActive, instance, packet, gameNodes));
        ReadText.Log.LogInfo($"MessagePopupMethodExecuted invoked.");
    }

    public void RaiseMessagePopupCloseExecuted(bool isActive, MessagePopup instance)
    {
        MessagePopupCloseExecuted?.Invoke(this, new MessagePopupCloseExecutedEventArgs(isActive, instance));
        ReadText.Log.LogInfo("MessagePopupCloseExecuted invoked.");
    }

    public void RaiseUIMapExecuted(GameObject gameObject, bool isActive, UIMapScene instance, UILocalizationPacket packet)
    {
        UIMapExecuted?.Invoke(this, new UIMapExecutedEventArgs( gameObject, isActive, instance, packet));
        ReadText.Log.LogInfo($"UIMapExecuted invoked.");
    }

    public void RaiseTerrainTilesExecuted(GameNode[] gameNodes)
    {
        TerrainNodesExecuted?.Invoke(this, new TerrainNodesEventArgs(gameNodes));
        ReadText.Log.LogInfo($"Terrain Nodes executed.");
    }
    
    
    public Dictionary<MethodInfo, string> GetMethodNameMap()
    {
        return MethodNameMap;
    }

}
    



public class MessagePopupMethodExecutedEventArgs : EventArgs
{
    public GameObject GameObject { get; }
    public bool IsActive { get; }
    public MessagePopup Instance { get; }
    public LocalizationPacket LocalizationPacket { get; }

    public GameNode[] GameNodes;

    public MessagePopupMethodExecutedEventArgs(GameObject gameObject, bool isActive, MessagePopup instance, LocalizationPacket packet, GameNode[] gameNodes = null)
    {
        GameObject = gameObject;
        IsActive = isActive;
        Instance = instance;
        LocalizationPacket = packet;
        GameNodes = gameNodes;

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

public class UIMapExecutedEventArgs : EventArgs
{

    public GameObject GameObject { get; }
    public bool IsActive { get; }
    public UIMapScene Instance { get; }
    public UILocalizationPacket LocalizationPacket { get; }

    public UIMapExecutedEventArgs(GameObject gameObject, bool isActive, UIMapScene instance, UILocalizationPacket packet)
    {
        GameObject = gameObject;
        IsActive = isActive;
        Instance = instance;
        LocalizationPacket = packet;
    }
}


public class TerrainNodesEventArgs : EventArgs
{
    public GameNode[] TerrainNodes;

    public TerrainNodesEventArgs(GameNode[] terrainNodes)
    {
        TerrainNodes = terrainNodes;
    }
    
}
