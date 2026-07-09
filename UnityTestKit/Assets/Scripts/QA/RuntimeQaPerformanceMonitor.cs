using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Writes lightweight FPS and scene complexity snapshots during Editor Play or Development Builds.</summary>
[DefaultExecutionOrder(-9999)]
public sealed class RuntimeQaPerformanceMonitor : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float sampleInterval = 0.5f;
    [SerializeField, Min(2f)] private float snapshotInterval = 5f;
    [SerializeField, Range(30, 600)] private int maxSamples = 240;

    private readonly Queue<float> frameTimes = new Queue<float>();
    private float nextSampleAt;
    private float nextSnapshotAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallAutomatically()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (FindFirstObjectByType<RuntimeQaPerformanceMonitor>() == null)
        {
            new GameObject("QA_PerformanceMonitor").AddComponent<RuntimeQaPerformanceMonitor>();
        }
#endif
    }

    private void Awake()
    {
        if (FindObjectsByType<RuntimeQaPerformanceMonitor>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        nextSampleAt = Time.realtimeSinceStartup + sampleInterval;
        nextSnapshotAt = Time.realtimeSinceStartup + snapshotInterval;
    }

    private void Update()
    {
        float now = Time.realtimeSinceStartup;
        if (now >= nextSampleAt)
        {
            nextSampleAt = now + sampleInterval;
            frameTimes.Enqueue(Time.unscaledDeltaTime);
            while (frameTimes.Count > maxSamples)
            {
                frameTimes.Dequeue();
            }
        }

        if (now < nextSnapshotAt) return;
        nextSnapshotAt = now + snapshotInterval;
        WriteSnapshot();
    }

    private void WriteSnapshot()
    {
        if (frameTimes.Count == 0) return;

        float totalFrameTime = 0f;
        float maxFrameTime = 0f;
        foreach (float frameTime in frameTimes)
        {
            totalFrameTime += frameTime;
            if (frameTime > maxFrameTime) maxFrameTime = frameTime;
        }

        float averageFrameTime = totalFrameTime / frameTimes.Count;
        RuntimePerformanceSnapshot snapshot = new RuntimePerformanceSnapshot
        {
            timestamp = DateTime.Now.ToString("O"),
            scene = SceneManager.GetActiveScene().name,
            averageFps = averageFrameTime > 0f ? 1f / averageFrameTime : 0f,
            minFps = maxFrameTime > 0f ? 1f / maxFrameTime : 0f,
            maxFrameMs = maxFrameTime * 1000f,
            gcMemoryMb = GC.GetTotalMemory(false) / 1024f / 1024f,
            transformCount = FindObjectsByType<Transform>(FindObjectsSortMode.None).Length,
            canvasCount = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length,
            cameraCount = FindObjectsByType<Camera>(FindObjectsSortMode.None).Length
        };

        try
        {
            string directory = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                "TestResults", "RuntimeMonitoring");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"Performance_Snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(path, JsonUtility.ToJson(snapshot, true));
            Debug.Log($"[QA Performance] Snapshot saved: {path}");
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[QA Performance] Snapshot save failed: {exception.Message}");
        }
    }

    [Serializable]
    private sealed class RuntimePerformanceSnapshot
    {
        public string timestamp;
        public string scene;
        public float averageFps;
        public float minFps;
        public float maxFrameMs;
        public float gcMemoryMb;
        public int transformCount;
        public int canvasCount;
        public int cameraCount;
    }
}
