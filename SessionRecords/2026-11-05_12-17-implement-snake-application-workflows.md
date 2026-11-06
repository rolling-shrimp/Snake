# Session Record — 2026-11-05 12:17

## Context

- **Project:** Snake (`C:\Users\wbcwb\Desktop\SDD_Coding\snake\Snake`)
- **Branch:** `v2`
- **Participants:** Scott Liu、Codex
- **Updated:** 2026-11-05 21:12

## Summary

本次工作承接已完成的 Pattern Network、Domains schema v3 與 `misfit.yaml` 重整，依新規格實作實際產品程式。核心目標是把原本由 Presenter 串接大量葉節點 Use Case 的流程，收斂成 `SnakeGameApplicationService` 的五個完整 Application Workflow：`StartCurrentLevel`、`SetSnakeDirection`、`ProcessGameTick`、`RestartGame` 與 `GetCurrentScreen`。

實作同時修正蛇身移動契約：一般移動可進入本次會離開的舊尾格；普通食物保留舊尾形成 +1；特殊食物依直後、左、右的固定候選順序增加第二格；所有成功狀態都必須維持蛇身順序、唯一與連續。整個 Tick 使用版本化工作快照，完成所有到期事件後只提交一次，任何契約失敗都不會留下部分狀態。

UI 已改為每個事件只呼叫一次 Application Workflow，先套用 `RuntimeDirective`，再渲染一次完整 `GameScreenOutput`。舊 38 個 `...UseCase.Execute()` 類別及其專用 Reactor、Port、DTO、讀模型與舊 acceptance 測試已刪除。五個自訂 Gate 已正式接入統一驗證流程；目前所有自動檢測通過，只保留實機 UI 閃爍人工 review 為 pending。

後續實機操作發現新版按方向鍵時蛇沒有轉向。Runtime trace 已確認目前版本的方向鍵先進入 `GameForm.ProcessCmdKey`，再由 `ProcessDialogKey` 回傳 `true` 並當成導覽鍵消耗，因此 `OnGameKeyDown` 與 `DirectionRequested` 都沒有執行。暫時在 `ProcessCmdKey` 攔截方向鍵的對照實驗證明 Presenter、Application Service、queued direction 原子提交與下一個 Tick 全部正常；問題斷點位於 WinForms 輸入邊界。所有暫時診斷程式與 trace 檔已移除，產品程式及受保護規格尚未修改。

## Detailed Q&A Record

### Q1. 為什麼不能繼續讓每個 Use Case 都是一個獨立 class，由 Presenter 排列執行順序？

問題不在 class 數量本身，而在完整使用情境的契約擁有者。舊設計把移動、碰撞、食物、護盾、死亡、通關、補物件與畫面查詢拆成公開葉節點，導致 Presenter 必須知道執行順序、互斥分支與中間狀態。

最終決策是由一個 Application Service 擁有玩家與 Timer 可觸發的完整 Workflow。內部仍可有清楚的私有步驟，但 UI 邊界不能再自行組合業務流程。對外公開的五個 Workflow 分別對應一個完整玩家／runtime 事件與一個完整結果。

### Q2. Gate 是否代表每個 `misfit.yaml` acceptance 都必須有一個一對一單元測試？

不是。Gate 是閘門與證據聚合機制，可以由靜態 checker、行為測試、時間測試、不變式測試、runtime UI 檢查或人工 review 提供證據。一個 checker 或測試集合可以同時覆蓋多個 scenario，不要求檔案或測試方法一對一對應 acceptance。

本次將五個原本 `not_run` 的自訂 checker 接上 `ProjectVerificationRunner`，分別檢查：

- Application Workflow 結構與舊葉節點 Use Case 殘留。
- Presenter 是否越權擁有業務順序。
- 蛇身移動、尾格、自撞與成長拓樸。
- UI 是否清空重建控制項，以及 renderer 是否擁有 Timer。
- 是否仍把 WASD 映射為方向命令。

