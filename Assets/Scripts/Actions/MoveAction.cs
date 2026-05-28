using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveAction : BaseAction
{
    public event EventHandler OnStartMoving;
    public event EventHandler OnStopMoving;
    
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private int maxMoveDistance = 1;
    
    private List<Vector3> positionList;
    //Guardamos la ruta lógica para poder calcular los saltos del Toroide en el Update
    private List<GridPosition> pathGridPositionList; 
    private int currentPositionIndex;

    private void Update()
{
    if (!isActive) return;
    
    Vector3 targetPosition = positionList[currentPositionIndex];
    Vector3 moveDir = (targetPosition - transform.position).normalized;
    
    if (moveDir != Vector3.zero)
    {
        transform.forward = Vector3.Lerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
    }
    
    // --- LOG DE MONITOREO DE FRAME ---
    // Nos dice dónde está la unidad en metros y hacia qué metro exacto calcula el motor que debe caminar
    Debug.Log($"[FRAME] Unidad en: {transform.position} -> Moviéndose a Target Físico Index [{currentPositionIndex}]: {targetPosition}. Distancia restante: {Vector3.Distance(transform.position, targetPosition):F2}m");

    if (Vector3.Distance(transform.position, targetPosition) >= 0.1f)
    {
        transform.position += moveDir * (moveSpeed * Time.deltaTime);
    }
    else
    {
        // ¡LLEGÓ AL TARGET ACTUAL!
        Debug.Log($"[LLEGADA] Unidad tocó Target Index [{currentPositionIndex}]: {targetPosition}. Evaluando lógica de cambio de nodo...");

        if (unit.GetGridType() == GridType.Toroid && LevelGrid.Instance is ToroidLevelGrid toroidGrid)
        {
            Debug.Log($"[CHECK-TOROIDE] La unidad es tipo Toroid. Revisando si el siguiente paso implica cruzar borde...");

            if (currentPositionIndex + 1 < pathGridPositionList.Count)
            {
                GridPosition currentGridPos = pathGridPositionList[currentPositionIndex];
                GridPosition nextGridPos = pathGridPositionList[currentPositionIndex + 1];

                Debug.Log($"[TRANSTURA] Comparando Casilla Actual: {currentGridPos} con Siguiente Casilla: {nextGridPos}");

                if (Mathf.Abs(currentGridPos.x - nextGridPos.x) > 1 || Mathf.Abs(currentGridPos.z - nextGridPos.z) > 1)
                {
                    Debug.LogWarning($"[¡WARP DETECTADO!] Salto crítico entre {currentGridPos} y {nextGridPos}. Calculando desfase físico...");
                    
                    Vector3 warpOffset = CalculateToroidWarpOffset(currentGridPos, nextGridPos, toroidGrid);
                    
                    Vector3 posAntes = transform.position;
                    transform.position += warpOffset;
                    
                    Debug.LogWarning($"[EJECUCIÓN WARP] Transform desplazado de {posAntes} a {transform.position} usando Offset: {warpOffset}");

                    // Forzamos actualización lógica
                    toroidGrid.UnitMovedGridPosition(unit, currentGridPos, nextGridPos);
                }
                else
                {
                    Debug.Log($"[PASO NORMAL] La distancia entre {currentGridPos} y {nextGridPos} es normal (<= 1). No requiere Warp.");
                }
            }
            else
            {
                Debug.Log("[CHECK-TOROIDE] No hay más casillas lógicas en 'pathGridPositionList' después de esta.");
            }
        }
        else
        {
            Debug.Log($"[IGNORADO] No se evalúa Toroide. ¿Tipo Unidad?: {unit.GetGridType()} | ¿Grid es ToroidLevelGrid?: {LevelGrid.Instance is ToroidLevelGrid}");
        }

        currentPositionIndex++;
        Debug.Log($"[ÍNDICE INCREMENTADO] Siguiente índice asignado: {currentPositionIndex} de un total de {positionList.Count}");

        if (currentPositionIndex >= positionList.Count)
        {
            Debug.Log("[FIN] Se agotaron los destinos físicos en 'positionList'. Finalizando MoveAction.");
            OnStopMoving?.Invoke(this, EventArgs.Empty);
            ActionComplete();
        }
    }
}

    public void Move(Vector3 targetPos, Action onActionComplete)
    {
        ActionStart(onActionComplete);
    }
    
    public override void TakeAction(GridPosition targetPos, Action onActionComplete)
    {
        // --- SOLUCIÓN: BUSQUEDA EXPLICITA DEL CEREBRO EN ESCENA ---
        Pathfinding activePathfinding = Pathfinding.Instance;

        // Si la unidad es toroidal, nos aseguramos de usar el buscador toroidal de la escena
        if (unit.GetGridType() == GridType.Toroid)
        {
            activePathfinding = FindFirstObjectByType<ToroidPathfinding>();
        
            // Si por algún motivo no lo encuentra en la escena, usamos la instancia base por defecto
            if (activePathfinding == null) activePathfinding = Pathfinding.Instance;
        }

        // Calculamos la ruta usando el componente verificado
        List<GridPosition> pathGridPositions = activePathfinding.FindPath(unit.GetGridPosition(), targetPos, out int pathLength);

        if (pathGridPositions == null)
        {
            ActionComplete();
            return;
        }

        currentPositionIndex = 0;
        positionList = new List<Vector3>();
        pathGridPositionList = new List<GridPosition>(pathGridPositions); // NUEVO: Clonamos la lista lógica para el Update

        foreach (GridPosition pathGridPosition in pathGridPositionList)
        {
            positionList.Add(LevelGrid.Instance.GetWorldPosition(pathGridPosition));
        }

        OnStartMoving?.Invoke(this, EventArgs.Empty);
        ActionStart(onActionComplete);
    }

    // NUEVO: Función matemática de soporte para el empujón físico adaptado a tus octágonos rotados
    private Vector3 CalculateToroidWarpOffset(GridPosition fromPos, GridPosition toPos, ToroidLevelGrid toroidGrid)
    {
        int width = toroidGrid.GetWidth();
        int height = toroidGrid.GetHeight();

        int directionX = 0;
        int directionZ = 0;

        if (Mathf.Abs(fromPos.x - toPos.x) > 1)
        {
            directionX = fromPos.x < toPos.x ? -1 : 1;
        }
        if (Mathf.Abs(fromPos.z - toPos.z) > 1)
        {
            directionZ = fromPos.z < toPos.z ? -1 : 1;
        }

        // Direcciones de los ejes físicos nativos de tu GridSystem original
        Vector3 cellDirX = toroidGrid.GetWorldPosition(new GridPosition(1, 0)) - toroidGrid.GetWorldPosition(new GridPosition(0, 0));
        Vector3 cellDirZ = toroidGrid.GetWorldPosition(new GridPosition(0, 1)) - toroidGrid.GetWorldPosition(new GridPosition(0, 0));

        return (cellDirX * (width * directionX)) + (cellDirZ * (height * directionZ));
    }
    
    public List<GridPosition> GetValidActionGridPositionListLegacy()
    {
        GridPosition unitGridPosition = unit.GetGridPosition();
        return Pathfinding.Instance.GetReachableGridPositionList(unitGridPosition, maxMoveDistance);
    }
    
    public override List<GridPosition> GetValidActionGridPositionList()
    {
        GridPosition unitGridPosition = unit.GetGridPosition();

        // Si la unidad es toroidal, forzamos al rango a calcularse usando las reglas del Toroide
        if (unit.GetGridType() == GridType.Toroid)
        {
            ToroidPathfinding toroidPathfinding = FindFirstObjectByType<ToroidPathfinding>();
            if (toroidPathfinding != null)
            {
                return toroidPathfinding.GetReachableGridPositionList(unitGridPosition, maxMoveDistance);
            }
        }

        // Flujo normal por defecto
        return Pathfinding.Instance.GetReachableGridPositionList(unitGridPosition, maxMoveDistance);
    }

    public override string GetActionName() => "Move";
    
    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        int targetCountAtGridPosition = unit.GetAction<ShootAction>().GetTargetCountAtGridPosition(gridPosition);
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = targetCountAtGridPosition * 10,
        };
    }
}
