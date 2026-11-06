# Session Record — 2026-11-05 00:10

## Context

- **Project:** Snake (`C:\Users\wbcwb\Desktop\SDD_Coding\snake\Snake`)
- **Branch:** `v2`
- **Participants:** Scott Liu、Codex

## Summary

本次工作重新釐清並實作 Snake 專案的 Pattern Network、Domains schema v3、Gate、Application Workflow 與 Presentation 契約。核心問題包括：蛇移動缺少權威執行順序、蛇身拓樸沒有契約、尾格即將移動時仍被誤判自撞、UI 各自編排與重建控制項造成閃爍，以及舊 Gate 錯誤要求 acceptance scenario 與單元測試一對一。

目前已完成 schema v3 Gate 基礎、Pattern Network doctrine／template／gate 更新、十個 Aggregate 契約、五個公開 Application Workflow、`PlayGame` Journey、八個 Presentation State 與單一 `RenderGameScreen` workflow。`GameScreenOutput` 與 `RuntimeDirective` 已集中由 `GetCurrentScreen` technical contract 定義，其他 technical contracts 透過 typed reference 使用。

Active catalog closure、Presentation Gate 與目前 59 個 Gate 測試皆通過。產品程式碼尚未依新規格重構；這是刻意保留的下一階段工作。目前仍有 38 組舊 leaf-use-case 規格，共 152 份無 schema v3 header 的 YAML，另有 `Domains/frame_types.yaml`，必須完成映射後才能刪除。

## Detailed Q&A Record

### Q1. `frame.yaml`、`domain_spec.yaml` 與 `technical_spec.yaml` 各自應負責什麼？

最終釐清如下：

- `frame.yaml` 描述 Problem Frame 情境中的問題世界、參與者、共享現象、既定 world facts 與 frame concerns。它不負責 Application Workflow 順序、DTO 欄位或 C# 類別設計。
- Aggregate `domain_spec.yaml` 是 Design by Contract 的主要 owner，定義 Aggregate state、value object、invariant、behavior、前後條件與領域事件。
- `technical_spec.yaml` 定義 logical operation 的 Command／Query／Result／DTO／Port 欄位與 Domain rule mapping，不重新定義商業流程。
- Application Workflow 的完整執行順序、分支、停止條件、原子提交與 output grain 由 `workflow_contract` 擁有。

### Q2. Gate 是否代表每個 acceptance scenario 都必須有一個單元測試？

不是。Gate 是「閘門」，可由任何已註冊的檢測器、測試套件、靜態分析、runtime UI checker 或人工 reviewer 提供證據。一個 checker 可以覆蓋多個 acceptance scenario；一個 scenario 也可以要求多種 evidence。

Gate 結果採：

- `status: pass | fail | pending | not_run`
- `mode: blocking | audit`
- blocking fail／pending 影響 exit code。
- 現有產品程式碼與尚未完成的 runtime UI 證據先列為 Audit，不偽裝成通過。

### Q3. 為何不能讓每個 Use Case 都是一個 class？

每個葉節點 Use Case 一個 class、固定一個 `Execute` 方法，會把完整玩家意圖拆成互不負責整體結果的小操作，使 UI 或 Presenter 被迫自行編排順序。

目前決定由內聚的 Application Service 擁有多個公開 workflow method；DDD 的 rich Aggregate 則以多個 behavior 維持 invariant。Application Workflow 是由外部 caller 與完整商業角色辨識，不由「一個 class」或固定 method 名稱定義。

目前五個公開 workflow 為：

| Workflow | Input grain | Output grain | Contract owner |
| --- | --- | --- | --- |
| `StartCurrentLevel` | 一個空的開始目前關卡 Command | 完整開始結果與一個 Playing Screen | Application Workflow |
| `SetSnakeDirection` | 一個已映射的 `Direction` | 方向接受／忽略結果與完整目前 Screen | Application Workflow |
| `ProcessGameTick` | 一個目前關卡累積毫秒觀察值 | 已處理事件、最終結果與一個 Screen | Application Workflow |
| `RestartGame` | 一個空的重玩 Command | 完整新輪結果與 FirstLevelPlaying Screen | Application Workflow |
| `GetCurrentScreen` | 一個空的 Query | 一份完整不可變 Screen 結果 | Application Workflow |

`PlayGame` Journey 描述跨時間如何反覆使用這五個 workflow；它不是第六個可執行 orchestrator class。

### Q4. 蛇移動與自撞誤判如何由契約修正？

`ProcessGameTick` 現在擁有完整權威順序，包括事件補算、同時刻門事件排序、移動規劃、碰撞、物件效果、死亡／護盾／通關互斥、補物件與原子提交。

關鍵規則：

- `Snake.body_cells` 必須由頭到尾有序、唯一，並依該關 Board policy 連續。
- 移動前先取得下一格物件所決定的 `growth_delta`，再建立 `SnakeMovementPlan`。
- 一般移動時舊尾格會離開，因此 self-collision 集合排除舊尾格。
- `+1` 保留舊尾格。
- `+2` 依直後、左側、右側的固定順序選擇第一個合法尾端格。
- `+2` 沒有合法尾端格時整次 Tick 回滾，特殊食物不消耗。
- 任何中間狀態都只存在於工作快照，最後一次提交並輸出一個 Screen。

