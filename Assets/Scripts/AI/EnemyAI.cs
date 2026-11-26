using System;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private enum State
    {
        WaitingForEnemyTurn,
        TakingTurn,
        Busy,
    }

    private State state;
    private float timer;

    private void Awake()
    {
        state = State.WaitingForEnemyTurn;
    }

    private void Start()
    {
        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
    }

    void Update()
    {
        if (TurnSystem.Instance.IsPlayerTurn())
        {
            return;
        }

        switch (state)
        {
            case State.WaitingForEnemyTurn:
                break;
            case State.TakingTurn:
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    if (TryTakeEnemyAIAction(SetStateTakingTurn))
                    {
                        state = State.Busy;
                    }
                    else
                    {
                        // No enemies have actions to take, end turn.
                        TurnSystem.Instance.NextTurn();
                    }
                }
                break;
            case State.Busy:
                break;
        }
    }

    private void SetStateTakingTurn()
    {
        timer = .5f;
        state = State.TakingTurn;
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        if (!TurnSystem.Instance.IsPlayerTurn())
        {
            state = State.TakingTurn;
            timer = 2f;
        }
    }

    private bool TryTakeEnemyAIAction(Action onEnemyAIActionComplete)
    {
        foreach (Unit enemyUnit in UnitManager.Instance.GetEnemyUnitList())
        {
            // Ignorar unidades que son clones espejo
            if (enemyUnit.IsMirrorClone)
            {
                continue;
            }

            if (TryTakeEnemyAIAction(enemyUnit, onEnemyAIActionComplete))
            {
                return true;
            }
        }
        return false;
    }

    private bool TryTakeEnemyAIAction(Unit enemyUnit, Action onEnemyAIActionComplete)
    {
        EnemyAIAction bestEnemyAIAction = null;
        BaseAction bestBaseAction = null;

        foreach (BaseAction baseAction in enemyUnit.GetBaseActionArray())
        {
            // La IA solo usa Confrontar y Mover (ignora Shoot, Sword, Grenade, Rotate, Evade)
            if (baseAction is not ConfrontAction && baseAction is not MoveAction)
            {
                continue;
            }

            // Validar el recurso correcto segun la accion
            bool canAfford = (baseAction is MoveAction)
                ? enemyUnit.CanSpendMovePointsToMove(baseAction)
                : enemyUnit.CanSpendActionPointsToTakeAction(baseAction);

            if (!canAfford) continue;

            EnemyAIAction testEnemyAIAction = baseAction.GetBestEnemyAIAction();
            if (testEnemyAIAction == null) continue;

            // Prioridad absoluta a Confrontar si tiene un objetivo valido
            if (baseAction is ConfrontAction)
            {
                testEnemyAIAction.actionValue += 1000;
            }

            if (bestEnemyAIAction == null || testEnemyAIAction.actionValue > bestEnemyAIAction.actionValue)
            {
                bestEnemyAIAction = testEnemyAIAction;
                bestBaseAction = baseAction;
            }
        }

        if (bestEnemyAIAction != null && enemyUnit.TrySpendActionPointsOrMovePointsToTakeActionOrMove(bestBaseAction))
        {
            bestBaseAction.TakeAction(bestEnemyAIAction.gridPosition, onEnemyAIActionComplete);
            return true;
        }

        return false;
    }
}
