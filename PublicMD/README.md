# PublicMD 문서 안내

`PublicMD`는 장기 유지되는 기획, 구조, 현재 구현과 개발 상태 문서를 보관한다. 모든 문서를 먼저 읽지 않고 작업 종류에 따라 필요한 문서만 선택한다.

## 기본 읽기 흐름

```text
AGENTS.md
  -> ProjectStructure.md의 작업 라우팅
    -> 관련 Systems 문서
      -> 계층형 기능이면 기능 README에서 leaf 선택
        -> leaf의 변경 유형별 최소 파일
```

- 게임 규칙을 설계하면 `Game_Plan.md`와 `SPEC.md`를 먼저 읽는다.
- 여러 기능의 책임·의존 방향을 바꾸면 `ARCHITECTURE.md`를 읽는다.
- C#을 실제로 수정하면 `CodeConvention.md`를 읽는다.
- 진행 우선순위와 완료 조건은 `PLAN.md`를 읽는다.

## 루트 기준 문서

| 문서 | 역할 |
|---|---|
| `Game_Plan.md` | 핵심 재미, 플레이 경험과 상위 게임 기획 |
| `SPEC.md` | 확정 요구사항과 미결정 질문 |
| `PLAN.md` | 구현 roadmap, 우선순위와 완료 조건 |
| `ARCHITECTURE.md` | 여러 기능에 공통인 설계 원칙과 의존 방향 |
| `ProjectStructure.md` | 전체 구조 지도와 작업별 기능 문서 routing |
| `CodeConvention.md` | 모든 C# 코드에 공통인 작성·검증 규칙 |

## 하위 폴더

| 경로 | 역할 |
|---|---|
| `Systems/` | 기능별 현재 구조, 주 소유 코드, 흐름과 Unity 배선 |
| `Status/` | 누적 구현 진행 기록과 최신 코드 리뷰 결과 |
| `Archive/Plans/` | 구현이 끝났거나 현재 기준이 아닌 상세 계획 |

## 이전 경로

2026-08-29 이전의 누적 기록과 plan에는 이동 전 경로가 남아 있을 수 있다.

| 이전 경로 | 현재 경로 |
|---|---|
| `PublicMD/PROGRESS.md` | `PublicMD/Status/PROGRESS.md` |
| `PublicMD/Code_Evaluation_Result.md` | `PublicMD/Status/Code_Evaluation_Result.md` |
| `PublicMD/InteractionProvider_Unification_Plan.md` | `PublicMD/Archive/Plans/InteractionProvider_Unification_Plan.md` |
| `Session_Plan/PublicMD_Progressive_Disclosure_Plan.md` | `PublicMD/Archive/Plans/PublicMD_Progressive_Disclosure_Plan.md` |

역사 문서는 당시 경로와 판단을 보존한다. 활성 지침과 현재 구조 문서만 새 경로를 기준으로 한다.

## 현재 사실의 기준

문서와 실제 코드·Unity 직렬화가 다르면 코드와 에셋을 현재 사실로 본다. 차이를 발견한 작업자는 같은 변경에서 주 소유 기능 문서를 갱신한다. 하나의 상세 사실은 한 문서만 소유하고 다른 문서는 링크로 연결한다.
