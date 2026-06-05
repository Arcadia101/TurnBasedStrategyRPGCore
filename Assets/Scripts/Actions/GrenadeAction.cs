using System;
using System.Collections.Generic;
using UnityEngine;

public class GrenadeAction : BaseAction
{
    [SerializeField] private Transform grenadePrefab;
    
    [SerializeField] private LayerMask obstaclesLayerMask;
    [SerializeField] private int maxthrowDistance = 7;
    
    void Update()
    {
        if (!isActive)
        {
            return;
        }
        
    }

    private void OnGrenadeBehaviourComplete()
    {
        ActionComplete();
    }

    public override string GetActionName()
    {
        return "Granade";
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        //Debug.Log("GrenadeAction");
        Transform grenade = Instantiate(grenadePrefab, unit.GetWorldPosition(), Quaternion.identity);
        GrenadeProjectile grenadeProjectile = grenade.GetComponent<GrenadeProjectile>();
        grenadeProjectile.Setup(gridPosition, OnGrenadeBehaviourComplete);
        
        ActionStart(onActionComplete);
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        
        GridPosition unitGridPosition = unit.GetGridPosition();
        
        for (int x = -maxthrowDistance; x <= maxthrowDistance; x++)
        {
            for (int z = -maxthrowDistance; z <= maxthrowDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!GetGridContext().IsValidGridPosition(testGridPosition))
                {
                    continue;
                }
                
                int testDistance = Math.Abs(x) + Math.Abs(z);
                if (testDistance > maxthrowDistance)
                {
                    continue;
                }
                
                //Show all possible grid positions (radial method).
                //validGridPositionList.Add(testGridPosition);
                
                /*
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

                Vector3 UnitWorldPosition = GetGridContext().GetWorldPosition(unitGridPosition);
                Vector3 shootDir = (targetUnit.GetWorldPosition() - UnitWorldPosition).normalized;
                float unitShoulderHeight = 1.7f;
                if (Physics.Raycast(UnitWorldPosition + Vector3.up * unitShoulderHeight, shootDir,
                        Vector3.Distance(UnitWorldPosition, targetUnit.GetWorldPosition()), obstaclesLayerMask))
                {
                    //Blocked by obstacle.
                    continue;
                }

                */
                //to do: Replace this to new function to update color of visual.
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
            actionValue = 0,
        };
    }
}
