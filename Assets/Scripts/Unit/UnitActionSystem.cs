using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitActionSystem : MonoBehaviour
{
    public static UnitActionSystem Instance { get; private set; }

    public event EventHandler OnSelectedUnitChanged;
    public event EventHandler OnSelectedActionChanged;
    public event EventHandler<bool> OnBusyChanged;
    public event EventHandler OnActionStarted;
    public event Action<LevelGrid> OnActiveGridChanged;

    [SerializeField] private Unit selectedUnit;
    [SerializeField] private LayerMask unitLayer;

    private BaseAction selectedAction;
    private int currentActionIndex = 0; // Registra el índice de la acción seleccionada con el mando
    private bool isBusy;

    [Header("Grid Context Warp")]
    private LevelGrid currentActiveGrid; // El mapa que el jugador está operando actualmente

    public LevelGrid GetCurrentActiveGrid() => currentActiveGrid;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There is already a UnitActionSystem in the scene!");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Al arrancar el juego, por defecto el jugador opera en la Grid Normal
        currentActiveGrid = LevelGrid.Instance;

        SetSelectedUnit(selectedUnit);

        // Suscripción al cambio de plano de la cámara
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.OnPlaneChanged += HandlePlaneChanged;
        }
    }

    private void Update()
    {
        // candado del Turno del Jugador e Interfaz de Usuario (UI)
        if (!TurnSystem.Instance.IsPlayerTurn()) return;
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // --- NUEVO CANDADO INTELIGENTE (SEMI-BUSY) ---
        if (isBusy)
        {
            // Si el sistema está ocupado, revisamos si la acción actual está esperando selección de blanco (Fase de Aiming)
            if (selectedAction != null && selectedAction.IsAwaitingTargetSelection())
            {
                // ¡EXCEPCIÓN VIP! El juego está Busy, pero permitimos EXCLUSIVAMENTE el ciclado de objetivos
                // y la segunda confirmación para disparar.
                if (HandleTargetOrUnitCycling()) return;
                if (TryHandleSelectedAction()) return;
            }

            // Si está Busy y la acción NO está esperando objetivo (ej: la bala ya va volando o el personaje se está moviendo),
            // bloqueamos por completo el input como siempre.
            return;
        }

        // --- FLUJO LIBRE DEL JUEGO (CUANDO ISBUSY ES FALSO) ---

        // 1. Ciclar entre las acciones disponibles de la unidad (Arriba/Abajo)
        if (HandleActionCycling()) return;

        // 2. Ciclar unidades aliadas de forma global (L1/R1 fuera de combate)
        if (HandleTargetOrUnitCycling()) return;

        // 3. Confirmar casilla táctica / Iniciar acción (Primer clic / Botón A)
        if (TryHandleSelectedAction()) return;

        // 4. Seleccionar una unidad directamente en la grid
        if (TryHandleUnitSelection()) return;

        // 5. Deseleccionar si confirmamos en el vacío
        if (InputManager.Instance.WasConfirmPressedThisFrame() && selectedUnit != null)
        {
            SetSelectedUnit(null);
        }
    }

    private void HandlePlaneChanged(GridType targetPlane)
    {
        LevelGrid targetGrid = (targetPlane == GridType.Normal) ? LevelGrid.Instance : ToroidLevelGrid.Instance;
        SetCurrentActiveGrid(targetGrid);
    }

    public void SetCurrentActiveGrid(LevelGrid newGrid)
    {
        if (currentActiveGrid == newGrid || newGrid == null) return;

        currentActiveGrid = newGrid;

        // Notificamos al puntero y a cualquier otro listener (UI, selección, etc.)
        OnActiveGridChanged?.Invoke(currentActiveGrid);

        // Deseleccionar unidad
        SetSelectedUnit(null);
    }

    private bool HandleActionCycling()
    {
        int direction = 0;

        // Evaluamos tus nuevos métodos del InputManager para el mando/teclado
        if (InputManager.Instance.WasCycleUpPressed()) direction = -1;    // Dirección hacia arriba en la barra
        if (InputManager.Instance.WasCycleDownPressed()) direction = 1;   // Dirección hacia abajo en la barra

        // Si no se presionó ningún botón de ciclo, salimos de inmediato
        if (direction == 0) return false;

        // Si no hay unidad seleccionada, no podemos ciclar acciones
        if (selectedUnit == null) return false;

        // Obtenemos el array de acciones que tiene la unidad seleccionada actualmente
        BaseAction[] unitActionArray = selectedUnit.GetBaseActionArray();

        // Si la unidad por alguna razón no tiene acciones (seguridad), salimos
        if (unitActionArray == null || unitActionArray.Length == 0) return false;

        // Calculamos el nuevo índice usando la magia matemática del ciclo infinito
        currentActionIndex = (currentActionIndex + direction + unitActionArray.Length) % unitActionArray.Length;

        // Hacemos el cambio oficial de la acción activa en el sistema
        BaseAction nextAction = unitActionArray[currentActionIndex];
        SetSelectedAction(nextAction);

        // Retornamos true porque el input fue consumido con éxito
        return true;
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
        // Si la acción activa dice que necesita elegir un objetivo, le cedemos el control
        if (selectedAction != null && selectedAction.IsAwaitingTargetSelection())
        {
            // Pasamos el control a la acción actual
            selectedAction.CycleTarget(cycleDirection);
            return true; // El input fue consumido
        }

        // Si no, ciclado global de unidades
        return CycleFriendlyUnitsGlobal(cycleDirection);
    }

    // 2. MÉTODO ADAPTADO: Ahora recibe la dirección (+1 o -1)
    private bool CycleFriendlyUnitsGlobal(int direction)
    {
        List<Unit> friendlyUnits = UnitManager.Instance.GetFriendlyUnitList();
        if (friendlyUnits == null || friendlyUnits.Count == 0) return false;

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

    private bool TryHandleSelectedAction()
    {
        if (!InputManager.Instance.WasConfirmPressedThisFrame()) return false;

        // Obtenemos la posición lógica de la cuadrícula
        GridPosition pointerGridPosition = GridPointer.Instance.GetGridPosition();

        // =================================================================
        // MOMENTO B (PASO 4 DEL FLUJO): SEGUNDA CONFIRMACIÓN (EL DISPARO)
        // =================================================================
        // Si ya hay una acción activa y está en modo Semi-Busy esperando objetivo...
        if (selectedAction != null && selectedAction.IsAwaitingTargetSelection())
        {
            if (TryConfirmAwaitingAction(selectedAction))
            {
                return true;
            }
        }

        // =================================================================
        // MOMENTO A (PASO 1 Y 2 DEL FLUJO): PRIMERA CONFIRMACIÓN (LA CASILLA)
        // =================================================================
        // Si no estamos ejecutando nada aún (la acción no está activa físicamente)...
        if (selectedUnit != null && selectedAction != null && !selectedAction.IsActionActive())
        {
            // Validamos que la casilla seleccionada por el puntero sea elegible para la acción
            if (selectedAction.IsValidActionGridPosition(pointerGridPosition))
            {
                // Intentamos cobrar los puntos de acción/movimiento
                if (selectedUnit.TrySpendActionPointsOrMovePointsToTakeActionOrMove(selectedAction))
                {
                    SetBusy(); // Activamos el estado Busy del sistema inmediatamente

                    // Iniciamos la acción física (esto pondrá a ShootAction en State.Aiming)
                    selectedAction.TakeAction(pointerGridPosition, UnsetBusy);

                    OnActionStarted?.Invoke(this, EventArgs.Empty);
                    return true;
                }
            }
        }

        return false;
    }
    
    // Maneja la confirmación secundaria de acciones en fase de apuntado/espera
    private bool TryConfirmAwaitingAction(BaseAction action)
    {
        switch (action)
        {
            case ShootAction shootAction when shootAction.ConfirmSelectedTarget():
            case HealAction healAction when healAction.ConfirmSelectedTarget():
            case RotateEnergyAction rotateEnergyAction when rotateEnergyAction.ConfirmRotation():
                return true;

            // NOTA DE EXPANSION: Aquí añadir a futuro las otras acciones (ej: SwordAction, etc.)
            default:
                return false;
        }
    }

    public bool TryHandleUnitSelection()
    {
        if (!InputManager.Instance.WasConfirmPressedThisFrame()) return false;

        // 1. Obtener la casilla seleccionada dependiendo del control activo.
        GridPosition targetGridPosition = InputManager.Instance.IsUsingMouse()
            ? currentActiveGrid.GetGridPosition(MouseWorld.GetPosition())
            : GridPointer.Instance.GetGridPosition();

        // 2. Buscar unidades en esa casilla
        List<Unit> unitsOnTile = currentActiveGrid.GetUnitListAtGridPosition(targetGridPosition);

        // Si no hay unidades en la casilla, devolvemos false para que el UnitActionSystem 
        // sepa que puede intentar ejecutar una acción o deseleccionar.
        if (unitsOnTile == null || unitsOnTile.Count == 0)
        {
            return false;
        }

        // 3. Lógica de selección y ciclado
        // Filtramos solo las unidades amigas
        List<Unit> friendlyUnits = unitsOnTile.Where(unit => !unit.IsEnemy()).ToList();

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

    public Unit GetSelectedUnit() => selectedUnit;

    public void SetSelectedAction(BaseAction action)
    {
        selectedAction = action;
        OnSelectedActionChanged?.Invoke(this, EventArgs.Empty);
    }

    public BaseAction GetSelectedAction() => selectedAction;

    private void OnDestroy()
    {
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.OnPlaneChanged -= HandlePlaneChanged;
        }
    }
}