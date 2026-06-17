using UnityEngine;

[CreateAssetMenu(fileName = "NewEmotype", menuName = "Energy/Emotype")]
public class EmotypeData : ScriptableObject
{
    [SerializeField] string emotypeName;
    
    [Header("Configuración de Energía")]
    [Tooltip("Selecciona exactamente 2 direcciones para este emotipo.")]
    [SerializeField] private EnergyDirection[] activeDirections = new EnergyDirection[2];

    public EnergyDirection[] GetActiveDirections() => activeDirections;
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