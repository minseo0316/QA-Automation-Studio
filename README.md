# QA Automation Studio

Unity PlayMode 테스트를 백그라운드에서 실행하고, 실패 순간의 게임 화면과 로그를 자동으로 수집해 HTML 및 Discord 보고서로 전달하는 Windows QA 도구입니다.

## 주요 기능

- Unity Editor를 별도 프로세스로 실행해 PlayMode 테스트 자동 수행
- 실패 시 실제 카메라 화면 PNG 캡처 및 중복 파일 보존
- 연속 프레임 기반 GIF 증거와 HTML 상세 보고서 생성
- 테스트명, 오류 내용, 소스 위치를 읽기 쉬운 결함 내역으로 정리
- PNG, GIF, HTML을 Discord Webhook으로 전송
- 일정 간격으로 반복 검사하는 자동 모니터링 모드
- 실행 기록과 증거 파일 히스토리 관리

## 구성

```text
QA-Automation-Studio/
  Controls/                  WinForms UI 컴포넌트
  UnityTestKit/              Unity 프로젝트에 설치할 테스트 코드
    Assets/Tests/
  .github/workflows/         Windows 실행 패키지 자동 빌드
  Form1.cs                   테스트 실행 및 결과 표시
  QaEvidenceBuilder.cs       GIF 및 HTML 증거 생성
  PathManager.cs             로컬 설정 관리
  build-release.ps1          배포용 ZIP 생성
  config.sample.json         설정 예시
```

## 빠른 시작

### 1. Unity 테스트 키트 설치

`UnityTestKit/Assets/Tests` 폴더를 대상 Unity 프로젝트의 `Assets/Tests`에 복사합니다. 자세한 의존성과 수정 지점은 [UnityTestKit 사용법](UnityTestKit/README.md)을 확인하세요.

Unity 프로젝트에는 Test Framework 패키지가 필요합니다.

```json
"com.unity.test-framework": "1.4.6"
```

### 2. 앱 설정

앱에서 다음 값을 지정하고 저장합니다.

1. `Unity.exe` 경로
2. 테스트할 Unity 프로젝트 루트
3. 결과 저장 폴더
4. Discord Webhook URL
5. 자동 모니터링용 Webhook URL

설정은 실행 파일 옆의 `config.json`에 저장됩니다. 실제 Webhook과 개인 경로가 포함되므로 Git에 커밋하지 않습니다.

### 3. 테스트 실행

`QA 테스트 시작`을 누르면 Unity PlayMode 테스트가 백그라운드에서 실행됩니다. 반복 검사가 필요하면 간격을 지정하고 `자동 모니터링 시작`을 누릅니다.

실행 결과로 다음 파일이 생성됩니다.

- `Result.xml`: Unity Test Runner 원본 결과
- `Unity_QA_Log.txt`: Unity 실행 로그
- `QA_Final_Report.txt`: 텍스트 결함 보고서
- `QA_Report_*.html`: 브라우저용 상세 보고서
- `QA_Evidence_*.gif`: 실패 전후 프레임 증거
- `Bug_Screenshot*.png`: 실패 순간 카메라 캡처

### 일반 Play 실시간 감시

QA Automation Studio와 Unity Editor를 함께 실행해도 됩니다. 프로그램은 Unity 프로젝트의 `TestResults/RuntimeMonitoring` 폴더를 감시하며, 일반 Play 중 새 런타임 결함이 기록되면 HUD가 포함된 PNG와 JSON 오류 정보를 자동 모드 Discord Webhook으로 즉시 전송합니다. 자동 모드 Webhook이 비어 있으면 일반 Webhook을 사용합니다.

이때 `QA 테스트 시작` 버튼은 누르지 않습니다. 백그라운드 PlayMode 테스트는 동일 프로젝트를 별도 Unity 프로세스로 열기 때문에 Unity Editor를 닫은 상태에서 실행해야 합니다.

## 실행 파일 만들기

.NET SDK가 설치된 Windows PowerShell에서 실행합니다.

```powershell
powershell -ExecutionPolicy Bypass -File .\build-release.ps1
```

`dist/QA-Automation-Studio-win-x64.zip`이 생성됩니다. ZIP에는 .NET 런타임을 포함한 단일 실행 파일과 설정 예시가 들어갑니다.

GitHub의 `Actions > Build Windows Release > Run workflow`에서도 같은 ZIP을 만들 수 있습니다. 배포 파일은 저장소 최상위에 커밋하지 않고 GitHub Releases 또는 Actions Artifacts에 첨부하는 방식을 권장합니다.

## 직접 개발 실행

```powershell
dotnet restore
dotnet run --project MyWinFormsApp.csproj
```

## 보안

- `config.json`, 테스트 결과, 빌드 산출물은 `.gitignore`로 제외됩니다.
- 실제 Discord Webhook URL을 코드나 README에 기록하지 않습니다.
- SSL 인증서 검증을 비활성화하지 않습니다.

## 포트폴리오 포인트

이 프로젝트는 테스트 실행에 그치지 않고 실패 재현, 시각 증거 수집, 소스 위치 추적, 보고서 생성, Discord 전달, 반복 모니터링까지 하나의 QA 파이프라인으로 연결합니다. `UnityTestKit`에는 실제 Knight Shift 프로젝트에서 사용한 결함 재현 테스트가 포함되어 있어 적용 사례도 함께 확인할 수 있습니다.