### Q3. 刪除舊 38 個 Use Case 與新增 Application Service 有什麼直接關聯？

Application Service 不是舊 Use Case 外面再包一層。如果保留舊公開葉節點，Presenter 或其他呼叫端仍能繞過完整 Workflow 契約，系統會同時存在兩套入口與兩種執行順序。

因此在五個新 Workflow 接管所有產品呼叫後，直接刪除舊 38 個 `...UseCase.Execute()` 類別，不保留 wrapper 或 deprecated compatibility layer。隨後再以跨檔案引用分析移除只為舊流程存在的 Reactor、Port、Command／Result、DTO、projection、read model 與測試 fixture。

### Q4. `ProcessGameTick` 如何保證順序與原子性？

`SnakeGameSession.Load()` 回傳同一版本的完整深複製快照；Workflow 只修改工作快照。到期事件依時間遞增排序，同時刻依 `DoorClosingWarning`、`DoorToggle`、`SnakeMoved` 排序。每次移動先決定接觸物與 `growthDelta`，再建立 `SnakeMovementPlan`，之後才判斷碰撞、提交蛇身、套用物件效果、處理死亡／護盾、通關、延後關門與補物件。

完成所有允許事件後只呼叫一次 `TryCommit(expectedVersion, snapshot)`。特殊食物找不到合法尾端延伸格、時間倒退或其他契約失敗時，回傳原始已提交畫面，時間、物件、蛇身、門與關卡結果都不變。

程式碼 review 額外發現並修正「開始第二／三關時部分物件生成失敗，Failure Result 卻顯示未提交工作快照」的問題；現在失敗畫面一律使用原始快照。

### Q5. 蛇身連續與自撞誤判如何修正？

`Snake.PlanMovement` 先產生完整計畫：

- `growthDelta = 0`：碰撞集合排除本次會離開的舊尾格。
- `growthDelta = 1`：保留完整舊蛇身，形成 +1。
- `growthDelta = 2`：保留完整舊蛇身後，再依直後、左、右選第一個合法尾端格。

`Snake.CommitMovement` 與 `ResetSafely` 都會透過 `Board.IsContinuous` 驗證順序、唯一與依邊界政策的連續性。可繞過此契約的舊 `MoveTo`、`Grow`、`Reset` 已刪除。`Board.FindSafeBody` 也會保留安全的下一步格，避免護盾重設後立即撞上自己的身體。

### Q6. UI 統一流程與閃爍問題如何處理？

`GamePresenter` 現在只依賴 `ISnakeGameApplicationService`。初始化、開始、方向、Tick、重玩各自只呼叫一個完整 Workflow，之後先呼叫 `ApplyRuntimeDirective`，再呼叫一次 `RenderGameScreen`。

`GameForm.RenderGameScreen` 不再控制 Timer，也不再呼叫 `Controls.Clear` 重建規則 Label。Board control 與規則控制項會重用既有 instance，並在 `SuspendLayout`／`ResumeLayout` 之間更新完整畫面。Timer 使用 `Stopwatch.ElapsedMilliseconds` 傳遞關卡開始後的單調累積時間，延遲 Tick 可以由 Application Workflow 補完所有到期事件。輸入只接受方向鍵，WASD 與其他按鍵被忽略。

程式碼 review 也修正 Playing 畫面仍顯示上一關 ResultCard 的問題；ResultCard 現在只在 Ready 結果畫面、GameOver 或 Completed 顯示。

### Q7. 最終驗證結果是什麼？

- `dotnet test Snake.sln`：31/31 通過。
- `PatternNetwork.Gates.Tests`：73/73 通過。
- 五個自訂 checker：全部 `pass`。
- Tick 原子性 review：`pass`。
- 五個 residual misfit：全部 `resolved`／`pass`。
- 統一 `verify-project`：無 blocking violation。
- 唯一保留：`SNAKE-REVIEW-UI-FLICKER = pending`。
- `git diff --check`：沒有 whitespace error。
- Gate 專案仍有 `NU1900`，原因是 NuGet 漏洞資料來源無法連線；不影響建置、測試或 Gate 結果。

