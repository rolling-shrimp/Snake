# Pattern Network／Domains Schema v3 重整計畫

- 狀態：已確認，待分批授權實作
- 日期：2026-11-04
- 範圍：規格、Pattern Network 與 Gate；不包含本階段的 Snake 產品程式碼重構

## 1. 目標與邊界

本階段處理：

- `Domains/**` 規格重組與 schema v3 遷移。
- `Pattern Network/**` Doctrine、Template、Gate 與驗證器重整。
- 根目錄 `misfit.yaml` 重新定位。
- 產生 verification manifest／report。
- 以 Audit 模式檢查目前程式碼的架構落差。

本階段不重構 `Snake.UseCases`、`Snake.Entities`、`Snake.WinForms` 產品程式碼；這些落差記入 residual misfit，留待下一階段。

目前共有 172 份 Domain YAML、38 組葉節點 Use Case 規格。遷移後不要求一對一保留檔案，但每個舊檔案與舊 ID 都必須有明確的 `replaced_by`、`merged_into`、`split_into` 或 `retired` 紀錄。

`Domains/**` 與 `Pattern Network/**` 是受保護目錄。每一批實作前都必須列出預計建立、修改或刪除的確切路徑、原因與影響，取得當次明確授權後才能動工；一般性的 Implement 指令不構成授權。

## 2. Schema v3 與目標契約

所有有效規格必須包含：

```yaml
schema_version: 3
spec_kind: <kind>
id: <globally-unique-id>
```

採嚴格欄位驗證：未知欄位、重複 ID、錯誤型別、遺失引用與跨種類錯誤引用皆為 Gate error。

| `spec_kind` | 責任 |
|---|---|
| `problem_frame` | 問題世界、參與者、共享現象、world facts、frame concerns；禁止流程順序、DTO、C# 型別與 UI method |
| `aggregate_contract` | Aggregate 狀態、值物件、不變式、行為、前後條件與領域事件 |
| `workflow_contract` | 完整應用流程的 input/output grain、owner、決策、順序、分支、停止與提交政策 |
| `technical_contract` | Command／Query／Result／DTO／Port 欄位及 Domain rule mapping；不得重定義商業流程 |
| `acceptance_contract` | Given／When／Then 與所需證據類型，不指定一對一單元測試 |
| `journey_contract` | 跨時間玩家流程、狀態轉換及重複 workflow |
| `presentation_state` | 畫面必要／禁止元素及可見狀態 |
| `presentation_workflow` | 單次完整渲染、控制項穩定性與可見提交契約 |
| `spec_catalog` | v3 有效規格入口與完整清單 |
| `migration_map` | v1 ID、來源路徑與 v3 ID／退役理由的追溯 |

v3 ID 統一採 `V3-{KIND}-{SCOPE}-{NAME}[-NNN]`；有效規格只能引用 v3 ID。舊 ID 只存在於 migration map，不作為 alias。

### 2.1 目標規格結構

將 38 個同層葉節點重新組成五個公開 Application Workflow：

| Workflow | Frame | Input grain | Output grain |
|---|---|---|---|
| `StartCurrentLevel` | CBF | 空的 `StartCurrentLevelCommand` | 完整開始結果與畫面 |
| `SetSnakeDirection` | SWF | `Direction` | 接受／忽略結果與畫面 |
| `ProcessGameTick` | RBF | 本關開始後的單調累積毫秒 | 已處理事件、最終結果與畫面 |
| `RestartGame` | CBF | 空的 `RestartGameCommand` | 第一關立即重新遊玩的結果與畫面 |
| `GetCurrentScreen` | IDF | 空的 Query | 純查詢畫面結果 |

另建立：

- `PlayGame` journey，組合開始、重複 Tick、方向、關卡轉換、終局與重玩。
- `render-game-screen` presentation workflow。
- 保留並遷移既有八種畫面狀態。
- Workflow step 直接宣告在其唯一 owner workflow 內；不再為每個碰撞、食物效果或補物件建立公開 Use Case 資料夾。
- Aggregate behavior 可被多個 workflow 引用，但 invariant 仍由 Aggregate 擁有。

Technical Contract 只定義 logical operation 與 `owner_role`，本階段不決定未來要分成幾個 Application Service class，也不要求 class 名稱或 `Execute` 方法。

### 2.2 共用輸出模型

正常結果包含：

- `IsSuccess`
- `Outcome`
- `FailureCode`
- `FailureMessage`
- `Screen: GameScreenOutput`
- `RuntimeDirective: Unchanged | StartOrReset | Stop`

