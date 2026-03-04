using System;
using UnityEngine;


public class Unit : MonoBehaviour
{
    [SerializeField] private Animator unitAnimator;
    
    
    private Vector3 _targetPos;
    private GridPosition gridPosition;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotateSpeed = 10f;

    private void Awake()
    {
        _targetPos = transform.position;
    }

    private void Start()
    {
        gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        LevelGrid.Instance.AddUnitAtGridPosition(gridPosition, this);
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
        
        GridPosition newGridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        if (newGridPosition != gridPosition)
        {
            LevelGrid.Instance.UnitMovedGridPosition(this, gridPosition, newGridPosition);
            gridPosition = newGridPosition;
        }
    }

    public void Move(Vector3 targetPos)
    {
        _targetPos = targetPos;
    }
}
