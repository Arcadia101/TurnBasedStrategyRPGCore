using System;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    // Eventos para la UI y la lógica de juego
    public event EventHandler OnDamaged;           // Notifica cambios para actualizar barras/animaciones
    public event EventHandler OnMoodDepleted;       // Llega a 0 -> Se envía a la Arquetopía
    public event EventHandler OnMoodMaximized;      // Llega al Máximo -> Se Materializa

    [Header("Configuración Base")]
    [SerializeField] private int maxHealth = 100;
    private int health;

    // Lista viva de gestos con sus usos en partida
    private List<RuntimeGesture> runtimeGestures = new List<RuntimeGesture>();

    private void Awake()
    {
        health = maxHealth / 2;
    }
    
    // Configura la salud y carga los gestos según el EmotypeData equipado
    public void SetupFromEmotype(EmotypeData emotypeData)
    {
        runtimeGestures.Clear();

        if (emotypeData == null) return;

        maxHealth = emotypeData.GetMaxMoodValue();
        health = emotypeData.GetInitialMoodValue();

        // Poblamos la lista de gestos disponibles en tiempo de ejecución
        foreach (GestureData gesture in emotypeData.GetAvailableGestures())
        {
            if (gesture != null)
            {
                runtimeGestures.Add(new RuntimeGesture(gesture));
            }
        }

        OnDamaged?.Invoke(this, EventArgs.Empty);
    }
    
    // Aplica reduccion (reduce los nudos hacia 0 / Arquetopía)
    public void Release(int damageAmount)
    {
        health -= damageAmount;

        if (health < 0)
        {
            health = 0;
        }

        OnDamaged?.Invoke(this, EventArgs.Empty);

        if (health == 0)
        {
            OnMoodDepleted?.Invoke(this, EventArgs.Empty);
        }
    }
    
    // Aplica aumento (sube los nudos hacia el Máximo / Materialización)
    public void Restrain(int healAmount)
    {
        health += healAmount;

        if (health > maxHealth)
        {
            health = maxHealth;
        }
        
        OnDamaged?.Invoke(this, EventArgs.Empty);
        
        if (health == maxHealth)
        {
            OnMoodMaximized?.Invoke(this, EventArgs.Empty);
        }
    }
    
    // Restaura todos los usos de los gestos (se llamará en turnos impares)
    public void RestoreAllGestureUses()
    {
        foreach (RuntimeGesture gesture in runtimeGestures)
        {
            gesture.ResetUses();
        }
    }

    // --- GETTERS ---
    public int GetHealth() => health;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthNormalized() => (float)health / (float)maxHealth;
    public List<RuntimeGesture> GetRuntimeGestures() => runtimeGestures;
    public bool IsAtBoundary() => health <= 0 || health >= maxHealth;
}