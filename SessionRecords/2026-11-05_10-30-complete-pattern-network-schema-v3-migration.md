# Session Record：完成 Pattern Network / Domains Schema v3 遷移

## 基本資訊

- 日期：2026-11-05 10:30（Asia/Taipei）
- 專案：Snake
- 專案路徑：`C:\Users\wbcwb\Desktop\SDD_Coding\snake\Snake`
- Git 分支：`v2`
- 參與者：Scott Liu、Codex
- 狀態：規格重整、舊規格遷移與統一驗證流程已完成；產品程式重構尚未執行
- Commit：本次工作尚未建立 Git commit

## 本次目標

重新釐清並調整 `Pattern Network/`、`Domains/` 與 `misfit.yaml` 的責任，使規格可以明確表達：

1. 完整 use case 的執行順序與層級。
2. 蛇移動時身體連續性、尾巴移動與自撞判定。
3. UI 事件、應用流程與渲染之間的唯一流程。
4. Gate 是可替換的檢測閘門，不是「每個 acceptance 必須對應一個單元測試」。
5. Domain、Application Workflow、Technical Contract、Presentation Contract 的契約責任邊界。
6. 舊規格到 Schema v3 的完整遷移、淘汰與驗證方式。

## 完成摘要

- 建立並執行 Schema v3 遷移計畫：`docs/plans/pattern-network-schema-v3-plan.md`。
- Pattern Network 已改為嚴格 Schema v3，包含 typed reference、entry closure、blocking/audit Gate、migration validation 與統一驗證報告。
- Domains 已完成 Schema v3 重建，目前共有 44 份 YAML，沒有 `Domains/v3` 平行版本資料夾，也沒有舊格式 Domain YAML。
- 建立 10 個 Aggregate、5 個 Application Workflow、1 個 Journey、8 個 Presentation State 與 1 個 Render Workflow。
- 完成 Snake 移動規則，包括尾巴離開、自撞判定、身體連續性、一般成長與特殊食物成長失敗時的原子回滾。
- `misfit.yaml` 已改為只描述檢測器、人工審查與尚未解決的產品落差，不再複製 use case acceptance 或指定測試專案命名。
- 建立 `verify-project` 單一入口，能統一驗證 Pattern Network、Domains、migration、presentation、code architecture 與 misfit。
- 移除 153 份舊 Domain 規格檔案；這些刪除仍可透過 Git 復原。
- 更新 `use_case.md` 的規格索引與錯字，使文件只連到目前存在的 Schema v3 規格。
- 最終 70/70 Gate 測試通過；`verify-project` 成功，所有 blocking checks 都通過。

## 詳細問答與決策

### Q1：`frame.yaml`、`domain_spec.yaml`、`technical_spec.yaml` 各自負責什麼？

決策如下：

- `frame.yaml` 描述 Problem Frame 的情境，包括既定世界事實、參與者、共享現象與關注問題。它不是主要的 Design by Contract 所有者。
- Aggregate 的 `domain_spec.yaml` 是領域行為與不變量的契約所有者，負責狀態、前置條件、後置條件、允許／禁止的狀態轉換及領域規則。
- `technical_spec.yaml` 描述邏輯操作所需的 input/output model、欄位、型別、port 與 mapping，不負責跨多個步驟的商業流程順序。
- 完整 use case 的步驟順序、分支、停止條件、交易／原子性與最終結果，由 Application Workflow 負責。

因此，「取得員工時員工必定有唯一識別資料」這類敘述是否放在 frame 或 domain，要看它是客觀世界事實，還是系統必須保證的契約；不能把所有硬性規則都塞進 frame。

### Q2：Gate 的正確定義是什麼？「改為 Gate 產生」代表什麼？

Gate 是規格進入下一階段前的檢測閘門。檢測證據可以來自：

- Schema／reference validator。
- 靜態分析或架構檢查。
- 單元測試、整合測試或端對端測試。
- 產生檔案的比對結果。
- 人工審查與其證據。

因此不再要求 `misfit.yaml` 中每一個 use case acceptance 都必須一對一對應測試專案內的單元測試。Schema v3 改為宣告 checker、mode 與 evidence；驗證器依登錄內容執行或回報 `not_run`，再由 blocking/audit mode 決定是否阻擋流程。

「Gate 產生」不是替每一條 acceptance 自動建立測試方法，而是由規格與 checker registry 產生一致的檢測工作及報告骨架，讓不同形式的檢測器都能提供標準化結果。

### Q3：`Snake.Usecase` 中每個 use case 一個 class，問題在哪裡？

「一個 use case 一個 class，而且都只有固定 `Execute`」不是 DDD 的必要形式，也會造成 leaf use case 各自為政：流程順序分散、共享狀態與原子性難以維護。

本次決策是：

