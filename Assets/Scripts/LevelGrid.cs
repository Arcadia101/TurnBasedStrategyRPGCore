using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelGrid : MonoBehaviour
{
    public static LevelGrid Instance { get; private set; }
    
    public event EventHandler OnAnyUnitMovedGridPosition;
    
    [SerializeField] protected int width = 5;
    [SerializeField] protected int height = 5;
    [SerializeField] protected float cellSize = 2.0f;
    
    protected GridSystem<GridObject> gridSystem;
    
    // Un diccionario donde la clave es la celda, y el valor es una lista de todas las unidades 
    // que actualmente están proyectando energía en esa celda específica.
    protected Dictionary<GridPosition, List<Unit>> globalEnergyMap = new Dictionary<GridPosition, List<Unit>>();
    
    protected virtual void Awake()
    {
        // CONDICIÓN CLAVE: Solo se asigna si este script es un LevelGrid puro, no un hijo.
        if (GetType() == typeof(LevelGrid))
        {
            if (Instance != null)
            {
                Debug.LogError("¡Hay más de un LevelGrid base en la escena!");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

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
        Pathfinding.Instance.Setup(width, height, cellSize);
    }

    public void AddUnitAtGridPosition(GridPosition gridPosition, Unit unit)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        if (gridObject != null && gridObject.CanAddUnit()) // Añadida verificación de null
        {
            gridObject.AddUnit(unit);
        }
        else if (gridObject == null)
        {
            Debug.LogWarning($"No se pudo añadir la unidad {unit.name} en la posición de cuadrícula inválida {gridPosition}.");
        }
    }
    
    public virtual List<Unit> GetUnitListAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        if (gridObject == null) return new List<Unit>(); // Devuelve una lista vacía si la posición es inválida
        return gridObject.GetUnitList();
    }
    
    

    public void RemoveUnitAtGridPosition(GridPosition gridPosition, Unit unit)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        if (gridObject != null) // Añadida verificación de null
        {
            gridObject.RemoveUnit(unit);
        }
    }

    public virtual void UnitMovedGridPosition(Unit unit, GridPosition fromGridPosition, GridPosition gridPosition)
    {
        RemoveUnitAtGridPosition(fromGridPosition, unit);
        AddUnitAtGridPosition(gridPosition, unit);
        OnAnyUnitMovedGridPosition?.Invoke(this, EventArgs.Empty);
    }
    
    public virtual GridPosition GetGridPosition(Vector3 WorldPosition) => gridSystem.GetGridPosition(WorldPosition);
    public virtual Vector3 GetWorldPosition(GridPosition gridPosition) => gridSystem.GetWorldPosition(gridPosition);
    public virtual bool IsValidGridPosition(GridPosition gridPosition) => gridSystem.IsValidGridPosition(gridPosition);
    public virtual int GetWidth() => gridSystem.GetWidth();
    public virtual int GetHeight() => gridSystem.GetHeight();
    public virtual float GetCellSize() => gridSystem.GetCellSize();
    public virtual bool HasAnyUnitOnGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject != null && gridObject.HasAnyUnit(); // Añadida verificación de null
    }
    
    public bool CanAddUnitAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject != null && gridObject.CanAddUnit(); // Añadida verificación de null
    }
    
    public virtual Unit GetUnitAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject?.GetUnit(); // Uso del operador ?. para seguridad de null
    }

    public virtual IInteractable GetInteractableAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject?.GetInteractable(); // Uso del operador ?. para seguridad de null
    }
    
    public void SetInteractableAtGridPosition(GridPosition gridPosition, IInteractable interactable)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        if (gridObject != null) // Añadida verificación de null
        {
            gridObject.SetInteractable(interactable);
        }
    }

    public TileType GetTileTypeAtGridPosition(GridPosition gridPosition)
    {
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        return gridObject != null ? gridObject.GetTileType() : TileType.Octagon; // Devuelve un valor predeterminado si es null
    }

    // Nuevo método para calcular la posición de formación de la unidad
    public virtual Vector3 GetUnitWorldPosition(Unit unit)
    {
        GridPosition gridPosition = unit.GetGridPosition();
        GridObject gridObject = gridSystem.GetGridObject(gridPosition);
        
        if (gridObject == null) return unit.transform.position; // Devuelve la posición actual de la unidad si la gridObject es null

        List<Unit> unitList = gridObject.GetUnitList();
        int unitIndex = unitList.IndexOf(unit);
        int unitCount = unitList.Count;

        Vector3 worldPosition = GetWorldPosition(gridPosition);

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

    public virtual GridSystem<GridObject> GetGridSystem() => gridSystem;
    
    // Método público para que UnitEnergy pueda leer el mapa de choques
    public virtual Dictionary<GridPosition, List<Unit>> GetGlobalEnergyMap()
    {
        return globalEnergyMap;
    }
    
    public virtual void RefreshGlobalEnergyMap(List<Unit> allUnitsInLevel)
    {
        // 1. Limpiamos el mapa viejo para no arrastrar fantasmas
        globalEnergyMap.Clear();

        // 2. Recorremos todas las unidades del nivel
        foreach (Unit unit in allUnitsInLevel)
        {
            // Le pedimos a su componente de energía las celdas que afecta
            UnitEnergy unitEnergy = unit.GetComponent<UnitEnergy>();
            if (unitEnergy == null) continue;

            List<GridPosition> energizedCells = unitEnergy.GetEnergizedPositions();

            // 3. Registramos cada celda en el diccionario global
            foreach (GridPosition cell in energizedCells)
            {
                if (!globalEnergyMap.ContainsKey(cell))
                {
                    globalEnergyMap[cell] = new List<Unit>();
                }
            
                // Añadimos esta unidad como una de las dueñas de la energía en esa casilla
                globalEnergyMap[cell].Add(unit);
            }
        }

        // 4. ¡EL DISPARADOR GLOBAL! 
        // Ahora que el mapa está actualizado, le avisamos a todas las unidades 
        // que ejecuten su auto-evaluación 'CheckForOverlap()'
        foreach (Unit unit in allUnitsInLevel)
        {
            UnitEnergy unitEnergy = unit.GetComponent<UnitEnergy>();
            if (unitEnergy != null)
            {
                unitEnergy.CheckForOverlap(); 
            }
        }
    }
    
    public virtual void TriggerEnergyRefresh()
    {
        globalEnergyMap.Clear();

        Unit[] unitsArray = FindObjectsOfType<Unit>();
        List<Unit> allUnits = new List<Unit>(unitsArray);

        foreach (Unit unit in allUnits)
        {
            // ¡EL FILTRO PROTECTOR!
            // Si la unidad está registrada en el Toroide, la Grid de abajo la ignora por completo
            if (unit.GetCurrentGridType() == GridType.Toroid) continue;

            UnitEnergy unitEnergy = unit.GetComponent<UnitEnergy>();
            if (unitEnergy == null) continue;

            List<GridPosition> energizedCells = unitEnergy.GetEnergizedPositions();
            foreach (GridPosition cell in energizedCells)
            {
                if (!globalEnergyMap.ContainsKey(cell)) globalEnergyMap[cell] = new List<Unit>();
                globalEnergyMap[cell].Add(unit);
            }
        }

        // Solo le pedimos auto-evaluarse a las unidades de abajo
        foreach (Unit unit in allUnits)
        {
            if (unit.GetCurrentGridType() == GridType.Normal)
            {
                unit.GetComponent<UnitEnergy>()?.CheckForOverlap();
            }
        }
    }
    
    protected void OnEnable()
    {
        // Nos suscribimos a los eventos globales
        GameGridEvents.OnAnyUnitMovementComplete += HandleGlobalRefresh;
        GameGridEvents.OnAnyUnitEnergyChanged += HandleGlobalRefresh;
    }

    protected void OnDisable()
    {
        // Es vital desuscribirse al destruir el objeto para evitar errores de referencia
        GameGridEvents.OnAnyUnitMovementComplete -= HandleGlobalRefresh;
        GameGridEvents.OnAnyUnitEnergyChanged -= HandleGlobalRefresh;
    }

    protected void HandleGlobalRefresh()
    {
        // Este método se ejecutará automáticamente en ambos tableros al mismo tiempo
        // cuando cualquier unidad termine su viaje.
        TriggerEnergyRefresh(); 
    }
}