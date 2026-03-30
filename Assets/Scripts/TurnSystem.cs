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
        isPlayerTurn = !isPlayerTurn;
        
        OnTurnChanged?.Invoke(this, EventArgs.Empty);
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
