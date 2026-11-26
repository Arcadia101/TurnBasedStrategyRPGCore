using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DemoFlowUI : MonoBehaviour
{
    [Header("Indicador de Pool de Relacion")]
    [SerializeField] private TextMeshProUGUI poolCounterText;

    [Header("Banner de Cambio de Animo (Toast)")]
    [SerializeField] private GameObject moodToastRoot;
    [SerializeField] private TextMeshProUGUI moodToastText;
    [SerializeField] private float toastDuration = 2.8f;

    [Header("Prompt Contextual (Rotar / Apuntar)")]
    [SerializeField] private GameObject contextPromptRoot;
    [SerializeField] private TextMeshProUGUI contextPromptText;

    [Header("Panel de Pausa / Fin de Demo")]
    [SerializeField] private GameObject pauseOverlayRoot;
    [SerializeField] private TextMeshProUGUI pauseTitleText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;

    private Coroutine toastRoutine;
    private bool isPaused;
    private bool isDemoEnded;

    private void Awake()
    {
        if (moodToastRoot != null) moodToastRoot.SetActive(false);
        if (contextPromptRoot != null) contextPromptRoot.SetActive(false);
        if (pauseOverlayRoot != null) pauseOverlayRoot.SetActive(false);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartScene);
    }

    private void Start()
    {
        if (RelationshipPoolManager.Instance != null)
        {
            RelationshipPoolManager.Instance.OnPoolUpdated += HandlePoolUpdated;
            RelationshipPoolManager.Instance.OnUnitMoodChanged += HandleUnitMoodChanged;
            RelationshipPoolManager.Instance.OnPoolExhausted += HandlePoolExhausted;

            HandlePoolUpdated(
                RelationshipPoolManager.Instance.GetRemainingPoolCount(),
                RelationshipPoolManager.Instance.GetInitialPoolSize()
            );
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;

        if (RelationshipPoolManager.Instance != null)
        {
            RelationshipPoolManager.Instance.OnPoolUpdated -= HandlePoolUpdated;
            RelationshipPoolManager.Instance.OnUnitMoodChanged -= HandleUnitMoodChanged;
            RelationshipPoolManager.Instance.OnPoolExhausted -= HandlePoolExhausted;
        }
    }

    private void Update()
    {
        // Tecla Escape para abrir/cerrar Pausa y Reinicio rapido
        if (!isDemoEnded && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else PauseGame("PAUSA");
        }

        UpdateContextPrompt();
    }

    private void UpdateContextPrompt()
    {
        if (contextPromptRoot == null || UnitActionSystem.Instance == null) return;

        BaseAction activeAction = UnitActionSystem.Instance.GetSelectedAction();
        bool awaiting = activeAction != null && activeAction.IsAwaitingTargetSelection();

        if (!awaiting)
        {
            if (contextPromptRoot.activeSelf) contextPromptRoot.SetActive(false);
            return;
        }

        if (!contextPromptRoot.activeSelf) contextPromptRoot.SetActive(true);

        if (contextPromptText != null)
        {
            if (activeAction is RotateEnergyAction)
            {
                contextPromptText.text = "ROTAR ENERGIA: L1 / R1 para girar 45°  |  Confirmar para fijar";
            }
            else
            {
                contextPromptText.text = "SELECCIONAR OBJETIVO: L1 / R1 para cambiar  |  Confirmar para ejecutar";
            }
        }
    }

    private void HandlePoolUpdated(int remaining, int total)
    {
        if (poolCounterText != null)
        {
            poolCounterText.text = $"Pool Relacion: {remaining}/{total}";
        }
    }

    private void HandleUnitMoodChanged(RelationshipPoolManager.MoodChangeInfo info)
    {
        string stateLabel = info.wasMaterialized ? "MATERIALIZACION" : "ARQUETOPIA";
        string prevName = info.previousEmotype != null ? info.previousEmotype.name : info.unit.name;
        string msg = info.newEmotype != null
            ? $"{stateLabel} ({prevName})\nCambio de animo -> {info.newEmotype.name} (Pool: {info.remainingInPool})"
            : $"{stateLabel} ({prevName})\nPool de relacion agotada";

        ShowToast(msg);
    }

    private void HandlePoolExhausted()
    {
        isDemoEnded = true;
        PauseGame("RELACION COMPLETADA");
        if (resumeButton != null) resumeButton.gameObject.SetActive(false);
    }

    private void ShowToast(string message)
    {
        if (moodToastRoot == null || moodToastText == null) return;

        moodToastText.text = message;
        moodToastRoot.SetActive(true);

        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(HideToastRoutine());
    }

    private IEnumerator HideToastRoutine()
    {
        yield return new WaitForSecondsRealtime(toastDuration);
        if (moodToastRoot != null) moodToastRoot.SetActive(false);
    }

    private void PauseGame(string title)
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pauseTitleText != null) pauseTitleText.text = title;
        if (pauseOverlayRoot != null) pauseOverlayRoot.SetActive(true);
        if (InputManager.Instance != null) InputManager.Instance.SwitchToMenuMap();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseOverlayRoot != null) pauseOverlayRoot.SetActive(false);
        if (InputManager.Instance != null) InputManager.Instance.SwitchToGameplayMap();
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
