using System;
using System.Collections.Generic;
using UnityEngine;

public class HealAction : BaseAction
{
    // Eventos específicos de la curación para el sistema de partículas o sonidos
    public static event EventHandler<OnHealEventArgs> OnAnyHeal;
    public event EventHandler<OnHealEventArgs> OnHeal;
    
    private float totalSpinAmount; //testing spin animation

    public class OnHealEventArgs : EventArgs
    {
        public Unit targetUnit;
        public Unit healingUnit;
    }
    
    private enum State
    {
        Aiming,      // Fase de selección: El jugador rota y cicla aliados en la casilla elegida
        Healing,     // Fase de ejecución: Se aplica la restauración de vida y los efectos visuales
        Cooloff,     // Fase de espera post-acción
    }
    
    private State state = State.Aiming;
    private float stateTimer;
    private Unit targetUnit;
    private bool canHeal;

    // Configuración de la acción en el Inspector
    [SerializeField] private int maxHealDistance = 5;
    [SerializeField] private int healAmount = 30;
    [SerializeField] private float aimingTime = 0.1f;
    [SerializeField] private float healTime = .1f;
    [SerializeField] private float cooloffTime = .5f;
    [SerializeField] private float rotateSpeed = 10f;

    private void Update()
    {
        // Si la acción no ha sido iniciada por el UnitActionSystem, no hacemos nada
        if (!isActive) return;
        
        stateTimer -= Time.deltaTime;
        
        switch (state)
        {
            case State.Aiming:
                // Sincronizamos constantemente el targetUnit con el índice que el jugador cicla en BaseAction
                if (targetsInTile.Count > 0)
                {
                    targetUnit = targetsInTile[currentTargetIndex];
                }

                // Rotación suave de la unidad hacia el aliado que está curando
                if (targetUnit != null) 
                {
                    Vector3 aimDir = (targetUnit.GetWorldPosition() - unit.GetWorldPosition()).normalized;
                    transform.forward = Vector3.Lerp(transform.forward, aimDir, Time.deltaTime * rotateSpeed);
                }
                break;

            case State.Healing:
                if (canHeal)
                {
                    ExecuteHeal();
                    float speedAddAmount = 360 * Time.deltaTime;
                    transform.eulerAngles += new Vector3(0, speedAddAmount, 0);
        
                    totalSpinAmount += speedAddAmount;
                    if (totalSpinAmount >= 360)
                    {
                        ActionComplete();
                    }
                    canHeal = false;
                }
                break;

            case State.Cooloff:
                break;
        }
        
        // El estado Aiming se bloquea para el jugador humano; avanza manualmente con ConfirmSelectedTarget
        // --- CANDADO INTELIGENTE DE AVANCE DE ESTADO ---
        // Avanzamos automáticamente si el tiempo se agota, A MENOS que tengamos múltiples objetivos 
        // y el temporizador esté congelado esperando la decisión del jugador humano.
        if (stateTimer <= 0)
        {
            if (state != State.Aiming || targetsInTile.Count <= 1)
            {
                NextState();
            }
        }
    }

    private void NextState()
    {
        switch (state)
        {
            case State.Aiming:
                state = State.Healing;
                stateTimer = healTime;
                break;
            case State.Healing:
                state = State.Cooloff;
                stateTimer = cooloffTime;
                break;
            case State.Cooloff:
                // Finaliza la acción y dispara el evento BaseAction.OnAnyActionCompleted de forma nativa
                ActionComplete();
                break;
        }
    }

    private void ExecuteHeal()
    {
        // Disparamos los eventos de curación
        OnAnyHeal?.Invoke(this, new OnHealEventArgs { targetUnit = targetUnit, healingUnit = unit });
        OnHeal?.Invoke(this, new OnHealEventArgs { targetUnit = targetUnit, healingUnit = unit });
        
        // Aplicamos la restauración de salud al aliado (Asegúrate de que tu script Unit tenga un método Heal o similar)
        // Si tu método se llama diferente (ej: RestoreHealth), cámbialo aquí.
        targetUnit.Heal(healAmount); 
    }
    
    public override string GetActionName()
    {
        return "Heal";
    }

