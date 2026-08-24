# 허수아비 키우기 (scarecrown)

> 짚 한 단부터 시작하는 대지의 신

밭에 박혀 있던 허수아비가 자동으로 몬스터를 쓰러뜨려 **낟알**을 모으고, 그 낟알을 **소원의 등불**에 태워 죽은 기사들의 장비를 얻고, 등불의 심지를 갈아 **더 좋은 장비가 나올 확률 자체를 올리는** 모바일 방치형 성장 RPG.

- 엔진: Unity 6000.3.10f1 / URP 2D / Input System
- 기획서: [docs/GDD.md](docs/GDD.md) · 본체 컨셉: [docs/CONCEPT.md](docs/CONCEPT.md) · 시네마틱: [docs/CINEMATIC.md](docs/CINEMATIC.md)
- 문서 뷰어: `docs/index.html` (3탭)

## 현재 상태

**M0 완료 / M1 진입.** 자동 전투·스테이지·스탯 강화·등불 봉헌·장비 8슬롯 장착·레이어 합성·세이브·오프라인 보상이 동작한다.
상세 표는 [GDD 16장](docs/GDD.md)을 볼 것. EditMode 테스트 54개 통과.

## 실행

1. Unity Hub에서 이 폴더를 6000.3.10f1로 연다
2. 메뉴 **GrowNa → Build Main Scene** — 씬은 손으로 만들지 않고 코드가 생성한다
3. 플레이

> **씬을 직접 편집하지 말 것.** `Assets/_Project/Editor/MainSceneBuilder.cs` / `UiBuilder.cs` / `PanelBuilder.cs` 를 고치고 다시 빌드하는 구조다.

## 메뉴

| 메뉴 | 용도 |
|---|---|
| `GrowNa/Build Main Scene` | 씬 전체 재생성 (스프라이트 임포트 설정 포함) |
| `GrowNa/Run EditMode Tests` | EditMode 테스트 실행, 결과를 콘솔에 요약 |
| `GrowNa/Verify Gear Layer Composition` | 장비 8레이어 크기·피벗·PPU·정렬순서 감사 |
| `GrowNa/Preview Full Gear Set` | 전설 풀세트 착용 실루엣 확인 |
| `GrowNa/QA/*` | 플레이 모드에서 재화 지급·강화·10연 봉헌·심지 교체·상태 리포트 |

## 구조

```
Assets/_Project/
  Scripts/   GrowNa.Runtime      — Core(재화·스탯·세이브) / Battle / Gear(등불·장비) / UI / Visual
  Editor/    GrowNa.Editor       — 씬·UI 빌더, 레이어 감사, QA 메뉴
  Tests/     GrowNa.Tests.EditMode
  Art/Sprites                    — 전부 플레이스홀더 (tools/gen_gear_layers.py 로 생성)
docs/                            — 기획서
```

## 주의

- 리그용 스프라이트(`gear_*`, `char_*`)는 **Single + FullRect + Center** 로 임포트해야 한다. Multiple 모드면 Unity가 알파 경계로 잘라서 레이어가 어긋난다 (빌더가 자동으로 강제한다)
- 수치 표기는 전부 `BigNum` 을 거친다. 자료형은 `double` 확정 — 근거와 교체 조건은 `BigNum` 클래스 주석에 있다
- 아트는 전부 플레이스홀더다. 실제 도트 아트 아님
