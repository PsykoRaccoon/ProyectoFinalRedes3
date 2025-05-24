using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth;
    public float currentHealth;

    public TextMeshProUGUI healthText;

    void Start()
    {
        currentHealth = 80;
        maxHealth = 100;
    }

    private void Update()
    {
        UpdateHealthUI();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

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

        return true;
    }

    void Die()
    {
        print("Morido");
        gameObject.SetActive(false);
    }
}