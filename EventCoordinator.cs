using System;
using FFG.Common;
using FFG.JIME;
using ReadTextMod;
using UnityEngine;




public class EventCoordinator
{
    public static EventCoordinator Instance { get; private set; }

    private GameNode[] TerrainNodeResult;

    private static readonly object Lock = new object();

    //Events
    public event EventHandler<MessagePopupMethodExecutedEventArgs> MessagePopupMethodExecuted;
    public event EventHandler<MessagePopupCloseExecutedEventArgs> MessagePopupCloseExecuted;
    public event EventHandler<UIMapExecutedEventArgs> UIMapExecuted;
    public event EventHandler<TerrainNodesEventArgs> TerrainNodesExecuted;





    public EventCoordinator(ReadText plugin)
    {

        Instance = this;
        TerrainNodeResult = null;
        ReadText.Log.LogInfo("Event Coordinator instance created.");
        
    }

    public static void Initialize(ReadText readText)
    {
        var coordinator = new EventCoordinator(readText);
    }

    public void RaiseMessagePopupMethodExecuted(GameObject gameObject, bool isActive, MessagePopup instance, LocalizationPacket packet, GameNode[] gameNodes = null)
    {
        MessagePopupMethodExecuted?.Invoke(this, new MessagePopupMethodExecutedEventArgs(gameObject, isActive, instance, packet, gameNodes));
        ReadText.Log.LogInfo($"MessagePopupMethodExecuted invoked.");
    }

    public void RaiseMessagePopupCloseExecuted(bool isActive, MessagePopup instance)
    {
        MessagePopupCloseExecuted?.Invoke(this, new MessagePopupCloseExecutedEventArgs(isActive, instance));
        ReadText.Log.LogInfo("MessagePopupCloseExecuted invoked.");
    }

    public void RaiseUIMapExecuted(GameObject gameObject, bool isActive, UIMapScene instance, UILocalizationPacket packet)
    {
        UIMapExecuted?.Invoke(this, new UIMapExecutedEventArgs(gameObject, isActive, instance, packet));
        ReadText.Log.LogInfo($"UIMapExecuted invoked.");
    }

    public void RaiseTerrainTilesExecuted(GameNode[] gameNodes)
    {
        TerrainNodesExecuted?.Invoke(this, new TerrainNodesEventArgs(gameNodes));
        ReadText.Log.LogInfo($"Terrain Nodes executed.");
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
            ReadText.Log.LogInfo($"Error in TerrainNodes: {ex.Message}");
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
                    ReadText.Log.LogInfo("TerrainNodesExecuted not triggered or no nodes");
                }
                // Call the original handler with the updated event args
                originalHandler(sender, e);
            }
            else
            {
                ReadText.Log.LogInfo("GameObject or Instance is null in MessagePopupMethodExecuted");
                originalHandler(sender, e); // Call original handler
            }
        }
        catch (Exception ex)
        {
            ReadText.Log.LogInfo($"Error in MessagePopup: {ex.Message}");
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