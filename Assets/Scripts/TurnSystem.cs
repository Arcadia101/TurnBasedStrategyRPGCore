using System;
using UnityEngine;

public class TurnSystem : MonoBehaviour
{
    public static TurnSystem Instance { get; private set; }
    
    public event EventHandler OnTurnChanged;
    
    private int turnNumber = 1;
    private bool isPlayerTurn = true;
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There is already a TurnSystem in the scene!");
            Destroy(gameObject);
        }
        Instance = this;
    }
    
    public void NextTurn()
    {
        turnNumber++;
        isPlayerTurn = (turnNumber % 2 != 0); // Turnos impares = Jugador 1

        // REGLA: Si el turno es impar, se restauran los gestos de todas las unidades
        if (turnNumber % 2 != 0)
        {
            RestoreAllUnitsGestures();
        }

        OnTurnChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RestoreAllUnitsGestures()
    {
        // Obtenemos todas las unidades de la escena a través de tu UnitManager / LevelGrid
        foreach (Unit unit in UnitManager.Instance.GetUnitList())
        {
            HealthSystem healthSystem = unit.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.RestoreAllGestureUses();
            }
        }
    }
    
    public int GetTurnNumber()
    {
        return turnNumber;
    }
    
    public bool IsPlayerTurn()
    {
        return isPlayerTurn;
    }
}
