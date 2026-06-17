using System;
using System.Collections.Generic;
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
    
    // Añadimos el booleano 'isToroid' al final de los parámetros
    public List<GridPosition> CalculateEnergyPositions(GridPosition unitPos, EmotypeData emotype, bool isToroid)
    {
        List<GridPosition> validEnergyPositions = new List<GridPosition>();

        validEnergyPositions.Add(unitPos);

        foreach (EnergyDirection direction in emotype.GetActiveDirections())
        {
            GridPosition offset = EnergyGridExtensions.GetDirectionOffset(direction);
            GridPosition targetPos = unitPos + offset;

            if (isToroid && ToroidLevelGrid.ToroidInstance != null)
            {
                targetPos = ToroidLevelGrid.ToroidInstance.GetWrappedGridPosition(targetPos);
            }

            // --- EL FILTRO GEOMÉTRICO ---
            //Validamos que la casilla exista en los límites del mapa
            if (IsValidGridPosition(targetPos))
            {
                // 1. Obtenemos el objeto genérico y lo casteamos a tu clase real de nodos
                // Reemplaza "GridObject" por el nombre exacto de tu clase si es diferente
                GridObject originNode = GetGridObject(unitPos) as GridObject;

                if (originNode != null)
                {
                    // 2. ¡Listo! Ahora el compilador sí te dejará leer el enum y los símbolos
                    bool originIsRombo = originNode.GetTileType() == TileType.Rhombus;

                    if (originIsRombo)
                    {
                        // REGLA ESTRICTA: Bloqueamos el salto diagonal (1,1) entre rombos
                        if (Mathf.Abs(offset.x) > 0 && Mathf.Abs(offset.z) > 0)
                        {
                            Debug.Log($"[ENERGÍA] Bloqueada proyección diagonal desde Rombo hacia {targetPos}");
                            continue; 
                        }
                    }
                }

                validEnergyPositions.Add(targetPos);
            }
        }

        return validEnergyPositions;
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

public static class EnergyGridExtensions
{
    public static GridPosition GetDirectionOffset(EnergyDirection direction)
    {
        // Al rotar la lógica 45 grados para compensar la inclinación del tablero:
        // Las direcciones cardinales (N, E, S, O) pasan a usar pasos diagonales en la matriz (1,1)
        // Las diagonales (NE, SE, SO, NO) pasan a usar pasos rectos en la matriz (1,0)
        return direction switch
        {
            EnergyDirection.North     => new GridPosition(1, 1),
            EnergyDirection.NorthEast => new GridPosition(1, 0),
            EnergyDirection.East      => new GridPosition(1, -1),
            EnergyDirection.SouthEast => new GridPosition(0, -1),
            EnergyDirection.South     => new GridPosition(-1, -1),
            EnergyDirection.SouthWest => new GridPosition(-1, 0),
            EnergyDirection.West      => new GridPosition(-1, 1),
            EnergyDirection.NorthWest => new GridPosition(0, 1),
            _ => new GridPosition(0, 0)
        };
    }
}

public static class GameGridEvents
{
    // Este evento se disparará cada vez que CUALQUIER unidad en el juego termine de moverse
    public static Action OnAnyUnitMovementComplete;

    // Este evento se disparará si en el futuro una unidad rota o cambia su energía
    public static Action OnAnyUnitEnergyChanged;

    public static void TriggerMovementComplete() => OnAnyUnitMovementComplete?.Invoke();
    public static void TriggerEnergyChanged() => OnAnyUnitEnergyChanged?.Invoke();
}