using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform player;
    public float spawnDistance = 12f;
    public float spawnRate = 2f;
    public float firstSpawnDelay = 2.5f;
    public int baseMaxEnemies = 7;
    public int maxEnemiesPerWave = 3;
    public int hardMaxEnemies = 48;
    public float bossUnlockTime = 110f;

    private SurvivorGameManager manager;
    private float spawnTimer;
    private float bossTimer;

    private void Start()
    {
        spawnDistance = Mathf.Max(spawnDistance, 12f);
        spawnRate = Mathf.Max(spawnRate, 2f);
        firstSpawnDelay = Mathf.Max(firstSpawnDelay, 2.5f);
        baseMaxEnemies = Mathf.Clamp(baseMaxEnemies, 5, 9);
        maxEnemiesPerWave = Mathf.Clamp(maxEnemiesPerWave, 2, 4);
        hardMaxEnemies = Mathf.Clamp(hardMaxEnemies, 32, 55);
        bossUnlockTime = Mathf.Max(bossUnlockTime, 110f);

        manager = SurvivorGameManager.InstanceOrCreate();
        if (manager != null)
        {
            manager.RegisterSpawner(this);
        }

        if (player == null && manager.PlayerTransform != null)
        {
            player = manager.PlayerTransform;
        }

        spawnTimer = firstSpawnDelay;
        bossTimer = bossUnlockTime;
    }

    private void Update()
    {
        if (manager == null || !manager.CanRunGame || enemyPrefab == null || player == null)
        {
            return;
        }

        int aliveEnemies = CountAliveEnemies();
        int maxEnemies = CurrentMaxEnemies();

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            int openSlots = Mathf.Max(0, maxEnemies - aliveEnemies);
            int count = Mathf.Min(CurrentBatchSize(), openSlots);
            for (int i = 0; i < count; i++)
            {
                SpawnSpecific(ChooseKind(), RandomSpawnPosition());
            }

            spawnTimer = openSlots > 0 ? CurrentSpawnInterval() : 0.55f;
        }

        bossTimer -= Time.deltaTime;
        if (manager.ElapsedSeconds >= bossUnlockTime && bossTimer <= 0f && aliveEnemies < maxEnemies + 1)
        {
            SpawnSpecific(EnemyKind.Boss, RandomSpawnPosition());
            bossTimer = Mathf.Max(55f, 110f - manager.Wave * 3f);
        }
    }

    public EnemyController SpawnSpecific(EnemyKind kind, Vector3 position)
    {
        if (enemyPrefab == null)
        {
            return null;
        }

        GameObject enemyObject = Instantiate(enemyPrefab, position, Quaternion.identity);
        EnemyController controller = enemyObject.GetComponent<EnemyController>();
        if (controller == null)
        {
            controller = enemyObject.AddComponent<EnemyController>();
        }

        controller.Configure(manager == null ? SurvivorGameManager.InstanceOrCreate() : manager, kind, manager == null ? 1 : manager.Wave);
        return controller;
    }

    public int CurrentMaxEnemies()
    {
        int wave = manager == null ? 1 : manager.Wave;
        return Mathf.Min(hardMaxEnemies, baseMaxEnemies + Mathf.Max(0, wave - 1) * maxEnemiesPerWave);
    }

    public float CurrentSpawnInterval()
    {
        int wave = manager == null ? 1 : manager.Wave;
        float progress = Mathf.InverseLerp(1f, 12f, wave);
        return Mathf.Lerp(spawnRate + 0.9f, 0.65f, progress);
    }

    public int CurrentBatchSize()
    {
        int wave = manager == null ? 1 : manager.Wave;
        return Mathf.Clamp(1 + Mathf.FloorToInt((wave - 1) / 4f), 1, 3);
    }

    private EnemyKind ChooseKind()
    {
        float roll = Random.value;
        int wave = manager == null ? 1 : manager.Wave;
        if (wave >= 7 && roll > 0.9f) return EnemyKind.Shooter;
        if (wave >= 5 && roll > 0.78f) return EnemyKind.Splitter;
        if (wave >= 4 && roll > 0.63f) return EnemyKind.Tank;
        if (wave >= 2 && roll > 0.42f) return EnemyKind.Runner;
        return EnemyKind.Grunt;
    }

    private Vector3 RandomSpawnPosition()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        if (randomDirection.sqrMagnitude < 0.01f)
        {
            randomDirection = Vector2.up;
        }

        float distance = spawnDistance;
        Camera camera = Camera.main;
        if (camera != null)
        {
            distance = Mathf.Max(distance, camera.orthographicSize * 1.85f);
        }

        return player.position + (Vector3)(randomDirection * distance);
    }

    private int CountAliveEnemies()
    {
        EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        int count = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null && !enemies[i].IsDead)
            {
                count++;
            }
        }

        return count;
    }
}
