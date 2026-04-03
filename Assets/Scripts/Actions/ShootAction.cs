using System;
using System.Collections.Generic;
using UnityEngine;

public class ShootAction : BaseAction
{
    public event EventHandler<OnShootEventArgs> OnShoot;

    public class OnShootEventArgs : EventArgs
    {
        public Unit targetUnit;
        public Unit shootingUnit;
    }
    private enum State
    {
        Aiming,
        Shooting,
        Cooloff,
    }
    
    private State state = State.Aiming;
    private float stateTimer;
    private Unit targetUnit;
    private bool canShoot;

    [SerializeField] private int maxShootDistance = 7;
    [SerializeField] private float aimingTime = 1.5f;
    [SerializeField] private float shootTime = .1f;
    [SerializeField] private float cooloffTime = .5f;
    
    [SerializeField] private float rotateSpeed = 10f;
    
    

    private void Update()
    {
        if (!isActive) return;
        
        stateTimer -= Time.deltaTime;
        
        switch (state)
        {
            case State.Aiming:
                Vector3 aimDir = (targetUnit.GetWorldPosition() - unit.GetWorldPosition()).normalized;
                transform.forward = Vector3.Lerp(transform.forward, aimDir, Time.deltaTime * rotateSpeed);
                break;
            case State.Shooting:
                if (canShoot)
                {
                    Shoot();
                    canShoot = false;
                }
                break;
            case State.Cooloff:
                
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
            case State.Aiming:
                state = State.Shooting;
                stateTimer = shootTime;
                break;
            case State.Shooting:
                state = State.Cooloff;
                stateTimer = cooloffTime;
                break;
            case State.Cooloff:
                ActionComplete();
                break;
        }

        Debug.Log(state);
    }

    private void Shoot()
    {
        OnShoot?.Invoke(this, new OnShootEventArgs{targetUnit = targetUnit,shootingUnit = unit});
        targetUnit.Damage(40);
    }
    
    public override string GetActionName()
    {
        return "Shoot";
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
        
        Debug.Log("Aiming");
        state = State.Aiming;
        stateTimer = aimingTime;

        canShoot = true;
        
        ActionStart(onActionComplete);
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        GridPosition unitGridPosition = unit.GetGridPosition();
        for (int x = -maxShootDistance; x <= maxShootDistance; x++)
        {
            for (int z = -maxShootDistance; z <= maxShootDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                {
                    continue;
                }
                
                int testDistance = Math.Abs(x) + Math.Abs(z);
                if (testDistance > maxShootDistance)
                {
                    continue;
                }
                
                //Show all possible grid positions (radial method).
                //validGridPositionList.Add(testGridPosition);
                
                if (!LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition))
                {
                    //Grid position is empty, no Unit.
                    continue;
                }
                
                Unit targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(testGridPosition);

                if (targetUnit.IsEnemy() == unit.IsEnemy())
                {
                    //The target is on the same team.
                    continue;
                }

                //to do: Replace this to new function to update color of visual.
                validGridPositionList.Add(testGridPosition);
            }
        }
        return validGridPositionList;
    }
    
    public Unit GetTargetUnit()
    {
        return targetUnit;
    }
}
