using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Records runtime exceptions and invalid scene state in Editor or Development Builds.</summary>
[DefaultExecutionOrder(-10000)]
public sealed class RuntimeQaMonitor : MonoBehaviour
{
    private const float DuplicateCooldown = 10f;

    [SerializeField, Min(0.5f)] private float scanInterval = 2f;
    [SerializeField, Min(0f)] private float sceneGracePeriod = 3f;
    [SerializeField] private bool monitorMainCamera = true;
    [SerializeField] private bool monitorInvalidTransforms = true;
    [SerializeField] private string[] requiredObjectNames = Array.Empty<string>();

    private readonly Dictionary<string, float> lastReportedAt = new Dictionary<string, float>();
    private float nextScanAt;
    private bool captureInProgress;
    private bool writingEvidence;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallAutomatically()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (FindFirstObjectByType<RuntimeQaMonitor>() == null)
        {
            new GameObject("QA_RuntimeMonitor").AddComponent<RuntimeQaMonitor>();
        }
#endif
    }

    private void Awake()
    {
        if (FindObjectsByType<RuntimeQaMonitor>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        DelayNextScan();
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup < nextScanAt) return;
        nextScanAt = Time.realtimeSinceStartup + scanInterval;
        ScanScene();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => DelayNextScan();

    private void DelayNextScan()
    {
        nextScanAt = Time.realtimeSinceStartup + sceneGracePeriod;
    }

    private void HandleLog(string message, string stackTrace, LogType type)
    {
        if (writingEvidence || message.StartsWith("[QA Runtime]", StringComparison.Ordinal)) return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            Report(type.ToString(), message, stackTrace);
        }
    }

    private void ScanScene()
    {
        Camera mainCamera = Camera.main;
        if (monitorMainCamera && (mainCamera == null || !mainCamera.enabled || !mainCamera.gameObject.activeInHierarchy))
        {
            Report("SceneIntegrity", "Main camera is missing or inactive.", string.Empty);
        }

        foreach (string objectName in requiredObjectNames)
        {
            if (!string.IsNullOrWhiteSpace(objectName) && GameObject.Find(objectName) == null)
            {
                Report("SceneIntegrity", $"Required object is missing: {objectName}", string.Empty);
            }
        }

        if (!monitorInvalidTransforms) return;
        foreach (Transform target in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (target == null || !target.gameObject.scene.IsValid() || !target.gameObject.activeInHierarchy) continue;
            if (IsInvalid(target.position) || IsInvalid(target.localScale) || IsInvalid(target.rotation))
            {
                Report("InvalidTransform", $"Invalid transform detected on '{GetHierarchyPath(target)}'.", string.Empty);
            }
        }
    }

    private void Report(string category, string message, string stackTrace)
    {
        string key = category + ":" + message;
        if (lastReportedAt.TryGetValue(key, out float previous)
            && Time.realtimeSinceStartup - previous < DuplicateCooldown) return;

        lastReportedAt[key] = Time.realtimeSinceStartup;
        StartCoroutine(CaptureEvidence(category, message, stackTrace));
    }

    private IEnumerator CaptureEvidence(string category, string message, string stackTrace)
    {
        while (captureInProgress) yield return null;
        captureInProgress = true;

        if (Application.isBatchMode) yield return null;
        else yield return new WaitForEndOfFrame();

        string outputDirectory = GetOutputDirectory();
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        string screenshotPath = GetAvailablePath(Path.Combine(outputDirectory, $"Runtime_Bug_{timestamp}.png"));
        string reportPath = Path.ChangeExtension(screenshotPath, ".json");

        try
        {
            Directory.CreateDirectory(outputDirectory);
            Texture2D screenshot = CaptureCameraFrame();
            if (screenshot != null)
            {
                File.WriteAllBytes(screenshotPath, screenshot.EncodeToPNG());
                Destroy(screenshot);
            }

            RuntimeQaIssue issue = new RuntimeQaIssue
            {
                timestamp = DateTime.Now.ToString("O"), category = category, message = message,
                stackTrace = stackTrace, scene = SceneManager.GetActiveScene().name,
                screenshotPath = File.Exists(screenshotPath) ? screenshotPath : string.Empty
            };
            File.WriteAllText(reportPath, JsonUtility.ToJson(issue, true));
            writingEvidence = true;
            Debug.Log($"[QA Runtime] Defect evidence saved: {reportPath}");
        }
        catch (Exception exception)
        {
            writingEvidence = true;
            Debug.LogWarning($"[QA Runtime] Evidence capture failed: {exception.Message}");
        }
        finally
        {
            writingEvidence = false;
            captureInProgress = false;
        }
    }

    private static Texture2D CaptureCameraFrame()
    {
        // Interactive capture includes Screen Space UI and the complete HUD.
        if (!Application.isBatchMode)
        {
            return ScreenCapture.CaptureScreenshotAsTexture();
        }

        Camera camera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (camera == null) return ScreenCapture.CaptureScreenshotAsTexture();

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture renderTexture = new RenderTexture(1280, 720, 24);
        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            Texture2D texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
            texture.Apply();
            return texture;
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    private static string GetOutputDirectory()
    {
        string requestedPath = GetArgument("-screenshotPath");
        if (!string.IsNullOrWhiteSpace(requestedPath))
            return Path.GetDirectoryName(requestedPath) ?? Application.persistentDataPath;

        return Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
            "TestResults", "RuntimeMonitoring");
    }

    private static string GetArgument(string name)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int index = 0; index < arguments.Length - 1; index++)
            if (arguments[index] == name) return arguments[index + 1];
        return string.Empty;
    }

    private static string GetAvailablePath(string path)
    {
        if (!File.Exists(path)) return path;
        return Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
            $"{Path.GetFileNameWithoutExtension(path)}_{Guid.NewGuid():N}{Path.GetExtension(path)}");
    }

    private static string GetHierarchyPath(Transform target)
    {
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }
        return path;
    }

    private static bool IsInvalid(Vector3 value) => !IsFinite(value.x) || !IsFinite(value.y) || !IsFinite(value.z);
    private static bool IsInvalid(Quaternion value) => !IsFinite(value.x) || !IsFinite(value.y) || !IsFinite(value.z) || !IsFinite(value.w);
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    [Serializable]
    private sealed class RuntimeQaIssue
    {
        public string timestamp;
        public string category;
        public string message;
        public string stackTrace;
        public string scene;
        public string screenshotPath;
    }
}
