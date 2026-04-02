using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : BaseAction
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;
    
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private int maxMoveDistance = 1;
    
    private Vector3 _targetPos;
    
    
    protected override void Awake()
    {
        base.Awake();
        _targetPos = transform.position;
    }

    private void Update()
    {
        if (!isActive) return;
        
        Vector3 moveDir = (_targetPos - transform.position).normalized;
        
        if (Vector3.Distance(transform.position, _targetPos) >= 0.1f)
        {
            transform.position += moveDir * (moveSpeed * Time.deltaTime);
            
        }
        else
        {
            Debug.Log(LevelGrid.Instance.GetGridPosition(transform.position));
            
            OnStopMoving?.Invoke(this, EventArgs.Empty);
            ActionComplete();
        }

        transform.forward = Vector3.Lerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
    }

    public void Move(Vector3 targetPos, Action onActionComplete)
    {
        ActionStart(onActionComplete);
        _targetPos = targetPos;
    }
    
    public override void TakeAction(GridPosition targetPos, Action onActionComplete)
    {
        ActionStart(onActionComplete);
        
        _targetPos = LevelGrid.Instance.GetWorldPosition(targetPos);
            
        OnStartMoving?.Invoke(this, EventArgs.Empty);
    }
    
    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();

        GridPosition unitGridPosition = unit.GetGridPosition();
        for (int x = -maxMoveDistance; x <= maxMoveDistance; x++)
        {
            for (int z = -maxMoveDistance; z <= maxMoveDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                {
                    continue;
                }

                if (unitGridPosition == testGridPosition)
                {
                    //misma posicion donde ya está la unidad
                    continue;
                }
                
                if (LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition))
                {
                    //posicion ocupada por otra unidad.
                    continue;
                }

                validGridPositionList.Add(testGridPosition);
            }
        }
        return validGridPositionList;
    }

    public override string GetActionName()
    {
        return "Move";
    }
}