    // PASO 1 Y 2: El jugador seleccionó una casilla táctica válida que contiene aliados
    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        state = State.Aiming;
        stateTimer = aimingTime;
        canHeal = true;

        // Preparamos la lista genérica de BaseAction
        targetsInTile.Clear();
        currentTargetIndex = 0;

        // Escaneamos la casilla para buscar objetivos válidos
        List<Unit> unitsOnTile = GetGridContext().GetUnitListAtGridPosition(gridPosition);
        foreach (Unit tileUnit in unitsOnTile)
        {
            // NUEVO FILTRO DE SOPORTE: Solo unidades del MISMO bando/equipo
            if (tileUnit.IsEnemy() == unit.IsEnemy())
            {
                targetsInTile.Add(tileUnit);
            }
        }

        // Fijamos el primer aliado de la lista por defecto
        if (targetsInTile.Count > 0)
        {
            targetUnit = targetsInTile[currentTargetIndex];
        }
        else
        {
            targetUnit = null;
        }

        // LÓGICA DE DETECCIÓN AUTOMÁTICA
        if (!unit.IsEnemy())
        {
            if (targetsInTile.Count > 1)
            {
                // Caso A: Hay múltiples objetivos en la celda dual.
                // Congelamos el tiempo para permitir el ciclado con el mando.
                stateTimer = float.MaxValue; 
            }
            else
            {
                // Caso B: Solo hay 1 objetivo (o ninguno, por seguridad).
                // No bloqueamos al jugador; el juego usará el 'aimingTime' normal
                // para que la unidad gire hacia el objetivo y dispare/cure automáticamente
                // al agotarse el tiempo, sin requerir un segundo clic.
                stateTimer = aimingTime; 
            }
        }

        ActionStart(onActionComplete);
    }

    // PASO 4: El jugador presiona confirmar por segunda vez sobre el aliado seleccionado
    public bool ConfirmSelectedTarget()
    {
        if (state == State.Aiming && targetUnit != null)
        {
            NextState();
            return true;
        }
        return false;
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        GridPosition unitGridPosition = unit.GetGridPosition();

        for (int x = -maxHealDistance; x <= maxHealDistance; x++)
        {
            for (int z = -maxHealDistance; z <= maxHealDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!GetGridContext().IsValidGridPosition(testGridPosition)) continue;
                
                int testDistance = Math.Abs(x) + Math.Abs(z);
                if (testDistance > maxHealDistance) continue;
                
                // Si no hay ninguna unidad en la casilla, no nos interesa para curar
                if (!GetGridContext().HasAnyUnitOnGridPosition(testGridPosition)) continue;
                
                List<Unit> unitsAtGridPosition = GetGridContext().GetUnitListAtGridPosition(testGridPosition);
                bool foundValidAlly = false;

                foreach (Unit potentialAlly in unitsAtGridPosition)
                {
                    // Regla: Tiene que ser del mismo equipo
                    if (potentialAlly.IsEnemy() == unit.IsEnemy())
                    {
                        // Como acordamos, no tiramos Physics.Raycast aquí porque la curación ignorará obstáculos físicos
                        foundValidAlly = true;
                        break; 
                    }
                }

                if (foundValidAlly)
                {
                    validGridPositionList.Add(testGridPosition);
                }
            }
        }
        return validGridPositionList;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        // Lógica para que la Inteligencia Artificial enemiga también pueda curar a sus aliados
        List<Unit> unitsAtGridPosition = GetGridContext().GetUnitListAtGridPosition(gridPosition);
        Unit bestTargetForAI = unitsAtGridPosition.Find(u => u.IsEnemy() == unit.IsEnemy());

        if (bestTargetForAI == null) return null;

        // La IA priorizará curar a las casillas donde los aliados tengan menos porcentaje de vida
        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = Mathf.RoundToInt((1 - bestTargetForAI.GetHealthNormalized()) * 100),
        };
    }

    // --- CONEXIÓN DE INTERCEPCIÓN (SEMI-BUSY) ---

    public override bool IsAwaitingTargetSelection()
    {
        return isActive && state == State.Aiming; 
    }

    public new Unit GetTargetUnit() 
    {
        return targetUnit;
    }
}
