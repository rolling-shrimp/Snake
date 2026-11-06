# UI 規格索引

本文件是人工閱讀索引，不是 UI 需求的來源。畫面狀態、必要／禁止元素、狀態轉換與渲染流程，以 `Domains/Game/presentation/` 的 schema v3 YAML 為唯一機器可驗證來源。

## 統一渲染流程

- `ProcessGameTick` 等 Application Workflow 每次回傳一份完整且不可變的 `GameScreenOutput`，以及 UI 機械式套用的 `RuntimeDirective`。
- 每個 UI event 或 Timer tick 只呼叫一個完整的 Application Workflow method。
- UI 先取得完整 Result，再套用 `RuntimeDirective`，最後呼叫 `RenderGameScreen` 恰好一次。
- `RenderGameScreen` 只依 `GameScreenOutput.ScreenKind` 選擇一個 Presentation State，並以一次可見提交套用完整畫面。
- Renderer 不讀取 Entity、Session 或其他 Use Case 結果，不負責啟停 Timer，也不編排後續商業流程。
- 相同語意的控制項保留穩定身分；棋盤格建立後重用，不在每次 Tick 清空並重建整個控制項集合。

渲染契約：`Domains/Game/presentation/workflows/render-game-screen/workflow.yaml`

驗收契約：`Domains/Game/presentation/workflows/render-game-screen/acceptance.yaml`

## 畫面狀態

| ScreenKind | Presentation State | 規格路徑 |
| --- | --- | --- |
| `FirstLevelReady` | `V3-PRES-STATE-GAME-FIRST-LEVEL-READY` | `Domains/Game/presentation/first-level-ready/presentation.yaml` |
| `FirstLevelPlaying` | `V3-PRES-STATE-GAME-FIRST-LEVEL-PLAYING` | `Domains/Game/presentation/first-level-playing/presentation.yaml` |
| `SecondLevelReady` | `V3-PRES-STATE-GAME-SECOND-LEVEL-READY` | `Domains/Game/presentation/second-level-ready/presentation.yaml` |
| `SecondLevelPlaying` | `V3-PRES-STATE-GAME-SECOND-LEVEL-PLAYING` | `Domains/Game/presentation/second-level-playing/presentation.yaml` |
| `ThirdLevelReady` | `V3-PRES-STATE-GAME-THIRD-LEVEL-READY` | `Domains/Game/presentation/third-level-ready/presentation.yaml` |
| `ThirdLevelPlaying` | `V3-PRES-STATE-GAME-THIRD-LEVEL-PLAYING` | `Domains/Game/presentation/third-level-playing/presentation.yaml` |
| `GameOver` | `V3-PRES-STATE-GAME-GAME-OVER` | `Domains/Game/presentation/game-over/presentation.yaml` |
| `GameCompleted` | `V3-PRES-STATE-GAME-GAME-COMPLETED` | `Domains/Game/presentation/game-completed/presentation.yaml` |

## 視覺驗收資料

`assets/level-1.html`、`assets/level-2.html`、`assets/level-3.html` 及其 PNG 配對，是三個 Ready 畫面的視覺驗收資料。它們不得反向定義新的畫面、Domain 規則或 Application Workflow。

Aggregate 契約不建立反向 presentation 關聯；Presentation State 只以 `rendered_by`、`application_output` 與 `visual_acceptance` 描述本層依賴。
