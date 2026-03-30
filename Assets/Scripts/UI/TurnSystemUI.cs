using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnSystemUI : MonoBehaviour
{
    [SerializeField] private Button endTurnButton;
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private GameObject enemyTurnVisualGO;
    

    private void Start()
    {
        endTurnButton.onClick.AddListener(() =>
        {
            TurnSystem.Instance.NextTurn();
        });
        
        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
        UpdateTurnText();
        UpdateEnemyTurnVisual();
        //UpdateEndTurnButtonVisibility();
    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        UpdateTurnText();
        UpdateEnemyTurnVisual();
        //UpdateEndTurnButtonVisibility();
    }

    private void UpdateTurnText()
    {
        turnText.text = "Turn: " + TurnSystem.Instance.GetTurnNumber();
    }

    private void UpdateEnemyTurnVisual()
    {
        enemyTurnVisualGO.SetActive(!TurnSystem.Instance.IsPlayerTurn());
    }

    private void UpdateEndTurnButtonVisibility()
    {
        endTurnButton.gameObject.SetActive(TurnSystem.Instance.IsPlayerTurn());
    }
}
