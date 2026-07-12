using System;
using System.Collections.Generic;
using UnityEngine;

public class GridSystemVisual : MonoBehaviour
{
    public static GridSystemVisual Instance { get; private set; }
    
    public enum GridVisualType
    {
        White,
        Brown,
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
        Gray,
        Orange,
        Pink,
        WhiteSoft,
        BrownSoft,
        RedSoft,
        BlueSoft,
        GreenSoft,
        YellowSoft,
        PurpleSoft,
        GraySoft,
        OrangeSoft,
        PinkSoft,
    }
    
    [Serializable]
    public struct GridVisualTypeColor
    {
        public GridVisualType gridVisualType;
        public Color color;
    }
    
    [SerializeField] protected Transform octagonVisualPrefab;
    [SerializeField] protected Transform rhombusVisualPrefab;
    [SerializeField] protected bool showGridCoordinates = false;
    [SerializeField] protected GridVisualTypeColor[] gridVisualTypeColorList;
    
    protected GridSystemVisualSingle[,] gridSystemVisualSingleArray;

    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    protected virtual void Start()
    {
        InitializeGridVisuals();
    }

    protected virtual void InitializeGridVisuals()
    {
        gridSystemVisualSingleArray = new GridSystemVisualSingle[LevelGrid.Instance.GetWidth(), LevelGrid.Instance.GetHeight()];
        for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.Instance.GetHeight(); z++)
            {
                GridPosition gridPosition = new GridPosition(x, z);
                TileType tileType = LevelGrid.Instance.GetTileTypeAtGridPosition(gridPosition);

                if (tileType == TileType.Empty) continue;

                Transform prefabToInstantiate = tileType == TileType.Octagon ? octagonVisualPrefab : rhombusVisualPrefab;
                
                Transform gridSystemVisualSingleTransform = Instantiate(prefabToInstantiate, LevelGrid.Instance.GetWorldPosition(gridPosition), Quaternion.identity);
                
                gridSystemVisualSingleArray[x,z] = gridSystemVisualSingleTransform.GetComponent<GridSystemVisualSingle>();
                gridSystemVisualSingleArray[x,z].SetCoordinates(gridPosition.ToString());
                gridSystemVisualSingleArray[x,z].ShowCoordinates(showGridCoordinates);
            }
        }

        UnitActionSystem.Instance.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged;
        LevelGrid.Instance.OnAnyUnitMovedGridPosition += LevelGrid_OnAnyUnitMovedGridPosition;
        
        UpdateGridVisual();
    }

    protected void ShowGridPositionRange(GridPosition gridPosition, int range, GridVisualType gridVisualType = GridVisualType.White)
    {
        List<GridPosition> gridPositionList = new List<GridPosition>();
        
        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                GridPosition testGridPosition = gridPosition + new GridPosition(x, z);
                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) continue;
                
                int testDistance = Math.Abs(x) + Math.Abs(z);
                if (testDistance > range) continue;
                
                gridPositionList.Add(testGridPosition);
            }
        }
        ShowGridPositionList(gridPositionList, gridVisualType);
    }
    
    protected void ShowGridPositionRangeSquare(GridPosition gridPosition, int range, GridVisualType gridVisualType = GridVisualType.White)
    {
        List<GridPosition> gridPositionList = new List<GridPosition>();
        
        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                GridPosition testGridPosition = gridPosition + new GridPosition(x, z);
                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) continue;
                
                gridPositionList.Add(testGridPosition);
            }
        }
        ShowGridPositionList(gridPositionList, gridVisualType);
    }

    protected void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        UpdateGridVisual();
    }
    
    protected void LevelGrid_OnAnyUnitMovedGridPosition(object sender, EventArgs e)
    {
        UpdateGridVisual();
    }

    protected Color GetGridVisualColor(GridVisualType gridVisualType)
    {
        foreach (GridVisualTypeColor gridVisualTypeColor in gridVisualTypeColorList)
        {
            if (gridVisualTypeColor.gridVisualType == gridVisualType)
            {
                return gridVisualTypeColor.color;
            }
        }
        Debug.LogError("GridVisualTypeColor not found for GridVisualType!" + gridVisualType);
        return Color.white;
    }
    
    protected virtual void UpdateGridVisual()
    {
        ResetAllGridPositions();
        
        Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
        if (selectedUnit == null) return;

        BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction();
        if (selectedAction == null) return;

        // ¡EL CAMBIO CLAVE! Obtenemos la Grid real de la acción (Normal o Toroidal)
        LevelGrid activeGrid = selectedAction.GetGridContext();

        GridVisualType gridVisualType;
        switch (selectedAction)
        {
            default:
            case MoveAction moveAction:
                gridVisualType = GridVisualType.Blue;
                break;
            case HealAction healAction:
                gridVisualType = GridVisualType.Blue;
                break;
            case ShootAction shootAction:
                gridVisualType = GridVisualType.Red;
                // Le pasamos la 'activeGrid' al dibujador de rango
                ShowGridPositionRange(selectedUnit.GetGridPosition(), shootAction.GetMaxShootDistance(), GridVisualType.RedSoft, activeGrid);
                break;
            case GrenadeAction grenadeAction:
                gridVisualType = GridVisualType.Green;
                break;
            case SwordAction swordAction:
                gridVisualType = GridVisualType.Brown;
                // Le pasamos la 'activeGrid' al dibujador de rango
                ShowGridPositionRangeSquare(selectedUnit.GetGridPosition(), swordAction.GetMaxSwordDistance(), GridVisualType.BrownSoft, activeGrid);
                break;
            case InteractAction interactAction:
                gridVisualType = GridVisualType.Blue;
                break;
        }
        
        // FILTRO INTELIGENTE: Solo rellenamos las casillas por dentro si NO es la acción de movimiento.
        // De esta manera el suelo del rango se queda transparente y limpio para tus energías tácticas.
        if (selectedAction is not MoveAction)
        {
            ShowGridPositionList(selectedAction.GetValidActionGridPositionList(), gridVisualType);
        }
    }

    // CORRECCIÓN: Usamos las dimensiones de la grid de este visualizador (evita desbordes)
    public void ResetAllGridPositions()
    {
        int width = gridSystemVisualSingleArray.GetLength(0);
        int height = gridSystemVisualSingleArray.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                if(gridSystemVisualSingleArray[x,z] != null)
                    gridSystemVisualSingleArray[x,z].ResetToDefaultColor();
            }
        }
    }
    
    public void ShowGridPositionList(List<GridPosition> gridPositionList, GridVisualType gridVisualType = GridVisualType.White)
    {
        foreach (GridPosition gridPosition in gridPositionList)  
        {
            // CONTROL DE SEGURIDAD: Validamos que la casilla esté dentro de los límites de NUESTRO array visual antes de pintar
            if (gridPosition.x >= 0 && gridPosition.z >= 0 && 
                gridPosition.x < gridSystemVisualSingleArray.GetLength(0) && 
                gridPosition.z < gridSystemVisualSingleArray.GetLength(1))
            {
                if(gridSystemVisualSingleArray[gridPosition.x, gridPosition.z] != null)
                    gridSystemVisualSingleArray[gridPosition.x, gridPosition.z].SetColor(GetGridVisualColor(gridVisualType));
            }
        }
    }

    // CORRECCIÓN: Recibe la Grid activa para validar la casilla usando el contexto correcto
    protected void ShowGridPositionRange(GridPosition gridPosition, int range, GridVisualType gridVisualType, LevelGrid activeGrid)
    {
        List<GridPosition> gridPositionList = new List<GridPosition>();
        
        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                GridPosition testGridPosition = gridPosition + new GridPosition(x, z);
                
                // Si estamos en un Toroide, envolvemos la casilla de rango suave (Pac-Man) antes de validarla
                if (activeGrid is ToroidLevelGrid toroidGrid)
                {
                    testGridPosition = toroidGrid.GetWrappedGridPosition(testGridPosition);
                }

                if (!activeGrid.IsValidGridPosition(testGridPosition)) continue;
                
                int testDistance = Math.Abs(x) + Math.Abs(z);
                if (testDistance > range) continue;
                
                gridPositionList.Add(testGridPosition);
            }
        }
        ShowGridPositionList(gridPositionList, gridVisualType);
    }
    
    // CORRECCIÓN: Recibe la Grid activa para validar la casilla usando el contexto correcto
    protected void ShowGridPositionRangeSquare(GridPosition gridPosition, int range, GridVisualType gridVisualType, LevelGrid activeGrid)
    {
        List<GridPosition> gridPositionList = new List<GridPosition>();
        
        for (int x = -range; x <= range; x++)
        {
            for (int z = -range; z <= range; z++)
            {
                GridPosition testGridPosition = gridPosition + new GridPosition(x, z);
                
                // Si estamos en un Toroide, envolvemos la casilla de rango suave de la espada (Pac-Man)
                if (activeGrid is ToroidLevelGrid toroidGrid)
                {
                    testGridPosition = toroidGrid.GetWrappedGridPosition(testGridPosition);
                }

                if (!activeGrid.IsValidGridPosition(testGridPosition)) continue;
                
                gridPositionList.Add(testGridPosition);
            }
        }
        ShowGridPositionList(gridPositionList, gridVisualType);
    }
    
    // Devuelve el componente visual single de una posición específica protegiendo los límites del array.
    public GridSystemVisualSingle GetGridSystemVisualSingleAtPosition(GridPosition gridPosition)
    {
        if (gridPosition.x >= 0 && gridPosition.z >= 0 && 
            gridPosition.x < gridSystemVisualSingleArray.GetLength(0) && 
            gridPosition.z < gridSystemVisualSingleArray.GetLength(1))
        {
            return gridSystemVisualSingleArray[gridPosition.x, gridPosition.z];
        }
        return null;
    }
}