- Application 層由具凝聚力的 Application Service 擁有多個 workflow method。
- 高階 workflow 負責完整玩家意圖，例如開始關卡、處理一個 tick、重開遊戲、取得目前畫面。
- Aggregate（例如 Snake）負責真正的領域行為與不變量，例如方向改變、移動計畫、身體拓撲與碰撞規則。
- 不以「Snake 一個 class 裡所有 method」作為跨層流程的唯一答案；Snake Aggregate 與 Application Service 的責任仍需分開。

這同時保留使用者希望的「行為聚合在正確的領域物件」以及完整 use case 由上層協調的結構。

### Q4：完整遊戲流程如何分層？

目前主要 Application Workflow 為：

- `StartCurrentLevel`
- `SetSnakeDirection`
- `ProcessGameTick`
- `RestartGame`
- `GetCurrentScreen`

玩家遊玩整體流程由 `PlayGame` Journey 串接。每一個 UI 事件或 timer tick 只能觸發一個完整 workflow；workflow 內部才協調 Snake、Food、Bomb、Fire、Shield、DoorAndMaze、Level、Game 與 GameResult 等 Aggregate 行為。

`ProcessGameTick` 是移動順序、碰撞、食物效果、關卡結果與原子提交的契約所有者，而不是讓 Presenter 或多個 leaf use case 自行決定呼叫順序。

### Q5：蛇尾會移動時，自撞與身體連續性如何判定？

Snake 的移動規則已明確化：

1. 蛇身是有順序、位置唯一且符合棋盤相鄰政策的連續 cell 序列。
2. 先決定本 tick 的 `growth_delta`，再產生 `SnakeMovementPlan`。
3. 一般移動時，舊尾巴會離開，因此碰撞集合排除即將離開的舊尾 cell。
4. `growth_delta = +1` 時保留舊尾。
5. `growth_delta = +2` 時，新增尾節依「尾巴直後、左側、右側」的固定順序選擇第一個合法 cell。
6. 若 `+2` 找不到合法的新尾 cell，整個 tick 回滾，特殊食物不消耗，也不留下部分移動結果。

這解決了舊契約把尾巴一律視為固定障礙而造成的自撞誤判，也避免身體更新由多個 use case 分別處理而形成不連續蛇身。

### Q6：UI 為什麼會閃爍？新的統一流程是什麼？

舊結構允許多個 UI 元件各自讀取 Session、控制 timer、執行 use case 與重建 controls，因此一次狀態變化可能發生多次、不一致的 render。

新的契約流程是：

```text
UI event / timer tick
  -> 一個完整 Application Workflow
  -> RuntimeDirective + GameScreenOutput
  -> 一次 render
```

限制如下：

- Presenter／Renderer 不協調商業流程。
- Renderer 不直接讀 Session 或 Entity。
- Renderer 不控制 timer 與遊戲狀態轉移。
- 未改變的 control tree 不應在每次更新時整批重建。
- `GameScreenOutput` 與 `RuntimeDirective` 的 canonical owner 是 `GetCurrentScreen` technical contract。

### Q7：為什麼沒有建立 `Domains/v3`？

使用者明確要求只保留一套現行版本，而不是建立新舊兩套 Domain 做長期對照。因此採用原路徑就地遷移：

- 現行規格直接放在 `Domains/`。
- 舊來源與新 target 的關係放在 migration map。
- 遷移完成後刪除舊格式檔案。
- 不保留 `Domains/v3` 版本資料夾。

版本差異由 Git 歷史與 migration map 負責，不以平行資料夾維護兩套真相。

### Q8：如何證明舊規格沒有漏遷？

新增：

- `Domains/catalog.yaml`
- `Domains/migration-map.yaml`
- `Pattern Network/migration-map.yaml`
- `validate-migration` Gate

Migration validator 會檢查：

- 每一個 legacy source 與 definition ID 恰好出現一次。
- disposition 只能是 `replaced_by`、`merged_into`、`split_into` 或 `retired`。
- 新 target 必須存在。
- 不得有重複來源。
- `retired` 必須說明原因。

完成檢查後移除 38 組舊 leaf-use-case 規格，每組 4 份 YAML，共 152 份；另移除 `Domains/frame_types.yaml`，合計 153 份。最終 legacy Domain YAML 數量為 0。

### Q9：`misfit.yaml` 現在扮演什麼角色？

`misfit.yaml` 現在只保留三類內容：

- `custom_checks`
- `manual_reviews`
- `residual_misfits`

已移除：

- use case acceptance 的複本。
- `required_unit_tests`。
- 測試專案與測試方法的命名公式。
- Pattern Network Gate 的重複定義。

目前記錄的五項產品程式落差為：

1. 仍存在 class-per-use-case／單一 `Execute` 結構。
2. Presenter 仍擁有 tick 流程並直接讀 Session。
3. Snake 程式尚未符合尾巴碰撞、身體拓撲與特殊食物成長契約。
4. `GameForm` 仍可能重建 controls，存在 UI 閃爍風險。
5. WASD 仍會送出方向命令，與目前輸入契約不一致。

這些項目是 Audit，不會讓已完成的規格遷移被誤判為失敗，但也不能被當成已修正。

