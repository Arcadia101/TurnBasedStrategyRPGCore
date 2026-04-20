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
    
    private Vector2 inputMoveDir;
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
        inputMoveDir = InputManager.Instance.GetCameraMoveVector();
        
        
        moveVector = transform.forward * inputMoveDir.y + transform.right * inputMoveDir.x;
        transform.position += moveVector * moveSpeed * Time.deltaTime;
    }

    private void HandleRotation()
    {
        rotationVector = new Vector3(0, 0, 0);
        
        rotationVector.y = InputManager.Instance.GetCameraRotateAmount();
        
        transform.eulerAngles += rotationVector * rotationSpeed * Time.deltaTime;
        
        //To Do: Make it smother.
    }

    private void HandleZoom()
    {
       followOffset.y += InputManager.Instance.GetCameraZoomAmount() * zoomAmount;

        
        followOffset.y = Mathf.Clamp(followOffset.y, MIN_FOLLOW_Y_OFFSET, MAX_FOLLOW_Y_OFFSET);
        smoothOffset = Vector3.Lerp(transposer.m_FollowOffset, followOffset, followOffsetSpeed * Time.deltaTime);
        transposer.m_FollowOffset = smoothOffset;
    }
    
}
