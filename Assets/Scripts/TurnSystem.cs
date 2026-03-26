using System;
using UnityEngine;

public class TurnSystem : MonoBehaviour
{
    public static TurnSystem Instance { get; private set; }
    
    public event EventHandler OnTurnChanged;
    
    private int turnNumber = 1;
    
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
        
        OnTurnChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public int GetTurnNumber()
    {
        return turnNumber;
    }
}