`ProcessGameTickResult` 另包含 `List<GameTickEventDTO> ProcessedEvents`。

`GameScreenOutput` 至少包含畫面種類、完整棋盤、頭到尾有序的蛇身、物件、牆與門、關卡狀態、規則提示、結果卡、可用操作及門的視覺狀態。它由 UseCases 擁有，不暴露 Entity。

預期 no-op 是成功的語意 Outcome；負時間、時間倒退或無效契約輸入才是失敗，且不得提交狀態。

### 2.3 ProcessGameTick 權威順序

1. 驗證 Playing 狀態與單調累積時間。
2. 在工作快照中找出截至輸入時間的所有到期事件。
3. 依時間排序；同一時間依「關門提醒 → 門切換／延後關閉 → 蛇移動」處理，完整保留 v1 行為。
4. 移動前建立 `SnakeMovementPlan`，先決定下一格物件與成長量，再計算尾巴是否離開。
5. 未成長時，自撞集合排除本次會離開的舊尾格；成長時使用完整既有蛇身。
6. 套用牆、邊界、門與自撞判定後，一次提交合法蛇身或產生死亡事件。
7. 套用食物、護盾、火焰或炸彈效果。
8. 死亡時只允許「護盾保命」或「Game Over」其中一條分支；非死亡時才判定通關。
9. 仍為 Playing 且未通關時才補充物件。
10. 完成所有到期事件後，一次原子提交並產生單一最終畫面；任一契約錯誤則整次 Tick 不提交。

Snake v3 invariant 必須保證：

- `body_cells` 由頭到尾有序、唯一且依該關 Board policy 連續。
- 一般移動移除舊尾格。
- `+1` 保留舊尾格。
- `+2` 再從尾端依「直後、左側、右側」固定順序選第一個合法相鄰格；三者皆不可用時整次 Tick 失敗、不消耗特殊食物。
- 成長、縮短、護盾重設及關卡重設後仍滿足相同拓樸 invariant。

方向輸入契約宣告 Up／Down／Left／Right；實體按鍵必須在控制項導覽前各建立一次 Direction Command。

## 3. Pattern Network、Gate 與 misfit

### 3.1 Pattern Network

- UseCases Doctrine 改為「Application Workflow owner」，允許一個內聚的 Application Service 擁有多個公開 workflow method。
- 移除「每個 Use Case 一個 class」「固定單一 `Execute`」的 Doctrine、Template 與 Gate。
- UseCases Template 改以 `service_name + operations[]` 產生多方法骨架。
- Entities Doctrine／Template 明確要求 rich Aggregate 擁有多個維持 invariant 的行為。
- WinForms Doctrine 規定一個 UI 事件只能呼叫一個完整 workflow，之後機械式套用 RuntimeDirective 並提交一次畫面；不得讀取 Session 後自行編排商業流程。
- Renderer 不得啟停 Timer、呼叫 UseCases、清空後重建未變更控制項，或顯示半完成布局。

### 3.2 Gate 執行模型

沿用 YamlDotNet，以 C# 強型別模型驗證結構，再做跨檔語意驗證，不新增 JSON Schema 雙重來源。

新增主要命令：

```text
validate-specs --domains <path> [--entry <file> ...]
verify-project --network <path> --domains <path> --misfit <file>
               --solution <file> --project-root <path> --output-dir <path>
```

- `--entry` 只載入試點規格及其 v3 reference closure，不解析 v1。
- 全量切換後，`verify-project` 掃描整個 catalog，任何 v1 YAML 都視為錯誤。
- 保留現有 focused commands 供診斷使用。

Checker 使用註冊表，不允許 YAML 任意執行 shell。每個結果包含：

- `status: pass | fail | pending | not_run`
- `mode: blocking | audit`
- `check_id`
- `rule_refs`／`scenario_refs`
- evidence 與 issues

Exit code：

- `0`：無 blocking fail／pending。
- `1`：存在 blocking fail／pending。
- `2`：設定、解析或 Gate 內部錯誤。
- Audit finding 不改變 exit code。

本階段 blocking 項目包括 schema、ID、引用、catalog、workflow owner、契約完整性、acceptance evidence declaration 與 migration map 完整性。現有產品程式碼、動態 UI 與尚未實作的行為證據先採 Audit。

### 3.3 Acceptance 與證據

Acceptance scenario 宣告所需證據類型，例如：

- behavioral
- invariant
- temporal
- static
- runtime_ui
- manual

Checker 或測試套件可同時提供多個 scenario/rule 的證據；不要求 scenario 與單元測試一對一。沒有 checker、reviewer 或 evidence 的必要證據必須顯示為 `pending`，不能假裝通過。

