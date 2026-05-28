using UnityEngine;

public class ToroidUnitMover : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    
    private ToroidLevelGrid toroidGrid;
    private GridPosition currentGridPosition;
    private GridPosition targetGridPosition;
    private Vector3 targetWorldPosition;
    private bool isMoving;

    private void Start()
    {
        // Buscamos de forma segura nuestra Grid Toroidal en la escena
        toroidGrid = FindFirstObjectByType<ToroidLevelGrid>();

        if (toroidGrid == null)
        {
            Debug.LogError("[TOROIDE] No se encontró ningún ToroidLevelGrid en la escena. ¡Asegúrate de haberlo cambiado en el inspector!");
            enabled = false;
            return;
        }
        
        // 1. Sincronizamos la posición física inicial exacta con la celda de origen
        currentGridPosition = toroidGrid.GetGridPosition(transform.position);
        transform.position = toroidGrid.GetWorldPosition(currentGridPosition);
        
        // 2. Arrancamos el ciclo infinito de caminata
        CalculateNextStep();
    }

    private void CalculateNextStep()
    {
        // Avanzamos 1 casilla hacia el norte en el espacio lógico
        GridPosition nextLogicalStep = currentGridPosition + new GridPosition(0, 1);
        
        // Obtenemos la casilla envuelta real del Toroide (Ej: si era 5, pasará a ser 0)
        targetGridPosition = toroidGrid.GetWrappedGridPosition(nextLogicalStep);
        
        if (nextLogicalStep != targetGridPosition)
        {
            // CASO LÍMITE (Desborde): 
            // Si la casilla da la vuelta, calculamos la posición en el mundo de la casilla "ficticia" 
            // (por ejemplo, la fila 5) para que la unidad camine físicamente hacia adelante cruzando el borde,
            // en lugar de intentar darse la vuelta de forma brusca hacia el origen (0).
            
            // Le pedimos al GridSystem original la dirección base restando dos casillas consecutivas
            Vector3 gridDirection = toroidGrid.GetWorldPosition(new GridPosition(0, 1)) - toroidGrid.GetWorldPosition(new GridPosition(0, 0));
            targetWorldPosition = toroidGrid.GetWorldPosition(currentGridPosition) + gridDirection;
        }
        else
        {
            // CASO NORMAL: La casilla está dentro de los límites del mapa
            targetWorldPosition = toroidGrid.GetWorldPosition(targetGridPosition);
        }

        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving) return;

        // Movimiento físico suave respetando las diagonales de tu grid original
        transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, speed * Time.deltaTime);

        // Al llegar milimétricamente al destino físico de la casilla...
        if (Vector3.Distance(transform.position, targetWorldPosition) < 0.01f)
        {
            isMoving = false; // Bloqueo de seguridad anti-ráfagas

            // Actualizamos la posición lógica actual a la casilla envuelta real
            currentGridPosition = targetGridPosition;

            // ¡EL WARP FÍSICO!
            // Teletransportamos instantáneamente el transform a la posición real del mundo de la nueva casilla.
            // Si estaba en el desborde visual, reaparecerá limpiamente en el inicio del mapa inclinado.
            transform.position = toroidGrid.GetWorldPosition(currentGridPosition);

            // Calculamos el siguiente paso para continuar el bucle
            CalculateNextStep();
        }
    }
}