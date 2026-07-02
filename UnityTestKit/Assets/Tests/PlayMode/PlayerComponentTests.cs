using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.IO;
using NUnit.Framework.Interfaces; // TestStatus 사용을 위해 추가

public class PlayerComponentTests
{
    // 💡 [개선 1] 테스트 전체에서 사용할 카메라 변수 선언
    private Camera _testCamera;
    private GameObject _playerObject;

    // 💡 [개선 2] 테스트 시작 전, 씬에 카메라를 강제로 생성
    [SetUp]
    public void SetUp()
    {
        // 테스트용 카메라가 없다면 생성
        _testCamera = Object.FindObjectOfType<Camera>();
        if (_testCamera == null)
        {
            var camGo = new GameObject("TestCamera");
            _testCamera = camGo.AddComponent<Camera>();
        }
        
        // 플레이어 오브젝트 생성
        _playerObject = new GameObject("TestPlayer");
        _playerObject.AddComponent<PlayerStatus>();
    }

    [UnityTest]
    public IEnumerator 플레이어_초기_체력_검증_시나리오()
    {
        var playerStatus = _playerObject.GetComponent<PlayerStatus>();

        yield return new WaitForSecondsRealtime(0.5f);

        // 의도적 실패 유도 (초기 Hp가 100이라면 실패하여 스크린샷 발동)
        Assert.AreEqual(99, playerStatus.currentHp, "플레이어의 초기 체력이 예상과 다릅니다!");
    }

    // 💡 [개선 3] UnityTearDown을 사용하여 테스트 종료 후 정리 및 스크린샷 처리
    [UnityTearDown]
    public IEnumerator UnityTearDown()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
        {
            // 💡 [수정] GetArg가 실패할 경우를 대비하여, WinForms 앱의 기본 스크린샷 경로와 동일한 구조의 경로를 사용하도록 수정합니다.
            // 이렇게 하면 커맨드 라인 인자 전달에 실패하더라도 일관된 위치에 스크린샷이 저장됩니다.
            string filePath = GetArg("-screenshotPath");
            // 💡 [수정] WinForms의 Application.StartupPath는 Unity 환경에서 사용할 수 없으므로 제거합니다.
            // 대신, 인자 전달 실패 시 Unity 프로젝트 루트에 저장되도록 절대 경로를 지정하여 경로 유효성을 보장합니다.
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Path.Combine(
                    Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                    "Bug_Screenshot.png");
                Debug.LogWarning($"[QA] -screenshotPath 인자를 찾지 못했습니다. 스크린샷을 프로젝트 루트에 저장합니다: {filePath}");
            }

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[QA] 스크린샷 폴더 생성 중 오류 발생: {ex.Message}");
                // 폴더 생성이 실패하면 더 이상 진행할 수 없으므로 return 합니다.
                yield break;
            }
            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
            
            // 💡 백그라운드 모드에서는 텍스처가 생성되는 데 아주 미세한 메모리 대기 시간이 필요합니다.
            int retryCount = 0;
            while (texture == null && retryCount < 5)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                retryCount++;
            }

            if (texture != null)
            {
                byte[] bytes = texture.EncodeToPNG();
                File.WriteAllBytes(filePath, bytes);
                Object.DestroyImmediate(texture);
                Debug.Log($"[QA] 백그라운드 최적화 인게임 스크린샷 캡처 완료: {filePath}");
            }
            else
            {
                Debug.LogError("[QA] 백그라운드 텍스처 버퍼 낚아채기 실패");
            }

            yield return null;
        }
    }

    private static string GetArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                return args[i + 1];
            }
        }
        // 💡 [수정 3] 찾지 못했을 때 명시적으로 null 반환
        return null;
    }
}

