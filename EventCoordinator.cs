using System;
using FFG.JIME;
using ReadTextMod;




public class EventCoordinator
{
    private GameNode[] TerrainNodeResult;
    
    private static readonly object Lock = new object();

    private readonly ReadText Plugin;
    public EventCoordinator(ReadText plugin)
    {
        TerrainNodeResult = null;
        Plugin = plugin;
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
                originalHandler(sender, e); // Call original handler even if no processing is done
            }
        }
        catch (Exception ex)
        {
            ReadText.Log.LogInfo($"Error in MessagePopup: {ex.Message}");
            throw;
        }
    }
}