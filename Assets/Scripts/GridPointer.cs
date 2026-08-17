using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridPointer : MonoBehaviour
{
    public static GridPointer Instance { get; private set; }

    [SerializeField] private LayerMask mousePlaneLayer;
    
    private GridPosition currentGridPosition;
    private Vector2 lastMousePosition;
    private float pointerMoveTimer;
    private const float POINTER_MOVE_SPEED = 0.15f;

    private LevelGrid activeGrid;
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Grid inicial
        activeGrid = LevelGrid.Instance;
        currentGridPosition = new GridPosition(activeGrid.GetWidth() / 2, activeGrid.GetHeight() / 2);
        UpdatePointerPosition();

        // Escuchamos el cambio reactivo de grid
        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.OnActiveGridChanged += HandleActiveGridChanged;
        }
    }

    private void HandleActiveGridChanged(LevelGrid newGrid)
    {
        RefreshGridContext(newGrid);
    }

    void Update()
    {
        Vector2 currentMousePosition = InputManager.Instance.GetMouseScreenPosition();
        
        // Prioridad 1: Ratón
        if (Vector2.Distance(currentMousePosition, lastMousePosition) > 0.5f)
        {
            Ray ray = Camera.main.ScreenPointToRay(currentMousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, mousePlaneLayer))
            {
                GridPosition targetPosition = activeGrid.GetGridPosition(hit.point);
                if (activeGrid.IsValidGridPosition(targetPosition))
                {
                    currentGridPosition = targetPosition;
                }
            }
        }
        // Prioridad 2: Mando/Teclado (8 Direcciones relativo a la cámara activa)
        else
        {
            pointerMoveTimer -= Time.deltaTime;
            if (pointerMoveTimer <= 0f)
            {
                Vector2 pointerInput = InputManager.Instance.GetPointerMoveVector();
                
                if (pointerInput.sqrMagnitude > 0.1f)
                {
                    pointerMoveTimer = POINTER_MOVE_SPEED;

                    Vector3 cameraForward = Camera.main.transform.forward;
                    cameraForward.y = 0;
                    cameraForward.Normalize();
                    
                    Vector3 cameraRight = Camera.main.transform.right;
                    cameraRight.y = 0;
                    cameraRight.Normalize();

                    Vector3 desiredWorldDir = (cameraForward * pointerInput.y + cameraRight * pointerInput.x).normalized;

                    GridPosition bestNeighbor = currentGridPosition;
                    float bestAlignment = -2f;

                    List<GridPosition> neighborOffsets = new List<GridPosition>
                    {
                        new GridPosition(1, 0), new GridPosition(-1, 0), new GridPosition(0, 1), new GridPosition(0, -1),
                        new GridPosition(1, 1), new GridPosition(1, -1), new GridPosition(-1, 1), new GridPosition(-1, -1)
                    };

                    Vector3 currentWorldPos = activeGrid.GetWorldPosition(currentGridPosition);

                    foreach (GridPosition offset in neighborOffsets)
                    {
                        GridPosition neighborPos = currentGridPosition + offset;
                        if (!activeGrid.IsValidGridPosition(neighborPos)) continue;

                        Vector3 neighborWorldPos = activeGrid.GetWorldPosition(neighborPos);
                        Vector3 directionToNeighbor = (neighborWorldPos - currentWorldPos).normalized;

                        float alignment = Vector3.Dot(desiredWorldDir, directionToNeighbor);
                        if (alignment > bestAlignment)
                        {
                            bestAlignment = alignment;
                            bestNeighbor = neighborPos;
                        }
                    }

                    if (bestAlignment > 0.38f) 
                    {
                        currentGridPosition = bestNeighbor;
                    }
                }
            }
            else if (InputManager.Instance.GetPointerMoveVector().sqrMagnitude < 0.1f)
            {
                pointerMoveTimer = 0f;
            }
        }

        lastMousePosition = currentMousePosition;
        UpdatePointerPosition();
    }

    private void UpdatePointerPosition()
    {
        Vector3 targetWorldPos = activeGrid.GetWorldPosition(currentGridPosition);
        transform.position = Vector3.Lerp(transform.position, targetWorldPos, Time.deltaTime * 20f);
    }
    
    public void RefreshGridContext(LevelGrid newGrid)
    {
        activeGrid = newGrid;

        // Aseguramos que la posición sea válida en el nuevo grid
        if (!activeGrid.IsValidGridPosition(currentGridPosition))
        {
            currentGridPosition = new GridPosition(activeGrid.GetWidth() / 2, activeGrid.GetHeight() / 2);
        }

        transform.position = activeGrid.GetWorldPosition(currentGridPosition);
    }

    public GridPosition GetGridPosition() => currentGridPosition;

    public void SnapToGridPosition(GridPosition newGridPosition)
    {
        if (activeGrid.IsValidGridPosition(newGridPosition))
        {
            currentGridPosition = newGridPosition;
            transform.position = activeGrid.GetWorldPosition(currentGridPosition);
        }
    }

    private void OnDestroy()
    {
        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.OnActiveGridChanged -= HandleActiveGridChanged;
        }
    }
}