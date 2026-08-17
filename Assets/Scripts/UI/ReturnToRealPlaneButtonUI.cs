using UnityEngine;
using UnityEngine.UI;

public class ReturnToRealPlaneButtonUI : MonoBehaviour
{
    [SerializeField] private Button returnButton;

    private void Awake()
    {
        returnButton.onClick.AddListener(() =>
        {
            CameraManager.Instance.ReturnToRealPlane();
        });
    }

    private void Start()
    {
        CameraManager.Instance.OnPlaneChanged += HandlePlaneChanged;
        HandlePlaneChanged(CameraManager.Instance.GetCurrentActivePlane());
    }

    private void HandlePlaneChanged(GridType activePlane)
    {
        gameObject.SetActive(activePlane == GridType.Toroid);
    }

    private void OnDestroy()
    {
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.OnPlaneChanged -= HandlePlaneChanged;
        }
    }
}