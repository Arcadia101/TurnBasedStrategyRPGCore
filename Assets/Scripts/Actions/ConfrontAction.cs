using System;
using System.Collections.Generic;
using UnityEngine;

public class ConfrontAction : BaseAction
{
    private Unit targetUnit;

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        // 1. Buscamos específicamente una unidad del bando contrario en esa casilla y en el mismo plano
        targetUnit = null;
        foreach (Unit candidate in GetGridContext().GetUnitListAtGridPosition(gridPosition))
        {
            if (candidate != null && candidate.IsEnemy() != unit.IsEnemy())
            {
                targetUnit = candidate;
                break;
            }
        }

        if (targetUnit == null)
        {
            Debug.LogWarning($"[ConfrontAction] No se encontro objetivo rival en {gridPosition}");
            onActionComplete?.Invoke();
            return;
        }

        // 2. Registramos el objetivo en targetsInTile para CameraManager
        targetsInTile.Clear();
        targetsInTile.Add(targetUnit);
        currentTargetIndex = 0;

        // 3. Disparamos ActionStart y arrancamos el duelo
        ActionStart(onActionComplete);
        CombatDuelSession.Instance.OnDuelEnded += HandleDuelEnded;
        CombatDuelSession.Instance.StartDuel(unit, targetUnit, CombatInteractionType.Confrontation);
    }
    
    private void HandleDuelEnded()
    {
        CombatDuelSession.Instance.OnDuelEnded -= HandleDuelEnded;
        ActionComplete();
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        foreach (Unit otherUnit in UnitManager.Instance.GetUnitList())
        {
            if (otherUnit == null || otherUnit == unit) continue;
            if (otherUnit.GetCurrentGridType() != unit.GetCurrentGridType()) continue;

            if (CombatContextValidator.CanConfront(unit, otherUnit))
            {
                validGridPositionList.Add(otherUnit.GetGridPosition());
            }
        }

        return validGridPositionList;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        Unit target = GetGridContext().GetUnitAtGridPosition(gridPosition);
        int targetHealth = target != null ? target.GetComponent<HealthSystem>().GetHealth() : 0;

        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 100 - targetHealth
        };
    }

    public override string GetActionName()
    {
        return "Confront";
    }
}