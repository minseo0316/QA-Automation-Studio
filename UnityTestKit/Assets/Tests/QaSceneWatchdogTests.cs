using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class QaSceneWatchdogTests
{
    [TearDown]
    public void TearDown()
    {
        QaScreenshotCapture.SaveIfCurrentTestFailed();
        CleanupQaObjects();
    }

    [UnityTest]
    [Order(1)]
    public IEnumerator RuntimeSceneWatchdog_HealthyScene_ReproductionScenario()
    {
        KnightShift_BugReproductionTests.BuildQaCaptureScene("QA_WatchdogHealthyScene", false);
        yield return null;
        yield return null;

        List<string> issues = QaSceneWatchdog.CollectCriticalIssues();
        Assert.IsEmpty(issues, string.Join("\n", issues));
    }

    [UnityTest]
    [Order(2)]
    public IEnumerator RuntimeSceneWatchdog_DetectsMissingEnemyBody_ReproductionScenario()
    {
        KnightShift_BugReproductionTests.BuildQaCaptureScene("QA_WatchdogBrokenEnemyScene", false);
        yield return null;
        yield return null;

        GameObject enemyBody = GameObject.Find("QA_Enemy_Body");
        Assert.IsNotNull(enemyBody, "QA_Enemy_Body should exist before we simulate a runtime bug.");
        Object.DestroyImmediate(enemyBody);

        yield return null;
        yield return null;

        List<string> issues = QaSceneWatchdog.CollectCriticalIssues();
        Assert.IsEmpty(issues, string.Join("\n", issues));
    }

    private static void CleanupQaObjects()
    {
        foreach (GameObject gameObject in Object.FindObjectsOfType<GameObject>())
        {
            if (gameObject != null && gameObject.name.StartsWith("QA_"))
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}

public static class QaSceneWatchdog
{
    private static readonly string[] RequiredObjectNames =
    {
        "QA_CaptureCamera",
        "QA_Player_Body",
        "QA_Player_Head",
        "QA_Enemy_Body",
        "QA_Enemy_Head",
        "QA_Ground",
        "QA_HudFrame",
        "QA_HealthBar"
    };

    public static List<string> CollectCriticalIssues()
    {
        List<string> issues = new List<string>();

        Camera mainCamera = Camera.main;
        if (mainCamera == null || !mainCamera.enabled || !mainCamera.gameObject.activeInHierarchy)
        {
            issues.Add("Main camera is missing or inactive.");
        }

        foreach (string objectName in RequiredObjectNames)
        {
            if (GameObject.Find(objectName) == null)
            {
                issues.Add($"Required object is missing: {objectName}");
            }
        }

        foreach (GameObject gameObject in Object.FindObjectsOfType<GameObject>())
        {
            if (gameObject == null || !gameObject.scene.IsValid() || !gameObject.scene.isLoaded || !gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 position = gameObject.transform.position;
            if (IsInvalid(position))
            {
                issues.Add($"Invalid transform position detected on {gameObject.name}.");
            }

            Vector3 scale = gameObject.transform.localScale;
            if (IsInvalid(scale))
            {
                issues.Add($"Invalid transform scale detected on {gameObject.name}.");
            }
        }

        return issues;
    }

    private static bool IsInvalid(Vector3 value)
    {
        return float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z)
            || float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z);
    }
}

