using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 8f;
    public Vector3 direction;
    public float lifeTime = 2f;

    private float damage = 10f;
    private int pierceLeft;
    private bool hostile;
    private float radius = 0.24f;
    private SurvivorGameManager manager;

    public void Configure(Vector2 shotDirection, float shotDamage, float shotSpeed, int pierce, bool enemyBullet, float shotRadius)
    {
        manager = SurvivorGameManager.InstanceOrCreate();
        direction = shotDirection.sqrMagnitude > 0.01f ? shotDirection.normalized : Vector2.right;
        damage = shotDamage;
        speed = shotSpeed;
        pierceLeft = pierce;
        hostile = enemyBullet;
        radius = shotRadius;
        lifeTime = hostile ? 5.5f : 2.6f;
        ApplyVisuals();
    }

    private void Start()
    {
        if (manager == null)
        {
            manager = SurvivorGameManager.InstanceOrCreate();
        }

        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector2.right;
        }

        ApplyVisuals();
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += direction.normalized * speed * Time.deltaTime;
        transform.Rotate(0f, 0f, (hostile ? -260f : 380f) * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hostile)
        {
            if (other.CompareTag("Player"))
            {
                if (manager != null)
                {
                    manager.DamagePlayer(damage);
                }
                Destroy(gameObject);
            }

            return;
        }

        if (!other.CompareTag("Enemy"))
        {
            return;
        }

        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
        else
        {
            Destroy(other.gameObject);
        }

        pierceLeft--;
        if (pierceLeft < 0)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyVisuals()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = SquareSpriteFactory.Bullet(hostile);
        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = hostile ? 8 : 9;
        transform.localScale = Vector3.one * radius * 2f;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }
}
