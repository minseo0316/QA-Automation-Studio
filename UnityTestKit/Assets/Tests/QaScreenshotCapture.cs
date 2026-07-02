using System.IO;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine;

public static class QaScreenshotCapture
{
    private const string ScreenshotArgumentName = "-screenshotPath";
    private static readonly string FallbackScreenshotPath = Path.Combine(
        Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
        "Bug_Screenshot.png");

    public static bool SaveIfCurrentTestFailed()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Failed)
        {
            return false;
        }

        string filePath = GetArg(ScreenshotArgumentName);
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = FallbackScreenshotPath;
            Debug.LogWarning($"[QA] {ScreenshotArgumentName} argument was not found. Saving screenshot to fallback path: {filePath}");
        }

        try
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Texture2D texture = Application.isBatchMode
                ? CaptureFromCamera()
                : ScreenCapture.CaptureScreenshotAsTexture();
            if (texture == null && !Application.isBatchMode)
            {
                texture = CaptureFromCamera();
            }

            if (texture == null)
            {
                Debug.LogError("[QA] Failed to capture a screenshot texture.");
                return false;
            }

            filePath = GetAvailableFilePath(filePath);
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(filePath, bytes);
            Object.DestroyImmediate(texture);
            Debug.Log($"[QA] Failure screenshot saved: {filePath}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[QA] Failed to save screenshot: {ex.Message}");
            return false;
        }
    }

    private static Texture2D CaptureFromCamera()
    {
        Camera camera = Object.FindObjectOfType<Camera>();
        GameObject temporaryCameraObject = null;

        if (camera == null)
        {
            temporaryCameraObject = new GameObject("QA_TemporaryScreenshotCamera");
            camera = temporaryCameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.02f, 0.02f);
            camera.transform.position = new Vector3(0f, 1f, -10f);
            camera.transform.rotation = Quaternion.identity;
        }

        RenderTexture previousRenderTexture = RenderTexture.active;
        RenderTexture renderTexture = null;

        try
        {
            renderTexture = new RenderTexture(1280, 720, 24);
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();

            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();
            return texture;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[QA] Camera fallback screenshot failed: {ex.Message}");
            return null;
        }
        finally
        {
            if (camera != null)
            {
                camera.targetTexture = null;
            }

            RenderTexture.active = previousRenderTexture;

            if (renderTexture != null)
            {
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
            }

            if (temporaryCameraObject != null)
            {
                Object.DestroyImmediate(temporaryCameraObject);
            }
        }
    }

    private static string GetAvailableFilePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return filePath;
        }

        string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string extension = Path.GetExtension(filePath);
        for (int i = 2; i < 1000; i++)
        {
            string candidate = Path.Combine(directory, $"{fileName}_{i}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(directory, $"{fileName}_{System.Guid.NewGuid():N}{extension}");
    }
    private static string GetArg(string name)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}





