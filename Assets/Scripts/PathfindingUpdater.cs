using System;
using UnityEngine;

public class PathfindingUpdater : MonoBehaviour
{
    private void Start()
    {
        DestructibleCrate.OnAnyDestructibleCrateDestroyed += DestructibleCrate_OnAnyDestructibleCrateDestroyed;
        
    }
    
    private void DestructibleCrate_OnAnyDestructibleCrateDestroyed(object sender, EventArgs e)
    {
        DestructibleCrate destructibleCrate = sender as DestructibleCrate;
        
        Pathfinding.Instance.SetIsWalkableGridPosition(destructibleCrate.GetGridPosition(), true);
    }
}
