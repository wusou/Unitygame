using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text levelNameText;

    private PlayerHealth playerHealth;

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerHealth = playerObj.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.HealthChanged += OnHealthChanged;
                OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }
        }

        if (levelNameText != null && GameManager.Instance != null)
        {
            levelNameText.text = GameManager.Instance.GetCurrentLevelName();
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged(int hp, int maxHp)
    {
        if (healthBar == null)
        {
            return;
        }

        healthBar.maxValue = maxHp;
        healthBar.value = hp;
    }
}
