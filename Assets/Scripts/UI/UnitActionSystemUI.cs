using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;



public class UnitActionSystemUI : MonoBehaviour
{
    [SerializeField] private Transform actionButtonPrefab;
    [SerializeField] private Transform actionButtonContainerTransform;
    [SerializeField] private TextMeshProUGUI actionPointsText;
    [SerializeField] private TextMeshProUGUI movePointsText;
    
    
    private List<ActionButtonUI> actionButtonUIList;
    
    private Unit selectedUnit;

    private void Awake()
    {
        actionButtonUIList = new List<ActionButtonUI>();
    }

    private void Start()
    {
        UnitActionSystem.Instance.OnSelectedUnitChanged += UnitActionSystem_OnSelectedUnitChanged;
        UnitActionSystem.Instance.OnSelectedActionChanged += UnitActionSystem_OnSelectedActionChanged;
        UnitActionSystem.Instance.OnActionStarted += UnitActionSystem_OnActionStarted;
        TurnSystem.Instance.OnTurnChanged += TurnSystem_OnTurnChanged;
        Unit.OnAnyActionPointsChanged += Unit_OnAnyActionPointsChanged;
        
        UpdateActionAndMovePoints();
        CreateUnitActionButtons();
        UpdateSelectedVisual();
    }

    private void CreateUnitActionButtons()
    {
        // Limpiamos los botones anteriores
        foreach (Transform buttonTransform in actionButtonContainerTransform)
        {
            Destroy(buttonTransform.gameObject);
        }
        actionButtonUIList.Clear();
        
        selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();

        // Si no hay unidad seleccionada, apagamos la UI y no intentamos crear botones
        if (selectedUnit == null)
        {
            actionButtonContainerTransform.gameObject.SetActive(false);
            actionPointsText.gameObject.SetActive(false);
            movePointsText.gameObject.SetActive(false);
            return;
        }

        // Si hay una unidad, encendemos la UI y creamos sus botones
        actionButtonContainerTransform.gameObject.SetActive(true);
        actionPointsText.gameObject.SetActive(true);
        movePointsText.gameObject.SetActive(true);

        foreach (BaseAction baseAction in selectedUnit.GetBaseActionArray())
        {
            Transform actionButtonTransform = Instantiate(actionButtonPrefab, actionButtonContainerTransform);
            ActionButtonUI actionButtonUI = actionButtonTransform.GetComponent<ActionButtonUI>();
            actionButtonUI.SetBaseAction(baseAction);
            
            actionButtonUIList.Add(actionButtonUI);
        }
    }

    private void UnitActionSystem_OnSelectedUnitChanged(object sender, EventArgs e)
    {
        CreateUnitActionButtons();
        UpdateSelectedVisual();
        UpdateActionAndMovePoints();
    }
    
    private void UnitActionSystem_OnSelectedActionChanged(object sender, EventArgs e)
    {
        UpdateSelectedVisual();
    }

    private void UnitActionSystem_OnActionStarted(object sender, EventArgs e)
    {
        UpdateActionAndMovePoints();
    }
    private void UpdateSelectedVisual()
    {
        // Seguro por si acaso se llama y no hay unidad
        if (UnitActionSystem.Instance.GetSelectedUnit() == null) return;

        foreach (ActionButtonUI actionButtonUI in actionButtonUIList)
        {
            actionButtonUI.UpdateSelectedVisual();
        }
    }

    private void UpdateActionAndMovePoints()
    {
        Unit selectedUnit = UnitActionSystem.Instance.GetSelectedUnit();
        
        // Evitamos el NullReferenceException si deseleccionamos a la unidad
        if (selectedUnit == null) return;
        
        actionPointsText.text = "Action Points: " + selectedUnit.GetActionPoints().ToString();
        movePointsText.text = "Move Points: " + selectedUnit.GetMovePoints().ToString();

    }

    private void TurnSystem_OnTurnChanged(object sender, EventArgs e)
    {
        UpdateActionAndMovePoints();
    }
    
    private void Unit_OnAnyActionPointsChanged(object sender, EventArgs e)
    {
        UpdateActionAndMovePoints();
    }
}