### Q8. 為什麼方向鍵在新版沒有反應？

WinForms 會先對某些按鍵做命令鍵或對話框導覽鍵的預處理；方向鍵可能在產生一般 `KeyDown` 前被目前具有焦點的控制項使用。`KeyPreview = true` 只能讓 Form 優先接收已經會形成 `KeyDown` 的按鍵，不能保證所有方向鍵都一定進入 Form 的 `KeyDown` handler。Microsoft 官方文件也說明了這個鍵盤訊息流程：<https://learn.microsoft.com/en-us/dotnet/framework/winforms/how-to-handle-keyboard-input-at-the-form-level>。

實際 runtime trace 已確認本次根因：方向鍵先進入 `GameForm.ProcessCmdKey`，`base.ProcessCmdKey` 回傳 `false`；接著進入 `GameForm.ProcessDialogKey`，`base.ProcessDialogKey` 回傳 `true`。訊息因此在導覽鍵預處理階段被消耗，沒有進入 `OnGameKeyDown`，也沒有產生 `DirectionRequested`。當時焦點位於 `GameBoardControl`，所以並非只有焦點停在開始或重玩按鈕時才會發生。

### Q9. v1 同樣使用方向鍵，為什麼沒有發生？

Git 版本比對顯示，v1 與目前版本的基本輸入機制相同：兩者都有 `KeyPreview = true`，也都把 Form 的 `KeyDown` 接到方向處理方法。v1 額外接受 WASD，目前版本依新規格只接受方向鍵；舊 Presenter 呼叫 `SetSnakeDirectionUseCase.Execute()`，新 Presenter 則呼叫 `SnakeGameApplicationService.SetSnakeDirection()` 並重新渲染。

目前版本的實際斷點已確認是 `ProcessDialogKey` 消耗方向鍵；但這仍未完整解釋 v1 為何在使用者環境正常。v1 與新版的控制項樹、焦點落點或實際使用的按鍵可能不同，若要解釋版本差異，仍需對 v1 做相同 runtime trace。先前在沒有 trace 前把 `ProcessCmdKey` 說成確定根因確實過早；本次已用 runtime evidence 補足該判斷。

### Q10. 是新規範的敘述造成這個問題嗎？

新規範沒有要求錯誤行為。`set-snake-direction` workflow 明確規定 UI 方向鍵 adapter 只把 Up／Down／Left／Right 轉成方向命令，WASD 與其他鍵不進入 workflow；接受條件也要求 Up 等方向鍵可以被接受。

Acceptance 文字本身其實有描述 `arrow_key: Up` 應產生 `command_direction: Up`，因此不能說它完全從 Command 之後才開始；精確問題是它的 evidence 沒有執行 Windows 實體按鍵與 WinForms 訊息路由。非方向鍵情境更直接從 `ui_key_down_received: true` 開始。現有 C# checker 只靜態搜尋 `Keys.Up`、`Keys.Down`、`Keys.Left`、`Keys.Right`，Presenter 測試則直接觸發 `DirectionRequested`。它們能證明「方向通知一旦送達，系統會處理」，卻不能證明「實體方向鍵一定會產生該通知」。因此 `SNAKE-MISFIT-WASD-DIRECTION` 僅憑靜態映射就標記 resolved，證據不足，後續應補上 runtime UI evidence 並重新評估。

### Q11. 實際追蹤後，方向輸入到底斷在哪一層？

基準追蹤得到以下順序：

```text
方向鍵
→ GameForm.ProcessCmdKey：收到方向鍵，base 回傳 false
→ GameForm.ProcessDialogKey：收到方向鍵，base 回傳 true
→ OnGameKeyDown：未執行
→ DirectionRequested：未發生
→ queued_direction：維持 none
→ 下一個 ProcessGameTick：沿原方向移動
```

