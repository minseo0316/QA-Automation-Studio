# Unity Test Kit

`Knight_Shift` 프로젝트에서 QA Automation Studio와 함께 사용한 PlayMode 테스트 코드입니다. Unity 6 `6000.0.32f1`과 Test Framework `1.4.6`에서 검증했습니다.

## 설치

1. 이 폴더의 `Assets/Tests`를 대상 Unity 프로젝트의 `Assets/Tests`로 복사합니다.
2. Package Manager에서 `com.unity.test-framework`를 설치합니다.
3. `Tests.PlayMode.asmdef`의 `references`에 게임 런타임 Assembly Definition 이름을 등록합니다.
4. QA Automation Studio에서 Unity Editor와 프로젝트 경로를 지정합니다.
5. `QA 테스트 시작`으로 PlayMode 테스트를 실행합니다.

## 파일 역할

- `RuntimeQaMonitor.cs`: 일반 Play 중 예외 로그, 카메라 비활성, 필수 오브젝트 누락, 비정상 Transform을 감지하고 PNG와 JSON 증거를 저장합니다.
- `QaScreenshotCapture.cs`: 실패한 테스트의 카메라 화면을 PNG로 저장합니다. 배치 모드에서는 RenderTexture 기반 캡처로 전환합니다.
- `QaFrameRecorder.cs`: 테스트 카메라에서 연속 프레임을 저장해 GIF 증거 생성을 지원합니다.
- `QaSceneWatchdogTests.cs`: 카메라, 필수 오브젝트, 비정상 Transform을 자동 감시합니다.
- `KnightShift_BugReproductionTests.cs`: 체력, 컨트롤러, 스태미나 및 전투 장면 실패를 재현하는 실제 적용 사례입니다.
- `PlayerComponentTests.cs`: 플레이어 컴포넌트와 실패 캡처를 검증한 초기 테스트 사례입니다.
- `Tests.PlayMode.asmdef`: PlayMode 테스트 어셈블리 설정입니다.

## 프로젝트별 수정 지점

`QaScreenshotCapture.cs`와 `QaFrameRecorder.cs`는 게임 코드 의존성이 없어 그대로 재사용할 수 있습니다.

아래 파일은 Knight Shift 도메인에 맞춘 샘플이므로 다른 게임에서는 수정해야 합니다.

- `KnightShift_BugReproductionTests.cs`의 `PlayerStatus`
- `Tests.PlayMode.asmdef`의 `GameMain` 참조
- `QaSceneWatchdogTests.cs`의 `RequiredObjectNames`
- 테스트에서 사용하는 예상 체력, 스태미나 값과 오브젝트 이름

## 커맨드라인 연동

QA Automation Studio는 Unity에 다음 인자를 전달합니다.

```text
-runTests
-testPlatform PlayMode
-testResults <Result.xml 경로>
-logFile <Unity_QA_Log.txt 경로>
-screenshotPath <PNG 경로>
-evidenceFramesPath <프레임 폴더>
```

실패한 테스트의 `[TearDown]` 또는 `[UnityTearDown]`에서 `QaScreenshotCapture.SaveIfCurrentTestFailed()`를 호출하면 앱이 지정한 경로에 증거가 저장됩니다.

```csharp
[TearDown]
public void TearDown()
{
    QaScreenshotCapture.SaveIfCurrentTestFailed();
}
```

실제 게임 화면을 남기려면 테스트가 실행되는 씬에 활성화된 Camera가 있어야 합니다. 카메라가 없으면 캡처 모듈이 임시 카메라를 생성하지만, 이 경우 게임 장면 대신 단색 배경만 기록될 수 있습니다.

## 일반 플레이 런타임 감시

`RuntimeQaMonitor`는 Editor와 Development Build에서 Play가 시작되면 자동 생성됩니다. 모든 씬에 직접 추가할 필요는 없습니다. 수동 설정이 필요하면 빈 GameObject에 컴포넌트를 추가하고 `requiredObjectNames`에 반드시 존재해야 하는 오브젝트 이름을 등록합니다.

감지 결과는 기본적으로 프로젝트의 `TestResults/RuntimeMonitoring`에 PNG와 JSON으로 저장됩니다. QA Automation Studio에서 실행한 경우에는 `-screenshotPath`가 가리키는 결과 폴더를 사용합니다.
