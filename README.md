# ReverseBell

리듬에 맞춰 성을 오르며 적의 공격을 패링하는 2D 액션 게임입니다.

## 개발 환경

- Unity `6000.3.19f1`
- Universal Render Pipeline 2D
- Input System
- 기준 플랫폼: Windows PC

Unity Hub에서 저장소 루트를 프로젝트로 등록한 뒤 열면 됩니다. `Library`,
`Temp`, `Logs`, `UserSettings`는 Git에 포함하지 않습니다.

## 프로젝트 구조

```text
Assets/_Project/
├─ Scripts/
│  └─ Core/
│     ├─ Runtime/
│     └─ Tests/EditMode/
└─ README.md
```

게임 전용 리소스와 코드는 `Assets/_Project` 아래에 둡니다. Unity 템플릿이
생성한 렌더 파이프라인 설정은 기존 `Assets/Settings`를 유지합니다.

## 코드 규칙

- 런타임 코드는 `ReverseBell.*` 네임스페이스를 사용합니다.
- 게임 시간 대신 음악 동기화에는 `AudioSettings.dspTime`을 사용합니다.
- `MonoBehaviour`에 계산 로직을 몰아넣지 않고 테스트 가능한 일반 C# 타입으로
  분리합니다.
- 씬 전환 후에도 유지되어야 하는 서비스만 부트스트랩에서 생성합니다.

## 첫 번째 마일스톤

1. 이동과 점프
2. 패링 입력 및 판정
3. 박자에 맞춰 공격하는 적 2종
4. 패링 콤보로 상승하는 공명 단계 4개
5. 16초 적응형 음악 루프

## 현재 프로토타입 조작

- 이동: `A/D` 또는 방향키
- 점프: `Space`
- 패링: `J` 또는 `K`
- 게임패드: 왼쪽 스틱, South 버튼 점프, West 버튼 패링

공격이 닿기 직전에 패링하면 `Perfect`, 조금 일찍 누르면 `Good` 판정을
받습니다. 연속 패링으로 공명 단계를 올리고 반사 공격으로 적을 처치합니다.
