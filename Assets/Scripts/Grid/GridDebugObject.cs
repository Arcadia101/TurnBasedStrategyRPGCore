using System;
using UnityEngine;
using TMPro;

public class GridDebugObject : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMeshPro;
    [SerializeField] private GameObject octagonVisual;
    [SerializeField] private GameObject rhombusVisual;
    
    private GridObject gridObject;

    public virtual void SetGridObject(object gridObject)
    {
        this.gridObject = (GridObject)gridObject;
    }

    protected virtual void Update()
    {
        textMeshPro.text = gridObject.ToString();
        
        switch (gridObject.GetTileType())
        {
            case TileType.Octagon:
                if (octagonVisual != null) octagonVisual.SetActive(true);
                if (rhombusVisual != null) rhombusVisual.SetActive(false);
                break;
            case TileType.Rhombus:
                if (octagonVisual != null) octagonVisual.SetActive(false);
                if (rhombusVisual != null) rhombusVisual.SetActive(true);
                break;
            case TileType.Empty:
                if (octagonVisual != null) octagonVisual.SetActive(false);
                if (rhombusVisual != null) rhombusVisual.SetActive(false);
                break;
        }
    }
}
