using System;
using FFG.Common;
using FFG.JIME;
using UnityEngine;
using JIME_TTS_MOD;
using JIME_TTS_MOD.Patches;



public class EventCoordinator
{
    public static EventCoordinator Instance { get; private set; }

    private GameNode[] TerrainNodeResult;

    private static readonly object Lock = new object();

    //Events
    public event EventHandler<MessagePopupMethodExecutedEventArgs> MessagePopupMethodExecuted;
    public event EventHandler<MessagePopupCloseExecutedEventArgs> MessagePopupCloseExecuted;
    public event EventHandler<UIMapExecutedEventArgs> UIMapExecuted;
    public event EventHandler<EnemyDialogEventArgs> EnemyDialogExecuted;
    public event EventHandler<TerrainNodesEventArgs> TerrainNodesExecuted;

    public EventCoordinator(JIME_TTS plugin)
    {

        Instance = this;
        TerrainNodeResult = null;
        JIME_TTS.Log.LogInfo("Event Coordinator instance created.");
        
    }

    public static void Initialize(JIME_TTS JIME_TTS)
    {
        var coordinator = new EventCoordinator(JIME_TTS);
    }

    public void RaiseMessagePopupMethodExecuted(GameObject gameObject, bool isActive, MessagePopup instance, LocalizationPacket packet, GameNode[] gameNodes = null)
    {
        MessagePopupMethodExecuted?.Invoke(this, new MessagePopupMethodExecutedEventArgs(gameObject, isActive, instance, packet, gameNodes));
        JIME_TTS.Log.LogInfo($"MessagePopupMethodExecuted invoked.");
    }

    public void RaiseMessagePopupCloseExecuted(bool isActive)
    {
        MessagePopupCloseExecuted?.Invoke(this, new MessagePopupCloseExecutedEventArgs(isActive));
        JIME_TTS.Log.LogInfo("MessagePopupCloseExecuted invoked.");
    }

    public void RaiseUIMapExecuted(UIMapScene uiMap, UILocalizationPacket packet)
    {
        UIMapExecuted?.Invoke(this, new UIMapExecutedEventArgs(uiMap, packet));
        JIME_TTS.Log.LogInfo($"UIMapExecuted invoked.");
    }

    public void RaiseEnemyDialogExecuted(GameObject gameObject, EnemyInfoDialog instance, UILocalizationPacket packet)
    {
        EnemyDialogExecuted?.Invoke(this, new EnemyDialogEventArgs(gameObject, instance, packet));
        JIME_TTS.Log.LogInfo($"EnemyDialog invoked.");
    }


    public void RaiseTerrainNodesExecuted(GameNode[] gameNodes)
    {
        TerrainNodesExecuted?.Invoke(this, new TerrainNodesEventArgs(gameNodes));
        JIME_TTS.Log.LogInfo($"Terrain Nodes executed.");
    }


    public void WrapTerrainNodes(object sender, TerrainNodesEventArgs e)
    {
        try
        {

            lock (Lock)
            {
                TerrainNodeResult = e.TerrainNodes;
            }

        }
        catch (Exception ex)
        {
            JIME_TTS.Log.LogInfo($"Error in TerrainNodes: {ex.Message}");
            throw;
        }
    }

    public void WrapMessagePopup(object sender, MessagePopupMethodExecutedEventArgs e, Action<object, MessagePopupMethodExecutedEventArgs> originalHandler)
    {
        try
        {
            GameNode[] terrainNodes;


            lock (Lock)
            {

                terrainNodes = TerrainNodeResult;

            }

            if (e.GameObject != null && e.Instance != null)
            {

                if (terrainNodes != null && terrainNodes.Length > 0)
                {
                    // Updated event with terrain nodes
                    e = new MessagePopupMethodExecutedEventArgs(e.GameObject, e.IsActive, e.Instance, e.LocalizationPacket, terrainNodes);
                }
                else
                {
                    JIME_TTS.Log.LogInfo("TerrainNodesExecuted not triggered or no nodes");
                }
                // Call the original handler with the updated event args
                originalHandler(sender, e);
            }
            else
            {
                JIME_TTS.Log.LogInfo("GameObject or Instance is null in MessagePopupMethodExecuted");
                originalHandler(sender, e); // Call original handler
            }
        }
        catch (Exception ex)
        {
            JIME_TTS.Log.LogInfo($"Error in MessagePopup: {ex.Message}");
            throw;
        }
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
    

    public MessagePopupCloseExecutedEventArgs(bool isActive)
    {
        IsActive = isActive;

    }
}

public class UIMapExecutedEventArgs : EventArgs
{
    public UIMapScene Instance { get; }

    public UILocalizationPacket LocalizationPacket { get; }

    public UIMapExecutedEventArgs(UIMapScene instance, UILocalizationPacket packet)
    {

        Instance = instance;
        LocalizationPacket = packet;

    }
}

public class EnemyDialogEventArgs: EventArgs
{   
    public GameObject GameObject { get; }

    public EnemyInfoDialog Instance { get; }

    public UILocalizationPacket LocalizationPacket { get; }

    public EnemyDialogEventArgs(GameObject gameObject, EnemyInfoDialog instance, UILocalizationPacket packet)
    {
        GameObject = gameObject;
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
