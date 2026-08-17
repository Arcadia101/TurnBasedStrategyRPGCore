using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    public event Action<GridType> OnPlaneChanged; // Notifica el cambio de plano

    [Header("Cámaras Cinemachine")]
    [SerializeField] private CinemachineCamera normalPlaneCamera;
    [SerializeField] private CinemachineCamera toroidPlaneCamera;
    [SerializeField] private CinemachineCamera actionCamera;

    [Header("Aislamiento de Capas")]
    [SerializeField] private string duelFocusLayerName = "CombatFocus";

    [Header("Configuración de Transición")]
    [SerializeField] private float transitionDuration = 0.8f;
    [SerializeField] private float elevationHeight = 8f;

    private GridType currentActivePlane = GridType.Normal;
    private bool isTransitioning = false;

    private int originalAttackerLayer;
    private int originalDefenderLayer;
    private Unit currentAttacker;
    private Unit currentDefender;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        BaseAction.OnAnyActionStarted += BaseAction_OnAnyActionStarted;
        BaseAction.OnAnyActionCompleted += BaseAction_OnAnyActionCompleted;

        SetPlanePriorities(GridType.Normal);
        HideActionCamera();
    }

    // --- TRANSICIONES DE PLANO ---

    public void SwitchToMentalPlane(Action onComplete = null)
    {
        if (currentActivePlane == GridType.Toroid || isTransitioning) return;
        StartCoroutine(PlaneTransitionRoutine(GridType.Toroid, onComplete));
    }

    public void ReturnToRealPlane(Action onComplete = null)
    {
        if (currentActivePlane == GridType.Normal || isTransitioning) return;
        StartCoroutine(PlaneTransitionRoutine(GridType.Normal, onComplete));
    }

    private IEnumerator PlaneTransitionRoutine(GridType targetPlane, Action onComplete)
    {
        isTransitioning = true;

        CinemachineCamera originCam = (targetPlane == GridType.Toroid) ? normalPlaneCamera : toroidPlaneCamera;
        CinemachineCamera destCam = (targetPlane == GridType.Toroid) ? toroidPlaneCamera : normalPlaneCamera;

        CinemachineFollow originFollow = originCam.GetComponent<CinemachineFollow>();
        CinemachineFollow destFollow = destCam.GetComponent<CinemachineFollow>();

        Vector3 originBaseOffset = originFollow != null ? originFollow.FollowOffset : originCam.transform.position;
        Vector3 destBaseOffset = destFollow != null ? destFollow.FollowOffset : destCam.transform.position;

        float halfDuration = transitionDuration / 2f;
        float elapsed = 0f;

        // 1. Elevación en origen
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            Vector3 elevatedPos = Vector3.Lerp(originBaseOffset, originBaseOffset + Vector3.up * elevationHeight, Mathf.SmoothStep(0f, 1f, t));

            if (originFollow != null) originFollow.FollowOffset = elevatedPos;
            else originCam.transform.position = elevatedPos;

            yield return null;
        }

        // 2. Cambio de Prioridad Cinemachine
        if (destFollow != null) destFollow.FollowOffset = destBaseOffset + Vector3.up * elevationHeight;
        else destCam.transform.position = destBaseOffset + Vector3.up * elevationHeight;

        SetPlanePriorities(targetPlane);

        // 3. Descenso en destino
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            Vector3 loweredPos = Vector3.Lerp(destBaseOffset + Vector3.up * elevationHeight, destBaseOffset, Mathf.SmoothStep(0f, 1f, t));

            if (destFollow != null) destFollow.FollowOffset = loweredPos;
            else destCam.transform.position = loweredPos;

            yield return null;
        }

        if (originFollow != null) originFollow.FollowOffset = originBaseOffset;
        else originCam.transform.position = originBaseOffset;

        if (destFollow != null) destFollow.FollowOffset = destBaseOffset;
        else destCam.transform.position = destBaseOffset;

        currentActivePlane = targetPlane;
        isTransitioning = false;

        onComplete?.Invoke();
    }

    private void SetPlanePriorities(GridType activeGrid)
    {
        normalPlaneCamera.Priority = (activeGrid == GridType.Normal) ? 10 : 0;
        toroidPlaneCamera.Priority = (activeGrid == GridType.Toroid) ? 10 : 0;

        OnPlaneChanged?.Invoke(activeGrid);
    }

    // --- CÁMARA DE ACCIÓN Y DUELO ---

    private void BaseAction_OnAnyActionStarted(object sender, EventArgs e)
    {
        if (sender is ConfrontAction confrontAction)
        {
            currentAttacker = confrontAction.GetUnit();
            currentDefender = confrontAction.GetTargetUnit();

            SetupActionCameraTransform(currentAttacker, currentDefender);
            ApplyFocusLayers(currentAttacker, currentDefender);
            ShowActionCamera();
        }
    }

    private void BaseAction_OnAnyActionCompleted(object sender, EventArgs e)
    {
        if (sender is ConfrontAction)
        {
            RestoreFocusLayers();
            HideActionCamera();
        }
    }

    private void SetupActionCameraTransform(Unit actor, Unit target)
    {
        Vector3 characterHeight = Vector3.up * 1.7f;
        Vector3 viewDir = (target.GetWorldPosition() - actor.GetWorldPosition()).normalized;
        float shoulderOffsetAmount = 0.5f;
        Vector3 shoulderOffset = Quaternion.Euler(0, 90, 0) * viewDir * shoulderOffsetAmount;

        Vector3 cameraPos = actor.GetWorldPosition() + characterHeight + shoulderOffset + (viewDir * -1.2f);

        actionCamera.transform.position = cameraPos;
        actionCamera.transform.LookAt(target.GetWorldPosition() + characterHeight);
    }

    private void ApplyFocusLayers(Unit attacker, Unit defender)
    {
        int focusLayer = LayerMask.NameToLayer(duelFocusLayerName);
        originalAttackerLayer = attacker.gameObject.layer;
        originalDefenderLayer = defender.gameObject.layer;

        SetLayerRecursively(attacker.gameObject, focusLayer);
        SetLayerRecursively(defender.gameObject, focusLayer);
    }

    private void RestoreFocusLayers()
    {
        if (currentAttacker != null) SetLayerRecursively(currentAttacker.gameObject, originalAttackerLayer);
        if (currentDefender != null) SetLayerRecursively(currentDefender.gameObject, originalDefenderLayer);

        currentAttacker = null;
        currentDefender = null;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void ShowActionCamera() => actionCamera.Priority = 20;
    private void HideActionCamera() => actionCamera.Priority = 0;

    public GridType GetCurrentActivePlane() => currentActivePlane;

    private void OnDestroy()
    {
        BaseAction.OnAnyActionStarted -= BaseAction_OnAnyActionStarted;
        BaseAction.OnAnyActionCompleted -= BaseAction_OnAnyActionCompleted;
    }
}