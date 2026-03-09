using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("生命值")]
    [SerializeField] private int maxHealth = 100;

    [Header("死亡表现")]
    [SerializeField] private Animator animator;
    [SerializeField] private string deathTrigger = "Die";
    [SerializeField] private float deathAnimationDuration = 2f;
    [SerializeField] private float disappearDelay = 0f;
    [SerializeField] private bool hideAfterDeath = true;
    [SerializeField] private bool respawnAfterDeath = true;

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

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
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

        DisableControlOnDeath();

        if (animator != null && !string.IsNullOrWhiteSpace(deathTrigger))
        {
            animator.SetTrigger(deathTrigger);
        }

        StartCoroutine(DeathSequence());
    }

    private void DisableControlOnDeath()
    {
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
        if (rb != null)
        {
            rb.simulated = false;
        }

        var allColliders = GetComponentsInChildren<Collider2D>(true);
        for (var i = 0; i < allColliders.Length; i++)
        {
            allColliders[i].enabled = false;
        }
    }

    private IEnumerator DeathSequence()
    {
        if (deathAnimationDuration > 0f)
        {
            yield return new WaitForSeconds(deathAnimationDuration);
        }

        if (hideAfterDeath)
        {
            HideCharacterVisuals();
        }

        if (disappearDelay > 0f)
        {
            yield return new WaitForSeconds(disappearDelay);
        }

        if (respawnAfterDeath)
        {
            GameManager.Instance?.Respawn();
        }
    }

    private void HideCharacterVisuals()
    {
        var spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (var i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].enabled = false;
        }
    }
}
