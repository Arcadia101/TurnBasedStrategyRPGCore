using System;
using UnityEngine;

public enum CombatInteractionType
{
    Confrontation, // Ataque / Aumentar nudos
    Evade    // Curar / Disminuir nudos en Toroidal
}

public class CombatDuelSession : MonoBehaviour
{
    public static CombatDuelSession Instance { get; private set; }

    // Eventos del Ciclo de Duelo
    public event Action<Unit, Unit, CombatInteractionType> OnDuelStarted;
    public event Action<Unit, Unit> OnRoundPrepared; // Despliega la UI de selección de gestos
    public event Action<CombatClashResult> OnClashResolved; // Notifica el resultado del impacto a la UI/Animación
    public event Action OnDuelEnded;

    private Unit activeAttacker;
    private Unit activeDefender;
    private CombatInteractionType currentInteractionType;
    private bool isDuelActive;

    public struct CombatClashResult
    {
        public GestureData attackerGesture;
        public GestureData defenderGesture;
        public int netAffectationValue;
        public bool wasMitigated;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Inicia el encuentro de combate
    /// </summary>
    public void StartDuel(Unit attacker, Unit defender, CombatInteractionType interactionType)
    {
        activeAttacker = attacker;
        activeDefender = defender;
        currentInteractionType = interactionType;
        isDuelActive = true;

        OnDuelStarted?.Invoke(activeAttacker, activeDefender, currentInteractionType);
        PrepareNextRound();
    }

    /// <summary>
    /// Prepara la selección de habilidades para la siguiente ronda del duelo
    /// </summary>
    public void PrepareNextRound()
    {
        if (!isDuelActive) return;

        // Verificamos si alguno ya llegó a un límite (0 o Max) antes de abrir selección
        if (IsAnyCombatantAtBoundary())
        {
            EndDuel();
            return;
        }

        OnRoundPrepared?.Invoke(activeAttacker, activeDefender);
    }

    /// <summary>
    /// Resuelve el intercambio entre el atacante y el defensor (o pase del defensor si es null)
    /// </summary>
    public void ResolveGestureClash(RuntimeGesture attackerRuntimeGesture, RuntimeGesture defenderRuntimeGesture)
    {
        if (!isDuelActive || attackerRuntimeGesture == null || !attackerRuntimeGesture.HasUses())
        {
            EndDuel();
            return;
        }

        // 1. Consumir uso del atacante
        attackerRuntimeGesture.ConsumeUse();
        GestureData atkData = attackerRuntimeGesture.GetData();

        int atkPower = atkData.GetBaseAffectationValue();
        int defPower = 0;
        bool mitigated = false;

        // 2. Evaluar respuesta del defensor
        if (defenderRuntimeGesture != null && defenderRuntimeGesture.HasUses())
        {
            defenderRuntimeGesture.ConsumeUse();
            defPower = defenderRuntimeGesture.GetData().GetBaseAffectationValue();
            mitigated = true;
        }

        // 3. Cálculo de afectación neta (mitigación por choque de fuerzas)
        int netAffectation = Mathf.Max(0, atkPower - (defPower / 2));

        HealthSystem targetHealth = activeDefender.GetComponent<HealthSystem>();
        if (targetHealth != null)
        {
            if (currentInteractionType == CombatInteractionType.Confrontation)
            {
                targetHealth.Restrain(netAffectation);
            }
            else if (currentInteractionType == CombatInteractionType.Evade)
            {
                targetHealth.Release(netAffectation);
            }
        }

        // 4. Notificar resultado del choque
        CombatClashResult result = new CombatClashResult
        {
            attackerGesture = atkData,
            defenderGesture = defenderRuntimeGesture?.GetData(),
            netAffectationValue = netAffectation,
            wasMitigated = mitigated
        };
        OnClashResolved?.Invoke(result);

        // 5. Verificación de corte forzoso por cambio de ánimo o falta de usos
        if (IsAnyCombatantAtBoundary() || !attackerRuntimeGesture.HasUses())
        {
            EndDuel();
        }
    }

    private bool IsAnyCombatantAtBoundary()
    {
        HealthSystem atkHealth = activeAttacker.GetComponent<HealthSystem>();
        HealthSystem defHealth = activeDefender.GetComponent<HealthSystem>();

        bool atkBoundary = atkHealth != null && atkHealth.IsAtBoundary();
        bool defBoundary = defHealth != null && defHealth.IsAtBoundary();

        return atkBoundary || defBoundary;
    }
    
    // Finaliza la sesión de combate y descuenta el AP formal
    public void EndDuel()
    {
        if (!isDuelActive) return;

        isDuelActive = false;

        OnDuelEnded?.Invoke();

        activeAttacker = null;
        activeDefender = null;
    }

    public bool IsDuelActive() => isDuelActive;
}