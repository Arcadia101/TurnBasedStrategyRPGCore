using UnityEngine;
using TMPro;

public class GridSystemVisualSingle : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private TextMeshPro textMeshPro;

    private Color defaultColor;

    private void Awake()
    {
        if (meshRenderer != null)
        {
            defaultColor = meshRenderer.material.color;
        }
    }

    public void SetColor(Color color)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = color;
        }
    }

    public void ResetToDefaultColor()
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = defaultColor;
        }
    }

    public void SetCoordinates(string coordinates)
    {
        if (textMeshPro != null)
        {
            textMeshPro.text = coordinates;
        }
    }

    public void ShowCoordinates(bool show)
    {
        if (textMeshPro != null)
        {
            textMeshPro.gameObject.SetActive(show);
        }
    }
}
