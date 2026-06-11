using System;
using UnityEngine;

public class ToroidMirrorIA : MonoBehaviour
{
    [Header("Referencias de las Unidades")]
    [SerializeField] private Unit bossUnitGrid1; // El Jefe original en la Grid 1 (Normal)
    [SerializeField] private Unit cloneUnitGrid2; // La pieza de Lego estándar en la Grid 2 (Toroide)

    [Header("Conexiones de Sistemas Futuros")]
    // Espacio reservado para el componente de gestión de energías que implementaremos más adelante.
    // De momento se queda declarado para dejar el gancho de arquitectura listo.
    private MonoBehaviour energySystemClone; 

    private void Start()
    {
        // 1. FILTRO DE SEGURIDAD: Validamos que las piezas esenciales estén conectadas en el Inspector
        if (bossUnitGrid1 == null || cloneUnitGrid2 == null)
        {
            Debug.LogError($"[{gameObject.name}] ToroidMirrorIA: Faltan referencias críticas por asignar en el Inspector.");
            return;
        }

        // 2. GANCHO DE ENERGÍAS (Tentativo para el futuro)
        // Aquí buscaremos el componente de energía una vez esté creado (ej: cloneUnitGrid2.GetComponent<EnergySystem>())
        // energySystemClone = cloneUnitGrid2.GetComponent<MonoBehaviour>();

        // 3. SUSCRIPCIÓN AL EVENTO GLOBAL DE MOVIMIENTO
        // Escuchamos el LevelGrid base (Grid 1). Cada vez que cualquier unidad termine de moverse lógicamente,
        // se disparará el evento y nuestro método interceptará la señal.
        if (LevelGrid.Instance != null)
        {
            LevelGrid.Instance.OnAnyUnitMovedGridPosition += LevelGrid_OnAnyUnitMovedGridPosition;
        }

        // --- CONEXIÓN FUTURA DE COMBATE (Estilo Pokémon) ---
        // Aquí nos colgaremos del evento de daño o selección de habilidad del jefe de la Grid 1
        // bossUnitGrid1.OnActionExecuted += BossUnitGrid1_OnActionExecuted;
    }

    /// <summary>
    /// Manejador del evento de movimiento global de la Grid 1.
    /// Se ejecuta silenciosamente tras bambalinas en cuanto una unidad cambia de celda.
    /// </summary>
    private void LevelGrid_OnAnyUnitMovedGridPosition(object sender, EventArgs e)
    {
        // 1. VALIDACIÓN DE IDENTIDAD: Verificamos si la unidad que disparó el evento es nuestro Jefe
        // Si el jefe no es la unidad que se acaba de mover en la Grid 1, ignoramos por completo el evento.
        if (bossUnitGrid1 == null || sender as Unit != bossUnitGrid1) 
        {
            return;
        }

        // 2. EXTRAER COORDENADAS LOGICAS DE LA GRID 1
        // Ya es seguro que el Jefe se movió. Le pedimos su posición de celda actual en la Grid 1.
        GridPosition bossCurrentGridPosition = bossUnitGrid1.GetGridPosition();

        // 3. TRADUCCIÓN MODULAR PARA LA GRID 2 (Mecánica Pac-Man)
        // Pasamos la coordenada de la Grid 1 por el filtro toroidal para obtener el índice real arriba.
        if (ToroidLevelGrid.ToroidInstance != null)
        {
            GridPosition mirrorTargetGridPosition = ToroidLevelGrid.ToroidInstance.GetWrappedGridPosition(bossCurrentGridPosition);

            // Convertimos esa celda de la Grid 2 en su posición física real en metros (añadiendo el offset de altura)
            Vector3 mirrorWorldPosition = ToroidLevelGrid.ToroidInstance.GetWorldPosition(mirrorTargetGridPosition);

            // 4. TELETRANSPORTE INSTANTÁNEO Y SINCRONIZACIÓN DE ESTADO
            // Guardamos la celda vieja donde estaba parado el clon en la Grid 2 antes del salto
            GridPosition cloneOldGridPosition = cloneUnitGrid2.GetGridPosition();

            // Desplazamos físicamente el Transform en el espacio 3D de forma instantánea (el jugador nunca ve esto)
            cloneUnitGrid2.transform.position = mirrorWorldPosition;

            // Forzamos la actualización de los diccionarios lógicos de la Grid 2 de manera manual y silenciosa.
            // Esto asegura que la Grid 2 sepa exactamente en qué celda está el clon para cuando el jugador regrese.
            ToroidLevelGrid.ToroidInstance.UnitMovedGridPosition(cloneUnitGrid2, cloneOldGridPosition, mirrorTargetGridPosition);

            // 5. GANCHO DE ENERGÍAS POST-MOVIMIENTO (Futuro)
            if (energySystemClone != null)
            {
                // Ejemplo: Invocación del consumo o reducción de energía del clon tras el movimiento espejo
                // energySystemClone.ExecuteEnergyDeduction();
            }

            Debug.Log($"[ESPEJO] Clon relocalizado silenciosamente en la Grid 2 en la celda toroidal: {mirrorTargetGridPosition}");
        }
    }

    /// <summary>
    /// Manejador de eventos futuro para las respuestas de combate interactivas (Estilo Pokémon).
    /// </summary>
    private void BossUnitGrid1_OnActionExecuted(object sender, EventArgs e)
    {
        // LÓGICA FUTURA PARA CONTRARRESTAR ATAQUES:
        // 1. Detectar qué habilidad/ataque usó el jugador contra el jefe en la Grid 1.
        // 2. Evaluar el pool de contraataques válidos del clon en la Grid 2.
        // 3. Inyectar de forma directa la respuesta óptima en la cola del turno actual.
    }

    private void OnDestroy()
    {
        // LIMPIEZA DE MEMORIA: Descolgamos el cable del evento global al destruir o apagar el objeto
        // para evitar fugas de memoria (Memory Leaks) en Unity al cambiar de escena.
        if (LevelGrid.Instance != null)
        {
            LevelGrid.Instance.OnAnyUnitMovedGridPosition -= LevelGrid_OnAnyUnitMovedGridPosition;
        }
        
        // if (bossUnitGrid1 != null) bossUnitGrid1.OnActionExecuted -= BossUnitGrid1_OnActionExecuted;
    }
}