using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Testing : MonoBehaviour
{
    [SerializeField] private Unit unit;
    
    
    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            GridSystemVisual.Instance.HideAllGridPosition();
            GridSystemVisual.Instance.ShowGridPositionList(unit.GetMoveAction().GetValidActionGridPositionList());
            //unit.GetMoveAction().GetValidActionGridPositionList();
        }
    }
}
