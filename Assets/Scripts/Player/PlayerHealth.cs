using System;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("生命值")]
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;
    private bool isAlive = true;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public event Action<int, int> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        currentHealth = maxHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (!isAlive || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (!isAlive)
        {
            return;
        }

        isAlive = false;
        Died?.Invoke();

        GameManager.Instance?.OnPlayerDeath(transform.position);

        var sr = GetComponent<SpriteRenderer>();
        sr.color = Color.black;

        var controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        var combat = GetComponent<PlayerCombat>();
        if (combat != null)
        {
            combat.enabled = false;
        }

        var rb = GetComponent<Rigidbody2D>();
        rb.simulated = false;

        Invoke(nameof(GoToDeathScreen), 1f);
    }

    private void GoToDeathScreen()
    {
        GameManager.Instance?.Respawn();
    }
}
