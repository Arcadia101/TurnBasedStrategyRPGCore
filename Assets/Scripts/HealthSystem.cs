using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public event EventHandler OnDead;
    //public event EventHandler OnHealed;  future addition
    public event EventHandler OnDamaged;
    
    [SerializeField] private int maxHealth = 100;
    private int health;
    
    private void Awake()
    {
        health = maxHealth;
    }

    public void Damage(int damageAmount)
    {
        health -= damageAmount;

        if (health < 0)
        {
            health = 0;
        }

        OnDamaged?.Invoke(this, EventArgs.Empty);

        if (health == 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        OnDead?.Invoke(this, EventArgs.Empty);
    }

    public float GetHealthNormalized()
    {
        return (float)health / (float)maxHealth;
    }
}
