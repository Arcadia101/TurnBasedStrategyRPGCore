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
        if (HandleTargetOrUnitCycling())
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

    // 1. EL ORQUESTADOR: Este método debe ir en tu Update()
    private bool HandleTargetOrUnitCycling()
    {
        int cycleDirection = 0;

        // Leemos el input una sola vez aquí arriba
        if (InputManager.Instance.WasCycleRightPressed()) cycleDirection = 1;
        else if (InputManager.Instance.WasCycleLeftPressed()) cycleDirection = -1;

        if (cycleDirection == 0) return false; // Si no se presionó nada, no hacemos nada

        // Evaluamos el contexto: ¿Estamos apuntando a algo o ciclando unidades?
        if (selectedAction != null && selectedAction.IsAwaitingTargetSelection())
        {
            // Pasamos el control a la acción actual
            selectedAction.CycleTarget(cycleDirection);
            return true;
        }
        else
        {
            // Usamos tu método adaptado pasándole la dirección
            return CycleFriendlyUnitsGlobal(cycleDirection);
        }

        return false;
    }

    // 2. MÉTODO ADAPTADO: Ahora recibe la dirección (+1 o -1)
    private bool CycleFriendlyUnitsGlobal(int direction)
    {
        List<Unit> friendlyUnits = UnitManager.Instance.GetFriendlyUnitList();
        if (friendlyUnits.Count == 0) return false;

        int currentIndex = friendlyUnits.IndexOf(selectedUnit);

        // MÁGIA MATEMÁTICA: 
        // Al usar 'direction' (+1 o -1) podemos hacerlo en una sola línea.
        // Sumamos friendlyUnits.Count antes de hacer el módulo (%) para evitar 
        // que C# nos devuelva índices negativos cuando direction es -1.
        currentIndex = (currentIndex + direction + friendlyUnits.Count) % friendlyUnits.Count;

        Unit nextUnit = friendlyUnits[currentIndex];
        SetSelectedUnit(nextUnit);

        // Hacemos que el puntero salte hacia la unidad que acabamos de seleccionar
        if (GridPointer.Instance != null)
        {
            GridPointer.Instance.SnapToGridPosition(nextUnit.GetGridPosition());
        }

        return true;
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
        GridPosition targetGridPosition;

        // 1. Obtener la casilla seleccionada dependiendo del control activo.
        // Necesitarás crear un método en tu InputManager que te diga si usas ratón.
        if (InputManager.Instance.IsUsingMouse()) 
        {
            // Usa el script MouseWorld que ya tienes para obtener el punto 3D y convertirlo a GridPosition
            targetGridPosition = LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition());
        }
        else 
        {
            targetGridPosition = GridPointer.Instance.GetGridPosition();
        }

        // 2. Buscar unidades en esa casilla
        List<Unit> unitsOnTile = LevelGrid.Instance.GetUnitListAtGridPosition(targetGridPosition);

        // Si no hay unidades en la casilla, devolvemos false para que el UnitActionSystem 
        // sepa que puede intentar ejecutar una acción o deseleccionar.
        if (unitsOnTile == null || unitsOnTile.Count == 0)
        {
            return false; 
        }

        // 3. Lógica de selección y ciclado
        // Filtramos solo las unidades amigas
        List<Unit> friendlyUnits = new List<Unit>();
        foreach (Unit unit in unitsOnTile)
        {
            if (!unit.IsEnemy())
            {
                friendlyUnits.Add(unit);
            }
        }

        if (friendlyUnits.Count > 0)
        {
            // Si la unidad que ya tenemos seleccionada está en esta casilla, pasamos a la siguiente
            if (selectedUnit != null && friendlyUnits.Contains(selectedUnit))
            {
                int currentIndex = friendlyUnits.IndexOf(selectedUnit);
                int nextIndex = (currentIndex + 1) % friendlyUnits.Count; // Vuelve al inicio si llega al final
                SetSelectedUnit(friendlyUnits[nextIndex]);
            }
            else
            {
                // Si es una casilla nueva o no teníamos nada seleccionado, elegimos la primera
                SetSelectedUnit(friendlyUnits[0]);
            }
            return true;
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
