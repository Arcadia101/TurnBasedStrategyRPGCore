using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelationshipPoolManager : MonoBehaviour
{
    public static RelationshipPoolManager Instance { get; private set; }

    public struct MoodChangeInfo
    {
        public Unit unit;
        public EmotypeData previousEmotype;
        public EmotypeData newEmotype;
        public bool wasMaterialized; // true = llego al Max (Materializacion), false = llego a 0 (Arquetopia)
        public int remainingInPool;
    }

    public event Action<MoodChangeInfo> OnUnitMoodChanged;
    public event Action<int, int> OnPoolUpdated; // (restantes, totalInicial)
    public event Action OnPoolExhausted;

    [Header("Jugador 1 (3 Genitos + 1 Cosmico)")]
    [SerializeField] private List<EmotypeData> playerGenitoEmotypes = new List<EmotypeData>();
    [SerializeField] private EmotypeData playerCosmicEmotype;

    [Header("Jugador 2 / Enemigo (3 Genitos + 1 Cosmico)")]
    [SerializeField] private List<EmotypeData> enemyGenitoEmotypes = new List<EmotypeData>();
    [SerializeField] private EmotypeData enemyCosmicEmotype;

    [Header("Reglas de Relacion")]
    [SerializeField] private bool assignInitialFromPoolOnStart = true;
    [SerializeField] private bool recyclePoolWhenEmpty = false;

    private readonly Queue<EmotypeData> sharedGenitoPool = new Queue<EmotypeData>();
    private readonly List<EmotypeData> discardPile = new List<EmotypeData>();
    private readonly List<HealthSystem> subscribedHealthSystems = new List<HealthSystem>();

    private int initialPoolSize;

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
        StartCoroutine(InitializeRelationshipRoutine());
    }

    private void OnDestroy()
    {
        foreach (HealthSystem hs in subscribedHealthSystems)
        {
            if (hs != null)
            {
                hs.OnMoodDepleted -= HandleMoodDepleted;
                hs.OnMoodMaximized -= HandleMoodMaximized;
            }
        }
        subscribedHealthSystems.Clear();
    }

    private IEnumerator InitializeRelationshipRoutine()
    {
        // Esperamos un frame para que todas las unidades se registren en UnitManager y su GridContext
        yield return null;

        BuildAndShufflePool();
        SubscribeAndAssignInitiativeUnits();
    }

    public void BuildAndShufflePool()
    {
        sharedGenitoPool.Clear();
        discardPile.Clear();

        List<EmotypeData> combined = new List<EmotypeData>();

        foreach (EmotypeData e in playerGenitoEmotypes)
        {
            if (e != null) combined.Add(e);
        }

        foreach (EmotypeData e in enemyGenitoEmotypes)
        {
            if (e != null) combined.Add(e);
        }

        ShuffleList(combined);

        foreach (EmotypeData e in combined)
        {
            sharedGenitoPool.Enqueue(e);
        }

        initialPoolSize = sharedGenitoPool.Count;
        OnPoolUpdated?.Invoke(sharedGenitoPool.Count, initialPoolSize);
        Debug.Log($"[RELACION] Pool de genitos barajada con {initialPoolSize} emotipos.");
    }

    private void SubscribeAndAssignInitiativeUnits()
    {
        if (UnitManager.Instance == null) return;

        foreach (Unit unit in UnitManager.Instance.GetUnitList())
        {
            if (unit == null || unit.IsMirrorClone) continue;

            // Solo las unidades en la Iniciativa (Grid Normal) consumen la pool de genitos
            if (unit.GetCurrentGridType() != GridType.Normal) continue;

            HealthSystem hs = unit.GetComponent<HealthSystem>();
            UnitEnergy energy = unit.GetComponent<UnitEnergy>();
            if (hs == null || energy == null) continue;

            hs.OnMoodDepleted += HandleMoodDepleted;
            hs.OnMoodMaximized += HandleMoodMaximized;
            subscribedHealthSystems.Add(hs);

            EmotypeData current = energy.GetCurrentAspect();
            bool isCosmicUnit = current != null && current.GetPrimaryClass() == EmotypePrimaryClass.Cosmico;

            if (assignInitialFromPoolOnStart && !isCosmicUnit)
            {
                if (TryDrawNextGenito(out EmotypeData drawn))
                {
                    ApplyEmotypeToUnit(unit, energy, hs, drawn);
                }
            }
            else if (current != null && hs.GetRuntimeGestures().Count == 0)
            {
                hs.SetupFromEmotype(current);
            }
        }

        GameGridEvents.TriggerEnergyChanged();
        OnPoolUpdated?.Invoke(sharedGenitoPool.Count, initialPoolSize);
    }

    private void HandleMoodDepleted(object sender, EventArgs e)
    {
        ProcessMoodBoundary(sender as HealthSystem, false);
    }

    private void HandleMoodMaximized(object sender, EventArgs e)
    {
        ProcessMoodBoundary(sender as HealthSystem, true);
    }

    private void ProcessMoodBoundary(HealthSystem hs, bool wasMaterialized)
    {
        if (hs == null) return;

        Unit unit = hs.GetComponent<Unit>();
        UnitEnergy energy = hs.GetComponent<UnitEnergy>();
        if (unit == null || energy == null) return;

        EmotypeData previous = energy.GetCurrentAspect();
        if (previous != null && previous.GetPrimaryClass() != EmotypePrimaryClass.Cosmico)
        {
            discardPile.Add(previous);
        }

        if (TryDrawNextGenito(out EmotypeData nextEmotype))
        {
            ApplyEmotypeToUnit(unit, energy, hs, nextEmotype);

            OnUnitMoodChanged?.Invoke(new MoodChangeInfo
            {
                unit = unit,
                previousEmotype = previous,
                newEmotype = nextEmotype,
                wasMaterialized = wasMaterialized,
                remainingInPool = sharedGenitoPool.Count
            });
        }
        else
        {
            // Pool vacia: notificamos cambio final y fin de relacion
            OnUnitMoodChanged?.Invoke(new MoodChangeInfo
            {
                unit = unit,
                previousEmotype = previous,
                newEmotype = null,
                wasMaterialized = wasMaterialized,
                remainingInPool = 0
            });

            OnPoolExhausted?.Invoke();
        }
        
        // Si el cambio de animo ocurrio dentro de una confrontacion, cerramos el duelo de inmediato
        if (CombatDuelSession.Instance != null && CombatDuelSession.Instance.IsDuelActive())
        {
            StartCoroutine(EndDuelNextFrameRoutine());
        }
    }

    private IEnumerator EndDuelNextFrameRoutine()
    {
        yield return null;

        if (CombatDuelSession.Instance != null && CombatDuelSession.Instance.IsDuelActive())
        {
            CombatDuelSession.Instance.EndDuel();
        }
    }
    
    public bool TryDrawNextGenito(out EmotypeData emotype)
    {
        if (sharedGenitoPool.Count == 0 && recyclePoolWhenEmpty && discardPile.Count > 0)
        {
            ShuffleList(discardPile);
            foreach (EmotypeData e in discardPile)
            {
                sharedGenitoPool.Enqueue(e);
            }
            discardPile.Clear();
        }

        if (sharedGenitoPool.Count > 0)
        {
            emotype = sharedGenitoPool.Dequeue();
            OnPoolUpdated?.Invoke(sharedGenitoPool.Count, initialPoolSize);
            return true;
        }

        emotype = null;
        return false;
    }

    private void ApplyEmotypeToUnit(Unit unit, UnitEnergy energy, HealthSystem hs, EmotypeData newEmotype)
    {
        if (newEmotype == null) return;

        energy.SetEmotype(newEmotype);
        hs.SetupFromEmotype(newEmotype);
        GameGridEvents.TriggerEnergyChanged();

        Debug.Log($"[CAMBIO DE ANIMO] {unit.name} adopta el emotipo: {newEmotype.name} (Quedan {sharedGenitoPool.Count} en pool)");
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    public EmotypeData GetPlayerCosmicEmotype() => playerCosmicEmotype;
    public EmotypeData GetEnemyCosmicEmotype() => enemyCosmicEmotype;
    public int GetRemainingPoolCount() => sharedGenitoPool.Count;
    public int GetInitialPoolSize() => initialPoolSize;
}
