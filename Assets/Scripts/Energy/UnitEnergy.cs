using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitEnergy : MonoBehaviour
{
    [Header("Configuración del Aspecto")]
    [SerializeField] private EmotypeData currentAspect;

    private Unit unit;

    [Header("Estados de Energía")]
    [SerializeField] private bool hasOverlappedEnergy; // Se puede ver en el inspector para debugear

    // Propiedad pública de solo lectura para que otros scripts la consulten
    public bool HasOverlappedEnergy => hasOverlappedEnergy;
    
    private void Awake()
    {
        unit = GetComponent<Unit>();
    }

    /// <summary>
    /// Devuelve la lista de posiciones de la cuadrícula que están actualmente 
    /// energizadas por el aspecto de esta unidad.
    /// </summary>
    public List<GridPosition> GetEnergizedPositions()
    {
        if (currentAspect == null)
        {
            // Si por alguna razón no hay aspecto, al menos la casilla central se energiza
            return new List<GridPosition> { unit.GetGridPosition() };
        }

        // 1. Extraemos la posición actual de la unidad
        GridPosition unitGridPos = unit.GetGridPosition();

        // 2. Le preguntamos a la unidad en qué contexto de Grid está viviendo actualmente.
        // Nota: Asegúrate de que tu Unit.cs tenga un método público o propiedad para obtener su 'currentGridContext' o si es toroidal.
        // Asumiendo que tu 'currentGridType' guarda si es Toroid o Normal:
        bool isToroid = unit.GetCurrentGridType() == GridType.Toroid;

        // 3. Conseguimos el GridSystem correcto a través del contexto actual de la unidad
        // Tu 'currentGridContext' (LevelGrid o ToroidLevelGrid) debería darte acceso a su GridSystem.
        // Si tu arquitectura hereda de una clase base de Grid, podemos llamar directamente al método que creamos:
        var activeGridSystem = unit.GetGridContext().GetGridSystem();

        // 4. Retornamos el cálculo matemático final de la Grid
        return activeGridSystem.CalculateEnergyPositions(unitGridPos, currentAspect, isToroid);
    }
    
    public void CheckForOverlap()
    {
        bool currentlyOverlapped = false;
    
        // 1. Conseguimos las celdas que esta unidad está energizando actualmente
        List<GridPosition> myEnergizedCells = GetEnergizedPositions();
    
        // 2. Le pedimos al tablero el mapa global de energías
        var globalEnergyMap = unit.GetGridContext().GetGlobalEnergyMap();

        // 3. Recorremos nuestras casillas energizadas
        foreach (GridPosition cell in myEnergizedCells)
        {
            if (globalEnergyMap.ContainsKey(cell))
            {
                List<Unit> unitsOnThisCell = globalEnergyMap[cell];
        
                // LOG DE CONTROL: Nos dirá cuántos dueños tiene esta casilla
                Debug.Log($"[DEBUG-SOLAPAMIENTO] Evaluando celda {cell}. Unidades proyectando aquí: {unitsOnThisCell.Count}");

                foreach (Unit otherUnit in unitsOnThisCell)
                {
                    if (otherUnit == this.unit) continue;
            
                    // ... (tu lógica de facción o filtrado)
                    currentlyOverlapped = true;
                }
            }
        }

        // 4. Encendemos o apagamos nuestra bandera interna
        this.hasOverlappedEnergy = currentlyOverlapped;
    
        Debug.Log($"[CEREBRO-ENERGÍA] {name} estado de solapamiento: {hasOverlappedEnergy}");
    }
    
    

    // Método utilitario para cambiar de aspecto dinámicamente en el futuro si se cumplen los objetivos
    public void SetEmotype(EmotypeData newEmotype)
    {
        currentAspect = newEmotype;
        // Alerta inmediata de que cambió un patrón de energía
        unit.GetGridContext().TriggerEnergyRefresh();
    }
    
    private void OnDrawGizmos()
    {
        // Solo dibujamos si el juego está corriendo para tener acceso a las celdas de las Grids
        if (!Application.isPlaying) return;

        List<GridPosition> energizedPositions = GetEnergizedPositions();

        if (energizedPositions == null) return;

        Gizmos.color = Color.cyan; // Color azul/celeste para la energía

        foreach (GridPosition gridPos in energizedPositions)
        {
            Vector3 worldPos;

            // Obtenemos la posición física real dependiendo de dónde está registrada la unidad
            if (unit.GetCurrentGridType() == GridType.Toroid)
            {
                worldPos = ToroidLevelGrid.ToroidInstance.GetWorldPosition(gridPos);
            }
            else
            {
                worldPos = LevelGrid.Instance.GetWorldPosition(gridPos);
            }

            // Dibujamos una esfera flotante un poco por encima del suelo en cada casilla energizada
            Gizmos.DrawWireSphere(worldPos + new Vector3(0f, 0.2f, 0f), 0.4f);
        }
    }
}