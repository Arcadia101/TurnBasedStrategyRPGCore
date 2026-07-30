using System.Collections.Generic;
using UnityEngine;

public class GridEnergyVisualBridge : MonoBehaviour
{
    private void Start()
    {
        // Nos suscribimos a tus delegados Action globales dentro de GridSystem.cs
        GameGridEvents.OnAnyUnitMovementComplete += HandleEnergyRefreshEvent;
        GameGridEvents.OnAnyUnitEnergyChanged += HandleEnergyRefreshEvent;

        // ¡EL REMEDIO! Forzamos la inicialización lógica y visual en el primer frame
        StartCoroutine(InitializeEnergyOnStartRoutine());
    }

    private void OnDestroy()
    {
        // Es vital desuscribirse para evitar fugas de memoria
        GameGridEvents.OnAnyUnitMovementComplete -= HandleEnergyRefreshEvent;
        GameGridEvents.OnAnyUnitEnergyChanged -= HandleEnergyRefreshEvent;
    }

    private System.Collections.IEnumerator InitializeEnergyOnStartRoutine()
    {
        // Esperamos al final del frame. Esto garantiza que todos los Awake y Start de las unidades
        // y los GridSystemVisualSingle ya se ejecutaron y están listos en memoria.
        yield return new WaitForEndOfFrame();

        // 1. FORZAMOS AL BACKEND a poblar el globalEnergyMap en frío por primera vez
        if (LevelGrid.Instance != null) 
            LevelGrid.Instance.TriggerEnergyRefresh();

        if (ToroidLevelGrid.ToroidInstance != null) 
            ToroidLevelGrid.ToroidInstance.TriggerEnergyRefresh();

        // 2. Ahora que el diccionario ya TIENE datos lógicos, mandamos a pintar el tablero
        RefreshAllEnergyVisuals();
    }

    private void HandleEnergyRefreshEvent()
    {
        RefreshAllEnergyVisuals();
    }

    public void RefreshAllEnergyVisuals()
    {
        // --- PROCESAMIENTO 1: GRID NORMAL ---
        if (LevelGrid.Instance != null && GridSystemVisual.Instance != null)
        {
            ResetVisualDefaults(LevelGrid.Instance, GridSystemVisual.Instance);
            ApplyEnergyToVisuals(LevelGrid.Instance, GridSystemVisual.Instance);
        }

        // --- PROCESAMIENTO 2: GRID TOROIDAL ---
        if (ToroidLevelGrid.ToroidInstance != null && ToroidGridSystemVisual.ToroidInstance != null)
        {
            ResetVisualDefaults(ToroidLevelGrid.ToroidInstance, ToroidGridSystemVisual.ToroidInstance);
            ApplyEnergyToVisuals(ToroidLevelGrid.ToroidInstance, ToroidGridSystemVisual.ToroidInstance);
        }
    }
    
    // Devuelve las celdas a su estado neutro usando el color asignado a 'White' en tu lista visual.
    private void ResetVisualDefaults(LevelGrid gridContext, GridSystemVisual visualContext)
    {
        int width = gridContext.GetWidth();
        int height = gridContext.GetHeight();

        Color defaultTileColor = visualContext.GetGridVisualColor(GridSystemVisual.GridVisualType.White);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                GridPosition pos = new GridPosition(x, z);
                var visualSingle = visualContext.GetGridSystemVisualSingleAtPosition(pos);
                if (visualSingle != null)
                {
                    visualSingle.UpdateTileDefaultColor(defaultTileColor);
                }
            }
        }
    }
    
    public Color SetEnergyColor(EmotypeData currentAspect)
    {
        EmotypePrimaryClass currentAspectClass = currentAspect.GetPrimaryClass();
        
        Color color = Color.white;
        switch (currentAspectClass)
        {
            case EmotypePrimaryClass.Rabia:
                //setColor.
                break;
            case EmotypePrimaryClass.Asombro:
                break;
            case EmotypePrimaryClass.Felicidad:
                break;
            case EmotypePrimaryClass.Amor:
                break;
            case EmotypePrimaryClass.Tristeza:
                break;
            case EmotypePrimaryClass.Asco:
                break;
            case EmotypePrimaryClass.Deseo:
                break;
            case EmotypePrimaryClass.Miedo:
                break;
            default:
                break;
        }
        return color;
    }

    
    // Lee tu globalEnergyMap y aplica Blue o Gray usando tu método nativo de materiales.
    private void ApplyEnergyToVisuals(LevelGrid gridContext, GridSystemVisual visualContext)
    {
        var globalEnergyMap = gridContext.GetGlobalEnergyMap();
        if (globalEnergyMap == null) return;

        Color energyColor = visualContext.GetGridVisualColor(GridSystemVisual.GridVisualType.Blue);
        Color overlapColor = visualContext.GetGridVisualColor(GridSystemVisual.GridVisualType.Gray);

        foreach (var kvp in globalEnergyMap)
        {
            GridPosition cellPos = kvp.Key;
            List<Unit> unitsProjecting = kvp.Value;

            if (unitsProjecting == null || unitsProjecting.Count == 0) continue;

            var visualSingle = visualContext.GetGridSystemVisualSingleAtPosition(cellPos);
            if (visualSingle == null) continue;

            Color targetColor = (unitsProjecting.Count > 1) ? overlapColor : energyColor;

            visualSingle.UpdateTileDefaultColor(targetColor);
        }
    }
}