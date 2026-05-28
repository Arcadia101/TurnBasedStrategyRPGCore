using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAction : MonoBehaviour
{
    public static event EventHandler OnAnyActionStarted;
    public static event EventHandler OnAnyActionCompleted;
    public static event EventHandler OnAnyTargetCycled;
    
    protected Unit unit;
    protected bool isActive;
    protected Action onActionComplete;
    [SerializeField] protected int ActionPointsCost = 1;
    // Lista protegida para que las subclases la llenen
    protected List<Unit> targetsInTile = new List<Unit>();
    protected int currentTargetIndex = 0;
    
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
    
    public virtual bool IsAwaitingTargetSelection() => false;
    
    public virtual void CycleTarget(int direction)
    {
        if (targetsInTile.Count <= 1) return;
        
        currentTargetIndex = (currentTargetIndex + direction + targetsInTile.Count) % targetsInTile.Count;

        Debug.Log("targuet changed to: " + targetsInTile[currentTargetIndex].name);
        // Notificamos que el objetivo seleccionado dentro de la casilla cambió
        OnAnyTargetCycled?.Invoke(this, EventArgs.Empty);
    }

    // Método para obtener el target seleccionado actualmente
    public Unit GetTargetUnit()
    {
        if (targetsInTile.Count > 0)
            return targetsInTile[currentTargetIndex];
        return null;
    }
    
    protected Vector3 CalculateToroidWarpOffset(GridPosition fromPos, GridPosition toPos, ToroidLevelGrid toroidGrid)
    {
        int width = toroidGrid.GetWidth();
        int height = toroidGrid.GetHeight();

        int directionX = 0;
        int directionZ = 0;

        // CORRECCIÓN DE SIGNOS: Si vas de 0 a 4 (de izquierda a derecha), el offset físico 
        // debe empujarte hacia la IZQUIERDA del espacio modular (-1) para simular que apareces por la derecha.
        if (Mathf.Abs(fromPos.x - toPos.x) > 1)
        {
            directionX = fromPos.x < toPos.x ? -1 : 1;
        }
        if (Mathf.Abs(fromPos.z - toPos.z) > 1)
        {
            directionZ = fromPos.z < toPos.z ? -1 : 1;
        }

        // Obtenemos la orientación real a 45 grados de tu rejilla dual de octágonos
        Vector3 cellDirX = toroidGrid.GetWorldPosition(new GridPosition(1, 0)) - toroidGrid.GetWorldPosition(new GridPosition(0, 0));
        Vector3 cellDirZ = toroidGrid.GetWorldPosition(new GridPosition(0, 1)) - toroidGrid.GetWorldPosition(new GridPosition(0, 0));

        // Retornamos el vector con la dirección corregida
        return (cellDirX * (width * directionX)) + (cellDirZ * (height * directionZ));
    }
}
