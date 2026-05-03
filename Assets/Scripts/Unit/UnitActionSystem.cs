using System;
using System.Collections.Generic;
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

        // 1. Probamos a cambiar de unidad con los botones (L1/R1)
        if (HandleUnitCycling())
        {
            return;
        }

        // 2. Probamos a seleccionar una unidad nueva
        if (TryHandleUnitSelection())
        {
            return;
        }
            
        // 3. Ejecutamos la acción seleccionada
        HandleSelectedAction();
        
        // 4. Si presionamos confirmar y no se ejecutó ni seleccionó nada, deseleccionamos
        if (InputManager.Instance.WasConfirmPressedThisFrame())
        {
            if (selectedUnit != null)
            {
                SetSelectedUnit(null);
            }
        }
    }

    private bool HandleUnitCycling()
    {
        bool cycleLeft = InputManager.Instance.WasCycleLeftPressed();
        bool cycleRight = InputManager.Instance.WasCycleRightPressed();

        if (cycleLeft || cycleRight)
        {
            List<Unit> friendlyUnits = UnitManager.Instance.GetFriendlyUnitList();
            if (friendlyUnits.Count == 0) return false;

            int currentIndex = friendlyUnits.IndexOf(selectedUnit);

            if (cycleRight)
            {
                currentIndex = (currentIndex + 1) % friendlyUnits.Count;
            }
            else if (cycleLeft)
            {
                currentIndex = (currentIndex - 1 + friendlyUnits.Count) % friendlyUnits.Count;
            }

            Unit nextUnit = friendlyUnits[currentIndex];
            SetSelectedUnit(nextUnit);

            // Hacemos que el puntero salte hacia la unidad que acabamos de seleccionar
            if (GridPointer.Instance != null)
            {
                GridPointer.Instance.SnapToGridPosition(nextUnit.GetGridPosition());
            }

            return true;
        }

        return false;
    }

    private void HandleSelectedAction()
    {
        if (InputManager.Instance.WasConfirmPressedThisFrame())
        {
            // Ahora la posición objetivo es la que marca nuestro GridPointer
            GridPosition pointerGridPosition = GridPointer.Instance.GetGridPosition();
            
            if (selectedUnit != null && selectedAction != null && selectedAction.IsValidActionGridPosition(pointerGridPosition))
            {
                if (selectedUnit.TrySpendActionPointsOrMovePointsToTakeActionOrMove(selectedAction))
                {
                    SetBusy();
                    selectedAction.TakeAction(pointerGridPosition, UnsetBusy);
                    
                    OnActionStarted?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }


    public bool TryHandleUnitSelection()
    {
        if (InputManager.Instance.WasConfirmPressedThisFrame())
        {
            // --- MÉTODO 1: Por Raycast (Click del ratón sobre el modelo 3D) ---
            Ray ray = Camera.main.ScreenPointToRay(InputManager.Instance.GetMouseScreenPosition());
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, unitLayer))
            {
                if (hit.transform.TryGetComponent<Unit>(out Unit unit))
                {
                    if (unit == selectedUnit) return false;
                    if (unit.IsEnemy()) return false;
                    
                    SetSelectedUnit(unit);
                    return true;
                }
            }
            
            // --- MÉTODO 2: Por GridPointer (Mando / Confirmar sobre la casilla) ---
            // Si el Raycast falló, intentamos seleccionar la unidad que esté en la casilla del GridPointer
            GridPosition pointerGridPosition = GridPointer.Instance.GetGridPosition();
            List<Unit> unitsOnTile = LevelGrid.Instance.GetUnitListAtGridPosition(pointerGridPosition);
            
            foreach(Unit unit in unitsOnTile)
            {
                if (!unit.IsEnemy() && unit != selectedUnit)
                {
                    SetSelectedUnit(unit);
                    return true;
                }
            }
        }

        return false;
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

    private void SetSelectedUnit(Unit unit)
    {
        selectedUnit = unit;
        
        SetSelectedAction(null);

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
