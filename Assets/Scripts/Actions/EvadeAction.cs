using System;
using System.Collections.Generic;
using UnityEngine;

public class EvadeAction : BaseAction
{
    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        ActionStart(onActionComplete);

        CameraManager.Instance.SwitchToMentalPlane(() =>
        {
            ActionComplete();
        });
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        if (CombatContextValidator.CanEnterToroidGrid(unit, UnitManager.Instance.GetUnitList()))
        {
            validGridPositionList.Add(unit.GetGridPosition());
        }

        return validGridPositionList;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 0
        };
    }

    public override string GetActionName() => "Evade";
}