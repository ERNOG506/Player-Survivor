using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public Slider healthSlider;

    private SurvivorGameManager manager;
    private bool syncingFromManager;

    private void Start()
    {
        currentHealth = maxHealth;
        manager = SurvivorGameManager.InstanceOrCreate();
        if (manager != null && manager.player == null)
        {
            manager.player = transform;
        }

        UpdateLegacySlider();
    }

    public void TakeDamage(int damage)
    {
        if (syncingFromManager)
        {
            return;
        }

        if (manager != null)
        {
            manager.DamagePlayer(damage);
            return;
        }

        currentHealth -= damage;
        UpdateLegacySlider();
        if (currentHealth <= 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void SyncFromManager(int current, int maximum)
    {
        syncingFromManager = true;
        maxHealth = maximum;
        currentHealth = current;
        UpdateLegacySlider();
        syncingFromManager = false;
    }

    private void UpdateLegacySlider()
    {
        if (healthSlider == null)
        {
            return;
        }

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }
}
