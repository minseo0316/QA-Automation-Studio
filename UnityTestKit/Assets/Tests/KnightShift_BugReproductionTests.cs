using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class KnightShift_BugReproductionTests
{
    [TearDown]
    public void TearDown()
    {
        QaScreenshotCapture.SaveIfCurrentTestFailed();
        CleanupQaObjects();
    }

    [Test]
    [Order(1)]
    public void DamageCalculation_ReproductionScenario()
    {
        GameObject playerObject = new GameObject("Test_Player_For_Damage_Test");
        var playerStatus = playerObject.AddComponent<PlayerStatus>();

        playerStatus.maxHp = 100;
        playerStatus.currentHp = playerStatus.maxHp;

        playerStatus.TakeDamage(10f);

        Assert.AreEqual(100, playerStatus.currentHp, "Damage calculation changed unexpectedly. Check PlayerStatus.TakeDamage.");
        Object.DestroyImmediate(playerObject);
    }

    [Test]
    [Order(2)]
    public void MissingThirdPersonController_ReproductionScenario()
    {
        GameObject playerObject = new GameObject("Test_Player_Without_Controller");
        var playerStatus = playerObject.AddComponent<PlayerStatus>();

        var controllerField = typeof(PlayerStatus).GetField(
            "controller",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var controllerValue = controllerField.GetValue(playerStatus);

        Assert.IsNotNull(controllerValue, "ThirdPersonController is missing. Check the player prefab.");
        Object.DestroyImmediate(playerObject);
    }

    [Test]
    [Order(3)]
    public void SkillStaminaCost_ReproductionScenario()
    {
        GameObject playerObject = new GameObject("Test_Player_For_Stamina_Test");
        var playerStatus = playerObject.AddComponent<PlayerStatus>();

        playerStatus.maxSp = 100;
        playerStatus.currentSp = playerStatus.maxSp;
        playerStatus.skillStamina = 0;

        playerStatus.SkillStamina();

        Assert.AreEqual(70, playerStatus.currentSp, "Skill stamina cost differs from the expected value. Check PlayerStatus.skillStamina.");
        Object.DestroyImmediate(playerObject);
    }

    [UnityTest]
    [Order(4)]
    public IEnumerator RuntimeBattleCameraCapture_ReproductionScenario()
    {
		BuildQaCaptureScene("QA_RuntimeBattleCameraScene", false);
        yield return null;
        yield return null;

        Assert.Fail("QA camera capture check: runtime battle scene failure screenshot should show the cube, ground, light, and camera view.");
    }

    [UnityTest]
    [Order(5)]
    public IEnumerator RuntimeMissingComponentCameraCapture_ReproductionScenario()
    {
		BuildQaCaptureScene("QA_RuntimeMissingComponentScene", true);
        yield return null;
        yield return null;

        Assert.Fail("QA camera capture check: runtime missing-component scene failure screenshot should show the sphere scenario state.");
    }

    internal static void BuildQaCaptureScene(string sceneName, bool isBossEncounter)
    {
        Scene scene = SceneManager.CreateScene(sceneName);
        SceneManager.SetActiveScene(scene);

        GameObject cameraObject = new GameObject("QA_CaptureCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
		camera.fieldOfView = 55f;
		camera.transform.position = new Vector3(0f, 4.8f, -9.5f);
		camera.transform.rotation = Quaternion.Euler(19f, 0f, 0f);
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<QaFrameRecorder>();

        GameObject lightObject = new GameObject("QA_KeyLight");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
		light.intensity = 1.35f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "QA_Ground";
        ground.transform.position = Vector3.zero;
		ground.transform.localScale = new Vector3(2.2f, 1f, 2.2f);
		SetMaterialColor(ground, new Color(0.11f, 0.15f, 0.13f));

		CreateFighter("QA_Player", new Vector3(-1.6f, 0f, -0.2f), new Color(0.1f, 0.45f, 0.95f), 1f);
		CreateFighter("QA_Enemy", new Vector3(1.8f, 0f, 1.1f), isBossEncounter ? new Color(0.65f, 0.05f, 0.08f) : new Color(0.75f, 0.22f, 0.08f), isBossEncounter ? 1.55f : 1.1f);
		CreateProp("QA_RockLeft", PrimitiveType.Cube, new Vector3(-4.5f, 0.55f, 2.7f), new Vector3(1.8f, 1.1f, 1.5f), new Color(0.2f, 0.23f, 0.22f));
		CreateProp("QA_RockRight", PrimitiveType.Cube, new Vector3(4.2f, 0.8f, 3.6f), new Vector3(1.4f, 1.6f, 1.2f), new Color(0.16f, 0.19f, 0.18f));
		CreateProp("QA_DroppedWeapon", PrimitiveType.Cube, new Vector3(0.15f, 0.12f, 0.5f), new Vector3(0.14f, 0.14f, 2.1f), new Color(0.8f, 0.82f, 0.85f));
		CreateWorldSpaceHud(isBossEncounter);
    }

	private static void CreateFighter(string name, Vector3 position, Color color, float scale)
	{
		CreateProp(name + "_Body", PrimitiveType.Capsule, position + Vector3.up * scale, new Vector3(0.8f, 1f, 0.8f) * scale, color);
		CreateProp(name + "_Head", PrimitiveType.Sphere, position + Vector3.up * 2.05f * scale, Vector3.one * 0.62f * scale, color * 1.15f);
		CreateProp(name + "_Sword", PrimitiveType.Cube, position + new Vector3(0.75f, 1.1f, 0f) * scale, new Vector3(0.12f, 1.5f, 0.12f) * scale, new Color(0.75f, 0.8f, 0.86f));
	}

	private static void CreateWorldSpaceHud(bool isBossEncounter)
	{
		CreateProp("QA_HudFrame", PrimitiveType.Cube, new Vector3(-3.8f, 4.55f, 2.7f), new Vector3(3.4f, 0.32f, 0.08f), Color.black);
		CreateProp("QA_HealthBar", PrimitiveType.Cube, new Vector3(-4.15f, 4.55f, 2.61f), new Vector3(2.6f, 0.18f, 0.1f), new Color(0.08f, 0.8f, 0.2f));
		if (isBossEncounter)
		{
			CreateProp("QA_BossFrame", PrimitiveType.Cube, new Vector3(0f, 4.05f, 3.8f), new Vector3(5.8f, 0.28f, 0.08f), Color.black);
			CreateProp("QA_BossHealth", PrimitiveType.Cube, new Vector3(-0.45f, 4.05f, 3.7f), new Vector3(4.7f, 0.15f, 0.1f), new Color(0.85f, 0.04f, 0.06f));
		}
	}

	private static GameObject CreateProp(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
	{
		GameObject prop = GameObject.CreatePrimitive(type);
		prop.name = name;
		prop.transform.position = position;
		prop.transform.localScale = scale;
		SetMaterialColor(prop, color);
		return prop;
	}

    private static void SetMaterialColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private static void CleanupQaObjects()
    {
        foreach (GameObject gameObject in Object.FindObjectsOfType<GameObject>())
        {
            if (gameObject.name.StartsWith("QA_") || gameObject.name.StartsWith("Test_Player_"))
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}

