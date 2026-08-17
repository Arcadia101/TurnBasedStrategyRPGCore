using System;
using System.Collections.Generic;
using UnityEngine;

public class ConfrontAction : BaseAction
{
    private Unit targetUnit;

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        // 1. Inicializa el ciclo de BaseAction con el callback de completado
        ActionStart(onActionComplete);

        targetUnit = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
        
        // 2. Escuchamos el evento de fin de duelo
        CombatDuelSession.Instance.OnDuelEnded += HandleDuelEnded;

        // 3. Arrancamos el duelo cinemático
        CombatDuelSession.Instance.StartDuel(unit, targetUnit, CombatInteractionType.Confrontation);
    }
    
    private void HandleDuelEnded()
    {
        CombatDuelSession.Instance.OnDuelEnded -= HandleDuelEnded;
        
        // Finaliza la acción y descuenta los AP de la unidad de forma segura
        ActionComplete();
    }
    
    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = unit.GetGridPosition();

        // Evaluamos las unidades presentes en el tablero
        foreach (Unit otherUnit in UnitManager.Instance.GetUnitList())
        {
            if (otherUnit == null || otherUnit == unit) continue;

            // Usamos las reglas del validador de combate
            if (CombatContextValidator.CanConfront(unit, otherUnit))
            {
                validGridPositionList.Add(otherUnit.GetGridPosition());
            }
        }

        return validGridPositionList;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        Unit target = LevelGrid.Instance.GetUnitAtGridPosition(gridPosition);
        int targetHealth = target != null ? target.GetComponent<HealthSystem>().GetHealth() : 0;

        // Prioridad básica para IA: mayor puntaje cuanto menor vida tenga el objetivo
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