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
    
    [SerializeField] private Transform octagonVisualPrefab;
    [SerializeField] private Transform rhombusVisualPrefab;
    [SerializeField] private bool showGridCoordinates = false;
    [SerializeField] private GridVisualTypeColor[] gridVisualTypeColorList;
    
    private GridSystemVisualSingle[,] gridSystemVisualSingleArray;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There is already a GridSystemVisual in the scene!");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
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

    void UpdateGridVisual()
    {
        ResetAllGridPositions();
        
        Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
        if (selectedUnit == null) return;

        BaseAction selectedAction = UnitActionSystem.Instance.GetSelectedAction();
        if (selectedAction == null) return;

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
                ShowGridPositionRange(selectedUnit.GetGridPosition(), shootAction.GetMaxShootDistance(), GridVisualType.RedSoft);
                break;
            case GrenadeAction grenadeAction:
                gridVisualType = GridVisualType.Green;
                break;
            case SwordAction swordAction:
                gridVisualType = GridVisualType.Brown;
                ShowGridPositionRangeSquare(selectedUnit.GetGridPosition(), swordAction.GetMaxSwordDistance(), GridVisualType.BrownSoft);
                break;
            case InteractAction interactAction:
                gridVisualType = GridVisualType.Blue;
                break;
        }
        
        ShowGridPositionList(selectedAction.GetValidActionGridPositionList(), gridVisualType);
    }

    public void ResetAllGridPositions()
    {
        for (int x = 0; x < LevelGrid.Instance.GetWidth(); x++)
        {
            for (int z = 0; z < LevelGrid.Instance.GetHeight(); z++)
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
            if(gridSystemVisualSingleArray[gridPosition.x, gridPosition.z] != null)
                gridSystemVisualSingleArray[gridPosition.x, gridPosition.z].SetColor(GetGridVisualColor(gridVisualType));
        }
    }

    private void ShowGridPositionRange(GridPosition gridPosition, int range, GridVisualType gridVisualType = GridVisualType.White)
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
    
    private void ShowGridPositionRangeSquare(GridPosition gridPosition, int range, GridVisualType gridVisualType = GridVisualType.White)
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

    private void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        UpdateGridVisual();
    }
    
    private void LevelGrid_OnAnyUnitMovedGridPosition(object sender, EventArgs e)
    {
        UpdateGridVisual();
    }

    private Color GetGridVisualColor(GridVisualType gridVisualType)
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
}