### Q5. UI 統一渲染流程如何定義？

每個 UI event 或 Timer tick 只能呼叫一個完整 Application Workflow。UI 依序：

1. 取得完整 Result。
2. 機械式套用 `RuntimeDirective`。
3. 呼叫 `RenderGameScreen` 恰好一次。

Renderer 只依 `GameScreenOutput.ScreenKind` 選擇一個 Presentation State；它不讀 Session／Entity、不呼叫 Use Case、不控制 Timer，也不得每次清空並重建控制項。相同語意控制項保持穩定 identity，全部差異完成後只做一次可見提交。

### Q6. 為何沒有建立 `Domains/v3`？

最終目標是 `Domains` 目錄只保留有效 schema v3，而不是長期並存多個版本資料夾。所有新規格均直接放在語意路徑下，例如：

- `Domains/Game/workflows/process-game-tick/`
- `Domains/Game/presentation/workflows/render-game-screen/`
- `Domains/Game/journeys/play-game/`

目前 v1 leaf 規格只是遷移期間暫留；完成 migration map 後會原地刪除，不建立 `Domains/v3`。

### Q7. 為何曾先執行 Gate 測試，而 Domain 尚未完成？

當時執行的是 Gate engine／schema loader 的基線與回歸測試，用來確認驗證器本身能辨識 schema v3、typed reference、entry closure、blocking／audit 與 exit code，不是宣稱舊 Domain 已符合新契約。

後續已明確採用兩種驗證方式：

- 遷移期間使用 `validate-specs --entry`，只驗證指定 v3 規格與 reference closure，忽略尚未遷移的 v1。
- 全量切換後使用整個 catalog／Domains 掃描；任何 v1 或無 v3 header 的 YAML 都必須失敗。

## Key Clarifications

- Gate 是任何形式的檢測閘門，不等於 acceptance scenario 與單元測試一對一。
- Use Case 的層級由完整玩家意圖與 Application Workflow 責任決定，不由「一個 class 一個 Execute」決定。
- Aggregate 是 invariant 的 owner；Application Workflow 是跨 Aggregate 順序與原子提交的 owner。
- `GameScreenOutput` 與 `RuntimeDirective` 的唯一模型來源是 `V3-TECH-GAME-GET-CURRENT-SCREEN`。
- UI 不再自行串接葉節點 Use Case；一個 event 只進入一個完整 workflow。
- 最終只保留 schema v3，但不建立 `Domains/v3` 版本資料夾。
- `Domains/**` 與 `Pattern Network/**` 是受保護目錄；每次新增受保護路徑仍需列出確切範圍並取得授權，一般性的未來授權不能取代專案規則。

## Decisions Made

- Schema v3 文件統一包含 `schema_version: 3`、`spec_kind` 與全域唯一 v3 ID。
- Frame、Aggregate、Workflow、Technical、Acceptance、Journey、Presentation State、Presentation Workflow、Catalog、Migration Map 各有單一清楚責任。
- `ProcessGameTick` 是蛇移動、門事件、碰撞、物件、護盾、死亡與通關的唯一完整時序 owner。
- `StartCurrentLevel` 在工作快照產生本關首批物件，全部成功後才切換 Playing。
- `RestartGame` 從 GameOver／Completed 直接原子建立新的 FirstLevelPlaying，不顯示中間 Ready。
- `SetSnakeDirection` 的 workflow input 是 `Direction`；只有 Arrow Key adapter 可以建立 Command，WASD 與其他鍵不得呼叫 workflow。
- `GetCurrentScreen` 是純查詢，永遠回傳 `RuntimeDirective.Unchanged`，不得改變 Domain 狀態。
- Migration 必須先完成「每個舊檔案與舊 ID 恰有一個處置」驗證，才能刪除舊規格。

## Code Changes

