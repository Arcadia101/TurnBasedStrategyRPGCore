using System;
using System.Collections.Generic;
using UnityEngine;

public class SpinAction : BaseAction
{
    private float totalSpinAmount;

    private void Update()
    {
        if (!isActive) return;
        
        float speedAddAmount = 360 * Time.deltaTime;
        transform.eulerAngles += new Vector3(0, speedAddAmount, 0);
        
        totalSpinAmount += speedAddAmount;
        if (totalSpinAmount >= 360)
        {
            isActive = false;
            onActionComplete();
        }
        
    }

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        this.onActionComplete = onActionComplete;
        totalSpinAmount = 0;
        isActive = true;
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        GridPosition unitGridPosition = unit.GetGridPosition();
        
        return new List<GridPosition> {unitGridPosition};
    }

    public override string GetActionName()
    {
        return "Spin";
    }
    
    public override int GetActionPointsCost()
    {
        return ActionPointsCost;
    }
}
