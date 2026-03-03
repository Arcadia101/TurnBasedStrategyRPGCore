using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseWorld : MonoBehaviour
{
    public static MouseWorld Instance;
    [SerializeField] private LayerMask mousePlaneLayer;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Mouse.current == null) return;
        
        transform.position = MouseWorld.GetPosition();
    }

    public static Vector3 GetPosition()
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, Instance.mousePlaneLayer);
        return hit.point;
    }
}