- `docs/plans/pattern-network-schema-v3-plan.md`：建立完整 schema v3 重整與驗證計畫。
- `Pattern Network/doctrine.yaml`、`Pattern Network/patterns/**`：更新 Application Workflow、rich Aggregate、統一 UI boundary doctrine、template、gate 與 prohibitions。
- `Pattern Network/harness/PatternNetwork.Gates/**`：加入 schema v3 registry、strict loader、typed reference closure、blocking／audit 結果模型、checker registry、報告基礎與 Presentation validator。
- `Pattern Network/harness/PatternNetwork.Gates.Tests/**`：加入 schema、reference、relative-path closure、Presentation 與 Gate 執行模型回歸測試；目前總數 59 個。
- `Domains/{Board,Bomb,DoorAndMaze,Fire,Food,Game,GameResult,Level,Shield,Snake}/domain_spec.yaml`：原地遷移為 rich Aggregate schema v3 契約。
- `Domains/Game/workflows/process-game-tick/`：新增完整 Tick 的 frame、workflow、technical、acceptance。
- `Domains/Game/workflows/start-current-level/`：新增目前關卡完整啟動契約。
- `Domains/Game/workflows/get-current-screen/`：新增純查詢畫面契約，並成為共用畫面 technical model owner。
- `Domains/Game/workflows/set-snake-direction/`：新增方向輸入契約與 Arrow Key boundary。
- `Domains/Game/workflows/restart-game/`：新增完整新輪原子重設契約。
- `Domains/Game/journeys/play-game/`：新增 `PlayGame` Journey 與 acceptance；頂層 Presentation State typed refs 尚待補強。
- `Domains/Game/presentation/**/presentation.yaml`：八個畫面狀態遷移為 schema v3。
- `Domains/Game/presentation/workflows/render-game-screen/`：新增單次完整渲染與 UI/runtime 邊界契約。
- `Domains/Game/presentation/presentation_catalog.yaml`：已刪除；由根目錄 active catalog 取代，可由 Git 復原。
- `Domains/catalog.yaml`：建立 active v3 catalog，已列入五個 workflows、Journey、八個畫面與 Render workflow。
- `Domains/migration-map.yaml`：已建立 Aggregate 與 Presentation 的初始 migration mapping；葉節點規格仍待補齊。
- `ui_spec.md`：建立人工閱讀用 UI 索引；machine source of truth 仍是 Presentation YAML。
- Snake 產品程式碼未修改。

## Follow-up Items

### Batch 1 — 完成 Journey typed reference 補強

- 修改 `Pattern Network/harness/PatternNetwork.Gates/SpecificationSchemaRegistry.cs`，允許 `journey_contract.state_refs`。
- 修改 `Pattern Network/harness/PatternNetwork.Gates.Tests/SpecificationValidatorTests.cs`，加入 Journey Presentation State closure 與錯誤種類測試。
- 修改 `Domains/Game/journeys/play-game/journey.yaml`，加入八個 Presentation State 的頂層 `state_refs`。
- 驗證錯誤／遺失畫面 ID 會被 blocking Gate 阻擋。

### Batch 2 — Migration 完整性 Gate 與映射

- 建立 migration semantic validation：每個舊來源檔案與舊 ID 恰有一個 `replaced_by`、`merged_into`、`split_into` 或 `retired` 處置。
- 驗證所有非 retired target ID 真實存在，且 disposition／欄位組合合法。
- 補齊 Domains 與 Pattern Network migration map。
- 這批只建立完整映射與 Gate，不刪除舊檔。

### Batch 3 — 移除全部舊規格

- 移除 `Domains/Game/{cbf,idf,rbf,rif,swf,tf}/` 下 38 個 leaf-use-case 群組，共 152 份舊 YAML。
- 移除舊 `Domains/frame_types.yaml`。
- 刪除前必須先通過 Batch 2 migration coverage。
- 刪除後執行全量 `validate-specs --domains Domains`，要求所有 YAML 都是有效 schema v3。

### Batch 4 — 重寫 `misfit.yaml` v3

- 只保留 `custom_checks`、`manual_reviews`、`residual_misfits`。
- 移除 acceptance 副本、`required_unit_tests`、測試專案命名公式與 Pattern Gate check 副本。
- 初始 residual 至少記錄：class-per-use-case、Presenter 擁有 Tick 商業順序、舊 Snake 移動／尾格／拓樸落差、GameForm 控制項重建與閃爍、WASD 仍發出方向命令。

### Batch 5 — 實作統一 `verify-project`

- 新增 `verify-project --network --domains --misfit --solution --project-root --output-dir`。
- 整合 schema、catalog、migration、Pattern Network、misfit 與產品程式架構 Audit。
- 產生但不進版控：
  - `.tmp/pattern-network/verification-manifest.generated.yaml`
  - `.tmp/pattern-network/verification-report.json`
  - `.tmp/pattern-network/verification-report.md`
- 將 `.tmp/` 加入 `.gitignore`。

### Batch 6 — 最終全量驗證與文件同步

- 確認全庫沒有 v1／無 schema header 的 Domain YAML，也沒有未映射舊 ID。
- `verify-project` 不得有 blocking fail／pending；現有產品程式落差必須以 Audit 呈現。
- 執行完整 Gate 測試、catalog closure、Presentation Gate 與全量 Domains 驗證。
- 修正 `use_case.md` 等文件中已刪除的 `presentation_catalog.yaml` 連結與過時描述。
- 確認沒有 `Domains/v3`、產生物未進版控、Snake 產品程式碼未被順帶重構。

## Verification Evidence

- Active catalog closure：通過。
- Presentation Gate：通過。
- `PatternNetwork.Gates.Tests`：59/59 通過。
- `GameScreenOutput`／`RuntimeDirective`：只有 `V3-TECH-GAME-GET-CURRENT-SCREEN` 定義，其他契約以 `technical_ref` 引用。
- 目前 Domains YAML：197 份；v3 44 份；舊無 schema header 153 份，其中 152 份屬於 38 個 leaf-use-case 群組，另 1 份是 `Domains/frame_types.yaml`。
- 已知非阻擋警告：NuGet vulnerability source 暫時無法取得（`NU1900`）；測試與 Gate 本身仍通過。
