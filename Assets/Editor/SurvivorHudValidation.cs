using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class SurvivorHudValidation
{
    public static void Run()
    {
        float previousTimeScale = Time.timeScale;
        List<GameObject> createdObjects = new List<GameObject>();

        try
        {
            ValidateAllSprites();
            ValidateMusicPlayer(createdObjects);
            Cleanup(createdObjects);
            ValidateHud(createdObjects);
            Cleanup(createdObjects);
            ValidatePowerUpChoices(createdObjects);
            Cleanup(createdObjects);
            ValidateSpawnerProgression(createdObjects);
            Cleanup(createdObjects);
            ValidateGameOver(createdObjects);
            Debug.Log("SurvivorHudValidation: all gameplay checks passed.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
        finally
        {
            Time.timeScale = previousTimeScale;
            Cleanup(createdObjects);
        }
    }

    private static void ValidateMusicPlayer(List<GameObject> createdObjects)
    {
        GameMusicPlayer player = GameMusicPlayer.EnsureExists();
        Require(player != null, "Music player must be created.");
        Require(player.GetComponent<AudioSource>() != null, "Music player must have an AudioSource.");
        Require(player.loadFromResources, "Music player must load tracks from Resources/Music.");
        Require(player.volume > 0f && player.volume <= 1f, "Music volume must be valid.");
        Require(Resources.Load<AudioClip>("Music/Speed_Dash_Souffle_Loop") != null, "Loop music must be available in Resources/Music.");
        Require(Resources.Load<AudioClip>("SFX/PlayerShot_Alt") != null, "Shot sound must be available in Resources/SFX.");
        Require(Resources.Load<AudioClip>("SFX/RestartMenu_Alt_Loop") != null, "Restart menu loop must be available in Resources/SFX.");
        Require(Resources.Load<AudioClip>("SFX/EnemyExplosion_Alt") != null, "Enemy explosion sound must be available in Resources/SFX.");
        Require(Resources.Load<AudioClip>("SFX/PlayerExplosion_Alt") != null, "Player explosion sound must be available in Resources/SFX.");
        Require(Resources.Load<AudioClip>("SFX/PowerUpClick_Alt") != null, "Power-up click sound must be available in Resources/SFX.");

        GameSfxPlayer sfxPlayer = GameSfxPlayer.EnsureExists();
        Require(sfxPlayer != null, "SFX player must be created.");
        Require(sfxPlayer.GetComponent<AudioSource>() != null, "SFX player must have an AudioSource.");

        if (!createdObjects.Contains(player.gameObject))
        {
            createdObjects.Add(player.gameObject);
        }

        if (!createdObjects.Contains(sfxPlayer.gameObject))
        {
            createdObjects.Add(sfxPlayer.gameObject);
        }
    }

    private static void ValidateAllSprites()
    {
        Require(SquareSpriteFactory.Player() != null, "Player sprite must be created.");
        Require(SquareSpriteFactory.Bullet(false) != null, "Player bullet sprite must be created.");
        Require(SquareSpriteFactory.Bullet(true) != null, "Enemy bullet sprite must be created.");
        Require(SquareSpriteFactory.Experience() != null, "Experience sprite must be created.");
        Require(SquareSpriteFactory.Heal() != null, "Heal sprite must be created.");
        Require(SquareSpriteFactory.Pixel() != null, "Pixel sprite must be created.");

        foreach (EnemyKind kind in Enum.GetValues(typeof(EnemyKind)))
        {
            Require(SquareSpriteFactory.Enemy(kind) != null, "Enemy sprite must be created: " + kind);
        }
    }

    private static void ValidateHud(List<GameObject> createdObjects)
    {
        SurvivorGameManager manager = CreateManager(createdObjects);
        Transform hud = RequireTransform(manager.canvas.transform, "Survivor HUD");

        Require(hud.GetComponent<RectTransform>() != null, "HUD root must use RectTransform.");
        Require(hud.gameObject.activeInHierarchy, "HUD must be active.");
        Require(RequireText(hud, "Score").text.Contains("Pontos: 0"), "Score text must show zero points on start.");
        Require(RequireText(hud, "Health Label").text == "Vida: 100 / 100", "Health label must show full life on start.");
        Require(RequireText(hud, "XP Label").text == "XP: 0 / 14", "XP label must show starting progress.");
        Require(Mathf.Approximately(RequireImage(hud, "Health Bar/Fill").fillAmount, 1f), "Health bar must start full.");

        ProceduralArena arena = UnityEngine.Object.FindFirstObjectByType<ProceduralArena>();
        Require(arena != null, "Procedural arena must be created.");
        Require(arena.target == manager.player, "Procedural arena must follow the player.");
        Require(!manager.useArenaBounds, "Player must be free to move through procedural arena.");
    }

    private static void ValidatePowerUpChoices(List<GameObject> createdObjects)
    {
        SurvivorGameManager manager = CreateManager(createdObjects);
        manager.AddExperience(14);

        Transform hud = RequireTransform(manager.canvas.transform, "Survivor HUD");
        Transform panel = RequireTransform(hud, "Power Up Panel");
        Require(panel.gameObject.activeSelf, "Power-up panel must appear after collecting enough XP.");
        Require(RequireText(panel, "Title").text.Contains("Nivel 2"), "Power-up title must show the new level.");

        for (int i = 1; i <= 4; i++)
        {
            Transform button = RequireTransform(panel, "PowerUp " + i);
            Require(button.gameObject.activeSelf, "Power-up choice must be visible: " + i);
            Require(button.GetComponent<Button>() != null && button.GetComponent<Button>().interactable, "Power-up button must be interactable: " + i);
            Require(!string.IsNullOrWhiteSpace(RequireText(button, "Label").text), "Power-up label must have text: " + i);
        }
    }

    private static void ValidateSpawnerProgression(List<GameObject> createdObjects)
    {
        SurvivorGameManager manager = CreateManager(createdObjects);

        GameObject spawnerObject = new GameObject("Validation EnemySpawner");
        createdObjects.Add(spawnerObject);
        EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
        spawner.player = manager.player;
        InvokePrivate(spawner, "Start");

        Require(spawner.CurrentMaxEnemies() <= 8, "Starting enemy cap must be low.");
        Require(spawner.CurrentBatchSize() == 1, "Starting spawn batch must be one enemy.");
        Require(spawner.CurrentSpawnInterval() >= 2.5f, "Starting spawn interval must be slow enough.");
        Require(spawner.bossUnlockTime >= 90f, "Boss must be delayed until the run has built up.");
    }

    private static void ValidateGameOver(List<GameObject> createdObjects)
    {
        SurvivorGameManager manager = CreateManager(createdObjects);
        manager.DamagePlayer(150f);

        Transform hud = RequireTransform(manager.canvas.transform, "Survivor HUD");
        Transform panel = RequireTransform(hud, "Game Over Panel");
        Transform button = RequireTransform(panel, "Restart Button");

        Require(manager.GameOver, "Manager must enter game over after lethal damage.");
        Require(Mathf.Approximately(Time.timeScale, 0f), "Game must pause after lethal damage.");
        Require(panel.gameObject.activeSelf, "Game over panel must be visible.");
        Require(button.GetComponent<Button>() != null && button.GetComponent<Button>().interactable, "Restart button must be interactable.");
        Require(RequireText(button, "Label").text == "Reiniciar", "Restart button label must be Reiniciar.");
        Require(RequireText(hud, "Health Label").text == "Vida: 0 / 100", "Health label must show zero life after death.");
    }

    private static SurvivorGameManager CreateManager(List<GameObject> createdObjects)
    {
        EventSystem existingEventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();

        GameObject playerObject = new GameObject("Validation Player");
        createdObjects.Add(playerObject);
        playerObject.tag = "Player";
        playerObject.AddComponent<PlayerHealth>();

        GameObject canvasObject = new GameObject("Validation Canvas");
        createdObjects.Add(canvasObject);
        Canvas canvas = canvasObject.AddComponent<Canvas>();

        GameObject managerObject = new GameObject("Validation SurvivorGameManager");
        managerObject.SetActive(false);
        createdObjects.Add(managerObject);
        SurvivorGameManager manager = managerObject.AddComponent<SurvivorGameManager>();
        manager.player = playerObject.transform;
        manager.canvas = canvas;

        InvokePrivate(manager, "Awake");
        InvokePrivate(manager, "Start");

        GameObject grid = GameObject.Find("Survivor Arena Grid");
        if (grid != null && !createdObjects.Contains(grid))
        {
            createdObjects.Add(grid);
        }

        EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        if (existingEventSystem == null && eventSystem != null && !createdObjects.Contains(eventSystem.gameObject))
        {
            createdObjects.Add(eventSystem.gameObject);
        }

        ProceduralArena arena = UnityEngine.Object.FindFirstObjectByType<ProceduralArena>();
        if (arena != null && !createdObjects.Contains(arena.gameObject))
        {
            createdObjects.Add(arena.gameObject);
        }

        return manager;
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Require(method != null, "Missing private method: " + methodName);
        method.Invoke(target, null);
    }

    private static Transform RequireTransform(Transform parent, string path)
    {
        Transform child = parent.Find(path);
        Require(child != null, "Missing object: " + path);
        return child;
    }

    private static Text RequireText(Transform parent, string path)
    {
        Text text = RequireTransform(parent, path).GetComponent<Text>();
        Require(text != null, "Missing Text component: " + path);
        return text;
    }

    private static Image RequireImage(Transform parent, string path)
    {
        Image image = RequireTransform(parent, path).GetComponent<Image>();
        Require(image != null, "Missing Image component: " + path);
        return image;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("SurvivorHudValidation failed: " + message);
        }
    }

    private static void Cleanup(List<GameObject> createdObjects)
    {
        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
    }
}
