using UnityEngine;

public class EnemyController : MonoBehaviour
{
    private SurvivorGameManager manager;
    private SpriteRenderer spriteRenderer;
    private float health;
    private float maxHealth;
    private float speed;
    private float touchDamage;
    private float contactTimer;
    private float shotTimer;
    private float pulseOffset;
    private int xpValue;
    private int scoreValue;
    private bool dead;

    public EnemyKind Kind { get; private set; }
    public float Radius { get; private set; } = 0.5f;
    public bool IsDead => dead;

    public void Configure(SurvivorGameManager owner, EnemyKind kind, int wave)
    {
        manager = owner;
        Kind = kind;
        pulseOffset = Random.value * 10f;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        ApplyStats(Mathf.Max(1, wave));
        spriteRenderer.sprite = SquareSpriteFactory.Enemy(kind);
        spriteRenderer.sortingOrder = 5;
        spriteRenderer.color = Color.white;
        gameObject.tag = "Enemy";
        transform.localScale = Vector3.one * Radius * 2f;

        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
        }
    }

    private void Start()
    {
        if (manager == null)
        {
            Configure(SurvivorGameManager.InstanceOrCreate(), EnemyKind.Grunt, 1);
        }
    }

    private void Update()
    {
        if (manager == null || manager.PlayerTransform == null || dead || manager.GameOver)
        {
            return;
        }

        Vector2 toPlayer = manager.PlayerTransform.position - transform.position;
        float distance = toPlayer.magnitude;
        Vector2 direction = distance > 0.01f ? toPlayer / distance : Random.insideUnitCircle.normalized;

        bool holdRange = (Kind == EnemyKind.Shooter && distance < 5.4f) || (Kind == EnemyKind.Boss && distance < 4.2f);
        if (!holdRange && manager.CanRunGame)
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        HandleContact(distance);
        HandleShooting(distance, direction);

        float pulse = 1f + Mathf.Sin(Time.time * 5f + pulseOffset) * 0.035f;
        transform.localScale = Vector3.one * Radius * 2f * pulse;
    }

    public void TakeDamage(float amount)
    {
        if (dead)
        {
            return;
        }

        health -= amount;
        if (spriteRenderer != null)
        {
            float hurt = 1f - Mathf.Clamp01(health / maxHealth);
            spriteRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.48f, 0.48f), hurt);
        }

        if (health <= 0f)
        {
            dead = true;
            ExplosionEffect.SpawnEnemy(transform.position, Radius);
            if (manager != null)
            {
                manager.EnemyKilled(this, xpValue, scoreValue);
            }

            Destroy(gameObject);
        }
    }

    private void HandleContact(float distance)
    {
        contactTimer -= Time.deltaTime;
        if (contactTimer > 0f || distance > Radius + 0.55f)
        {
            return;
        }

        contactTimer = 0.55f;
        manager.DamagePlayer(touchDamage);
    }

    private void HandleShooting(float distance, Vector2 direction)
    {
        if (Kind != EnemyKind.Shooter && Kind != EnemyKind.Boss)
        {
            return;
        }

        shotTimer -= Time.deltaTime;
        if (shotTimer > 0f || distance > 9f)
        {
            return;
        }

        shotTimer = Kind == EnemyKind.Boss ? 1.1f : 2.05f;
        manager.SpawnEnemyBullet(transform.position, direction, Kind == EnemyKind.Boss ? 14f : 8f);
    }

    private void ApplyStats(int wave)
    {
        float scale = 1f + (wave - 1) * 0.17f;
        switch (Kind)
        {
            case EnemyKind.Runner:
                maxHealth = 16f * scale;
                speed = 3.9f + wave * 0.04f;
                touchDamage = 8f;
                xpValue = 2;
                scoreValue = 18;
                Radius = 0.32f;
                break;
            case EnemyKind.Tank:
                maxHealth = 80f * scale;
                speed = 1.35f + wave * 0.025f;
                touchDamage = 18f;
                xpValue = 7;
                scoreValue = 55;
                Radius = 0.72f;
                break;
            case EnemyKind.Splitter:
                maxHealth = 38f * scale;
                speed = 2.15f + wave * 0.03f;
                touchDamage = 12f;
                xpValue = 5;
                scoreValue = 38;
                Radius = 0.5f;
                break;
            case EnemyKind.Shooter:
                maxHealth = 34f * scale;
                speed = 1.65f + wave * 0.02f;
                touchDamage = 7f;
                xpValue = 6;
                scoreValue = 48;
                Radius = 0.48f;
                shotTimer = Random.Range(0.35f, 1.4f);
                break;
            case EnemyKind.Mini:
                maxHealth = 9f * scale;
                speed = 3.25f + wave * 0.04f;
                touchDamage = 6f;
                xpValue = 1;
                scoreValue = 8;
                Radius = 0.23f;
                break;
            case EnemyKind.Boss:
                maxHealth = 420f * scale;
                speed = 1.12f + wave * 0.02f;
                touchDamage = 30f;
                xpValue = 45;
                scoreValue = 650;
                Radius = 1.15f;
                shotTimer = 0.8f;
                break;
            default:
                maxHealth = 24f * scale;
                speed = 2.35f + wave * 0.035f;
                touchDamage = 10f;
                xpValue = 3;
                scoreValue = 24;
                Radius = 0.43f;
                break;
        }

        health = maxHealth;
    }
}
