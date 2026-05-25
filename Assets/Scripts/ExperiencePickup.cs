using UnityEngine;

public class ExperiencePickup : MonoBehaviour
{
    private SurvivorGameManager manager;
    private int value;
    private bool heal;
    private float spin;

    public void Configure(SurvivorGameManager owner, int amount, bool healing)
    {
        manager = owner;
        value = amount;
        heal = healing;
        spin = Random.Range(-130f, 130f);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = heal ? SquareSpriteFactory.Heal() : SquareSpriteFactory.Experience();
        spriteRenderer.sortingOrder = 7;
        transform.localScale = Vector3.one * (heal ? 0.75f : 0.48f);
    }

    private void Update()
    {
        if (manager == null || manager.PlayerTransform == null || manager.GameOver)
        {
            return;
        }

        Vector2 toPlayer = manager.PlayerTransform.position - transform.position;
        float distance = toPlayer.magnitude;
        if (distance < manager.MagnetRadius)
        {
            float speed = Mathf.Lerp(16f, 5f, distance / Mathf.Max(0.01f, manager.MagnetRadius));
            transform.position += (Vector3)(toPlayer.normalized * speed * Time.deltaTime);
        }

        transform.Rotate(0f, 0f, spin * Time.deltaTime);

        if (distance <= manager.PickupRadius)
        {
            if (heal)
            {
                manager.HealPlayer(value);
            }
            else
            {
                manager.AddExperience(value);
            }

            Destroy(gameObject);
        }
    }
}
