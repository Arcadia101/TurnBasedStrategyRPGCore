using Cinemachine;
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
        if (Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera.Priority > 15)
        {
            cinemachineCurrentImpulseSource = cinemachineActionCameraImpulseSource;
        }
        else
        {
            cinemachineCurrentImpulseSource = cinemachineDronCameraImpulseSource;
        }
    }
    
    public void Shake(float intensity)
    {
        SelectCurrentInputSource();
        cinemachineCurrentImpulseSource.GenerateImpulse(intensity);
    }
}
