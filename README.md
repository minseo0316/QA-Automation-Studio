# MyWinFormsApp

Unity PlayMode 테스트를 실행하고, 결과를 사람이 바로 읽을 수 있게 묶어주는 QA 자동화 도구입니다.

## 핵심 기능

- Unity Editor를 외부 프로세스로 실행해 PlayMode 테스트 수행
- 실패 시 게임 화면 스크린샷 저장
- 연속 프레임을 모아 짧은 GIF 증거 생성
- HTML 보고서와 텍스트 리포트 자동 생성
- Discord Webhook으로 스크린샷, GIF, HTML 보고서 전송
- 자동 모니터링 모드 지원
- 테스트 실패 시 소스 위치와 오류 메시지 정리
- WinForms 기반의 대시보드 UI 제공

## 실행 전 준비

1. Unity Editor 경로를 설정합니다.
2. Unity 프로젝트 경로를 설정합니다.
3. 결과 저장 폴더를 지정합니다.
4. Discord Webhook URL을 입력합니다.

설정은 실행 파일 옆의 `config.json`에 저장됩니다. 저장소에는 포함하지 않는 구성을 권장합니다.

## 실행 방법

1. Visual Studio에서 `MyWinFormsApp`를 실행합니다.
2. 좌측 설정 패널에서 경로와 웹훅을 입력합니다.
3. `설정 저장`을 누릅니다.
4. `QA 테스트 시작` 또는 `자동 모니터링 시작`을 사용합니다.

## 결과물

- `Result.xml`: Unity 테스트 결과 XML
- `Unity_QA_Log.txt`: Unity 실행 로그
- `QA_Final_Report.txt`: 최종 텍스트 보고서
- `QA_Report_*.html`: 웹 브라우저용 상세 보고서
- `QA_Evidence_*.gif`: 짧은 시각 증거
- `Bug_Screenshot*.png`: 스크린샷 증거

## 폴더 구조

```text
MyWinFormsApp/
  Controls/
  Form1.cs
  PathManager.cs
  QaEvidenceBuilder.cs
  QATestLauncher.cs
  SettingsForm.cs
  UiTheme.cs
  config.sample.json
  README.md
```

## 포폴 포인트

- 실제 Unity 테스트를 구동하는 자동화 흐름
- 실패 화면을 증거로 남기는 QA 방식
- Discord 전송까지 이어지는 배포형 워크플로우
- 자동 모니터링과 수동 실행을 모두 다루는 운영성

## 주의 사항

- `config.json`에는 개인 웹훅이나 로컬 경로가 들어갈 수 있으니 저장소에 올리지 않습니다.
- `bin/`, `obj/`, `TestResults/`는 빌드 산출물이라 GitHub에는 포함하지 않는 편이 좋습니다.
