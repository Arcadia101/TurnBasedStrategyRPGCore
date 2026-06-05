using System;
using System.Collections.Generic;
using UnityEngine;

public class ShootAction : BaseAction
{
    public static event EventHandler<OnShootEventArgs> OnAnyShoot;
    public event EventHandler<OnShootEventArgs> OnShoot;

    public class OnShootEventArgs : EventArgs
    {
        public Unit targetUnit;
        public Unit shootingUnit;
    }
    
    private enum State
    {
        Aiming,   // Fase de apuntado: Aquí el jugador cicla los objetivos en la casilla seleccionada
        Shooting, // Fase de disparo: Se ejecuta el daño y los efectos visuales
        Cooloff,  // Fase de recuperación o espera post-disparo
    }
    
    private State state = State.Aiming;
    private float stateTimer;
    private Unit targetUnit;
    private bool canShoot;

    [SerializeField] private LayerMask obstaclesLayerMask;
    [SerializeField] private int maxShootDistance = 7;
    [SerializeField] private float aimingTime = 1.5f;
    [SerializeField] private float shootTime = .1f;
    [SerializeField] private float cooloffTime = .5f;
    [SerializeField] private float rotateSpeed = 10f;

    // Guarda la última casilla apuntada para evitar recalcular la lista innecesariamente
    private GridPosition lastAimedGridPosition;

    private void Update()
    {
        // Si la acción no ha sido iniciada por el UnitActionSystem, no ejecutamos el Update
        if (!isActive) return;
        
        stateTimer -= Time.deltaTime;
        
        switch (state)
        {
            case State.Aiming:
                // Mientras estemos en la fase de Aiming, el targetUnit se actualiza en tiempo real
                // basándose en el índice de la lista protegida que modificamos con el ciclado.
                if (targetsInTile.Count > 0)
                {
                    targetUnit = targetsInTile[currentTargetIndex];
                }

                // Rotación suave de la unidad hacia el objetivo seleccionado actualmente
                if (targetUnit != null) 
                {
                    Vector3 aimDir = (targetUnit.GetWorldPosition() - unit.GetWorldPosition()).normalized;
                    transform.forward = Vector3.Lerp(transform.forward, aimDir, Time.deltaTime * rotateSpeed);
                }
                break;

            case State.Shooting:
                if (canShoot)
                {
                    Shoot();
                    canShoot = false;
                }
                break;

            case State.Cooloff:
                break;
        }
        
        // Bloqueamos el avance automático si estamos en Aiming (el jugador humano decide cuándo avanzar)
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
                state = State.Shooting;
                stateTimer = shootTime;
                break;
            case State.Shooting:
                state = State.Cooloff;
                stateTimer = cooloffTime;
                break;
            case State.Cooloff:
                // Finaliza la acción y devuelve el control al UnitActionSystem
                ActionComplete();
                break;
        }
    }

    private void Shoot()
    {
        OnAnyShoot?.Invoke(this, new OnShootEventArgs{targetUnit = targetUnit, shootingUnit = unit});
        OnShoot?.Invoke(this, new OnShootEventArgs{targetUnit = targetUnit, shootingUnit = unit});
        targetUnit.Damage(40);
    }
    
    public override string GetActionName()
    {
        return "Shoot";
    }

    // PASO 1 Y 2: El jugador confirmó la zona/casilla táctica.
    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        state = State.Aiming;
        stateTimer = aimingTime;
        canShoot = true;

        // Limpiamos y preparamos la lista genérica heredada de BaseAction
        targetsInTile.Clear();
        currentTargetIndex = 0;

        // Escaneamos la casilla para rellenar los objetivos disponibles
        List<Unit> unitsOnTile = GetGridContext().GetUnitListAtGridPosition(gridPosition);
        foreach (Unit tileUnit in unitsOnTile)
        {
            // Filtro: Solo unidades del equipo contrario (como mueren y se destruyen al instante, no hace falta comprobar vida)
            if (tileUnit.IsEnemy() != unit.IsEnemy())
            {
                targetsInTile.Add(tileUnit);
            }
        }

        // Asignamos el primer objetivo de la lista como predeterminado
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

    // PASO 4: El jugador presiona confirmar por segunda vez sobre el objetivo fijado.
    public bool ConfirmSelectedTarget()
    {
        if (state == State.Aiming && targetUnit != null)
        {
            // Avanzamos manualmente al estado de disparo
            NextState();
            return true;
        }
        return false;
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        GridPosition unitGridPosition = unit.GetGridPosition();
        return GetValidActionGridPositionList(unitGridPosition);
    }
    
    public List<GridPosition> GetValidActionGridPositionList(GridPosition unitGridPosition)
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        Vector3 unitWorldPosition = GetGridContext().GetWorldPosition(unitGridPosition);
        float unitShoulderHeight = 1.7f;
        
        for (int x = -maxShootDistance; x <= maxShootDistance; x++)
        {
            for (int z = -maxShootDistance; z <= maxShootDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!GetGridContext().IsValidGridPosition(testGridPosition)) continue;
                
                int testDistance = Math.Abs(x) + Math.Abs(z);
                if (testDistance > maxShootDistance) continue;
                
                if (!GetGridContext().HasAnyUnitOnGridPosition(testGridPosition)) continue;
                
                // Evaluamos si al menos una de las unidades en la casilla dual es un enemigo visible
                List<Unit> unitsAtGridPosition = GetGridContext().GetUnitListAtGridPosition(testGridPosition);
                bool foundValidTarget = false;

                foreach (Unit potentialTarget in unitsAtGridPosition)
                {
                    if (potentialTarget.IsEnemy() == unit.IsEnemy()) continue;

                    Vector3 shootDir = (potentialTarget.GetWorldPosition() - unitWorldPosition).normalized;
                    
                    // Raycast físico para verificar obstáculos y coberturas (Línea de Visión)
                    if (!Physics.Raycast(unitWorldPosition + Vector3.up * unitShoulderHeight, shootDir,
                            Vector3.Distance(unitWorldPosition, potentialTarget.GetWorldPosition()), obstaclesLayerMask))
                    {
                        foundValidTarget = true;
                        break; // Si hay al menos un enemigo visible, la casilla es válida para el Paso 1
                    }
                }

                if (foundValidTarget)
                {
                    validGridPositionList.Add(testGridPosition);
                }
            }
        }
        return validGridPositionList;
    }
    
    public int GetMaxShootDistance()
    {
        return maxShootDistance;
    }
    
    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        // La IA selecciona automáticamente al primer enemigo válido en la casilla y calcula su valor
        List<Unit> unitsAtGridPosition = GetGridContext().GetUnitListAtGridPosition(gridPosition);
        Unit bestTargetForAI = unitsAtGridPosition.Find(u => u.IsEnemy() != unit.IsEnemy());

        if (bestTargetForAI == null) return null;

        return new EnemyAIAction
        {
            gridPosition = gridPosition,
            actionValue = 100 + Mathf.RoundToInt((1 - bestTargetForAI.GetHealthNormalized()) * 100),
        };
    }

    public int GetTargetCountAtGridPosition(GridPosition gridPosition)
    {
        return GetValidActionGridPositionList(gridPosition).Count;
    }
    
    // --- CONEXIÓN DE INTERCEPCIÓN ---

    public override bool IsAwaitingTargetSelection()
    {
        // PASO 3: Mientras estemos ejecutando la acción y estemos en Aiming, desviamos L1/R1 / Tab
        return isActive && state == State.Aiming; 
    }

    public new Unit GetTargetUnit() 
    {
        return targetUnit;
    }
}