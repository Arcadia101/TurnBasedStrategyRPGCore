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
    private const float POINTER_MOVE_SPEED = 0.15f; // Ligeramente más rápido para un control de 8 direcciones más ágil

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There is already a GridPointer in the scene!");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        currentGridPosition = new GridPosition(LevelGrid.Instance.GetWidth() / 2, LevelGrid.Instance.GetHeight() / 2);
        UpdatePointerPosition();
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
                GridPosition targetPosition = LevelGrid.Instance.GetGridPosition(hit.point);
                if (LevelGrid.Instance.IsValidGridPosition(targetPosition))
                {
                    currentGridPosition = targetPosition;
                }
            }
        }
        // Prioridad 2: Mando/Teclado (Relativo a la Cámara y en 8 Direcciones)
        else
        {
            pointerMoveTimer -= Time.deltaTime;
            if (pointerMoveTimer <= 0f)
            {
                Vector2 pointerInput = InputManager.Instance.GetPointerMoveVector();
                
                if (pointerInput.sqrMagnitude > 0.1f)
                {
                    pointerMoveTimer = POINTER_MOVE_SPEED;

                    // 1. Convertir el input 2D en una dirección 3D basada en hacia dónde mira la cámara
                    Vector3 cameraForward = Camera.main.transform.forward;
                    cameraForward.y = 0; // Ignoramos la inclinación hacia abajo/arriba de la cámara
                    cameraForward.Normalize();
                    
                    Vector3 cameraRight = Camera.main.transform.right;
                    cameraRight.y = 0;
                    cameraRight.Normalize();

                    // La dirección en el mundo hacia la que el jugador quiere mover el cursor
                    Vector3 desiredWorldDir = (cameraForward * pointerInput.y + cameraRight * pointerInput.x).normalized;

                    // 2. Buscar cuál de las 8 casillas vecinas se alinea mejor con esa dirección
                    GridPosition bestNeighbor = currentGridPosition;
                    float bestAlignment = -2f; // El mínimo posible del Dot Product es -1

                    // Los 8 offsets posibles (Cardinales y Diagonales en el Grid)
                    List<GridPosition> neighborOffsets = new List<GridPosition>
                    {
                        new GridPosition(1, 0), new GridPosition(-1, 0), new GridPosition(0, 1), new GridPosition(0, -1),
                        new GridPosition(1, 1), new GridPosition(1, -1), new GridPosition(-1, 1), new GridPosition(-1, -1)
                    };

                    Vector3 currentWorldPos = LevelGrid.Instance.GetWorldPosition(currentGridPosition);

                    foreach (GridPosition offset in neighborOffsets)
                    {
                        GridPosition neighborPos = currentGridPosition + offset;
                        
                        if (!LevelGrid.Instance.IsValidGridPosition(neighborPos)) continue;

                        Vector3 neighborWorldPos = LevelGrid.Instance.GetWorldPosition(neighborPos);
                        Vector3 directionToNeighbor = (neighborWorldPos - currentWorldPos).normalized;

                        // El Dot Product nos dice cuán alineadas están dos direcciones (1 es perfecto, 0 es perpendicular, -1 es opuesto)
                        float alignment = Vector3.Dot(desiredWorldDir, directionToNeighbor);

                        if (alignment > bestAlignment)
                        {
                            bestAlignment = alignment;
                            bestNeighbor = neighborPos;
                        }
                    }

                    // 3. Mover a ese vecino si la intención es clara
                    // 0.38f asegura que se mueva incluso si el joystick no está perfectamente alineado
                    if (bestAlignment > 0.38f) 
                    {
                        currentGridPosition = bestNeighbor;
                    }
                }
            }
            else if (InputManager.Instance.GetPointerMoveVector().sqrMagnitude < 0.1f)
            {
                // Reseteamos el temporizador al soltar para que la respuesta sea inmediata la próxima vez
                pointerMoveTimer = 0f;
            }
        }

        lastMousePosition = currentMousePosition;
        UpdatePointerPosition();
    }

    private void UpdatePointerPosition()
    {
        Vector3 targetWorldPos = LevelGrid.Instance.GetWorldPosition(currentGridPosition);
        transform.position = Vector3.Lerp(transform.position, targetWorldPos, Time.deltaTime * 20f); // Un Lerp un poco más rápido para las 8 direcciones
    }

    public GridPosition GetGridPosition()
    {
        return currentGridPosition;
    }

    public void SnapToGridPosition(GridPosition newGridPosition)
    {
        if (LevelGrid.Instance.IsValidGridPosition(newGridPosition))
        {
            currentGridPosition = newGridPosition;
            transform.position = LevelGrid.Instance.GetWorldPosition(currentGridPosition);
        }
    }
}
