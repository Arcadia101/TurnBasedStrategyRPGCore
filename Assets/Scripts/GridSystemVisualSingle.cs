using UnityEngine;

public class GridSystemVisualSingle : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    
    void Start()
    {
        meshRenderer.enabled = false;
    }
    
    public void Show()
    {
        meshRenderer.enabled = true;
    }
    
    public void Hide()
    {
        meshRenderer.enabled = false;
    }
}
