using System.Collections.Generic;
using UnityEngine;

public class GridObject
{
    private GridSystem<GridObject> gridSystem;
    private GridPosition gridPosition;
    private List<Unit> unitList;
    private IInteractable interactable;

    private TileType TileType;
    private int maxUnitCount;

    public GridObject(GridSystem<GridObject> gridSystem, GridPosition gridPosition, TileType tileType)
    {
        this.gridSystem = gridSystem;
        this.gridPosition = gridPosition;
        this.TileType = tileType;
        unitList = new List<Unit>();

        switch (tileType)
        {
            case TileType.Octagon:
                maxUnitCount = 3;
                break;
            case TileType.Rhombus:
                maxUnitCount = 1;
                break;
            case TileType.Empty:
                maxUnitCount = 0;
                break;
        }
    }

    public override string ToString()
    {
        string uniString = "";
        foreach (Unit unit in unitList)
        {
            uniString+= unit.ToString() + "\n";
        }
        return gridPosition.ToString() + "\n" + uniString;
    }

    public void AddUnit(Unit unit)
    {
        unitList.Add(unit);
    }

    public void RemoveUnit(Unit unit)
    {
        unitList.Remove(unit);
    }

    public List<Unit> GetUnitList()
    {
        return unitList;
    }

    public bool HasAnyUnit()
    {
        return unitList.Count > 0;
    }
    
    public Unit GetUnit(int index = 0)
    {
        if (HasAnyUnit())
        {
            return unitList[index];
        }
        else
        {
            return null;
        }
    }
    
    public bool CanAddUnit()
    {
        return unitList.Count < maxUnitCount;
    }

    public IInteractable GetInteractable()
    {
        return interactable;
    }
    
    public void SetInteractable(IInteractable interactable)
    {
        this.interactable = interactable;
    }
    
    public TileType GetTileType()
    {
        return TileType;
    }
}
