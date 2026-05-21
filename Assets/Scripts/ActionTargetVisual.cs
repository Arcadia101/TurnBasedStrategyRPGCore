using System;
using UnityEngine;

public class ActionTargetVisual : MonoBehaviour
{
    [SerializeField] private GameObject reticlePrefab; // El clon visual (flecha, círculo, etc.)
    [SerializeField] private Vector3 offset = new Vector3(0, 2.2f, 0); // Altura sobre la cabeza de la unidad
    [SerializeField] private float MoveSpeed = 15f; // Velocidad de interpolación de la retícula

    private GameObject instantiatedReticle;
    private Unit currentVisualTarget;

    private void Start()
    {
        // Instanciamos la retícula apagada al inicio
        instantiatedReticle = Instantiate(reticlePrefab, Vector3.zero, Quaternion.identity);
        instantiatedReticle.SetActive(false);

        // Nos suscribimos a los eventos del sistema de acciones y del ciclado genérico
        UnitActionSystem.Instance.OnActionStarted += UnitActionSystem_OnActionStarted;
        BaseAction.OnAnyActionCompleted += BaseAction_OnAnyActionCompleted;
        BaseAction.OnAnyTargetCycled += BaseAction_OnAnyTargetCycled;
    }

    private void Update()
    {
        // Si hay un objetivo visual activo, hacemos que la retícula lo siga suavemente (Lerp)
        // Esto se ve genial si la unidad se está moviendo o respirando con su animación idle
        if (instantiatedReticle.activeSelf && currentVisualTarget != null)
        {
            Vector3 targetPosition = currentVisualTarget.GetWorldPosition() + offset;
            instantiatedReticle.transform.position = Vector3.Lerp(instantiatedReticle.transform.position, targetPosition, Time.deltaTime * MoveSpeed);
        }
    }

    private void UpdateReticleTarget()
    {
        // Le preguntamos al UnitActionSystem cuál es la acción seleccionada actualmente
        BaseAction activeAction = UnitActionSystem.Instance.GetSelectedAction();

        // Si la acción está activamente esperando la selección de un blanco (State.Aiming)...
        if (activeAction != null && activeAction.IsAwaitingTargetSelection())
        {
            // Obtenemos la unidad apuntada actualmente en el índice gracias al método que limpiamos ayer
            Unit targetUnit = activeAction.GetTargetUnit();

            if (targetUnit != null)
            {
                currentVisualTarget = targetUnit;
                
                // Si la retícula estaba apagada, la encendemos y la posicionamos instantáneamente
                if (!instantiatedReticle.activeSelf)
                {
                    instantiatedReticle.transform.position = currentVisualTarget.GetWorldPosition() + offset;
                    instantiatedReticle.SetActive(true);
                }
                return;
            }
        }

        // Si no se cumple ninguna condición, nos aseguramos de apagar el visual
        HideReticle();
    }

    private void HideReticle()
    {
        currentVisualTarget = null;
        if (instantiatedReticle != null)
        {
            instantiatedReticle.SetActive(false);
        }
    }

    // --- SUSCRIPCIÓN A EVENTOS ---

    private void UnitActionSystem_OnActionStarted(object sender, EventArgs e)
    {
        // Justo cuando se presiona el primer confirmar y entramos a Aiming, actualizamos el visual
        UpdateReticleTarget();
    }

    private void BaseAction_OnAnyActionCompleted(object sender, EventArgs e)
    {
        // Cuando el tiro sale y volvemos al estado normal, escondemos la retícula
        HideReticle();
    }

    private void BaseAction_OnAnyTargetCycled(object sender, EventArgs e)
    {
        // Cada vez que el jugador pulsa L1/R1 o Tab, actualizamos a qué unidad debe mirar el script
        UpdateReticleTarget();
    }

    private void OnDestroy()
    {
        // Desuscripción limpia de eventos para evitar fugas de memoria
        UnitActionSystem.Instance.OnActionStarted -= UnitActionSystem_OnActionStarted;
        BaseAction.OnAnyActionCompleted -= BaseAction_OnAnyActionCompleted;
        BaseAction.OnAnyTargetCycled -= BaseAction_OnAnyTargetCycled;
    }
}