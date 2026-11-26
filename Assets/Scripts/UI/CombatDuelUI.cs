using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CombatDuelUI : MonoBehaviour
{
    [Header("Contenedores Principales")]
    [SerializeField] private GameObject duelPanelRoot;
    [SerializeField] private GameObject hudToHideDuringDuel;
    [SerializeField] private Button retreatButton;
    [SerializeField] private TextMeshProUGUI interactionTypeText;

    [Header("Lista de Expresiones")]
    [SerializeField] private Transform expressionsContainer;
    [SerializeField] private ExpressionButtonUI expressionButtonPrefab;

    [Header("Infobox Defensor (Arriba Derecha)")]
    [SerializeField] private Image defenderBoxBg;
    [SerializeField] private TextMeshProUGUI defenderNameText;
    [SerializeField] private TextMeshProUGUI defenderKnotsText;
    [SerializeField] private Image defenderKnotsBar;

    [Header("Infobox Atacante (Abajo Derecha)")]
    [SerializeField] private Image attackerBoxBg;
    [SerializeField] private TextMeshProUGUI attackerNameText;
    [SerializeField] private TextMeshProUGUI attackerKnotsText;
    [SerializeField] private Image attackerKnotsBar;

    [Header("Feedback de Choque")]
    [SerializeField] private GameObject clashFeedbackRoot;
    [SerializeField] private TextMeshProUGUI clashFeedbackText;
    [SerializeField] private float clashDisplaySeconds = 1.25f;

    private Unit currentAttacker;
    private Unit currentDefender;
    private bool isWaitingClashFeedback;
    private Coroutine clashRoutine;

    private void Awake()
    {
        if (retreatButton != null)
        {
            retreatButton.onClick.AddListener(OnRetreatClicked);
        }

        if (duelPanelRoot != null)
            duelPanelRoot.SetActive(false);

        if (clashFeedbackRoot != null)
            clashFeedbackRoot.SetActive(false);
    }

    private void Start()
    {
        if (CombatDuelSession.Instance == null)
        {
            Debug.LogWarning("[CombatDuelUI] No se encontro CombatDuelSession.Instance en la escena.");
            return;
        }

        CombatDuelSession.Instance.OnDuelStarted += HandleDuelStarted;
        CombatDuelSession.Instance.OnRoundPrepared += HandleRoundPrepared;
        CombatDuelSession.Instance.OnClashResolved += HandleClashResolved;
        CombatDuelSession.Instance.OnDuelEnded += HandleDuelEnded;
    }

    private void OnDestroy()
    {
        if (CombatDuelSession.Instance != null)
        {
            CombatDuelSession.Instance.OnDuelStarted -= HandleDuelStarted;
            CombatDuelSession.Instance.OnRoundPrepared -= HandleRoundPrepared;
            CombatDuelSession.Instance.OnClashResolved -= HandleClashResolved;
            CombatDuelSession.Instance.OnDuelEnded -= HandleDuelEnded;
        }
    }

    private void HandleDuelStarted(Unit attacker, Unit defender, CombatInteractionType interactionType)
    {
        currentAttacker = attacker;
        currentDefender = defender;
        isWaitingClashFeedback = false;

        EnsureGesturesInitialized(currentAttacker);
        EnsureGesturesInitialized(currentDefender);

        if (hudToHideDuringDuel != null)
            hudToHideDuringDuel.SetActive(false);

        if (duelPanelRoot != null)
            duelPanelRoot.SetActive(true);

        if (clashFeedbackRoot != null)
            clashFeedbackRoot.SetActive(false);

        if (interactionTypeText != null)
        {
            if (currentAttacker != null && currentAttacker.IsEnemy())
            {
                interactionTypeText.text = "TE CONFRONTAN (ELIGE DEFENSA)";
            }
            else
            {
                interactionTypeText.text = interactionType == CombatInteractionType.Confrontation
                    ? "CONFRONTACION"
                    : "EVASION";
            }
        }

        ApplyPlaneTheme();
        RefreshUnitBoxes();

        if (InputManager.Instance != null)
            InputManager.Instance.SwitchToMenuMap();
    }

    private void HandleRoundPrepared(Unit attacker, Unit defender)
    {
        currentAttacker = attacker;
        currentDefender = defender;
        isWaitingClashFeedback = false;

        if (clashFeedbackRoot != null)
            clashFeedbackRoot.SetActive(false);

        RefreshUnitBoxes();
        RebuildExpressionButtons();
    }

    private void HandleClashResolved(CombatDuelSession.CombatClashResult result)
    {
        RefreshUnitBoxes();
        SetExpressionButtonsInteractable(false);

        string atkName = result.attackerGesture != null ? result.attackerGesture.GetGestureName() : "-";
        string defName = result.defenderGesture != null ? result.defenderGesture.GetGestureName() : "Sin respuesta";
        string mitText = result.wasMitigated ? " (Mitigado)" : "";

        if (clashFeedbackText != null)
        {
            clashFeedbackText.text = $"{atkName}  vs  {defName}\nAfectacion neta: {result.netAffectationValue}{mitText}";
        }

        if (clashFeedbackRoot != null)
            clashFeedbackRoot.SetActive(true);

        if (clashRoutine != null)
            StopCoroutine(clashRoutine);

        clashRoutine = StartCoroutine(AdvanceAfterClashRoutine());
    }

    private IEnumerator AdvanceAfterClashRoutine()
    {
        isWaitingClashFeedback = true;
        yield return new WaitForSeconds(clashDisplaySeconds);
        isWaitingClashFeedback = false;

        if (CombatDuelSession.Instance != null && CombatDuelSession.Instance.IsDuelActive())
        {
            CombatDuelSession.Instance.PrepareNextRound();
        }
    }

    private void HandleDuelEnded()
    {
        if (clashRoutine != null)
        {
            StopCoroutine(clashRoutine);
            clashRoutine = null;
        }

        isWaitingClashFeedback = false;
        currentAttacker = null;
        currentDefender = null;

        if (clashFeedbackRoot != null)
            clashFeedbackRoot.SetActive(false);

        if (duelPanelRoot != null)
            duelPanelRoot.SetActive(false);

        if (hudToHideDuringDuel != null)
            hudToHideDuringDuel.SetActive(true);

        if (InputManager.Instance != null)
            InputManager.Instance.SwitchToGameplayMap();
    }

    private void OnExpressionSelected(RuntimeGesture playerSelectedGesture)
    {
        if (isWaitingClashFeedback || CombatDuelSession.Instance == null || !CombatDuelSession.Instance.IsDuelActive())
            return;

        bool enemyIsAttacking = currentAttacker != null && currentAttacker.IsEnemy();

        if (enemyIsAttacking)
        {
            RuntimeGesture enemyAttackGesture = PickDefenderAutoGesture(currentAttacker);
            CombatDuelSession.Instance.ResolveGestureClash(enemyAttackGesture, playerSelectedGesture);
        }
        else
        {
            RuntimeGesture enemyDefenseGesture = PickDefenderAutoGesture(currentDefender);
            CombatDuelSession.Instance.ResolveGestureClash(playerSelectedGesture, enemyDefenseGesture);
        }
    }

    private void OnRetreatClicked()
    {
        if (CombatDuelSession.Instance != null && CombatDuelSession.Instance.IsDuelActive())
        {
            CombatDuelSession.Instance.EndDuel();
        }
    }

    private void RebuildExpressionButtons()
    {
        if (expressionsContainer == null || expressionButtonPrefab == null || currentAttacker == null)
            return;

        foreach (Transform child in expressionsContainer)
        {
            Destroy(child.gameObject);
        }

        Unit playerUnit = currentAttacker.IsEnemy() ? currentDefender : currentAttacker;
        if (playerUnit == null) return;

        if (interactionTypeText != null && currentAttacker.IsEnemy())
        {
            interactionTypeText.text = "TE CONFRONTAN (ELIGE DEFENSA)";
        }

        HealthSystem playerHealth = playerUnit.GetComponent<HealthSystem>();
        if (playerHealth == null) return;

        List<RuntimeGesture> gestures = playerHealth.GetRuntimeGestures();
        Button firstInteractable = null;

        foreach (RuntimeGesture gesture in gestures)
        {
            ExpressionButtonUI btnUI = Instantiate(expressionButtonPrefab, expressionsContainer);
            btnUI.Setup(gesture, OnExpressionSelected);

            if (firstInteractable == null && btnUI.GetButton() != null && btnUI.GetButton().interactable)
            {
                firstInteractable = btnUI.GetButton();
            }
        }

        if (EventSystem.current != null && firstInteractable != null)
        {
            EventSystem.current.SetSelectedGameObject(firstInteractable.gameObject);
        }
    }

    private void SetExpressionButtonsInteractable(bool interactable)
    {
        if (expressionsContainer == null) return;

        foreach (Button btn in expressionsContainer.GetComponentsInChildren<Button>())
        {
            btn.interactable = interactable;
        }
    }

    private void RefreshUnitBoxes()
    {
        bool enemyIsAttacking = currentAttacker != null && currentAttacker.IsEnemy();
        Unit topEnemyUnit = enemyIsAttacking ? currentAttacker : currentDefender;
        Unit bottomPlayerUnit = enemyIsAttacking ? currentDefender : currentAttacker;

        UpdateSingleUnitBox(topEnemyUnit, defenderNameText, defenderKnotsText, defenderKnotsBar);
        UpdateSingleUnitBox(bottomPlayerUnit, attackerNameText, attackerKnotsText, attackerKnotsBar);
    }

    private void UpdateSingleUnitBox(Unit unit, TextMeshProUGUI nameTxt, TextMeshProUGUI knotsTxt, Image knotsBar)
    {
        if (unit == null) return;

        UnitEnergy energy = unit.GetComponent<UnitEnergy>();
        EmotypeData aspect = energy != null ? energy.GetCurrentAspect() : null;
        string displayName = aspect != null ? aspect.name : unit.name;

        if (nameTxt != null)
            nameTxt.text = displayName;

        HealthSystem hs = unit.GetComponent<HealthSystem>();
        if (hs != null)
        {
            if (knotsTxt != null)
                knotsTxt.text = $"Nudos: {hs.GetHealth()}/{hs.GetMaxHealth()}";

            if (knotsBar != null)
                knotsBar.fillAmount = hs.GetHealthNormalized();
        }
    }

    private void ApplyPlaneTheme()
    {
        bool isIntrospection = CameraManager.Instance != null &&
                               CameraManager.Instance.GetCurrentActivePlane() == GridType.Toroid;

        Color boxBg = isIntrospection ? Color.black : Color.white;
        Color txtColor = isIntrospection ? Color.white : Color.black;

        if (defenderBoxBg != null) defenderBoxBg.color = boxBg;
        if (attackerBoxBg != null) attackerBoxBg.color = boxBg;
        if (defenderNameText != null) defenderNameText.color = txtColor;
        if (attackerNameText != null) attackerNameText.color = txtColor;
        if (defenderKnotsText != null) defenderKnotsText.color = txtColor;
        if (attackerKnotsText != null) attackerKnotsText.color = txtColor;
    }

    private RuntimeGesture PickDefenderAutoGesture(Unit defender)
    {
        if (defender == null) return null;
        HealthSystem hs = defender.GetComponent<HealthSystem>();
        if (hs == null) return null;

        foreach (RuntimeGesture gesture in hs.GetRuntimeGestures())
        {
            if (gesture != null && gesture.HasUses())
                return gesture;
        }
        return null;
    }

    private void EnsureGesturesInitialized(Unit unit)
    {
        if (unit == null) return;
        HealthSystem hs = unit.GetComponent<HealthSystem>();
        UnitEnergy energy = unit.GetComponent<UnitEnergy>();
        if (hs != null && energy != null && hs.GetRuntimeGestures().Count == 0 && energy.GetCurrentAspect() != null)
        {
            hs.SetupFromEmotype(energy.GetCurrentAspect());
        }
    }
}