### 3.4 misfit.yaml v3

只保留：

- `custom_checks`
- `manual_reviews`
- `residual_misfits`

移除 acceptance 副本、`required_unit_tests`、測試專案命名公式及 Pattern Gate check 副本。

初始 Audit residual 至少記錄：

- class-per-use-case／單一 Execute 結構。
- Presenter 擁有 Tick 商業順序並直接讀 Session。
- Snake 尾格自撞、拓樸與特殊食物成長尚未符合 v3。
- GameForm 重建控制項與可能閃爍。
- 方向鍵可能在 WinForms 控制項導覽階段被消耗，未建立 Direction Command。

Gate 產生但不進版控：

```text
.tmp/pattern-network/verification-manifest.generated.yaml
.tmp/pattern-network/verification-report.json
.tmp/pattern-network/verification-report.md
```

並將 `.tmp/` 加入 `.gitignore`。

## 4. 實作順序

1. 建立本計畫文件並保存目前 172 份 YAML、38 組 workflow 與 33 個通過 Gate 測試的基線。
2. 產生每一批受保護檔案的確切變更清單，暫停並取得使用者授權。
3. 先以 Gate test fixtures 建立 v3 header、所有 typed model、嚴格 loader、reference resolver、catalog 與 migration validator。
4. 更新 Pattern Network Doctrine、Prohibitions、Templates、Gate 定義及 C# checker；新的程式碼架構檢查先設為 Audit。
5. 建立真實垂直試點：
   - 遷移三個 workflow：`StartCurrentLevel`、`ProcessGameTick`、`GetCurrentScreen`。
   - 同步遷移其 reference closure 所需的 Aggregate 與 Presentation 基礎規格。
   - 加入 `render-game-screen` presentation workflow。
6. 使用 `validate-specs --entry` 驗證試點，修正 schema 本身的問題後凍結 v3 欄位；不建立 v1 parser。
7. 全量遷移剩餘 Domains：
   - `SetSnakeDirection`、`RestartGame` 與 `PlayGame`。
   - 將 38 組葉節點規則與 acceptance 移入五個 workflow、Aggregate behavior 或 Presentation。
   - 建立 Domains 與 Pattern Network migration map。
   - 確認每個舊檔案及舊 ID 都有處置後，刪除被合併的 v1 目錄與格式。
8. 重寫 `misfit.yaml`，接上統一 `verify-project`、manifest 與 report。
9. 執行完整 Gate；規格與 Gate 必須無 blocking finding，既有程式落差則輸出 Audit residual，留待下一階段重構。

## 5. 驗證與完成條件

Gate／schema 測試必須涵蓋：

- 每種 `spec_kind` 的合法與非法文件。
- 未知欄位、缺欄位、重複 ID、錯誤 reference type、缺檔與孤兒 step。
- 每個舊 ID 恰有一個遷移處置，且所有新 ID 真實存在。
- v1 文件在全量切換後必須被拒絕。
- Blocking／Audit、pending manual review、exit code 及三種報告格式。
- 一個 checker 覆蓋多個 acceptance，以及一個 acceptance 由多個 checker 提供證據。

Workflow acceptance 至少包含：

- `1000ms` 一次輸入補完四次 250ms 移動。
- `4000ms` 關門提醒先於同時刻移動；`6000ms` 門切換／延後關閉先於移動。
- 一般移動進入即將離開的尾格合法；撞到仍保留的身體才是自撞。
- 一般移動、`+1`、`+2`、縮短、護盾重設後蛇身仍連續。
- 特殊食物尾端候選順序及無合法候選時的原子回滾。
- 死亡／護盾／通關互斥，以及通關或死亡後不得補物件。
- 第一、二關通關輸出下一關 Ready 畫面；第三關通關輸出 Completed。
- Restart 從終局直接進入第一關 Playing、產生草莓並重設時間。
- GetCurrentScreen 不改變任何 Domain 狀態。
- 相同畫面重複 render 保持控制項身分、只做一次可見提交，且 Renderer 不控制 Timer。
- 只有方向鍵會產生方向命令。

完成時必須同時滿足：

- 既有 33 個 Gate 測試及新增 v3 測試全部通過。
- 全庫只存在有效 v3 規格，沒有未映射舊 ID。
- `verify-project` 沒有 blocking fail／pending。
- 產生物未進版控。
- Snake 產品程式碼未在本階段被順帶重構；所有已知程式落差均可由 Audit report 與 `misfit.yaml` 追蹤。
