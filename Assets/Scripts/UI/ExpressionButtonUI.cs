using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExpressionButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI usesText;

    private RuntimeGesture runtimeGesture;

    public void Setup(RuntimeGesture gesture, Action<RuntimeGesture> onSelected)
    {
        runtimeGesture = gesture;
        GestureData data = gesture.GetData();

        if (nameText != null)
            nameText.text = data != null ? data.GetGestureName() : "Expresion";

        if (powerText != null)
            powerText.text = data != null ? $"Potencia: {data.GetBaseAffectationValue()}" : "Potencia: 0";

        if (usesText != null)
        {
            int maxUses = data != null ? data.GetMaxUses() : 0;
            usesText.text = $"{gesture.GetCurrentUses()}/{maxUses}";
        }

        if (backgroundImage != null && data != null)
        {
            backgroundImage.color = GetAffinityColor(data.GetAffinity());
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = gesture.HasUses();
            button.onClick.AddListener(() =>
            {
                if (runtimeGesture != null && runtimeGesture.HasUses())
                {
                    onSelected?.Invoke(runtimeGesture);
                }
            });
        }
    }

    public Button GetButton() => button;

    public static Color GetAffinityColor(EmotypeSecondaryClass affinity)
    {
        switch (affinity)
        {
            case EmotypeSecondaryClass.Rabia:     return new Color(0.84f, 0.12f, 0.15f, 1f); // Rojo
            case EmotypeSecondaryClass.Asombro:   return new Color(0.96f, 0.77f, 0.11f, 1f); // Amarillo
            case EmotypeSecondaryClass.Felicidad: return new Color(0.95f, 0.40f, 0.14f, 1f); // Naranja
            case EmotypeSecondaryClass.Amor:      return new Color(0.91f, 0.36f, 0.62f, 1f); // Rosa
            case EmotypeSecondaryClass.Tristeza:  return new Color(0.13f, 0.61f, 0.88f, 1f); // Azul
            case EmotypeSecondaryClass.Asco:      return new Color(0.47f, 0.75f, 0.15f, 1f); // Verde
            case EmotypeSecondaryClass.Deseo:     return new Color(0.56f, 0.27f, 0.68f, 1f); // Morado
            case EmotypeSecondaryClass.Miedo:     return new Color(0.48f, 0.53f, 0.56f, 1f); // Gris
            default:                              return new Color(0.25f, 0.25f, 0.25f, 1f);
        }
    }
}
