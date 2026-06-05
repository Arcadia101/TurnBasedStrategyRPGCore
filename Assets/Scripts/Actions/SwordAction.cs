using System;
using System.Collections.Generic;
using UnityEngine;

public class SwordAction : BaseAction
{
    public static event EventHandler OnAnySwordHit;
    public event EventHandler OnSwordActionStarted;
    public event EventHandler OnSwordActionCompleted;
    
    private enum State
    {
        SwingingSwordBeforeHit,
        SwingingSwordAfterHit,
    }
    
    [SerializeField] private int maxSwordDistance = 1;
    [SerializeField] private float afterHitStateTime = .5f;
    [SerializeField] private float beforeHitStateTime = .7f;
    [SerializeField] private float rotateSpeed = 10f;
    

    private State state;
    private float stateTimer;
    private Unit targetUnit;


    private void Update()
    {
        if (!isActive)
        {
            return;
        }
        
        stateTimer -= Time.deltaTime;
        
        switch (state)
        {
            case State.SwingingSwordBeforeHit:
                Vector3 aimDir = (targetUnit.GetWorldPosition() - unit.GetWorldPosition()).normalized;
                transform.forward = Vector3.Lerp(transform.forward, aimDir, Time.deltaTime * rotateSpeed);
                break;
            case State.SwingingSwordAfterHit:
                break;
        }
        
        if (stateTimer <= 0)
        {
            NextState();
        }
    }
    
    private void NextState()
    {
        switch (state)
        {
            case State.SwingingSwordBeforeHit:
                state = State.SwingingSwordAfterHit;
                stateTimer = afterHitStateTime;
                targetUnit.Damage(100);
                OnAnySwordHit?.Invoke(this, EventArgs.Empty);
                break;
            
            case State.SwingingSwordAfterHit:
                OnSwordActionCompleted?.Invoke(this, EventArgs.Empty);
                ActionComplete();
                break;
        }

        Debug.Log(state);
    }

    public int GetMaxSwordDistance()
    {
        return maxSwordDistance;   
    }
    
    public override string GetActionName()
    {
        return "Sword";
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        targetUnit = GetGridContext().GetUnitAtGridPosition(gridPosition);
        state = State.SwingingSwordBeforeHit;
        stateTimer = beforeHitStateTime;
        OnSwordActionStarted?.Invoke(this, EventArgs.Empty);
        ActionStart(onActionComplete);
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        
        GridPosition unitGridPosition = unit.GetGridPosition();
        
        for (int x = -maxSwordDistance; x <= maxSwordDistance; x++)
        {
            for (int z = -maxSwordDistance; z <= maxSwordDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!GetGridContext().IsValidGridPosition(testGridPosition))
                {
                    continue;
                }
                
                if (!GetGridContext().HasAnyUnitOnGridPosition(testGridPosition))
                {
                    //Grid position is empty, no Unit.
                    continue;
                }
                
                Unit targetUnit = GetGridContext().GetUnitAtGridPosition(testGridPosition);

                if (targetUnit.IsEnemy() == unit.IsEnemy())
                {
                    //The target is on the same team.
                    continue;
                }
                
                validGridPositionList.Add(testGridPosition);
            }
        }
        return validGridPositionList;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction()
        {
            gridPosition = gridPosition,
            actionValue = 200,
        };
    }
}
