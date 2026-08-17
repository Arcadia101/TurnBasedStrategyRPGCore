using System.Collections.Generic;
using UnityEngine;

public static class CombatContextValidator
{
    // Regla 1: Confrontar (Atacar)
    // - Normal: Adyacente Y Solapando Energía.
    // - Toroidal: Solo Solapando Energía.
    public static bool CanConfront(Unit attacker, Unit target)
    {
        if (attacker == null || target == null || attacker == target) return false;
        if (attacker.IsEnemy() == target.IsEnemy()) return false;

        bool hasEnergyOverlap = CheckEnergyOverlap(attacker, target);
        bool isToroid = attacker.GetCurrentGridType() == GridType.Toroid;

        if (isToroid)
        {
            return hasEnergyOverlap;
        }
        else
        {
            bool isAdjacent = AreUnitsAdjacent(attacker, target);
            return isAdjacent && hasEnergyOverlap;
        }
    }

    /// Regla 2: Evitar (Entrar a Grid Toroidal desde Normal)
    // Requiere: Estar en Rombo + Sin enemigos adyacentes + Sin solapar energía con nadie
    public static bool CanEnterToroidGrid(Unit unit, List<Unit> allActiveUnits)
    {
        if (unit == null || unit.GetCurrentGridType() == GridType.Toroid) return false;

        // 1. Validar casilla romboide
        var gridSystem = unit.GetGridContext().GetGridSystem();
        GridObject currentNode = gridSystem.GetGridObject(unit.GetGridPosition()) as GridObject;
        if (currentNode == null || currentNode.GetTileType() != TileType.Rhombus) return false;

        // 2 y 3. Validar contra el resto de unidades activas
        foreach (Unit otherUnit in allActiveUnits)
        {
            if (otherUnit == null || otherUnit == unit) continue;

            // No estar adyacente a enemigos
            if (otherUnit.IsEnemy() != unit.IsEnemy() && AreUnitsAdjacent(unit, otherUnit))
            {
                return false;
            }

            // No solapar energía con ninguna unidad (aliada o enemiga)
            if (CheckEnergyOverlap(unit, otherUnit))
            {
                return false;
            }
        }

        return true;
    }

    
    // Regla 3: Evadir / "Curar" (Grid Toroidal)
    // Requiere: Estar en Toroidal + NO solapar energía con el objetivo
    public static bool CanEvadeInToroid(Unit user, Unit target)
    {
        if (user == null || target == null || user.GetCurrentGridType() != GridType.Toroid) return false;

        bool hasEnergyOverlap = CheckEnergyOverlap(user, target);
        return !hasEnergyOverlap;
    }

    // --- MÉTODOS AUXILIARES DIRECTOS ---

    // Verifica si dos posiciones en la cuadrícula son adyacentes (ortogonal o diagonalmente a distancia <= 1)
    public static bool AreUnitsAdjacent(Unit a, Unit b)
    {
        GridPosition posA = a.GetGridPosition();
        GridPosition posB = b.GetGridPosition();

        int deltaX = Mathf.Abs(posA.x - posB.x);
        int deltaZ = Mathf.Abs(posA.z - posB.z);

        // Adyacente si la diferencia máxima en cualquier eje es 1 y no están en la misma celda
        return (deltaX <= 1 && deltaZ <= 1) && !(deltaX == 0 && deltaZ == 0);
    }

    // Comprueba si las áreas de energía de dos unidades se cruzan entre sí
    public static bool CheckEnergyOverlap(Unit a, Unit b)
    {
        UnitEnergy energyA = a.GetComponent<UnitEnergy>();
        UnitEnergy energyB = b.GetComponent<UnitEnergy>();

        if (energyA == null || energyB == null) return false;

        List<GridPosition> positionsA = energyA.GetEnergizedPositions();
        List<GridPosition> positionsB = energyB.GetEnergizedPositions();

        // Si alguna posición de A coincide con una posición de B, hay solapamiento
        foreach (GridPosition pos in positionsA)
        {
            if (positionsB.Contains(pos))
            {
                return true;
            }
        }

        return false;
    }
}