### Q10：統一驗證入口是什麼？

新增的命令介面為：

```text
verify-project --network --domains --misfit --solution --project-root --output-dir
```

它整合以下驗證：

- Pattern Network Schema。
- 全部 Domain 規格與 entry closure。
- Migration completeness。
- Presentation contracts。
- Code architecture。
- `misfit.yaml` 設定與 checker 狀態。

輸出：

- `.tmp/pattern-network/verification-manifest.generated.yaml`
- `.tmp/pattern-network/verification-report.json`
- `.tmp/pattern-network/verification-report.md`

`.tmp/` 已加入 `.gitignore`，報告不會被誤納入版本控制。

## 主要檔案變更

### 計畫與人工閱讀文件

- 新增 `docs/plans/pattern-network-schema-v3-plan.md`。
- 更新 `use_case.md` 的現行規格入口、presentation 索引與第二關文字錯誤。
- 更新 `.gitignore`，忽略 `.tmp/` 驗證輸出。

### Pattern Network

- 更新 Schema v3 規則、契約分工、templates 與 gates。
- 新增 `Pattern Network/migration-map.yaml`。
- 新增／更新 Gate harness：
  - `MigrationMapValidator.cs`
  - `MigrationMapValidationResult.cs`
  - `MigrationMapValidatorTests.cs`
  - `ProjectVerificationRunner.cs`
  - `ProjectVerificationRunnerTests.cs`
  - `GateApplication.cs`
  - `GateApplicationTests.cs`

### Domains

- 新增 `Domains/catalog.yaml` 與 `Domains/migration-map.yaml`。
- 完成 10 個 Aggregate：Board、Bomb、DoorAndMaze、Fire、Food、Game、GameResult、Level、Shield、Snake。
- 完成 5 個 Application Workflow、1 個 Journey、8 個 Presentation State 與 `RenderGameScreen` workflow。
- 重寫 Snake movement、tail collision、growth 與 tick atomicity 契約。
- 刪除 153 份已完成遷移的舊規格檔案。

### Misfit 與 Gate

- 將 `misfit.yaml` 重寫為 Schema v3 的 checker／manual review／residual misfit 清單。
- 新增 `validate-migration`。
- 新增統一 `verify-project` 執行流程與 YAML／JSON／Markdown 報告。

## 驗證結果

- Gate 測試：70/70 通過。
- `verify-project`：exit code 0。
- `is_successful = true`。
- `configuration_error = false`。
- 總檢查數：19。
- Blocking non-pass：0。
- 6 個 blocking checks 全部通過：network、specifications、migration、presentations、code architecture、misfit config。
- Audit 狀態：6 fail、2 pending、5 not_run；這些對應尚待產品程式重構、人工審查或尚未實作／登錄執行器的 custom checker。
- Domains：44 份 Schema v3 YAML；legacy 數量 0；沒有 `Domains/v3`。
- `git diff --check` 通過，只有 line-ending warnings。
- `.tmp` tracked file 數量為 0。
- 現行文件對已刪除規格的過時引用數量為 0。
- NuGet 出現 `NU1900`（無法取得 vulnerability feed），不影響測試與本次 blocking Gate 結果。

## 尚未完成與後續工作

規格與驗證基礎已完成，但產品程式碼尚未依新契約重構。下一階段建議依序處理：

1. 將 leaf use case classes 收斂為具凝聚力的 Application Service workflow methods。
2. 讓 `ProcessGameTick` 成為唯一 tick 協調者，移除 Presenter 對流程與 Session 的所有權。
3. 實作 `SnakeMovementPlan`、尾巴離開碰撞規則、身體連續性及 `+2` 成長失敗的整個 tick 回滾。
4. 將 UI 改為一次事件取得一次 `RuntimeDirective + GameScreenOutput`，並避免重建未變動 controls。
5. 依輸入契約移除或調整 WASD 方向命令。
6. 執行並關閉兩項人工審查：tick atomicity、runtime UI flicker。
7. 為目前 `not_run` 的五個 custom checkers 提供可執行檢測器或明確 evidence provider。
8. 重構完成後再次執行 70 個 Gate tests 與 `verify-project`，並檢查 Audit 項目是否可以轉為 pass。

## 下一個 Session 的建議起點

若要開始產品程式重構，先讀取：

1. 本 SessionRecord。
2. `docs/plans/pattern-network-schema-v3-plan.md`。
3. `Domains/catalog.yaml`。
4. `Domains/Game/application/process-game-tick/` 的 workflow 與 technical contract。
5. `Domains/Snake/` 的 Aggregate 契約。
6. `misfit.yaml` 的 residual misfits 與 manual reviews。
7. 最新 `.tmp/pattern-network/verification-report.md`（若工作區仍保留該次輸出）。

開始實作前，需先把第一批預計修改的產品程式檔案、契約對應與驗證方式列清楚；若後續還要修改 `Domains/**` 或 `Pattern Network/**`，必須依專案規則重新取得當次、逐檔的明確同意。
