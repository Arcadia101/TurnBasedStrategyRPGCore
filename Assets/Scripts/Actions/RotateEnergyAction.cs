using System;
using System.Collections.Generic;
using UnityEngine;

public class RotateEnergyAction : BaseAction
{
    private enum State
    {
        Rotating, // El jugador está ajustando el ángulo de su energía
        Finish    // Cierre de la acción
    }

    private State state;
    private UnitEnergy unitEnergy;

    protected override void Awake()
    {
        base.Awake();
        unitEnergy = GetComponent<UnitEnergy>();
    }

    private void Update()
    {
        // Si la acción no ha sido iniciada por el ActionSystem, no hacemos nada
        if (!isActive) return;

        switch (state)
        {
            case State.Rotating:
                // Durante este estado, el juego se detiene aquí.
                // El jugador humano puede presionar botones para rotar o el botón de confirmar.
                break;

            case State.Finish:
                // Finaliza la acción de forma segura en la arquitectura global
                ActionComplete();
                break;
        }
    }

    public override void TakeAction(GridPosition targetGridPosition, Action onActionComplete)
    {
        // Iniciamos la máquina de estados en fase de rotación libre
        state = State.Rotating;

        ActionPointsCost = 0;
        
        ActionStart(onActionComplete);
    }
    
    public override void CycleTarget(int cycleDirection)
    {
        // Reutilizamos el método que ya valida y gasta Move Points
        RotateInput(cycleDirection);
    }

    
    // Método público que llamará tu ActionSystem o sistema de Input (ej: presionar flecha derecha o L1/R1).
    // <param name="steps">1 para rotar 45° a la derecha, -1 para la izquierda.</param>
    public bool RotateInput(int steps)
    {
        if (state != State.Rotating) return false;
    
        ActionPointsCost = 1;
        
        // Le preguntamos a la unidad si tiene puntos de movimiento disponibles para costear ESTE giro individual.
        // Como tu método centralizado cobra automáticamente al validar con éxito, lo llamamos directamente:
        if (unit.TrySpendActionPointsOrMovePointsToTakeActionOrMove(this))
        {
            // Si el cobro fue exitoso, aplicamos el giro real en las esferas
            unitEnergy.RotateEnergy(steps);
            ActionPointsCost = 0;
            return true;
        }

        Debug.LogWarning("[ROTAR ENERGÍA] No te quedan más Move Points para seguir rotando.");
        ActionPointsCost = 0;
        return false;
    }

    /// <summary>
    /// PASO DE CONFIRMACIÓN: El jugador presiona "Aceptar" (equivalente al ConfirmSelectedTarget de ShootAction).
    /// </summary>
    public bool ConfirmRotation()
    {
        if (state == State.Rotating)
        {
            // Avanzamos al estado de cierre
            state = State.Finish;
            ActionComplete(); // Opcional si quieres pasar directo, o dejas que el Update lo limpie
            return true;
        }
        return false;
    }

    // --- CONEXIÓN DE INTERCEPCIÓN DE CONTROLES (Igual que en ShootAction) ---

    public override bool IsAwaitingTargetSelection()
    {
        // Mientras la acción esté activa y estemos rotando, desviamos el Input de la interfaz hacia esta acción
        return isActive && state == State.Rotating;
    }

    public override string GetActionName()
    {
        return "Rotate";
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        // Al ser una acción que se ejecuta sobre uno mismo, la única casilla válida para hacer "click"
        // es la posición actual en la que está parada la unidad.
        return new List<GridPosition> { unit.GetGridPosition() };
    }
    

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        return new EnemyAIAction()
        {
            gridPosition = gridPosition,
            actionValue = 100,
        };
    }
}
