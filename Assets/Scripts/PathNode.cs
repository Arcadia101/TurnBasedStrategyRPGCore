using UnityEngine;

public class PathNode
{
    private GridPosition gridPosition;
    private int gCost;
    private int hCost;
    private int fCost;
    private bool isWalkable = true;
    private PathNode cameFromPathNode;
    
    public PathNode(GridPosition gridPosition)
    {
        this.gridPosition = gridPosition;
    }
    
    public override string ToString()
    {
        return gridPosition.ToString();
    }

    public int GetGCost()
    {
        return gCost;
    }
    
    public int GetHCost()
    {
        return hCost;
    }
    
    public int GetFCost()
    {
        return fCost;
    }

    public void SetGCost(int gCost)
    {
        this.gCost = gCost;
    }
    
    public void SetHCost(int hCost)
    {
        this.hCost = hCost;
    }
    
    public void CalculateFCost()
    {
        fCost = gCost + hCost;
    }

    public void ResetCameFromPathNode()
    {
        cameFromPathNode = null;
    }
    
    public GridPosition GetGridPosition()
    {
        return gridPosition;
    }

    public void SetCameFormPathNode(PathNode pathNode)
    {
        cameFromPathNode = pathNode;
    }

    public bool IsWalkable()
    {
        return isWalkable;
    }

    public void SetWalkable(bool isWalkable)
    {
        this.isWalkable = isWalkable;
    }
    
    public PathNode GetCameFromPathNode()
    {
        return cameFromPathNode;
    }

}
