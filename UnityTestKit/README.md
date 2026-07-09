# Unity Test Kit

Knight Shift 프로젝트에서 QA Automation Studio와 함께 사용한 Unity PlayMode 테스트/런타임 감시 코드입니다. Unity 6 `6000.0.32f1`과 Test Framework `1.4.6` 기준으로 구성했습니다.

## 설치

1. 이 폴더의 `Assets/Tests`를 Unity 프로젝트의 `Assets/Tests`로 복사합니다.
2. 이 폴더의 `Assets/Scripts/QA`를 Unity 프로젝트의 `Assets/Scripts/QA`로 복사합니다.
3. Package Manager에서 `com.unity.test-framework`를 설치합니다.
4. `Tests.PlayMode.asmdef`의 `references`에 게임 코드 Assembly Definition 이름을 등록합니다.
5. QA Automation Studio에서 Unity Editor와 프로젝트 경로를 지정합니다.

## 파일 역할

- `RuntimeQaMonitor.cs`: 일반 Play 중 예외 로그, 카메라 비활성화, 필수 오브젝트 누락, 비정상 Transform을 감지하고 PNG/JSON 증거를 저장합니다.
- `RuntimeQaDemoTrigger.cs`: 포트폴리오 촬영용 F8 체력 오류, F9 적 소실, F10 런타임 예외를 발생시킵니다.
- `RuntimeQaPerformanceMonitor.cs`: 일반 Play 중 FPS, 메모리, Transform/Canvas/Camera 수를 `Performance_Snapshot_*.json`으로 저장합니다.
- `QaScreenshotCapture.cs`: 실패한 테스트의 카메라 화면을 PNG로 저장합니다.
- `QaFrameRecorder.cs`: 테스트 카메라에서 연속 프레임을 저장해 GIF 증거 생성을 지원합니다.
- `QaSceneWatchdogTests.cs`: 카메라, 필수 오브젝트, 비정상 Transform을 자동 감시합니다.
- `KnightShift_BugReproductionTests.cs`: 체력, 컨트롤러, 스태미나, 전투 화면 실패를 재현하는 적용 사례입니다.
- `PlayerComponentTests.cs`: 플레이어 컴포넌트와 실패 캡처를 검증한 초기 테스트 사례입니다.
- `Tests.PlayMode.asmdef`: PlayMode 테스트 어셈블리 설정입니다.

## 프로젝트별 수정 지점

`QaScreenshotCapture.cs`와 `QaFrameRecorder.cs`는 게임 코드 의존성이 없어 그대로 재사용할 수 있습니다.

아래 파일은 Knight Shift 도메인에 맞춘 샘플이므로 다른 게임에서는 수정이 필요합니다.

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

실패한 테스트의 `[TearDown]` 또는 `[UnityTearDown]`에서 `QaScreenshotCapture.SaveIfCurrentTestFailed()`를 호출하면 앱이 지정한 경로로 증거가 저장됩니다.

```csharp
[TearDown]
public void TearDown()
{
    QaScreenshotCapture.SaveIfCurrentTestFailed();
}
```

## 일반 Play 런타임 감시

`RuntimeQaMonitor`와 `RuntimeQaPerformanceMonitor`는 Editor 또는 Development Build에서 Play가 시작되면 자동 생성됩니다. 모든 씬에 직접 추가할 필요는 없습니다.

감지 결과:

- 결함 증거: `TestResults/RuntimeMonitoring/Runtime_Bug_*.png`, `Runtime_Bug_*.json`
- 성능 스냅샷: `TestResults/RuntimeMonitoring/Performance_Snapshot_*.json`

QA Automation Studio가 켜져 있으면 새 런타임 결함 증거는 Discord로 즉시 전송됩니다. 성능 스냅샷은 앱의 `성능 진단 실행` 버튼을 눌렀을 때 코드 분석 결과와 함께 리포트에 포함됩니다.

## 촬영용 단축키

- `F8`: 플레이어 체력을 -50으로 바꾸고 UI를 갱신합니다.
- `F9`: 현재 전투 씬의 적 하나를 제거합니다.
- `F10`: 테스트용 런타임 예외를 발생시킵니다.

단축키는 Editor와 Development Build에서만 활성화되며 일반 Release Build에는 포함되지 않습니다.
