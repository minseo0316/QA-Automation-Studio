using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Development-only defect triggers used to demonstrate RuntimeQaMonitor.</summary>
[DefaultExecutionOrder(-9999)]
public sealed class RuntimeQaDemoTrigger : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallAutomatically()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (FindFirstObjectByType<RuntimeQaDemoTrigger>() == null)
        {
            GameObject triggerObject = new GameObject("QA_DemoTrigger");
            DontDestroyOnLoad(triggerObject);
            triggerObject.AddComponent<RuntimeQaDemoTrigger>();
        }
#endif
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f8Key.wasPressedThisFrame) TriggerInvalidPlayerHealth();
        if (keyboard.f9Key.wasPressedThisFrame) TriggerMissingEnemy();
        if (keyboard.f10Key.wasPressedThisFrame) TriggerRuntimeException();
#endif
    }

    private static void TriggerInvalidPlayerHealth()
    {
        PlayerStatus player = FindFirstObjectByType<PlayerStatus>();
        if (player == null)
        {
            Debug.LogError("[QA Demo] F8 failed: PlayerStatus was not found in the active scene.");
            return;
        }

        player.currentHp = -50f;
        PlayerUI playerUi = FindFirstObjectByType<PlayerUI>();
        if (playerUi != null) playerUi.UpdateUI();

        Debug.LogError("[QA Demo] Player HP dropped below zero. Expected range: 0 to maxHp; actual: -50.");
    }

    private static void TriggerMissingEnemy()
    {
        GameObject enemy = FindEnemyObject();
        if (enemy == null)
        {
            Debug.LogError("[QA Demo] F9 requires a battle scene: no supported enemy was found in the active scene.");
            return;
        }

        string enemyName = enemy.name;
        Destroy(enemy);
        Debug.LogError($"[QA Demo] Required enemy object disappeared unexpectedly: {enemyName}.");
    }

    private static GameObject FindEnemyObject()
    {
        EnemyController enemyController = FindFirstObjectByType<EnemyController>();
        if (enemyController != null) return enemyController.gameObject;

        BearAI bear = FindFirstObjectByType<BearAI>();
        if (bear != null) return bear.gameObject;

        GolemAI golem = FindFirstObjectByType<GolemAI>();
        if (golem != null) return golem.gameObject;

        HoundAI hound = FindFirstObjectByType<HoundAI>();
        return hound != null ? hound.gameObject : null;
    }

    private static void TriggerRuntimeException()
    {
        throw new InvalidOperationException("[QA Demo] Simulated runtime exception for evidence capture.");
    }
}
