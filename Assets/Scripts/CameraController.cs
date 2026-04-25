using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    // Cinemachine 3: Usamos CinemachineCamera en lugar de CinemachineVirtualCamera
    [SerializeField] private CinemachineCamera virtualCamera;
    
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
    
    // Cinemachine 3: El componente Transposer ahora se llama CinemachineFollow
    private CinemachineFollow cinemachineFollow;

    private void Start()
    {
        if (virtualCamera != null)
        {
            // Cinemachine 3: Los componentes ahora son "hermanos" de la cámara virtual en el mismo GameObject.
            // Ya no están "ocultos" ni se usa GetCinemachineComponent<T>(). Se usa GetComponent normal.
            cinemachineFollow = virtualCamera.GetComponent<CinemachineFollow>();
            
            if (cinemachineFollow != null)
            {
                // Cinemachine 3: Adiós al prefijo m_
                followOffset = cinemachineFollow.FollowOffset;
            }
            else
            {
                Debug.LogError("Error: A tu CinemachineCamera le falta el componente 'CinemachineFollow'. Por favor, añádelo en el Inspector de Unity.");
            }
        }
        else
        {
            Debug.LogError("Error: No has asignado la Virtual Camera en el script CameraController del Inspector.");
        }
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
        if (cinemachineFollow == null) return; // Protección contra el error visual

        followOffset.y += InputManager.Instance.GetCameraZoomAmount() * zoomAmount;
        
        followOffset.y = Mathf.Clamp(followOffset.y, MIN_FOLLOW_Y_OFFSET, MAX_FOLLOW_Y_OFFSET);
        
        // Cinemachine 3: Reemplazamos transposer.m_FollowOffset por cinemachineFollow.FollowOffset
        smoothOffset = Vector3.Lerp(cinemachineFollow.FollowOffset, followOffset, followOffsetSpeed * Time.deltaTime);
        cinemachineFollow.FollowOffset = smoothOffset;
    }
}
