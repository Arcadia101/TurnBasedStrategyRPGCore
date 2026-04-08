using UnityEngine;

public class GridSystemVisualSingle : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    
    void Start()
    {
        meshRenderer.enabled = false;
    }
    
    public void Show(Material material)
    {
        meshRenderer.enabled = true;
        meshRenderer.material = material;
    }
    
    public void Hide()
    {
        meshRenderer.enabled = false;
    }
}
