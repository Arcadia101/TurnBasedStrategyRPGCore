using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    
    
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float zoomAmount = 1f;
    [SerializeField] private float followOffsetSpeed = 10f;
    
    
    private const float MIN_FOLLOW_Y_OFFSET = 5f;
    private const float MAX_FOLLOW_Y_OFFSET = 15f;
    
    private Vector3 inputMoveDir;
    private Vector3 moveVector;
    private Vector3 rotationVector;
    private Vector3 followOffset;
    private Vector3 smoothOffset;
    private CinemachineTransposer transposer;

    private void Start()
    {
        transposer = transposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
        followOffset = transposer.m_FollowOffset;
    }

    void Update()
    {
        HandleMovement();

        HandleRotation();

        HandleZoom();
    }

    private void HandleMovement()
    {
        inputMoveDir = new Vector3(0, 0, 0);
        if (Keyboard.current.wKey.isPressed)
        {
            inputMoveDir.z = +1f;
        }

        if (Keyboard.current.sKey.isPressed)
        {
            inputMoveDir.z = -1f;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            inputMoveDir.x = -1f;
        }

        if (Keyboard.current.dKey.isPressed)
        {
            inputMoveDir.x = +1f;
        }
        
        moveVector = transform.forward * inputMoveDir.z + transform.right * inputMoveDir.x;
        transform.position += moveVector * moveSpeed * Time.deltaTime;
    }

    private void HandleRotation()
    {
        rotationVector = new Vector3(0, 0, 0);
        
        if (Keyboard.current.qKey.isPressed)
        {
            rotationVector.y = +1f;
        }

        if (Keyboard.current.eKey.isPressed)
        {
            rotationVector.y = -1f;
        }
        
        transform.eulerAngles += rotationVector * rotationSpeed * Time.deltaTime;
    }

    private void HandleZoom()
    {
        if (Mouse.current.scroll.ReadValue().y > 0)
        {
            followOffset.y -= zoomAmount;
        }

        if (Mouse.current.scroll.ReadValue().y < 0)
        {
            followOffset.y += zoomAmount;
        }

        
        followOffset.y = Mathf.Clamp(followOffset.y, MIN_FOLLOW_Y_OFFSET, MAX_FOLLOW_Y_OFFSET);
        smoothOffset = Vector3.Lerp(transposer.m_FollowOffset, followOffset, followOffsetSpeed * Time.deltaTime);
        transposer.m_FollowOffset = smoothOffset;
    }
    
}
