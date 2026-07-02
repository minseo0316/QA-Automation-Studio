using System;
using System.IO;
using UnityEngine;

public sealed class QaFrameRecorder : MonoBehaviour
{
    private const int MaxFrames = 16;
    private static int evidenceFrameIndex;
    private int frameIndex;
    private Camera captureCamera;
    private string outputDirectory;

    private void Awake()
    {
        captureCamera = GetComponent<Camera>();
        outputDirectory = GetArg("-evidenceFramesPath");
        if (!string.IsNullOrEmpty(outputDirectory)) Directory.CreateDirectory(outputDirectory);
    }

    private void LateUpdate()
    {
        if (frameIndex >= MaxFrames || captureCamera == null || string.IsNullOrEmpty(outputDirectory)) return;
        GameObject enemy = GameObject.Find("QA_Enemy_Body");
        if (enemy != null) enemy.transform.position += new Vector3(-0.035f, 0f, -0.012f);

        RenderTexture previous = RenderTexture.active;
        RenderTexture renderTexture = new RenderTexture(960, 540, 24);
        try
        {
            captureCamera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            captureCamera.Render();
            Texture2D texture = new Texture2D(960, 540, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            texture.Apply();
            string framePath = GetAvailableFramePath();
            File.WriteAllBytes(framePath, texture.EncodeToPNG());
            DestroyImmediate(texture);
            frameIndex++;
        }
        finally
        {
            captureCamera.targetTexture = null;
            RenderTexture.active = previous;
            renderTexture.Release();
            DestroyImmediate(renderTexture);
        }
    }

    private string GetAvailableFramePath()
    {
        while (true)
        {
            string candidate = Path.Combine(outputDirectory, $"frame_{evidenceFrameIndex++:000}.png");
            if (!File.Exists(candidate)) return candidate;
        }
    }

    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
        return null;
    }
}

