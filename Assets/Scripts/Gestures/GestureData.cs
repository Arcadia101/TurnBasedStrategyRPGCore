using UnityEngine;

[CreateAssetMenu(fileName = "NewGesture", menuName = "Combat/Gesture Data")]
public class GestureData : ScriptableObject
{
    [Header("Identificación")]
    [SerializeField] private string gestureName;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [SerializeField] private EmotypeSecondaryClass gestureAffinity;

    [Header("Valores de Combate")]
    [SerializeField] private int baseAffectationValue = 10; // Daño o Afectación de Ánimo
    [SerializeField] private int maxUses = 3;

    // Getters públicos
    public string GetGestureName() => gestureName;
    public string GetDescription() => description;
    public EmotypeSecondaryClass GetAffinity() => gestureAffinity;
    public int GetBaseAffectationValue() => baseAffectationValue;
    public int GetMaxUses() => maxUses;
}
