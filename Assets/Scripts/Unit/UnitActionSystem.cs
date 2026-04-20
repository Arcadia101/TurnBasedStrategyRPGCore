using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class UnitActionSystem : MonoBehaviour
{
    public static UnitActionSystem Instance { get; private set; }
    
    public event EventHandler OnSelectedUnitChanged;
    public event EventHandler OnSelectedActionChanged;
    public event EventHandler<bool> OnBusyChanged;
    public event EventHandler OnActionStarted;
    
    [SerializeField] private Unit selectedUnit;
    [SerializeField] private LayerMask unitLayer;
    
    private BaseAction selectedAction;
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


    private void Start()
    {
        SetSelectedUnit(selectedUnit);
    }

    private void Update()
    {
        if (isBusy)
        {
            return;
        }

        if (!TurnSystem.Instance.IsPlayerTurn())
        {
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (TryHandleUnitSelection())
        {
            return;
        }
            
        
        HandleSelectedAction();
        
        if (InputManager.Instance.WasConfirmPressedThisFrame())
        {
            if (selectedUnit != null)
            {
                SetSelectedUnit(null);
            }
        }
    }

    private void HandleSelectedAction()
    {
        if (InputManager.Instance.WasConfirmPressedThisFrame())
        {
            GridPosition mouseGridPosition = LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition());
            if (selectedUnit != null && selectedAction.IsValidActionGridPosition(mouseGridPosition))
            {
                if (selectedUnit.TrySpendActionPointsOrMovePointsToTakeActionOrMove(selectedAction))
                {
                    SetBusy();
                    selectedAction.TakeAction(mouseGridPosition, UnsetBusy);
                    
                    OnActionStarted?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    private void SetBusy()
    {
        isBusy = true;
        
        OnBusyChanged?.Invoke(this, isBusy);
    }

    private void UnsetBusy()
    {
        isBusy = false;
        
        OnBusyChanged?.Invoke(this, isBusy);
    }

    public bool TryHandleUnitSelection()
    {
        if (InputManager.Instance.WasConfirmPressedThisFrame())
        {
            Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition());
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, unitLayer))
            {
                if (hit.transform.TryGetComponent<Unit>(out Unit unit))
                {
                    if (unit == selectedUnit)
                    {
                        //Same Unit selected.
                        return false;
                    }

                    if (unit.IsEnemy())
                    {
                        //Click on enemy unit.
                        return false;
                    }
                    SetSelectedUnit(unit);
                    return true;
                }
                //else SetSelectedUnit(null);
            }
        }

        return false;
    }

    private void SetSelectedUnit(Unit unit)
    {
        selectedUnit = unit;
        SetSelectedAction(unit.GetAction<MoveAction>());
        OnSelectedUnitChanged?.Invoke(this, EventArgs.Empty);
    }

    public Unit GetSelectedUnit()
    {
        return selectedUnit;
    }
    
    public void SetSelectedAction(BaseAction action)
    {
        selectedAction = action;
        OnSelectedActionChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public BaseAction GetSelectedAction()
    {
        return selectedAction;
    }
}
