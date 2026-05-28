using System;
using System.Collections.Generic;
using UnityEngine;

public class ToroidLevelGrid : LevelGrid
{
    // Método central matemático para envolver las posiciones antes de que toquen el GridSystem
    public GridPosition GetWrappedGridPosition(GridPosition gridPosition)
    {
        int width = GetWidth();
        int height = GetHeight();

        // Aritmética modular estricta para el Toroide (Soporta positivos y negativos)
        int wrappedX = (gridPosition.x % width + width) % width;
        int wrappedZ = (gridPosition.z % height + height) % height;

        return new GridPosition(wrappedX, wrappedZ);
    }

    // --- SOBRESCRITURA POLIMÓRFICA ---

    // Toda posición en el infinito es válida en el Toroide porque siempre se envuelve
    public override bool IsValidGridPosition(GridPosition gridPosition)
    {
        return true;
    }
    
    public override void UnitMovedGridPosition(Unit unit, GridPosition fromGridPosition, GridPosition toGridPosition)
    {
        // Aseguramos que tanto el origen como el destino pasen por el filtro Pac-Man
        GridPosition wrappedFrom = GetWrappedGridPosition(fromGridPosition);
        GridPosition wrappedTo = GetWrappedGridPosition(toGridPosition);

        // Ejecutamos el movimiento base de forma segura con los índices corregidos
        base.UnitMovedGridPosition(unit, wrappedFrom, wrappedTo);
    }

    // Cuando el sistema pida la posición en el mundo, la calculamos usando el índice envuelto
    public override Vector3 GetWorldPosition(GridPosition gridPosition)
    {
        GridPosition wrapped = GetWrappedGridPosition(gridPosition);
        return base.GetWorldPosition(wrapped); // Llama a la matemática original de tu GridSystem
    }

    // Cuando el sistema pida la casilla según el mundo, envolvemos el resultado por seguridad
    public override GridPosition GetGridPosition(Vector3 worldPosition)
    {
        GridPosition normalGridPos = base.GetGridPosition(worldPosition);
        return GetWrappedGridPosition(normalGridPos);
    }
}