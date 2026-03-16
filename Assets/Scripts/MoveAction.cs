using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : MonoBehaviour
{
    [SerializeField] private Animator unitAnimator;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private int maxMoveDistance = 1;
    
    private Vector3 _targetPos;
    private Unit unit;
    
    private void Awake()
    {
        unit = GetComponent<Unit>();
        _targetPos = transform.position;
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, _targetPos) >= 0.1f)
        {
            Vector3 moveDir = (_targetPos - transform.position).normalized;
            transform.position += moveDir * (moveSpeed * Time.deltaTime);
            
            transform.forward = Vector3.Lerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
            unitAnimator.SetBool( "IsWalking", true);
        }
        else
        {
            unitAnimator.SetBool( "IsWalking", false);
        }

    }

    public void Move(Vector3 targetPos)
    {
        _targetPos = targetPos;
    }
    
    public void Move(GridPosition targetPos)
    {
        _targetPos = LevelGrid.Instance.GetWorldPosition(targetPos);
    }

    public bool IsValidActionGridPosition(GridPosition gridPosition)
    {
        List<GridPosition> validGridPositionList = GetValidActionGridPositionList();
        return validGridPositionList.Contains(gridPosition);
    }

    public List<GridPosition> GetValidActionGridPositionList()
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
                Debug.Log(testGridPosition);
            }
        }
        return validGridPositionList;
    }
}