隨後以不保留的暫時攔截做對照實驗。`ProcessCmdKey` 接住 `Down` 並送出 `DirectionRequested` 後，Presenter 呼叫 `SetSnakeDirection`，Application Service 回傳 `Accepted` 並提交 `queued=Down`；下一個 Tick 讀到 `current=Right、queued=Down、next=Down`，蛇頭由 `(8,1)` 移至 `(8,2)`，完成後成為 `current=Down、queued=none`。Up、Left、Right 也得到相同的完整路徑結果。

因此 Presenter、Application Service、snapshot commit 與 `ProcessGameTick` 都沒有斷。唯一已確認斷點是目前 `GameForm` 只在 `KeyDown` 建立方向事件，但方向鍵已先被 WinForms `ProcessDialogKey` 消耗。

## Key Clarifications

- Problem Frame 的 `world_facts` 描述該情境既存的世界事實，不是 Design by Contract 規則集合。
- Aggregate `domain_spec.yaml` 才是領域規則、不變式、行為與硬性契約的擁有者。
- Workflow `technical_spec.yaml` 描述具體輸入／輸出 model、欄位、型別、Port 與錯誤映射。
- Gate 是可由不同證據來源組成的閘門，不是 acceptance 與單元測試的一對一清單。
- DDD 不代表每個 Use Case 都必須是一個 class，也不代表只要把方法集中到名為 Service 的 class 就完成設計；關鍵是契約擁有者、完整使用情境與 Aggregate 行為邊界。
- Application Service 必須真正取代舊公開入口，不能只是包裝舊 38 個 Execute 類別。
- UI Renderer 只機械式套用完整輸出，不得再讀 Session、Entity 或呼叫後續 Workflow 補齊畫面。
- v1 與目前版本都使用 `KeyPreview` 加 Form `KeyDown`；目前版本已確認方向鍵在 `ProcessDialogKey` 被消耗，但 v1 為何未出現同樣現象仍未做對照 trace。
- `KeyPreview = true` 無法保證方向鍵產生 `KeyDown`；本次實測由 `ProcessDialogKey` 回傳 `true` 並終止路徑。
- `ProcessCmdKey` 在控制項導覽前攔截方向鍵的對照實驗已證明能打通完整方向提交與下一個 Tick，但尚未作為正式產品修正保留。
- Acceptance 有描述 `arrow_key → command_direction` 的邏輯關係；缺少的是 Windows 實體按鍵、焦點與 WinForms 訊息路由的 runtime evidence。

## Decisions Made

- 對外只保留五個完整 Application Workflow。
- 舊 38 個 `...UseCase.Execute()` 類別直接刪除，不保留 wrapper。
- `ProcessGameTick` 擁有完整 Tick 順序、死亡互斥、回滾與單次提交。
- 蛇身的移動與成長一律先建立 plan，再由 Aggregate 驗證並提交。
- UI 每個事件只呼叫一次 Workflow，並只渲染一次最終完整畫面。
- Timer directive 與 renderer 分離。
- WASD 不再映射為方向輸入。
- 自動證據足夠的 residual misfit 標記 resolved；實機 UI 閃爍 review 繼續 pending。
- 本次沒有建立相容層，也沒有建立額外版本資料夾。
- Runtime trace 已將方向鍵斷點定位在 `GameForm` 的 WinForms 預處理階段；正式修正位置應在 UI input adapter，而不是 Presenter、Application Service 或 Domain。
- 本輪只做暫時診斷與對照實驗；尚未保留 `ProcessCmdKey` 修正，也沒有變更產品行為。
- 若要把實體按鍵傳遞與焦點行為納入受保護規格或 Gate，必須先列出確切檔案與內容，重新取得使用者對該次變更的明確同意。

## Code Changes

