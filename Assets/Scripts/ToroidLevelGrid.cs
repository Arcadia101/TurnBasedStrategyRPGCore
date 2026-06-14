using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ToroidLevelGrid : LevelGrid
{
    public static ToroidLevelGrid ToroidInstance { get; private set; }
    
    protected override void Awake()
    {
        // OMITIMOS base.Awake() de forma intencional para no pisar el Instance base
    
        if (ToroidInstance != null)
        {
            Debug.LogError("¡Hay más de un ToroidLevelGrid en la escena!");
            Destroy(gameObject);
            return;
        }
        ToroidInstance = this;

        gridSystem = new GridSystem<GridObject>(width, height, cellSize, transform.position,
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
        if (ToroidPathfinding.ToroidInstance != null)
        {
            ToroidPathfinding.ToroidInstance.Setup(GetWidth(), GetHeight(), GetCellSize());
            Debug.Log("[CEREBRO-PATHFINDING] Inicializada la matriz de nodos para el Toroide.");
        }
    }
    
    // Método central matemático para envolver las posiciones antes de que toquen el GridSystem
    public GridPosition GetWrappedGridPosition(GridPosition gridPosition)
    {
        int width = GetWidth();
        int height = GetHeight();

        // Aritmética modular estricta para el Toroide (Soporta positivos y negativos)
        int wrappedX = (gridPosition.x % width + width) % width;
        int wrappedZ = (gridPosition.z % height + height) % height;

        return new GridPosition(wrappedX, wrappedZ);
    }

    // --- SOBRESCRITURA POLIMÓRFICA ---
    
    public override void UnitMovedGridPosition(Unit unit, GridPosition fromGridPosition, GridPosition toGridPosition)
    {
        // Aseguramos que tanto el origen como el destino pasen por el filtro Pac-Man
        GridPosition wrappedFrom = GetWrappedGridPosition(fromGridPosition);
        GridPosition wrappedTo = GetWrappedGridPosition(toGridPosition);

        // Ejecutamos el movimiento base de forma segura con los índices corregidos
        base.UnitMovedGridPosition(unit, wrappedFrom, wrappedTo);
    }
    
    public override GridPosition GetGridPosition(Vector3 worldPosition)
    {
        // Restamos la posición del objeto en el mundo para volver la coordenada "local" al Toroide
        // GridSystem ya maneja originPosition
    
        // Ejecutamos la matemática base con la posición local corregida
        return base.GetGridPosition(worldPosition);
    }

    public override Vector3 GetWorldPosition(GridPosition gridPosition)
    {
        // Al vector del mundo base le sumamos el offset de dónde está el objeto en la escena
        // GridSystem ya maneja originPosition
        return base.GetWorldPosition(gridPosition);
    }

    // 2. Blindamos el validador de posiciones para que las casillas "virtuales" 
    // (como la 5,4) sean siempre válidas porque se convierten a casillas reales
    public override bool IsValidGridPosition(GridPosition gridPosition)
    {
        // 1. Calculamos la posición del mundo en metros de la casilla que se está evaluando
        Vector3 worldPosOfGrid = GetWorldPosition(gridPosition);

        // 2. Calculamos la distancia entre el centro de esta Grid Toroidal y la casilla
        // Si la casilla evaluada está físicamente en el mapa normal (a 40 o 100 metros de distancia),
        // la distancia será enorme, lo que significa que NO pertenece a este Toroide.
        float distanceToGridCenter = Vector3.Distance(transform.position, worldPosOfGrid);

        // Ajustamos el radio de tolerancia según el tamaño de tu mapa (ej: diagonal máxima del tablero)
        float maxGridRadius = (width * cellSize) * 1.5f; 

        if (distanceToGridCenter > maxGridRadius)
        {
            return false; // Si está muy lejos físicamente, esta grid no la gobierna
        }

        // 3. Si pasó el filtro físico, aplicamos tu validación modular estándar
        GridPosition wrappedPos = GetWrappedGridPosition(gridPosition);
        return wrappedPos.x >= 0 && 
               wrappedPos.x < width && 
               wrappedPos.z >= 0 && 
               wrappedPos.z < height;
    }
    
    public override bool HasAnyUnitOnGridPosition(GridPosition gridPosition)
    {
        GridPosition wrappedPos = GetWrappedGridPosition(gridPosition);
        GridObject gridObject = gridSystem.GetGridObject(wrappedPos);
        return gridObject.HasAnyUnit();
    }

    public override List<Unit> GetUnitListAtGridPosition(GridPosition gridPosition)
    {
        GridPosition wrappedPos = GetWrappedGridPosition(gridPosition);
        GridObject gridObject = gridSystem.GetGridObject(wrappedPos);
        return gridObject.GetUnitList();
    }

    public override Unit GetUnitAtGridPosition(GridPosition gridPosition)
    {
        GridPosition wrappedPos = GetWrappedGridPosition(gridPosition);
        GridObject gridObject = gridSystem.GetGridObject(wrappedPos);
        return gridObject.GetUnit();
    }

    public override IInteractable GetInteractableAtGridPosition(GridPosition gridPosition)
    {
        GridPosition wrappedPos = GetWrappedGridPosition(gridPosition);
        GridObject gridObject = gridSystem.GetGridObject(wrappedPos);
        return gridObject.GetInteractable();
    }
}