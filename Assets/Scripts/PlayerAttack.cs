using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public GameObject bulletPrefab;
    public float attackRate = 1f;
    public AudioClip shotSound;
    public float shotVolume = 0.55f;

    private SurvivorGameManager manager;
    private float attackTimer;
    private AudioSource shotAudioSource;

    private void Start()
    {
        manager = SurvivorGameManager.InstanceOrCreate();
        if (manager != null)
        {
            manager.RegisterAttack(this);
        }

        if (shotSound == null)
        {
            shotSound = Resources.Load<AudioClip>("SFX/PlayerShot_Alt");
        }

        shotAudioSource = GetComponent<AudioSource>();
        if (shotAudioSource == null)
        {
            shotAudioSource = gameObject.AddComponent<AudioSource>();
        }

        shotAudioSource.playOnAwake = false;
        shotAudioSource.spatialBlend = 0f;
    }

    private void Update()
    {
        if (manager == null || !manager.CanRunGame)
        {
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f)
        {
            return;
        }

        EnemyController nearestEnemy = manager.FindNearestEnemy(transform.position);
        if (nearestEnemy == null)
        {
            return;
        }

        attackTimer = 1f / Mathf.Max(0.05f, manager.AttackRate);
        Vector2 baseDirection = (nearestEnemy.transform.position - transform.position).normalized;
        int shots = Mathf.Max(1, manager.ProjectileCount);
        float spreadStep = shots == 1 ? 0f : 10f;
        float startAngle = -spreadStep * (shots - 1) * 0.5f;

        PlayShotSound();
        for (int i = 0; i < shots; i++)
        {
            Vector2 direction = Rotate(baseDirection, startAngle + spreadStep * i);
            Fire(direction);
        }
    }

    private void Fire(Vector2 direction)
    {
        if (bulletPrefab == null)
        {
            return;
        }

        GameObject bulletObject = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet == null)
        {
            bullet = bulletObject.AddComponent<Bullet>();
        }

        bullet.Configure(direction, manager.WeaponDamage, manager.ProjectileSpeed, manager.ProjectilePierce, false, manager.ProjectileRadius);
    }

    private void PlayShotSound()
    {
        if (shotAudioSource != null && shotSound != null)
        {
            shotAudioSource.PlayOneShot(shotSound, Mathf.Clamp01(shotVolume));
        }
    }

    private static Vector2 Rotate(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos).normalized;
    }
}