- **Snake.Entities**
  - 新增 `SnakeMovementPlan`、`SnakeMovementPlanResult`。
  - `Snake` 新增 `PlanMovement`、`CommitMovement`、`ResetSafely`，並移除可繞過契約的舊 mutation method。
  - `Board` 新增連續性、尾端候選與安全蛇身搜尋。
  - `Game` 新增累積時間基準；Door、Food 與其他 Aggregate 增加 Workflow 所需的明確行為與深複製。
  - 移除舊流程專用 Domain Event 類別。

- **Snake.UseCases**
  - 新增 `ISnakeGameApplicationService` 與 `SnakeGameApplicationService`。
  - 新增五個 Workflow 的 Command／Query、Result、Outcome 與完整 `GameScreenOutput` model。
  - 新增版本化完整快照、snapshot store、spawn selector Port 與畫面輸出 factory。
  - `SnakeGameSession` 簡化為只負責版本化完整快照與 compare-and-swap 提交。
  - 刪除舊 38 個公開 Use Case class 及其專用 Reactor、Port、DTO、projection 與 read model。

- **Snake.WinForms**
  - `GamePresenter` 改為只依賴 Application Service。
  - `IGameView` 改為 `ApplyRuntimeDirective` 與 `RenderGameScreen`。
  - `GameForm` 採完整畫面渲染、穩定規則控制項、累積時間 Timer 與方向鍵限定。
  - 新增 `RandomSpawnPositionSelector`，並更新 `Program.cs` 組裝流程。

- **Snake.Test**
  - 新增 Application Workflow、原子快照、蛇身拓樸、事件排序、死亡互斥、通關與 UI 穩定 identity 測試。
  - 刪除只驗證舊葉節點 Use Case 的 acceptance 測試與 fixture。

- **Pattern Network Gate**
  - 新增五個 C# 自訂 checker。
  - `GateCheckerRegistry` 新增指定 checker 執行能力。
  - `ProjectVerificationRunner` 會實際執行 `misfit.yaml` 中已註冊 checker，不再一律回報 `not_run`。
  - 新增 checker regression tests，並更新 project verification tests。

- **misfit.yaml**
  - Tick 原子性 review 改為 pass 並記錄證據。
  - 五個 residual misfit 改為 resolved。
  - UI flicker runtime review 保留 pending。

本輪方向鍵診斷曾暫時加入 `ProcessCmdKey`、`ProcessDialogKey` 與跨層 trace，完成基準與對照實驗後已全部移除；trace log 也已刪除。最終沒有留下產品程式碼、`Domains/**`、`Pattern Network/**` 或 `misfit.yaml` 變更，`Snake.WinForms` 重新建置成功且為 0 warning／0 error。

## Follow-up Items

- 在實際 WinForms 執行環境連續操作與重複渲染，確認沒有可見空白、半完成布局或閃爍；完成後更新 `SNAKE-REVIEW-UI-FLICKER` reviewer 與 evidence。
- 依已取得的 runtime 證據，決定是否正式由 `GameForm.ProcessCmdKey` 統一攔截方向鍵；若採用，應共用單一方向轉換方法，避免與原有 `KeyDown` 重複觸發方向命令。
- 新增能覆蓋 WinForms 實際鍵盤路由與焦點狀態的 regression test，並重新評估方向輸入 misfit 的 resolved 證據。
- 若仍需完整解釋 v1 與新版差異，對 v1 執行相同的 `ProcessCmdKey`／`ProcessDialogKey` runtime trace，比較控制項樹與焦點落點。
- 如需調整受保護的 `Domains/**` 或 `Pattern Network/**`，先依 `AGENTS.md` 列出確切路徑、原因、預計內容與影響，取得本次明確授權後才能修改。
- 若 NuGet 網路可用，重新取得漏洞資料，確認 `NU1900` 消失。
- 目前工作樹包含先前 schema v3 規格遷移與本次產品實作的大量未提交變更；提交前應分組檢視 staged files，避免混入無關檔案。
