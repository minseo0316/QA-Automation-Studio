# QA Automation Studio

Unity PlayMode 테스트 실행, 런타임 결함 감시, 게임 화면 증거 수집, Discord 알림, 성능 진단 리포트까지 하나로 묶은 Windows QA 자동화 도구입니다.

## 주요 기능

- Unity PlayMode 테스트를 별도 프로세스로 실행하고 결과 XML/로그를 분석
- 실패 순간의 게임 화면 PNG, 짧은 GIF, HTML 보고서 자동 생성
- Unity Editor 일반 Play 중 발생한 런타임 오류를 실시간 감시
- Discord Webhook으로 스크린샷, JSON, HTML 보고서 전송
- 기존 증거 파일을 덮어쓰지 않고 시간 기반 파일명으로 누적 저장
- 성능 진단 버튼으로 Update 구조, Find 계열 호출, Instantiate/Destroy, UI Canvas 후보 분석
- Unity Play 중 FPS/메모리/Transform/Canvas/Camera 스냅샷 수집

## 구성

```text
QA-Automation-Studio/
  Controls/                  WinForms UI 컴포넌트
  UnityTestKit/              Unity 프로젝트에 복사할 QA/성능 모니터링 코드
  Form1.cs                   테스트 실행, 결과 표시, 기본 UI
  RuntimeEvidenceMonitoring.cs
                             Unity Play 중 생성된 런타임 결함 증거 감시
  PerformanceDiagnostics.cs  성능 정적 분석 및 리포트 생성
  QaEvidenceBuilder.cs       GIF/HTML 증거 생성
  PathManager.cs             로컬 설정 관리
  build-release.ps1          배포용 ZIP 생성
  config.sample.json         설정 예시
```

## 📺 QA Automation Studio 프로그램 구동 영상
*(아래 링크를 클릭하시면 자막이 포함된 전체 자동화 프로그램 구동 영상으로 이동합니다.)*

- [▶ QA Automation Studio 시뮬레이션 및 검증 영상 보러가기](https://youtu.be/WsvE69Xakds)

## 빠른 시작

### 1. Unity 테스트 키트 설치

`UnityTestKit/Assets` 안의 파일을 Unity 프로젝트의 `Assets` 폴더에 복사합니다.

포함 파일:

- `Assets/Tests`: PlayMode 테스트와 실패 증거 캡처 코드
- `Assets/Scripts/QA/RuntimeQaMonitor.cs`: 런타임 결함 감시
- `Assets/Scripts/QA/RuntimeQaDemoTrigger.cs`: F8/F9/F10 데모 결함 트리거
- `Assets/Scripts/QA/RuntimeQaPerformanceMonitor.cs`: FPS/메모리 성능 스냅샷 수집

Unity 프로젝트에는 Test Framework 패키지가 필요합니다.

```json
"com.unity.test-framework": "1.4.6"
```

### 2. 앱 설정

앱에서 다음 값을 설정하고 저장합니다.

1. `Unity.exe` 경로
2. 테스트할 Unity 프로젝트 루트
3. 결과물 저장 폴더
4. 스크린샷 저장 폴더
5. Discord Webhook URL
6. 자동 모드 Discord Webhook URL

설정은 실행 파일 옆의 `config.json`에 저장됩니다. 실제 Webhook과 개인 경로가 포함되므로 Git에 커밋하지 않습니다.

### 3. 백그라운드 PlayMode 테스트

Unity Editor를 닫은 상태에서 `QA 테스트 시작` 버튼을 누릅니다.

생성 결과:

- `Result.xml`: Unity Test Runner 원본 결과
- `Unity_QA_Log.txt`: Unity 실행 로그
- `QA_Final_Report.txt`: 텍스트 결함 보고서
- `QA_Report_*.html`: 브라우저용 상세 보고서
- `QA_Evidence_*.gif`: 실패 전후 프레임 증거
- `Bug_Screenshot*.png`: 실패 순간 게임 화면

### 4. Unity 일반 Play 실시간 감시

QA Automation Studio와 Unity Editor를 함께 실행해도 됩니다. 이 모드는 Unity에서 직접 게임을 플레이하면서 런타임 오류를 잡는 흐름입니다.

1. QA Automation Studio를 켜고 경로/웹훅을 저장합니다.
2. Unity Editor에서 Play를 누릅니다.
3. 실제 버그가 발생하거나 데모용 `F8`, `F9`, `F10`을 누릅니다.
4. `TestResults/RuntimeMonitoring`에 PNG/JSON 증거가 저장됩니다.
5. QA Automation Studio가 새 증거를 감지해 Discord로 즉시 전송합니다.

이때 `QA 테스트 시작` 버튼은 누르지 않습니다. 백그라운드 PlayMode 테스트는 같은 프로젝트를 별도 Unity 프로세스로 열기 때문에 Unity Editor를 닫은 상태에서 실행합니다.

### 5. 성능 진단

Unity Play를 5초 이상 실행하면 `RuntimeQaPerformanceMonitor`가 `Performance_Snapshot_*.json` 파일을 생성합니다.

그 다음 QA Automation Studio에서 `성능 진단 실행` 버튼을 누르면 다음 항목을 분석합니다.

- 평균 FPS, 최저 FPS, 최대 프레임 시간
- 관리 메모리 사용량
- Transform, Canvas, Camera 수
- `Update`, `FixedUpdate`, `LateUpdate` 메서드 수
- `Update` 안의 `FindObject`, `FindGameObjectsWithTag`, `GetComponent`
- 런타임 `Instantiate`/`Destroy` 사용 후보
- UI `SetActive` 다량 호출과 Canvas 리빌드 위험 후보
- 오브젝트 풀링 사용 흔적

결과는 `Performance_Audit_*.txt`, `Performance_Audit_*.html`로 저장되고 Discord Webhook에도 전송됩니다.

## 실행 파일 만들기

Windows PowerShell에서 실행합니다.

```powershell
powershell -ExecutionPolicy Bypass -File .\build-release.ps1
```

`dist/QA-Automation-Studio-win-x64.zip`이 생성됩니다.

## 직접 개발 실행

```powershell
dotnet restore
dotnet run --project MyWinFormsApp.csproj
```

## 보안

- `config.json`, 테스트 결과, 빌드 산출물은 `.gitignore`로 제외합니다.
- 실제 Discord Webhook URL은 코드나 README에 기록하지 않습니다.
- SSL 인증서 검증을 비활성화하지 않습니다.

## 포트폴리오 포인트

이 프로젝트는 단순 테스트 실행기가 아니라 실패 재현, 시각 증거 수집, 소스 위치 추적, 보고서 생성, Discord 전달, 반복 모니터링, 런타임 성능 진단까지 연결한 QA 파이프라인입니다. `UnityTestKit`에는 실제 Knight Shift 프로젝트에서 사용한 테스트/감시 코드가 포함되어 있어 적용 사례를 함께 확인할 수 있습니다.
