using System;
using System.Collections.Generic;
using UnityEngine;

public class ToroidGridSystemVisual : GridSystemVisual
{
    public static ToroidGridSystemVisual ToroidInstance { get; private set; }

    protected override void Awake()
    {
        if (ToroidInstance != null)
        {
            Destroy(gameObject);
            return;
        }
        ToroidInstance = this;
    }

    protected override void Start()
    {
        InitializeGridVisuals();
    }
    
    protected override void InitializeGridVisuals()
    {
        if (ToroidLevelGrid.ToroidInstance == null) return;

        int width = ToroidLevelGrid.ToroidInstance.GetWidth();
        int height = ToroidLevelGrid.ToroidInstance.GetHeight();

        gridSystemVisualSingleArray = new GridSystemVisualSingle[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                
                TileType tileType = ToroidLevelGrid.ToroidInstance.GetTileTypeAtGridPosition(gridPosition);

                if (tileType == TileType.Empty) continue;

                Transform prefabToInstantiate = tileType == TileType.Octagon ? octagonVisualPrefab : rhombusVisualPrefab;
                
                Vector3 worldPosition = ToroidLevelGrid.ToroidInstance.GetWorldPosition(gridPosition);
                
                Transform gridSystemVisualSingleTransform = Instantiate(prefabToInstantiate, worldPosition, Quaternion.identity, transform);
                
                gridSystemVisualSingleArray[x,z] = gridSystemVisualSingleTransform.GetComponent<GridSystemVisualSingle>();
                gridSystemVisualSingleArray[x,z].SetCoordinates(gridPosition.ToString());
                gridSystemVisualSingleArray[x,z].ShowCoordinates(showGridCoordinates);
            }
        }

        UnitActionSystem.Instance.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged;
        
        ToroidLevelGrid.ToroidInstance.OnAnyUnitMovedGridPosition += LevelGrid_OnAnyUnitMovedGridPosition;
        
        UpdateGridVisual();
    }

    // ¡EL FILTRO ABSOLUTO PARA DETENER EL EFECTO ESPEJO!
    protected override void UpdateGridVisual()
    {
        // 1. Siempre limpiamos nuestros propios octágonos del Toroide superior
        ResetAllGridPositions();

        Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
        if (selectedUnit == null) return;

        // 2. ¡EL CANDADO! Si la unidad seleccionada es de la Grid Normal, este script visual 
        // del Toroide se cruza de brazos y no dibuja nada en el mapa de arriba.
        if (selectedUnit.GetGridType() != GridType.Toroid) return;

        // 3. Si la unidad es Toroidal, ejecutamos la actualización base que ahora calcula 
        // los rangos usando la activeGrid y aplica el Wrapped de forma perfecta.
        base.UpdateGridVisual();
    }
}