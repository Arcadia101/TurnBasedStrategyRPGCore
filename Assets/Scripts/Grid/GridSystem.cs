using System;
using UnityEngine;

public class GridSystem<TGridObject>
{
    private int width;
    private int height;
    private float cellSize;
    private TGridObject[,] gridObjectArray;

    public GridSystem(int width, int height, float cellSize, Func<GridSystem<TGridObject>, GridPosition, TGridObject> createGridObject)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;

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
        
        return new Vector3(worldX, 0, worldZ);
    }
    
    public GridPosition GetGridPosition(Vector3 worldPosition)
    {
        float x = worldPosition.x / (cellSize * 0.5f);
        float z = worldPosition.z / (cellSize * 0.5f);

        int gridX = Mathf.RoundToInt(((z + x) / 2f) + (width - 1) / 2f);
        int gridZ = Mathf.RoundToInt(((z - x) / 2f) + (height - 1) / 2f);

        return new GridPosition(gridX, gridZ);
    }

    public TGridObject GetGridObject(GridPosition gridPosition)
    {
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
