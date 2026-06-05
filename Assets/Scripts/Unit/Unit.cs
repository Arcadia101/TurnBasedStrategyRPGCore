using System;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private int maxActionPoints;
    [SerializeField] private int maxMovePoints;
    [SerializeField] private bool isEnemy;
    
    [Header("Grid Customization")]
    [SerializeField] private GridType currentGridType = GridType.Normal;
    private LevelGrid gridContext;
    
    private GridPosition gridPosition;
    private HealthSystem healthSystem;
    private BaseAction[] baseActionArray;
    private int actionPoints;
    private int movePoints;

    public static event EventHandler OnAnyActionPointsChanged;
    public static event EventHandler OnAnyUnitSpawned;
    public static event EventHandler OnAnyUnitDead;
    

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        baseActionArray = GetComponents<BaseAction>();
        
        actionPoints = maxActionPoints;
        movePoints = maxMovePoints;
    }


    private void Start()
    {
        // 1. Primero determinamos el contexto de forma dinámica (El método que diseñamos antes)
        DetermineGridContext();

        // 2. Ahora que 'myGridContext' ya está asignado de forma segura, extraemos la posición lógica inicial
        // CORRECCIÓN POLIMÓRFICA: Usamos gridContext. El registro en la lista ya se hace dentro de DetermineGridContext.
        gridPosition = gridContext.GetGridPosition(transform.position);

        // 3. Continuamos con el resto de tus inicializaciones y eventos intactos
        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
        healthSystem.OnDead += HealthSystem_OnDead;

        OnAnyUnitSpawned?.Invoke(this, EventArgs.Empty);
    }

    private void Update()
    {
        MoveAction moveAction = GetAction<MoveAction>();
        bool isMoving = moveAction != null && moveAction.IsActionActive();

        // Solo permitimos que la unidad cambie de casilla LÓGICA si está ejecutando
        // activamente una acción de movimiento. 
        if (isMoving)
        {
            // CORRECCIÓN: Cambiado 'LevelGrid.Instance' por 'gridContext' para que verifique su propio mapa
            GridPosition newGridPosition = gridContext.GetGridPosition(transform.position);
            if (newGridPosition != gridPosition)
            {
                GridPosition oldGridPosition = gridPosition;
                gridPosition = newGridPosition;
                // CORRECCIÓN: Notificamos el movimiento al contexto polimórfico correspondiente
                gridContext.UnitMovedGridPosition(this, oldGridPosition, newGridPosition);
            }
        }
        else
        {
            // Si está quieta, aplicamos el offset visual sin afectar su casilla lógica
            HandleFormationMovement();
        }
    }

    private void HandleFormationMovement()
    {
        // CORRECCIÓN: Obtenemos la posición de formación ideal desde nuestro 'gridContext' dinámico
        Vector3 targetPosition = gridContext.GetUnitWorldPosition(this);
        
        float stopDistance = 0.05f;
        if (Vector3.Distance(transform.position, targetPosition) > stopDistance)
        {
            Vector3 moveDir = (targetPosition - transform.position).normalized;
            float moveSpeed = 4f;
            transform.position += moveDir * (moveSpeed * Time.deltaTime);

            // Rotación suave hacia adelante o hacia el centro de la formación (opcional)
            float rotateSpeed = 10f;
            transform.forward = Vector3.Lerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
        }
    }

    public T GetAction<T>() where T : BaseAction
    {
        foreach (BaseAction baseAction in baseActionArray)
        {
            if (baseAction is T)
            {
                return (T)baseAction;
            }
        }
        return null;
    }

    public GridPosition GetGridPosition()
    {
        return gridPosition;
    }

    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }
    
    public BaseAction[] GetBaseActionArray()
    {
        return baseActionArray;
    }

    public bool CanSpendActionPointsToTakeAction(BaseAction baseAction)
    {
        if (actionPoints >= baseAction.GetActionPointsCost())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool TrySpendActionPointsOrMovePointsToTakeActionOrMove(BaseAction baseAction)
    {
        if (baseAction is MoveAction)
        {
            if (CanSpendMovePointsToMove(baseAction))
            {
                SpendMovePoints(baseAction.GetActionPointsCost());
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (CanSpendActionPointsToTakeAction(baseAction))
            {
                SpendActionPoints(baseAction.GetActionPointsCost());
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool CanSpendMovePointsToMove(BaseAction baseAction)
    {
        if (movePoints >= baseAction.GetActionPointsCost())
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    private void SpendActionPoints(int amount)
    {
        actionPoints -= amount;
        
        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
    }
    private void SpendMovePoints(int amount)
    {
        movePoints -= amount;
        
        OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetActionPoints()
    {
        return actionPoints;
    }
    
    public int GetMovePoints()
    {
        return movePoints;
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        if ((IsEnemy() && !TurnSystem.Instance.IsPlayerTurn()) || (!IsEnemy() && TurnSystem.Instance.IsPlayerTurn()))
        {
            actionPoints = maxActionPoints;
            movePoints = maxMovePoints;
        
            OnAnyActionPointsChanged?.Invoke(this, EventArgs.Empty);
        }
        
    }

    private void HealthSystem_OnDead(object sender, EventArgs e)
    {
        // CORRECCIÓN: Removemos la unidad usando el contexto polimórfico dinámico
        gridContext.RemoveUnitAtGridPosition(gridPosition, this);
        Destroy(gameObject);
        
        OnAnyUnitDead?.Invoke(this, EventArgs.Empty);
    }
    public bool IsEnemy()
    {
        return isEnemy;
    }

    public void Damage(int damageAmount)
    {
        healthSystem.Damage(damageAmount);
    }

    public void Heal(int healAmount)
    {
        healthSystem.Heal(healAmount);
    }
    
    public float GetHealthNormalized()
    {
        return healthSystem.GetHealthNormalized();
    }
    
    // Métodos públicos para que las acciones puedan consultar o cambiar el estado
    public GridType GetGridType() => currentGridType;
    public void SetGridType(GridType newGridType) => currentGridType = newGridType;
    
    private void DetermineGridContext()
    {
        // --- 1. VALIDACIÓN PARA EL PLANO TOROIDAL ---
        if (ToroidLevelGrid.ToroidInstance != null)
        {
            // Calculamos el tamaño físico real en metros del mapa toroidal
            float gridWidthMetres = ToroidLevelGrid.ToroidInstance.GetWidth() * ToroidLevelGrid.ToroidInstance.GetCellSize();
            float gridHeightMetres = ToroidLevelGrid.ToroidInstance.GetHeight() * ToroidLevelGrid.ToroidInstance.GetCellSize();

            // Obtenemos el origen físico de donde colocaste el objeto ToroidLevelGrid en la escena
            Vector3 toroidOrigin = ToroidLevelGrid.ToroidInstance.transform.position;

            // Creamos una caja virtual (Bounds) que representa el terreno exacto del Toroide
            Bounds toroidBounds = new Bounds(
            toroidOrigin + new Vector3(gridWidthMetres / 2f, 0f, gridHeightMetres / 2f), 
            new Vector3(gridWidthMetres, 10f, gridHeightMetres) // 10 metros de altura de margen
            );

            // Si la posición física de la unidad está DENTRO de la caja del Toroide, se queda aquí
            if (toroidBounds.Contains(transform.position))
            {
                gridContext = ToroidLevelGrid.ToroidInstance;
                currentGridType = GridType.Toroid;

                // Calculamos su posición usando el sistema toroidal
                GridPosition localPos = gridContext.GetGridPosition(transform.position);
                gridContext.AddUnitAtGridPosition(localPos, this);
            
                Debug.Log($"[CEREBRO-GRID] {name} se registró con éxito en la Grid TOROIDAL.");
                return;
            }
        }

        // --- 2. VALIDACIÓN PARA EL PLANO NORMAL (Por descarte o vecindad) ---
        if (LevelGrid.Instance != null)
        {
            gridContext = LevelGrid.Instance;
            currentGridType = GridType.Normal;

            GridPosition localPos = gridContext.GetGridPosition(transform.position);
            gridContext.AddUnitAtGridPosition(localPos, this);
        
            Debug.Log($"[CEREBRO-GRID] {name} se registró con éxito en la Grid NORMAL.");
        }
    }

    // Expón el contexto para que tus acciones lo consuman de forma dinámica
    public LevelGrid GetGridContext() => gridContext;
    
}

public enum GridType
{
    Normal,
    Toroid
}
