using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public bool IsAlive => isAlive;

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

    public void RestoreHealthForSave(int value)
    {
        currentHealth = Mathf.Clamp(value, 1, maxHealth);
        isAlive = true;
        HealthChanged?.Invoke(currentHealth, maxHealth);
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
            RequestRespawn();
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

    private void RequestRespawn()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Respawn();
            return;
        }

        // 兜底：即便场景中漏挂 GameManager，也能重载当前场景继续游玩。
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }
}