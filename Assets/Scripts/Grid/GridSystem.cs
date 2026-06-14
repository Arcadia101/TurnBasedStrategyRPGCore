using System;
using UnityEngine;

public class GridSystem<TGridObject>
{
    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private TGridObject[,] gridObjectArray;

    public GridSystem(int width, int height, float cellSize, Vector3 originPosition, Func<GridSystem<TGridObject>, GridPosition, TGridObject> createGridObject)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridObjectArray = new TGridObject[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                gridObjectArray[x,z] = createGridObject(this, gridPosition);
            }
        }
    }

    public Vector3 GetWorldPosition(GridPosition gridPosition)
    {
        float centeredX = gridPosition.x - (width - 1) / 2f;
        float centeredZ = gridPosition.z - (height - 1) / 2f;

        float worldX = (centeredX - centeredZ) * cellSize * 0.5f;
        float worldZ = (centeredX + centeredZ) * cellSize * 0.5f;
        
        return new Vector3(worldX, 0, worldZ) + originPosition;
    }
    
    public GridPosition GetGridPosition(Vector3 worldPosition)
    {
        Vector3 localPosition = worldPosition - this.originPosition;

        // Invertir la transformación isométrica
        float isoCellSize = cellSize * 0.5f;
        float invIsoCellSize = 1f / isoCellSize;

        float centeredGridXFloat = (localPosition.z * invIsoCellSize + localPosition.x * invIsoCellSize) / 2f;
        float centeredGridZFloat = (localPosition.z * invIsoCellSize - localPosition.x * invIsoCellSize) / 2f;

        // Ajustar de nuevo a las coordenadas de la cuadrícula no centradas
        // Usar RoundToInt para mayor precisión al mapear del mundo a la cuadrícula
        int gridX = Mathf.RoundToInt(centeredGridXFloat + (width - 1) / 2f);
        int gridZ = Mathf.RoundToInt(centeredGridZFloat + (height - 1) / 2f);

        return new GridPosition(gridX, gridZ);
    }

    public TGridObject GetGridObject(GridPosition gridPosition)
    {
        if (!IsValidGridPosition(gridPosition))
        {
            Debug.LogError($"GetGridObject: Invalid GridPosition ({gridPosition.x}, {gridPosition.z}). Width: {width}, Height: {height}");
            return default;
        }
        return gridObjectArray[gridPosition.x, gridPosition.z];
    }
    
    public bool IsValidGridPosition(GridPosition gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < width && gridPosition.z >= 0 && gridPosition.z < height;
    }
    
    public int GetWidth()
    {
        return width;
    }
    
    public int GetHeight()
    {
        return height;
    }
    
    public float GetCellSize()
    {
        return cellSize;
    }
}