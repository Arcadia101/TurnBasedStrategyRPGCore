using UnityEngine;
using TMPro;

public class GridSystemVisualSingle : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private TextMeshPro textMeshPro;

    // El color de fábrica de la casilla (Estado neutro por defecto, p.ej. Blanco)
    private Color tileDefaultColor;

    private void Awake()
    {
        if (meshRenderer != null)
        {
            // Inicializamos el default con el color que traiga el material del prefab
            tileDefaultColor = meshRenderer.material.color;
        }
    }

    public void SetColor(Color color)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = color;
        }
    }


    // El sistema de acciones usa esto para limpiar los rangos de movimiento o ataque.
    // Regresa a su default dinámico actual (Blanco, Azul o Gris según su energía).
    public void ResetToDefaultColor()
    {
        if (meshRenderer != null)
        {
            meshRenderer.material.color = tileDefaultColor;
        }
    }

    /// <summary>
    /// El sistema de energías usa esto en caliente para redefinir el estado base del tile.
    /// </summary>
    public void UpdateTileDefaultColor(Color newDefaultColor)
    {
        tileDefaultColor = newDefaultColor;
        
        // Aplicamos el cambio visual inmediatamente si ninguna acción lo está pisando
        ResetToDefaultColor();
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