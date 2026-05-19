using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    
    private InputSystem_Actions inputSystemActions;
        
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There is already a InputManager in the scene!");
            Destroy(gameObject);
        }
        Instance = this;

        inputSystemActions = new InputSystem_Actions();
        inputSystemActions.Player.Enable();
    }
    

    public bool IsUsingMouse() 
    {
        // Revisa si el último control activo pertenece al ratón
        var lastDevice = InputSystem.devices.OrderByDescending(d => d.lastUpdateTime).FirstOrDefault();
    
        if (lastDevice is Mouse) 
        {
            return true;
        }
        return false;
    }

    public Vector2 GetPointerMoveVector()
    {
        return inputSystemActions.Player.PointerMovement.ReadValue<Vector2>();
    }

    public bool WasCycleLeftPressed()
    {
        return inputSystemActions.Player.CycleLeft.WasPressedThisFrame();
    }

    public bool WasCycleRightPressed()
    {
        return inputSystemActions.Player.CycleRight.WasPressedThisFrame();
    }
    
    public Vector2 GetMouseScreenPosition()
    {
        return Mouse.current.position.ReadValue();
    }

    public bool WasConfirmPressedThisFrame()
    {
        return inputSystemActions.Player.Confirm.WasPressedThisFrame();
        /*
        Fixed method, debug only
        return Mouse.current.leftButton.wasPressedThisFrame;
        */
    }

    public Vector2 GetCameraMoveVector()
    {
        return inputSystemActions.Player.CameraMovement.ReadValue<Vector2>();
        
        /*
        Fixed method, debug only
        Vector2 inputMoveDir = new Vector2(0, 0);
        if (Keyboard.current.wKey.isPressed)
        {
            inputMoveDir.y = +1f;
        }

        if (Keyboard.current.sKey.isPressed)
        {
            inputMoveDir.y = -1f;
        }

        if (Keyboard.current.aKey.isPressed)
        {
            inputMoveDir.x = -1f;
        }

        if (Keyboard.current.dKey.isPressed)
        {
            inputMoveDir.x = +1f;
        }
        
        return inputMoveDir;
        */
    }

    public float GetCameraRotateAmount()
    {
        return inputSystemActions.Player.CameraRotate.ReadValue<float>();
        /*
        Fixed method, debug only
        float rotateAmount = 0f;
        if (Keyboard.current.qKey.isPressed)
        {
            rotateAmount =+ 1f;
        }
        if (Keyboard.current.eKey.isPressed)
        {
            rotateAmount -= 1f;
        }

        return rotateAmount;
        */
    }

    public float GetCameraZoomAmount()
    {
        return inputSystemActions.Player.CameraZoom.ReadValue<float>();
        /*
        Fixed method, debug only
        float zoomAmount = 0f;

        if (Mouse.current.scroll.ReadValue().y > 0)
        {
            zoomAmount = -1f;
        }

        if (Mouse.current.scroll.ReadValue().y < 0)
        {
            zoomAmount = +1f;
        }

        return zoomAmount;
        */
    }
}
