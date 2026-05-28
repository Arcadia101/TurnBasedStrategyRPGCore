using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelGrid : MonoBehaviour
{
    public static LevelGrid Instance { get; protected set; }
    
    public event EventHandler OnAnyUnitMovedGridPosition;
    
    [SerializeField] private int width = 5;
    [SerializeField] private int height = 5;
    [SerializeField] private float cellSize = 2.0f;
    
    private GridSystem<GridObject> gridSystem;
    
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There is already a LevelGrid in the scene!");
            Destroy(gameObject);
        }
        Instance = this;

        gridSystem = new GridSystem<GridObject>(width, height, cellSize, 
            (GridSystem<GridObject> g, GridPosition gridPosition) =>
            {
                // Patrón de tablero de ajedrez para todo el grid
                if ((gridPosition.x + gridPosition.z) % 2 == 0)
                {
                    return new GridObject(g, gridPosition, TileType.Octagon);
                }
                else
                {
                    return new GridObject(g, gridPosition, TileType.Rhombus);
                }
            });
    }

    private void Start()
    {
        Pathfinding.Instance.Setup(width, height, cellSize);
    }

    public void AddUnitAtGridPosition(GridPosition gridPosition, Unit unit)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        if (gridObject.CanAddUnit())
        {
            gridObject.AddUnit(unit);
        }
    }
    
    public List<Unit> GetUnitListAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.GetUnitList();
    }
    
    

    public void RemoveUnitAtGridPosition(GridPosition gridPosition, Unit unit)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        gridObject.RemoveUnit(unit);
    }

    public virtual void UnitMovedGridPosition(Unit unit, GridPosition fromGridPosition, GridPosition toGridPosition)
    {
        RemoveUnitAtGridPosition(fromGridPosition, unit);
        AddUnitAtGridPosition(toGridPosition, unit);
        OnAnyUnitMovedGridPosition?.Invoke(this, EventArgs.Empty);
    }
    
    public virtual GridPosition GetGridPosition(Vector3 WorldPosition) => gridSystem.GetGridPosition(WorldPosition);
    public virtual Vector3 GetWorldPosition(GridPosition gridPosition) => gridSystem.GetWorldPosition(gridPosition);
    public virtual bool IsValidGridPosition(GridPosition gridPosition) => gridSystem.IsValidGridPosition(gridPosition);
    public virtual int GetWidth() => gridSystem.GetWidth();
    public virtual int GetHeight() => gridSystem.GetHeight();
    public bool HasAnyUnitOnGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.HasAnyUnit();
    }
    
    public bool CanAddUnitAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.CanAddUnit();
    }
    
    public Unit GetUnitAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.GetUnit();
    }

    public IInteractable GetInteractableAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.GetInteractable();
    }
    
    public void SetInteractableAtGridPosition(GridPosition gridPosition, IInteractable interactable)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        gridObject.SetInteractable(interactable);
    }

    public TileType GetTileTypeAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject.GetTileType();
    }

    // Nuevo método para calcular la posición de formación de la unidad
    public Vector3 GetUnitWorldPosition(Unit unit)
    {
        GridPosition gridPosition = unit.GetGridPosition();
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        
        List<Unit> unitList = gridObject.GetUnitList();
        int unitIndex = unitList.IndexOf(unit);
        int unitCount = unitList.Count;

        Vector3 worldPosition = gridSystem.GetWorldPosition(gridPosition);

        if (unitIndex == -1) return worldPosition;

        float offsetAmount = 0.6f; // Distancia de separación (puedes ajustarla)

        if (unitCount == 2)
        {
            if (unitIndex == 0) return worldPosition + new Vector3(-offsetAmount, 0, 0);
            if (unitIndex == 1) return worldPosition + new Vector3(offsetAmount, 0, 0);
        }
        else if (unitCount >= 3)
        {
            if (unitIndex == 0) return worldPosition + new Vector3(0, 0, offsetAmount); // Frente
            if (unitIndex == 1) return worldPosition + new Vector3(-offsetAmount, 0, -offsetAmount); // Atrás Izquierda
            if (unitIndex == 2) return worldPosition + new Vector3(offsetAmount, 0, -offsetAmount); // Atrás Derecha
        }

        return worldPosition; // Centro si solo hay 1 unidad
    }
    
    
}
