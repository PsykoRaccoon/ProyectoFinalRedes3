using Mirror;
using TMPro;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    public float maxHealth;
    public float currentHealth;

    public TextMeshProUGUI healthText;

    void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        UpdateHealthUI();
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {currentHealth}/{maxHealth}";
        }
    }

    public bool Heal(float amount)
    {
        if (currentHealth >= maxHealth)
            return false;


        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateHealthUI();
        return true;
    }

    void Die()
    {
        print("Morido");
        gameObject.SetActive(false);
    }
}