using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }

    [SerializeField] private CinemachineImpulseSource cinemachineActionCameraImpulseSource;
    [SerializeField] private CinemachineImpulseSource cinemachineDronCameraImpulseSource;
    
    private CinemachineImpulseSource cinemachineCurrentImpulseSource;
    
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("There is already a ScreenShake in the scene!");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        cinemachineCurrentImpulseSource = cinemachineDronCameraImpulseSource;
    }
    
    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            cinemachineCurrentImpulseSource.GenerateImpulse();
        }
    }

    public void SelectCurrentInputSource()
    {
        var brain = Camera.main.GetComponent<CinemachineBrain>();
        if (brain != null && brain.ActiveVirtualCamera != null)
        {
            var ActiveCamera = brain.ActiveVirtualCamera as CinemachineCamera;
            
            // Comprobación de seguridad: Verificamos que no sea nulo antes de acceder a su prioridad.
            if (ActiveCamera != null && ActiveCamera.Priority.Value > 15)
            {
                cinemachineCurrentImpulseSource = cinemachineActionCameraImpulseSource;
                return;
            }
        }
        
        // Default
        cinemachineCurrentImpulseSource = cinemachineDronCameraImpulseSource;
    }
    
    public void Shake(float intensity)
    {
        SelectCurrentInputSource();
        cinemachineCurrentImpulseSource.GenerateImpulse(intensity);
    }
}
