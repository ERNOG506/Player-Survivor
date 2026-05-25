using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SurvivorSceneAutoInstaller
{
    private const string ScenePath = "Assets/Scenes 1/Jogo.unity";

    [InitializeOnLoadMethod]
    private static void InstallAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (EditorSceneManager.GetActiveScene().path == ScenePath)
            {
                InstallInOpenScene(saveScene: true);
            }
        };
    }

    [MenuItem("Survivor Squares/Aplicar na cena Jogo")]
    public static void InstallInOpenSceneMenu()
    {
        InstallInOpenScene(saveScene: true);
    }

    public static void InstallInOpenScene(bool saveScene)
    {
        SurvivorGameManager manager = Object.FindFirstObjectByType<SurvivorGameManager>();
        if (manager == null)
        {
            GameObject managerObject = new GameObject("SurvivorGameManager");
            manager = managerObject.AddComponent<SurvivorGameManager>();
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        PlayerAttack attack = player == null ? null : player.GetComponent<PlayerAttack>();

        if (spawner != null)
        {
            SerializedObject spawnerSerialized = new SerializedObject(spawner);
            spawnerSerialized.FindProperty("spawnDistance").floatValue = 12f;
            spawnerSerialized.FindProperty("spawnRate").floatValue = 2f;
            spawnerSerialized.FindProperty("firstSpawnDelay").floatValue = 2.5f;
            spawnerSerialized.FindProperty("baseMaxEnemies").intValue = 7;
            spawnerSerialized.FindProperty("maxEnemiesPerWave").intValue = 3;
            spawnerSerialized.FindProperty("hardMaxEnemies").intValue = 48;
            spawnerSerialized.FindProperty("bossUnlockTime").floatValue = 110f;
            spawnerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        SerializedObject serialized = new SerializedObject(manager);
        serialized.FindProperty("player").objectReferenceValue = player == null ? null : player.transform;
        serialized.FindProperty("canvas").objectReferenceValue = canvas;
        serialized.FindProperty("enemySpawner").objectReferenceValue = spawner;
        serialized.FindProperty("enemyPrefab").objectReferenceValue = spawner == null ? null : spawner.enemyPrefab;
        serialized.FindProperty("bulletPrefab").objectReferenceValue = attack == null ? null : attack.bulletPrefab;
        serialized.FindProperty("useProceduralArena").boolValue = true;
        serialized.FindProperty("useArenaBounds").boolValue = false;
        serialized.FindProperty("cameraOrthographicSize").floatValue = 6.8f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.backgroundColor = new Color(0.055f, 0.06f, 0.085f);
            camera.orthographic = true;
            camera.orthographicSize = 6.8f;
            EditorUtility.SetDirty(camera);
        }

        GameObject background = GameObject.Find("Background");
        if (background != null)
        {
            SpriteRenderer renderer = background.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = new Color(0.055f, 0.06f, 0.085f);
                renderer.sortingOrder = -10;
                background.transform.position = Vector3.zero;
                background.transform.localScale = new Vector3(34f, 20f, 1f);
                EditorUtility.SetDirty(renderer);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        if (saveScene && !string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }

    [MenuItem("Survivor Squares/Abrir cena Jogo e aplicar")]
    public static void OpenAndInstall()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogError("Cena nao encontrada: " + ScenePath);
            return;
        }

        EditorSceneManager.OpenScene(ScenePath);
        InstallInOpenScene(saveScene: true);
    }
}
