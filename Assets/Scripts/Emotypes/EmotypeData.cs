using UnityEngine;

[CreateAssetMenu(fileName = "NewEmotype", menuName = "Energy/Emotype")]
public class EmotypeData : ScriptableObject
{
    [SerializeField] string emotypeName;
    
    [Header("Configuracion de Clase")]
    [Tooltip("Selecciona la clase del emotipo.")]
    [SerializeField] private EmotypePrimaryClass primaryClass;
    [SerializeField] private EmotypeSecondaryClass secondaryClass;
    
    [Header("Configuración de Energía")]
    [Tooltip("Selecciona exactamente 2 direcciones para este emotipo.")]
    [SerializeField] private EnergyDirection[] activeDirections = new EnergyDirection[2];
    
    [Header("Configuracion de Stats")]
    [Tooltip("Selecciona las estadisticas del emotipo.")]
    [SerializeField] private int impulse = 1;
    [SerializeField] private int gestures = 1;
    [SerializeField] private int permanency = 1;
    [SerializeField] private int essencials_Knots = 1;
    [SerializeField] private int transferable_Knots = 1;
    [SerializeField] private int total_Knots = 1;
    

    public EnergyDirection[] GetActiveDirections() => activeDirections;
    public EmotypePrimaryClass GetPrimaryClass() => primaryClass;
    public EmotypeSecondaryClass GetSecondaryClass() => secondaryClass;
    public int GetImpulse() => impulse;
    public int GetGestures() => gestures;
    public int GetPermanency() => permanency;
    public int GetEssencials_Knots() => essencials_Knots;
    public int GetTransferable_Knots() => transferable_Knots;
    public int GetTotal_Knots() => total_Knots;
}

public enum EmotypePrimaryClass
{
    Cosmico,
    Rabia,
    Asombro,
    Felicidad,
    Amor,
    Tristeza,
    Asco,
    Deseo,
    Miedo
}

public enum EmotypeSecondaryClass
{
    Rabia,
    Asombro,
    Felicidad,
    Amor,
    Tristeza,
    Asco,
    Deseo,
    Miedo
}

public enum EnergyDirection
{
    North = 0,
    NorthEast = 1,
    East = 2,
    SouthEast = 3,
    South = 4,
    SouthWest = 5,
    West = 6,
    NorthWest = 7
}