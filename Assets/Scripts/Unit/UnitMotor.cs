using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitMotor : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotateSpeed = 10f;

    private List<Vector3> worldPositions = new List<Vector3>();
    private List<GridPosition> pathGridPositions = new List<GridPosition>();
    private int currentIndex;
    private Action onMovementComplete;
    private bool isMoving = false;
    private Unit unit;
    private Vector3 targetPosition;

    private void Awake()
    {
        unit = GetComponent<Unit>();
    }
    
    private void Start()
    {
        targetPosition = transform.position; 
    }
    
    private void Update()
    {
        if (!isMoving) return;

        targetPosition = worldPositions[currentIndex];

        // Ahora calculamos la dirección hacia el nodo actual
        Vector3 moveDir = (targetPosition - transform.position).normalized;

        if (moveDir != Vector3.zero)
        {
            transform.forward = Vector3.Lerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
        }

        if (Vector3.Distance(transform.position, targetPosition) >= 0.1f)
        {
            transform.position += moveDir * (moveSpeed * Time.deltaTime);
        }
        else
        {
            // =================================================================
            // ¡EL FIX SOBERANO! Evaluamos el Warp ÚNICAMENTE cuando completamos un nodo
            // y vamos a saltar al siguiente de la lista estática
            // =================================================================
            if (unit.GetGridType() == GridType.Toroid && ToroidLevelGrid.ToroidInstance != null)
            {
                // Solo si nos queda un nodo adelante en la ruta planificada
                if (currentIndex + 1 < pathGridPositions.Count)
                {
                    GridPosition currentStepGridPos = pathGridPositions[currentIndex];
                    GridPosition nextStepGridPos = pathGridPositions[currentIndex + 1];

                    // Si el siguiente nodo de la ruta implica cruzar el borde Pac-Man
                    if (Mathf.Abs(currentStepGridPos.x - nextStepGridPos.x) > 1 || Mathf.Abs(currentStepGridPos.z - nextStepGridPos.z) > 1)
                    {
                        // Calculamos el offset basándonos estrictamente en la ruta estática planificada
                        Vector3 warpOffset = CalculateWarpOffsetDirect(currentStepGridPos, nextStepGridPos, ToroidLevelGrid.ToroidInstance);
                        
                        // Teletransportamos el transform físico al borde real opuesto
                        transform.position += warpOffset;
                        
                        Debug.Log($"[MOTOR] Warp Exitoso. Cruzando borde de {currentStepGridPos} a {nextStepGridPos}.");
                    }
                }
            }

            currentIndex++;
            if (currentIndex >= worldPositions.Count)
            {
                isMoving = false;
                onMovementComplete?.Invoke();
            }
        }
    }

    public void StartMovement(List<Vector3> positions, List<GridPosition> logicalGridPositions, Action onComplete)
    {
        worldPositions = positions;
        pathGridPositions = logicalGridPositions;
        onMovementComplete = onComplete;
        currentIndex = 0;
        isMoving = true;
    }

    private Vector3 CalculateWarpOffsetDirect(GridPosition fromPos, GridPosition toPos, ToroidLevelGrid toroidGrid)
    {
        int width = toroidGrid.GetWidth();
        int height = toroidGrid.GetHeight();
        int directionX = 0;
        int directionZ = 0;

        // CORRECCIÓN DE SIGNOS: Si 'fromPos' es menor que 'toPos' (ej: de 0 a 4), 
        // significa que lógicamente cruzamos el borde hacia atrás, por lo que el offset físico
        // nos debe empujar hacia ADELANTE (1) para aparecer en la celda real correspondiente.
        if (Mathf.Abs(fromPos.x - toPos.x) > 1) directionX = fromPos.x < toPos.x ? 1 : -1;
        if (Mathf.Abs(fromPos.z - toPos.z) > 1) directionZ = fromPos.z < toPos.z ? 1 : -1;

        Vector3 cellDirX = toroidGrid.GetWorldPosition(new GridPosition(1, 0)) - toroidGrid.GetWorldPosition(new GridPosition(0, 0));
        Vector3 cellDirZ = toroidGrid.GetWorldPosition(new GridPosition(0, 1)) - toroidGrid.GetWorldPosition(new GridPosition(0, 0));

        return (cellDirX * (width * directionX)) + (cellDirZ * (height * directionZ));
    }
}