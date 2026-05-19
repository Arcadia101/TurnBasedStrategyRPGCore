using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{
    public static event EventHandler OnAnyActionStarted;
    public static event EventHandler OnAnyActionCompleted;
    
    protected Unit unit;
    protected bool isActive;
    protected Action onActionComplete;
    [SerializeField] protected int ActionPointsCost = 1;
    
    
    protected virtual void Awake()
    {
        unit = GetComponent<Unit>();
    }
    
    public abstract string GetActionName();
    
    public abstract void TakeAction(GridPosition gridPosition, Action onActionComplete);
    
    public virtual bool IsValidActionGridPosition(GridPosition gridPosition)
    {
        List<GridPosition> validGridPositionList = GetValidActionGridPositionList();
        return validGridPositionList.Contains(gridPosition);
    }

    public abstract List<GridPosition> GetValidActionGridPositionList();

    public virtual int GetActionPointsCost()
    {
        return ActionPointsCost;
    }

    protected void ActionStart(Action onActionComplete)
    {
        isActive = true;
        this.onActionComplete = onActionComplete;
        
        OnAnyActionStarted?.Invoke(this, EventArgs.Empty);
    }
    
    protected void ActionComplete()
    {
        isActive = false;
        onActionComplete();
        
        OnAnyActionCompleted?.Invoke(this, EventArgs.Empty);
    }

    public Unit GetUnit()
    {
        return unit;
    }

    public EnemyAIAction GetBestEnemyAIAction()
    {
        List<EnemyAIAction> enemyAIActionList = new List<EnemyAIAction>();
        List<GridPosition> validActionGridPositionList = GetValidActionGridPositionList();

        foreach (GridPosition gridPosition in validActionGridPositionList)
        {
            EnemyAIAction enemyAIAction = GetEnemyAIAction(gridPosition);
            enemyAIActionList.Add(enemyAIAction);
        }

        if (enemyAIActionList.Count > 0)
        {
            enemyAIActionList.Sort((EnemyAIAction a, EnemyAIAction b) => b.actionValue - a.actionValue);
            return enemyAIActionList[0];
        }
        else
        {
            //No possible Enemy AI Actions.
            return null;
        }
    }

    public abstract EnemyAIAction GetEnemyAIAction(GridPosition gridPosition);

    public bool IsActionActive()
    {
        return isActive;
    }
    
    // Nos dice si esta acción está actualmente esperando que el jugador elija un objetivo
    public virtual bool IsAwaitingTargetSelection() 
    {
        return false; // Por defecto es falso para acciones como MoveAction o SpinAction
    }

    // Recibe la dirección del ciclado (1 para derecha/siguiente, -1 para izquierda/anterior)
    public virtual void CycleTarget(int direction) 
    {
        // Por defecto no hace nada
    }
}
