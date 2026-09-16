# 슈퍼맨 키우기

> 맨몸부터 시작하는 태양의 아들

초원의 맨몸 청년이 자동으로 몬스터를 쓰러뜨려 **햇살**을 모으고, 그 햇살을 **태양의 등불**에 담아 영웅의 슈트를 얻고, 등불의 심지를 갈아 **더 좋은 장비가 나올 확률 자체를 올리는** 모바일 방치형 성장 RPG.

- 엔진: Unity 6000.3.10f1 / URP 2D / Input System
- 플랫폼: 한국어 모바일 세로형
- 핵심 루프: 자동 전투 → 햇살 획득 → 태양의 등불 봉헌 → 장비 성장 → 심지 교체

## 현재 상태

핵심 방치형 성장 루프와 주요 메타 시스템이 연결된 플레이 가능 단계다.

- 자동 전투, 일반/보스 스테이지, 경험치와 레벨업
- 골드 강화와 전투력 계산
- 햇살 봉헌, 10연 바닥 보정, 천장, 심지 확률 성장
- 장비 8슬롯, 희귀도·레벨 2축 성장, 강화·합성·자동 봉헌
- 스킬 6칸, 쿨타임 전투, 다이아 뽑기
- 펫 뽑기·장착·능력치 보너스
- 반복 퀘스트 5종과 다이아 보상
- 최대 8시간 오프라인 적립과 방치 보물상자 수령
- v8 세이브, 정상 v7 세이브 1회 변환, 손상 세이브 쓰기 차단
- EditMode 테스트 225개

## 게임 흐름

1. 플레이어가 몬스터를 자동 공격하고 처치 보상으로 골드·햇살·경험치를 얻는다.
2. 골드로 기본 능력치를 강화하고, 레벨업과 장비·펫·스킬로 전투력을 높인다.
3. 햇살을 태양의 등불에 봉헌해 장비를 획득한다. 중복 장비는 성장 재료가 된다.
4. 심지를 교체하면 등불 레벨과 고등급 장비 확률이 영구적으로 오른다.
5. 스테이지를 진행해 더 강한 적과 보상을 열고, 퀘스트와 방치 보상으로 성장 공백을 메운다.

확률·비용·성장 곡선은 UI가 아니라 각 도메인의 `*Table` 순수 함수에만 정의한다.

## 실행

1. Unity Hub에서 이 폴더를 6000.3.10f1로 연다
2. 메뉴 **GrowNa → Build Main Scene** — 씬은 손으로 만들지 않고 코드가 생성한다
3. 플레이

> **씬을 직접 편집하지 말 것.** `Assets/Editor/MainSceneBuilder.cs` / `UiBuilder.cs` / `PanelBuilder.cs` 를 고치고 다시 빌드하는 구조다.

## 메뉴

| 메뉴 | 용도 |
|---|---|
| `GrowNa/Build Main Scene` | 씬 전체 재생성 (스프라이트 임포트 설정 포함) |
| `GrowNa/Run EditMode Tests` | EditMode 테스트 실행, 결과를 콘솔에 요약 |
| `GrowNa/Verify Gear Layer Composition` | 장비 8레이어 크기·피벗·PPU·정렬순서 감사 |
| `GrowNa/Diagnose Gear Sprite Import` | 장비 스프라이트 임포트 설정 진단 |
| `GrowNa/Preview Full Gear Set` | 전설 풀세트 착용 실루엣 확인 |
| `GrowNa/QA/Verify Button Wiring` | 런타임 버튼 연결 검증 |
| `GrowNa/QA/*` | 플레이 모드에서 재화 지급·강화·10연 봉헌·심지 교체·상태 리포트 |

## 구조

```
Assets/
  Scripts/
    Composition/   씬 합성 루트와 의존성 주입
    Persistence/   세이브 스키마·검증·파일 저장
    Core/          재화·스탯·경험치·강화·퀘스트
    Battle/        전투·스테이지·오프라인·방치 상자
    Gear/          등불·장비·인벤토리·강화·합성
    Skill/         스킬 뽑기·장착·쿨타임
    Pet/           펫 뽑기·장착·보너스
    Players/       캐릭터 애니메이션·장비 리그·체력바
    Monsters/      몬스터와 팩토리
    UI/            HUD와 각 기능 패널
  Editor/          씬·UI 생성기와 QA 메뉴
  Tests/           EditMode·PlayMode 테스트
  Art/             UI·장비·애니메이션 리소스
  Scenes/Main.unity
```

## 런타임 구조

`SceneRuntimeBootstrap`이 유일한 합성 루트다. 각 서비스는 `static Instance`를 사용하지 않고
bootstrap에서 명시적으로 주입된다. 전투 로직은 렌더링과 분리되어 있으며 UI는 서비스 이벤트를
구독해 갱신한다.

의존 방향은 다음과 같다.

```text
Composition → Persistence / UI / Players / Gear / Skill / Pet / Battle
Persistence → Core / Gear / Skill / Pet / Battle
UI / Players → Core / Gear / Skill / Pet / Battle
Skill → Core + Battle
Gear / Pet → Core
Battle → Core + Monsters
```

씬과 UI의 원본은 `Assets/Editor`의 빌더 코드다. `Main.unity`는 빌더 결과물이므로 배치 변경은
빌더에 반영한 뒤 `GrowNa/Build Main Scene`으로 재생성한다.

## 주의

- 리그용 스프라이트(`gear_*`, `char_*`)는 **Single + FullRect + Center** 로 임포트해야 한다. Multiple 모드면 Unity가 알파 경계로 잘라서 레이어가 어긋난다 (빌더가 자동으로 강제한다)
- 수치 표기는 전부 `BigNum` 을 거친다. 자료형은 `double` 확정 — 근거와 교체 조건은 `BigNum` 클래스 주석에 있다
- 본체는 Axel, 배경은 Plains 바이옴. UI·장비 레이어는 아직 플레이스홀더다
