using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractAction : BaseAction
{
    [SerializeField] private int maxInteractDistance = 1;

    
    private void Update()
    {
        if (isActive)
        {
            return;
        }
    }

    public override string GetActionName()
    {
        return "Interact";
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        IInteractable interactable = GetGridContext().GetInteractableAtGridPosition(gridPosition);
        interactable.Interact(OnInteractComplete);
        ActionStart(onActionComplete);
    }

    private void OnInteractComplete()
    {
        ActionComplete();
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        
        GridPosition unitGridPosition = unit.GetGridPosition();
        
        for (int x = -maxInteractDistance; x <= maxInteractDistance; x++)
        {
            for (int z = -maxInteractDistance; z <= maxInteractDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!GetGridContext().IsValidGridPosition(testGridPosition))
                {
                    continue;
                }

                IInteractable interactable = GetGridContext().GetInteractableAtGridPosition(testGridPosition);
                
                if (interactable == null)
                {
                    //Grid position is empty, no Interactable.
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
            actionValue = 0,
        };
    }
}
