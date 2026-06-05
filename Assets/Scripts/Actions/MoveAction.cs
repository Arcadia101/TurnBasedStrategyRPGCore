using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : BaseAction
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;
    
    [SerializeField] private int maxMoveDistance = 4;
    private UnitMotor unitMotor;

    protected override void Awake()
    {
        base.Awake();
        unitMotor = GetComponent<UnitMotor>();
    }
    
    public override void TakeAction(GridPosition targetPos, Action onActionComplete)
    {
        // --- SELECCIÓN DINÁMICA DEL MOTOR DE PATHFINDING ---
        List<GridPosition> pathGridPositions = null;
        int pathLength = 0;

        if (unit.GetGridType() == GridType.Toroid)
        {
            if (ToroidPathfinding.ToroidInstance != null)
            {
                pathGridPositions = ToroidPathfinding.ToroidInstance.FindPath(unit.GetGridPosition(), targetPos, out pathLength);
            }
        }
        else
        {
            if (Pathfinding.Instance != null)
            {
                pathGridPositions = Pathfinding.Instance.FindPath(unit.GetGridPosition(), targetPos, out pathLength);
            }
        }

        if (pathGridPositions == null || pathGridPositions.Count == 0)
        {
            ActionComplete();
            return;
        }

        // Convertimos la ruta lógica a vectores usando el contexto dinámico de la unidad
        List<Vector3> worldPositions = new List<Vector3>();
        foreach (GridPosition folderPos in pathGridPositions)
        {
            worldPositions.Add(unit.GetGridContext().GetWorldPosition(folderPos));
        }

        OnStartMoving?.Invoke(this, EventArgs.Empty);
        ActionStart(onActionComplete);

        // Mandamos al motor a caminar
        unitMotor.StartMovement(worldPositions, pathGridPositions, () => {
            OnStopMoving?.Invoke(this, EventArgs.Empty);
            ActionComplete();
        });
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        GridPosition unitGridPosition = unit.GetGridPosition();

        // --- SELECCIÓN DINÁMICA PARA PINTAR LOS CUADROS AZULES ---
        if (unit.GetGridType() == GridType.Toroid)
        {
            if (ToroidPathfinding.ToroidInstance != null)
            {
                return ToroidPathfinding.ToroidInstance.GetReachableGridPositionList(unitGridPosition, maxMoveDistance);
            }
        }

        return Pathfinding.Instance.GetReachableGridPositionList(unitGridPosition, maxMoveDistance);
    }
    

    public override string GetActionName() => "Move";
    
    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        int targetCountAtGridPosition = unit.GetAction<ShootAction>().GetTargetCountAtGridPosition(gridPosition);
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = targetCountAtGridPosition * 10,
        };
    }
}