using System;
using UnityEngine;


public class Unit : MonoBehaviour
{
    [SerializeField] private int maxActionPoints;
    [SerializeField] private int maxMovePoints;
    [SerializeField] private bool isEnemy;
    
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
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);
        
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
            GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
            if (newGridPosition != gridPosition)
            {
                GridPosition oldGridPosition = gridPosition;
                gridPosition = newGridPosition;
                LevelGrid.Instance.UnitMovedGridPosition(this, oldGridPosition, newGridPosition);
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
        // Obtenemos la posición de formación ideal desde el LevelGrid
        Vector3 targetPosition = LevelGrid.Instance.GetUnitWorldPosition(this);
        
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
        LevelGrid.Instance.RemoveUnitAtGridPosition(gridPosition, this);
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
    
    public float GetHealthNormalized()
    {
        return healthSystem.GetHealthNormalized();
    }
    
}
