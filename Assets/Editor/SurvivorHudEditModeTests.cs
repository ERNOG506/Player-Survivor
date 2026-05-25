using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SurvivorHudEditModeTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();
    private float previousTimeScale;

    [SetUp]
    public void SetUp()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = previousTimeScale;

        for (int i = createdObjects.Count - 1; i >= 0; i--)
        {
            if (createdObjects[i] != null)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void AwakeCreatesHudWithScoreHealthAndExperienceReadouts()
    {
        SurvivorGameManager manager = CreateManagerForTest();

        Transform hud = manager.canvas.transform.Find("Survivor HUD");
        Assert.That(hud, Is.Not.Null);
        Assert.That(hud.GetComponent<RectTransform>(), Is.Not.Null);
        Assert.That(hud.gameObject.activeInHierarchy, Is.True);
        Assert.That(hud.Find("Score").GetComponent<Text>().text, Does.Contain("Pontos: 0"));
        Assert.That(hud.Find("Health Label").GetComponent<Text>().text, Is.EqualTo("Vida: 100 / 100"));
        Assert.That(hud.Find("XP Label").GetComponent<Text>().text, Is.EqualTo("XP: 0 / 14"));
        Assert.That(hud.Find("Health Bar/Fill").GetComponent<Image>().fillAmount, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void LethalDamageShowsRestartButtonAndKeepsHudUpdated()
    {
        SurvivorGameManager manager = CreateManagerForTest();

        manager.DamagePlayer(150f);

        Transform hud = manager.canvas.transform.Find("Survivor HUD");
        Transform panel = hud.Find("Game Over Panel");
        Transform restartButton = panel.Find("Restart Button");

        Assert.That(manager.GameOver, Is.True);
        Assert.That(Time.timeScale, Is.EqualTo(0f));
        Assert.That(panel.gameObject.activeSelf, Is.True);
        Assert.That(restartButton.GetComponent<Button>().interactable, Is.True);
        Assert.That(restartButton.Find("Label").GetComponent<Text>().text, Is.EqualTo("Reiniciar"));
        Assert.That(hud.Find("Health Label").GetComponent<Text>().text, Is.EqualTo("Vida: 0 / 100"));
    }

    private SurvivorGameManager CreateManagerForTest()
    {
        EventSystem existingEventSystem = Object.FindFirstObjectByType<EventSystem>();

        GameObject playerObject = new GameObject("Test Player");
        createdObjects.Add(playerObject);
        playerObject.tag = "Player";
        playerObject.AddComponent<PlayerHealth>();

        GameObject canvasObject = new GameObject("Test Canvas");
        createdObjects.Add(canvasObject);
        Canvas canvas = canvasObject.AddComponent<Canvas>();

        GameObject managerObject = new GameObject("Test SurvivorGameManager");
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

        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (existingEventSystem == null && eventSystem != null && !createdObjects.Contains(eventSystem.gameObject))
        {
            createdObjects.Add(eventSystem.gameObject);
        }

        return manager;
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        method.Invoke(target, null);
    }
}
