using System;
using System.Collections.Generic;
using UnityEngine;

public class ToroidMirrorIA : MonoBehaviour
{
    [Header("Referencias de las Unidades")]
    [SerializeField] private Unit bossUnitGrid1; // El Jefe original en la Grid 1 (Normal)
    [SerializeField] private Unit cloneUnitGrid2; // El Clon estándar en la Grid 2 (Toroide)

    private MoveAction bossMoveAction; // La referencia directa a la acción del Jefe
    private UnitMotor cloneUnitMotor;  // El motor del clon para sincronizar su target físico

    private void Start()
    {
        // 1. FILTRO DE SEGURIDAD: Validamos referencias en el Inspector
        if (bossUnitGrid1 == null || cloneUnitGrid2 == null)
        {
            Debug.LogError($"[{gameObject.name}] ToroidMirrorIA: Faltan referencias críticas en el Inspector.");
            return;
        }

        // Marcar el clon como unidad espejo para que EnemyAI lo ignore
        cloneUnitGrid2.IsMirrorClone = true;

        // Obtener el motor del clon para poder notificarle los saltos de posición
        cloneUnitMotor = cloneUnitGrid2.GetComponent<UnitMotor>();

        // 2. EL AISLAMIENTO: Buscamos el MoveAction EXCLUSIVAMENTE en la instancia del Jefe
        bossMoveAction = bossUnitGrid1.GetAction<MoveAction>();

        if (bossMoveAction != null)
        {
            // Nos colgamos ÚNICAMENTE del evento de parada del Jefe.
            // Esto es inmune a lo que hagan las demás unidades del escenario.
            bossMoveAction.OnStopMoving += BossMoveAction_OnStopMoving;
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] ToroidMirrorIA: El Jefe asignado no tiene un componente MoveAction.");
        }
    }

    /// <summary>
    /// Este método se disparará UNA SOLA VEZ por turno, justo cuando el Jefe consolida 
    /// su posición física final en el suelo de la Grid 1.
    /// </summary>
    private void BossMoveAction_OnStopMoving(object sender, EventArgs e)
    {
        if (bossUnitGrid1 == null || cloneUnitGrid2 == null) return;

        // 1. EXTRAER DESTINO CONSOLIDADO
        GridPosition bossFinalGridPos = bossUnitGrid1.GetGridPosition();

        // 2. TRADUCCIÓN MODULAR (Matemática de la Grid 2)
        if (ToroidLevelGrid.ToroidInstance != null)
        {
            // Convertimos la casilla plana a la coordenada envuelta del Toroide
            GridPosition mirrorTargetGridPos = ToroidLevelGrid.ToroidInstance.GetWrappedGridPosition(bossFinalGridPos);

            // Obtenemos el vector físico en metros de la Grid 2 (con su respectiva altura)
            Vector3 mirrorWorldPosition = ToroidLevelGrid.ToroidInstance.GetWorldPosition(mirrorTargetGridPos);

            // 3. TELETRANSPORTE FÍSICO Y LÓGICO
            GridPosition cloneOldGridPos = cloneUnitGrid2.GetGridPosition();

            // 4. Teletransportamos el Transform físicamente arriba
            cloneUnitGrid2.transform.position = mirrorWorldPosition;

            // 5. Sincronizamos los diccionarios de ocupación de la Grid 2
            ToroidLevelGrid.ToroidInstance.UnitMovedGridPosition(cloneUnitGrid2, cloneOldGridPos, mirrorTargetGridPos);

            // 6. ¡EL CABLE CORRECTOR! Pacificamos el motor del clon
            // Le pasamos una lista con su propia nueva posición en metros. 
            // Al llamar a StartMovement, el motor limpia su 'targetPosition' viejo, 
            // lo sobreescribe con el vector del warp y se da por satisfecho sin intentar regresar.
            if (cloneUnitMotor != null)
            {
                List<Vector3> singleWorldPos = new List<Vector3> { mirrorWorldPosition };
                List<GridPosition> singleGridPos = new List<GridPosition> { mirrorTargetGridPos };
    
                cloneUnitMotor.StartMovement(singleWorldPos, singleGridPos, () => {
                    // Callback vacío: no requiere caminar ni animar porque ya está ahí
                });
            }
            
            cloneUnitGrid2. SetGridPosition( mirrorTargetGridPos);
            Debug.Log($"[ESPEJO AISLADO] El Jefe terminó su MoveAction. Clon reposicionado con éxito en la celda toroidal: {mirrorTargetGridPos}");
        }
    }

    private void OnDestroy()
    {
        // LIMPIEZA DE MEMORIA LOCAL: Descolgamos el cable únicamente de la acción del jefe
        if (bossMoveAction != null)
        {
            bossMoveAction.OnStopMoving -= BossMoveAction_OnStopMoving;
        }
    }
}