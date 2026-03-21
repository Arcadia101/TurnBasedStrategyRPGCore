using System;
using UnityEngine;
using UnityEngine.InputSystem;
public class UnitActionSystem : MonoBehaviour
{
    public static UnitActionSystem Instance { get; private set; }
    
    public event EventHandler OnSelectedUnitChanged;
    [SerializeField] private Unit selectedUnit;
    [SerializeField] private LayerMask unitLayer;
    
    private bool isBusy;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There is already a UnitActionSystem in the scene!");
            Destroy(gameObject);
        }
        Instance = this;
    }

    private void Update()
    {
        if (isBusy)
        {
            return;
        }
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if(TryHandleUnitSelection()) return;
            GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition());
            if (selectedUnit != null)
            {
                
                if (selectedUnit.GetMoveAction().IsValidActionGridPosition(mouseGridPosition))
                {
                    //SetBusy();
                    //movimiento por grid
                    selectedUnit.GetMoveAction().Move(mouseGridPosition);
                }
                //movimiento libre de grid
                //selectedUnit.GetMoveAction().Move(MouseWorld.GetPosition());
                
            }
        }
        
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if(TryHandleUnitSelection()) return;
            if (selectedUnit != null)
            {
                //SetBusy();
                selectedUnit.GetSpinAction().Spin();
            }
        }
    }

    private void SetBusy()
    {
        isBusy = true;
    }

    private void UnsetBusy()
    {
        isBusy = false;
    }

    public bool TryHandleUnitSelection()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, unitLayer))
        {
            if (hit.transform.TryGetComponent<Unit>(out Unit unit))
            {
                SetSelectedUnit(unit);
                return true;
            }
            //else selectedUnit = null;
        }

        return false;
    }

    private void SetSelectedUnit(Unit unit)
    {
        selectedUnit = unit;
        
        OnSelectedUnitChanged?.Invoke(this, EventArgs.Empty);
    }

    public Unit GetSelectedUnit()
    {
        return selectedUnit;
    }
}
