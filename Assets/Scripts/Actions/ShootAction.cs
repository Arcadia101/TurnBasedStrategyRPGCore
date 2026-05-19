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
        Aiming,
        Shooting,
        Cooloff,
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
    
    // --- VARIABLES DE CICLADO DE OBJETIVO ---
    private List<Unit> validTargetsInReticule = new List<Unit>();
    private int currentTargetIndex = 0;
    private GridPosition lastAimedGridPosition;

    private void Update()
    {
        // --- NUEVA LÓGICA DE PREVISUALIZACIÓN Y APUNTADO ---
        if (!isActive)
        {
            // Solo actualizamos la lista si estamos seleccionando objetivo
            GridPosition pointerGridPosition;
            if (InputManager.Instance.IsUsingMouse()) 
            {
                pointerGridPosition = LevelGrid.Instance.GetGridPosition(MouseWorld.GetPosition());
            }
            else 
            {
                // Asegúrate de que GridPointer tenga un Singleton o pasa la referencia correcta
                pointerGridPosition = GridPointer.Instance.GetGridPosition(); 
            }

            if (pointerGridPosition != lastAimedGridPosition)
            {
                lastAimedGridPosition = pointerGridPosition;
                UpdateTargetList(pointerGridPosition);
            }
            return; // Salimos para no ejecutar la lógica de combate aún
        }
        
        // --- LÓGICA ORIGINAL DE COMBATE ---
        stateTimer -= Time.deltaTime;
        
        switch (state)
        {
            case State.Aiming:
                // Verificamos que tengamos un target (por si acaso)
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
        
        if (stateTimer <= 0)
        {
            NextState();
        }
    }

    // --- NUEVO MÉTODO PARA LLENAR LA LISTA DE ENEMIGOS EN LA MIRA ---
    private void UpdateTargetList(GridPosition targetGridPosition)
    {
        validTargetsInReticule.Clear();
        currentTargetIndex = 0; 

        if (!IsValidActionGridPosition(targetGridPosition)) return;

        List<Unit> unitsOnTile = LevelGrid.Instance.GetUnitListAtGridPosition(targetGridPosition);
        
        foreach(Unit tileUnit in unitsOnTile)
        {
            // Comprobamos que sea del equipo contrario
            if (tileUnit.IsEnemy() != unit.IsEnemy()) 
            {
                validTargetsInReticule.Add(tileUnit);
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

    public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
    {
        // --- NUEVA LÓGICA DE ASIGNACIÓN DE OBJETIVO ---
        if (validTargetsInReticule.Count > 0)
        {
            // Si el jugador eligió un target con el ciclado, lo usamos
            targetUnit = validTargetsInReticule[currentTargetIndex];
        }
        else
        {
            // Fallback: Si la IA ejecuta esto (ya que la IA no mueve el mouse ni previsualiza),
            // buscamos el primer enemigo válido en esa casilla.
            List<Unit> unitsOnTile = LevelGrid.Instance.GetUnitListAtGridPosition(gridPosition);
            targetUnit = unitsOnTile.Find(u => u.IsEnemy() != unit.IsEnemy());
        }
        
        Debug.Log("Aiming at " + targetUnit);
        state = State.Aiming;
        stateTimer = aimingTime;
        canShoot = true;
        
        ActionStart(onActionComplete);
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        GridPosition unitGridPosition = unit.GetGridPosition();
        return GetValidActionGridPositionList(unitGridPosition);
    }
    
    public List<GridPosition> GetValidActionGridPositionList(GridPosition unitGridPosition)
    {
        List<GridPosition> validGridPositionList = new List<GridPosition>();
        Vector3 unitWorldPosition = LevelGrid.Instance.GetWorldPosition(unitGridPosition);
        float unitShoulderHeight = 1.7f;
        
        for (int x = -maxShootDistance; x <= maxShootDistance; x++)
        {
            for (int z = -maxShootDistance; z <= maxShootDistance; z++)
            {
                GridPosition offsetGridPosition = new GridPosition(x, z);
                GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition)) continue;
                
                int testDistance = Math.Abs(x) + Math.Abs(z);
                if (testDistance > maxShootDistance) continue;
                
                if (!LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition)) continue;
                
                // --- NUEVA LÓGICA: Evaluar múltiples unidades en la casilla ---
                List<Unit> unitsAtGridPosition = LevelGrid.Instance.GetUnitListAtGridPosition(testGridPosition);
                bool foundValidTarget = false;

                foreach (Unit potentialTarget in unitsAtGridPosition)
                {
                    // Si es del mismo equipo, la ignoramos
                    if (potentialTarget.IsEnemy() == unit.IsEnemy()) continue;

                    Vector3 shootDir = (potentialTarget.GetWorldPosition() - unitWorldPosition).normalized;
                    
                    // Comprobación de obstáculos
                    if (!Physics.Raycast(unitWorldPosition + Vector3.up * unitShoulderHeight, shootDir,
                            Vector3.Distance(unitWorldPosition, potentialTarget.GetWorldPosition()), obstaclesLayerMask))
                    {
                        // Hay línea de visión directa a al menos un enemigo en esta casilla
                        foundValidTarget = true;
                        break; 
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
    
    public Unit GetTargetUnit()
    {
        // Si aún no disparamos, mostramos al target que tenemos en la mira (útil para la UI)
        if (!isActive && validTargetsInReticule.Count > 0)
        {
            return validTargetsInReticule[currentTargetIndex];
        }
        
        // Si ya estamos disparando, devolvemos el target fijado
        return targetUnit;
    }

    public int GetMaxShootDistance()
    {
        return maxShootDistance;
    }
    
    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        // --- NUEVA LÓGICA: Encontrar el objetivo para la IA ---
        List<Unit> unitsAtGridPosition = LevelGrid.Instance.GetUnitListAtGridPosition(gridPosition);
        Unit bestTargetForAI = unitsAtGridPosition.Find(u => u.IsEnemy() != unit.IsEnemy());

        if (bestTargetForAI == null) return null; // Por seguridad

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
    
    // --- MÉTODOS SOBRESCRITOS DE BASEACTION PARA EL CICLADO ---
    public override bool IsAwaitingTargetSelection()
    {
        // Devuelve true si la acción está seleccionada en la UI, pero el personaje aún no está disparando
        return IsActionActive() && !isActive; 
    }

    public override void CycleTarget(int direction)
    {
        if (validTargetsInReticule.Count <= 1) return; // Si hay 0 o 1 enemigo, no hay nada que ciclar

        // Actualizamos el índice usando la fórmula para que sea un ciclo infinito
        currentTargetIndex = (currentTargetIndex + direction + validTargetsInReticule.Count) % validTargetsInReticule.Count;

        // EJEMPLO FUTURO: Llamar al evento para actualizar la UI
        // OnTargetCycled?.Invoke(validTargetsInReticule[currentTargetIndex]);
    }
}