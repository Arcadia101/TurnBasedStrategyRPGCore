using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoardHUDUI : MonoBehaviour
{
    [Header("Contenedor Principal")]
    [SerializeField] private GameObject hudRoot;

    [Header("Barra Superior Izquierda (Plano y Turno)")]
    [SerializeField] private Image planeBadgeBg;
    [SerializeField] private Outline planeBadgeOutline;
    [SerializeField] private TextMeshProUGUI planeBadgeText;
    [SerializeField] private TextMeshProUGUI turnNumberText;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button returnToRealPlaneButton;

    [Header("Banner Turno Enemigo (Arriba Centro)")]
    [SerializeField] private GameObject enemyTurnBanner;

    [Header("Infobox Unidad Seleccionada (Arriba Derecha)")]
    [SerializeField] private GameObject unitInfoBoxRoot;
    [SerializeField] private Image unitInfoBoxBg;
    [SerializeField] private Outline unitInfoBoxOutline;
    [SerializeField] private TextMeshProUGUI unitEmotypeNameText;
    [SerializeField] private TextMeshProUGUI unitKnotsText;
    [SerializeField] private Image unitKnotsBarFill;
    [SerializeField] private Image impulsesBoxBg;
    [SerializeField] private TextMeshProUGUI impulsesValueText;
    [SerializeField] private Image gesturesBoxBg;
    [SerializeField] private TextMeshProUGUI gesturesValueText;
    [SerializeField] private GameObject overlapBadgeRoot;

    private Unit trackedUnit;
    private HealthSystem trackedHealth;

    private void Awake()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(() =>
            {
                if (TurnSystem.Instance != null && TurnSystem.Instance.IsPlayerTurn())
                {
                    TurnSystem.Instance.NextTurn();
                }
            });
        }

        if (returnToRealPlaneButton != null)
        {
            returnToRealPlaneButton.onClick.AddListener(() =>
            {
                if (CameraManager.Instance != null)
                {
                    CameraManager.Instance.ReturnToRealPlane();
                }
            });
        }
    }

    private void Start()
    {
        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.OnSelectedUnitChanged += HandleSelectedUnitChanged;
            UnitActionSystem.Instance.OnActionStarted += HandleAnyPointsChanged;
        }

        Unit.OnAnyActionPointsChanged += HandleAnyPointsChanged;
        GameGridEvents.OnAnyUnitMovementComplete += HandleEnergyUpdated;
        GameGridEvents.OnAnyUnitEnergyChanged += HandleEnergyUpdated;

        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.OnTurnChanged += HandleTurnChanged;
        }

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.OnPlaneChanged += HandlePlaneChanged;
        }

        if (CombatDuelSession.Instance != null)
        {
            CombatDuelSession.Instance.OnDuelStarted += HandleDuelStarted;
            CombatDuelSession.Instance.OnDuelEnded += HandleDuelEnded;
        }

        GridType initialPlane = CameraManager.Instance != null
            ? CameraManager.Instance.GetCurrentActivePlane()
            : GridType.Normal;

        ApplyPlaneTheme(initialPlane);
        RefreshTurnUI();
        BindSelectedUnit();
    }

    private void OnDestroy()
    {
        if (UnitActionSystem.Instance != null)
        {
            UnitActionSystem.Instance.OnSelectedUnitChanged -= HandleSelectedUnitChanged;
            UnitActionSystem.Instance.OnActionStarted -= HandleAnyPointsChanged;
        }

        Unit.OnAnyActionPointsChanged -= HandleAnyPointsChanged;
        GameGridEvents.OnAnyUnitMovementComplete -= HandleEnergyUpdated;
        GameGridEvents.OnAnyUnitEnergyChanged -= HandleEnergyUpdated;

        if (TurnSystem.Instance != null)
        {
            TurnSystem.Instance.OnTurnChanged -= HandleTurnChanged;
        }

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.OnPlaneChanged -= HandlePlaneChanged;
        }

        if (CombatDuelSession.Instance != null)
        {
            CombatDuelSession.Instance.OnDuelStarted -= HandleDuelStarted;
            CombatDuelSession.Instance.OnDuelEnded -= HandleDuelEnded;
        }

        UnsubscribeTrackedHealth();
    }

    private void HandleSelectedUnitChanged(object sender, EventArgs e)
    {
        BindSelectedUnit();
    }

    private void HandleAnyPointsChanged(object sender, EventArgs e)
    {
        RefreshUnitInfo();
    }

    private void HandleEnergyUpdated()
    {
        RefreshOverlapBadge();
    }

    private void HandleTurnChanged(object sender, EventArgs e)
    {
        RefreshTurnUI();
        RefreshUnitInfo();
    }

    private void HandlePlaneChanged(GridType activePlane)
    {
        ApplyPlaneTheme(activePlane);
        RefreshTurnUI();
    }

    private void HandleDuelStarted(Unit attacker, Unit defender, CombatInteractionType type)
    {
        if (hudRoot != null) hudRoot.SetActive(false);
    }

    private void HandleDuelEnded()
    {
        if (hudRoot != null) hudRoot.SetActive(true);
        RefreshTurnUI();
        RefreshUnitInfo();
    }

    private void BindSelectedUnit()
    {
        UnsubscribeTrackedHealth();

        trackedUnit = UnitActionSystem.Instance != null
            ? UnitActionSystem.Instance.GetSelectedUnit()
            : null;

        if (trackedUnit != null)
        {
            trackedHealth = trackedUnit.GetComponent<HealthSystem>();
            if (trackedHealth != null)
            {
                trackedHealth.OnDamaged += HandleTrackedUnitDamaged;
            }
        }

        RefreshUnitInfo();
    }

    private void UnsubscribeTrackedHealth()
    {
        if (trackedHealth != null)
        {
            trackedHealth.OnDamaged -= HandleTrackedUnitDamaged;
            trackedHealth = null;
        }
    }

    private void HandleTrackedUnitDamaged(object sender, EventArgs e)
    {
        RefreshUnitInfo();
    }

    private void RefreshUnitInfo()
    {
        if (unitInfoBoxRoot == null) return;

        if (trackedUnit == null)
        {
            unitInfoBoxRoot.SetActive(false);
            return;
        }

        unitInfoBoxRoot.SetActive(true);

        UnitEnergy energy = trackedUnit.GetComponent<UnitEnergy>();
        EmotypeData aspect = energy != null ? energy.GetCurrentAspect() : null;

        if (unitEmotypeNameText != null)
        {
            unitEmotypeNameText.text = aspect != null ? aspect.name : trackedUnit.name;
        }

        if (trackedHealth != null)
        {
            if (unitKnotsText != null)
                unitKnotsText.text = $"Nudos: {trackedHealth.GetHealth()}/{trackedHealth.GetMaxHealth()}";

            if (unitKnotsBarFill != null)
                unitKnotsBarFill.fillAmount = trackedHealth.GetHealthNormalized();
        }

        if (impulsesValueText != null)
            impulsesValueText.text = $"Impulsos: {trackedUnit.GetMovePoints()}";

        if (gesturesValueText != null)
            gesturesValueText.text = $"Gestos: {trackedUnit.GetActionPoints()}";

        RefreshOverlapBadge();
    }

    private void RefreshOverlapBadge()
    {
        if (overlapBadgeRoot == null) return;

        if (trackedUnit == null)
        {
            overlapBadgeRoot.SetActive(false);
            return;
        }

        UnitEnergy energy = trackedUnit.GetComponent<UnitEnergy>();
        overlapBadgeRoot.SetActive(energy != null && energy.HasOverlappedEnergy);
    }

    private void RefreshTurnUI()
    {
        bool isPlayerTurn = TurnSystem.Instance == null || TurnSystem.Instance.IsPlayerTurn();
        int turnNumber = TurnSystem.Instance != null ? TurnSystem.Instance.GetTurnNumber() : 1;
        bool isToroid = CameraManager.Instance != null &&
                        CameraManager.Instance.GetCurrentActivePlane() == GridType.Toroid;

        if (turnNumberText != null)
        {
            string refreshNote = (turnNumber > 1 && turnNumber % 2 != 0) ? " (Gestos recargados)" : "";
            turnNumberText.text = $"Turno {turnNumber}{refreshNote}";
        }

        if (enemyTurnBanner != null)
            enemyTurnBanner.SetActive(!isPlayerTurn);

        if (endTurnButton != null)
            endTurnButton.gameObject.SetActive(isPlayerTurn && !isToroid);

        if (returnToRealPlaneButton != null)
            returnToRealPlaneButton.gameObject.SetActive(isPlayerTurn && isToroid);
    }

    private void ApplyPlaneTheme(GridType activePlane)
    {
        bool isIntrospection = activePlane == GridType.Toroid;

        Color bg = isIntrospection ? Color.black : Color.white;
        Color fg = isIntrospection ? Color.white : Color.black;
        Color subBg = isIntrospection ? new Color(0.12f, 0.12f, 0.12f, 1f) : new Color(0.93f, 0.93f, 0.93f, 1f);

        if (planeBadgeBg != null) planeBadgeBg.color = bg;
        if (planeBadgeOutline != null) planeBadgeOutline.effectColor = fg;
        if (planeBadgeText != null)
        {
            planeBadgeText.color = fg;
            planeBadgeText.text = isIntrospection ? "INTROSPECCION" : "INICIATIVA";
        }
        if (turnNumberText != null) turnNumberText.color = fg;

        if (unitInfoBoxBg != null) unitInfoBoxBg.color = bg;
        if (unitInfoBoxOutline != null) unitInfoBoxOutline.effectColor = fg;
        if (unitEmotypeNameText != null) unitEmotypeNameText.color = fg;
        if (unitKnotsText != null) unitKnotsText.color = fg;

        if (impulsesBoxBg != null) impulsesBoxBg.color = subBg;
        if (impulsesValueText != null) impulsesValueText.color = fg;
        if (gesturesBoxBg != null) gesturesBoxBg.color = subBg;
        if (gesturesValueText != null) gesturesValueText.color = fg;
    }
}
