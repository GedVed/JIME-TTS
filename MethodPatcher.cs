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
    private readonly Dictionary<MethodInfo, string> MethodNameMap = new Dictionary<MethodInfo, string>(); // Maps MethodInfo to methodName
    private readonly List<string> PatchedMethods = new List<string>();
    private readonly List<BasePatch> Patches;
    
    /*private bool IsMessagePatched = false;
    private bool IsAdventurePatched = false;
    private bool IsUIMapPatched = false;
    private bool IsMessageClosePatched = false;*/



    //Events
    public event EventHandler<MessagePopupMethodExecutedEventArgs> MessagePopupMethodExecuted;
    public event EventHandler<MessagePopupCloseExecutedEventArgs> MessagePopupCloseExecuted;
    public event EventHandler<UIMapExecutedEventArgs> UIMapExecuted;



    public MethodPatcher(Harmony harmony)
    {
        Instance = this;
        Harmony = harmony ?? throw new ArgumentNullException(nameof(harmony));
        ReadText.Log.LogInfo("MethodPatcher instance created.");
        Patches = new List<BasePatch>
        {
            new AdventurePatch(MethodNameMap, PatchedMethods, Harmony),
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
    
    public void RaiseMessagePopupMethodExecuted(string methodName, GameObject gameObject, bool isActive, MessagePopup instance, LocalizationPacket packet)
    {
        MessagePopupMethodExecuted?.Invoke(this, new MessagePopupMethodExecutedEventArgs(methodName, gameObject, isActive, instance, packet));
        ReadText.Log.LogInfo($"MessagePopupMethodExecuted invoked for {methodName}.");
    }

    public void RaiseMessagePopupCloseExecuted(bool isActive, MessagePopup instance)
    {
        MessagePopupCloseExecuted?.Invoke(this, new MessagePopupCloseExecutedEventArgs(isActive, instance));
        ReadText.Log.LogInfo("MessagePopupCloseExecuted invoked.");
    }

    public void RaiseUIMapExecuted(string methodName, GameObject gameObject, bool isActive, UIMapScene instance, UILocalizationPacket packet)
    {
        UIMapExecuted?.Invoke(this, new UIMapExecutedEventArgs(methodName, gameObject, isActive, instance, packet));
        ReadText.Log.LogInfo($"UIMapExecuted invoked for {methodName}.");
    }
    
    // Expose methodNameMap for patches to access
    public Dictionary<MethodInfo, string> GetMethodNameMap()
    {
            return MethodNameMap;
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

public class UIMapExecutedEventArgs : EventArgs
{
    public string MethodName { get; }
    public GameObject GameObject { get; }
    public bool IsActive { get; }
    public UIMapScene Instance { get; }
    public UILocalizationPacket LocalizationPacket{ get; }

    public UIMapExecutedEventArgs(string methodName, GameObject gameObject, bool isActive, UIMapScene instance, UILocalizationPacket packet)
    {
        MethodName = methodName;
        GameObject = gameObject;
        IsActive = isActive;
        Instance = instance;
        LocalizationPacket = packet;

    }
}
