# Session Record — 2026-11-03 23:38

## Context

- **Project:** Snake
- **Branch:** v2
- **Participants:** Scott Liu

## Summary

本次延續 v1 與 v2 的規格和實作比較，集中釐清「v2 沒有一份完整 tick 契約」到底代表什麼。結論不是 v2 目前排出的 happy path 必然不合理，而是一次 250ms 遊戲步進的完整順序、跨 use case 的資料語意、互斥分支、失敗後狀態、同時事件優先序與最終結果，目前主要由 `GamePresenter` 的實作臨時承擔，沒有單一可重用且可端到端驗收的契約負責。

討論也區分了不同層級的缺口：尾格碰撞與蛇身不連續不只是「順序契約」問題，還涉及 Domain 規則、Aggregate invariant 與 acceptance scenario 不完整；通關重複判斷、部分完成狀態、門與移動的先後、其他入口重複編排，則更直接反映完整 workflow owner 缺席。

最後重新檢查 v2 UI 更新路徑，修正「每次讀取 `GameSession` 都會重畫」的誤解。正常 tick 通常只在最後呼叫一次 `RenderCurrentScreen()`；v2 的閃爍議題是一次 `GameForm.Render()` 會分別更新多個子控制項，尤其每次清除並重建規則清單，而整個 Form 沒有像 v1 一樣透過單一雙緩衝 `OnPaint()` 統一呈現。依靜態程式碼只能確認閃爍風險，不能斷言一定已產生肉眼可見的閃爍。

## Detailed Q&A Record

### Q1. 「v2 自己拼接小 use case」是指依賴外部物件決定要呼叫哪個 use case，還是因為沒有執行順序，所以 AI 憑直覺拼接？

「自行拼接」主要指後者，但需更精確地說：v2 並不是完全沒有順序，現在的順序已寫在 `Snake.WinForms/GamePresenter.cs::OnTickRequested()` 中；問題是沒有一份上層規格明確宣告這個順序是唯一正確的產品行為，也沒有一個 application workflow use case 擁有並回傳完整 tick 結果。

目前 Presenter 大致自行編排：

```text
UpdateThirdLevelDoorState
→ AdvanceSnakeStep
→ ReadGameplay
→ 處理碰撞
→ 處理物件接觸
→ EvaluateLevelCompletion
→ Presenter 再次判斷是否通關
→ ReplenishTriggeredObject
→ RenderCurrentScreen
```

各個小 use case 的結構化規格可以各自完整，但「這些局部行為如何組成一次完整 tick」仍需要額外的 workflow contract。這不等於小 use case 設計本身錯誤，也不等於規格檔案數量越多越糟；真正差異在於是否存在完整流程的契約擁有者。

### Q2. Presenter 為什麼會需要知道草莓、火焰、炸彈、通關和補物件的順序？這只是因為都依賴 `GameSession` 嗎？

不是單純因為它們都讀寫 `GameSession`。`GameSession` 是共享的即時遊戲狀態，不是過期快照；關鍵是 Presenter 必須根據共享狀態作出商業分支：接觸哪一種物件要呼叫哪個 use case、致命火焰或炸彈是否進入護盾救援或結束關卡、通關後是否還要補物件等。

這表示 Presenter 不只是把 ViewModel 交給 View，而是在決定 application workflow。若產品與 Pattern Network 原本宣告 UseCases 擁有 application orchestration、WinForms 不擁有 business decision，這些判斷就形成責任外洩。若產品反過來正式指定 Presenter 為 tick workflow owner，設計仍可成立，但必須為 Presenter 補上完整順序、失敗規則與端到端驗收，並接受其他入口難以直接重用 WinForms Presenter 的代價。

### Q3. v2 看起來順序合理，究竟會有什麼問題？

目前 happy path 的順序確實看似合理，問題是它的正確性主要依賴當前 Presenter 實作，而非正式契約。具體風險包括：

1. `EvaluateLevelCompletionUseCase` 與 Presenter 重複知道通關條件。若第三關改成「長度達標且通過出口」，只修改 Evaluate 不夠；Presenter 若仍比較 `CurrentLength >= TargetLength`，就會錯誤結束關卡。
2. 小 use case 的局部前後條件可能不足以描述一次移動所需的完整語意，例如本次是否成長、尾巴是否會離開，以及進入尾格是否碰撞。
3. 每個局部結果都合法，不代表組合後的蛇身仍是連續拓樸。
4. 多個 use case 依序修改共享狀態，中途失敗可能留下部分完成狀態。
5. 門切換與蛇移動同時到期時，先後順序會改變結果，但目前優先序只存在於 Presenter 程式碼。
6. 新物件會迫使 UI Presenter 理解更多商業效果及後續流程。
7. Console、Web、Replay、AI 或 headless runner 若不能呼叫完整 tick 入口，就必須各自重寫相同編排。

### Q4. 為什麼「未成長時可以進入本次即將離開的尾格」，v2 卻可能誤判自撞？

假設蛇身為：

```text
頭 A → B → C → 尾 D
```

本次下一個頭位置剛好是 D。如果沒有成長，正常移動會同時移除舊尾 D，因此移動後的佔用格應是：

```text
新頭 D → A → B → C
```

這可以是合法移動。但 v2 的 `AdvanceSnakeStepUseCase` 先以整個現有 `BodyCells` 判斷：

```csharp
if (_session.Snake.BodyCells.Contains(movement.Position))
{
    // SelfCollision
}
```

目前尾格 D 仍包含在 `BodyCells`，因此可能在真正執行 `MoveTo()`、移除尾巴之前就回報自撞。

需要修正先前過度概括的說法：這不是因為 use case 技術上讀不到 `GameSession`。它持有 session，也能取得狀態；真正缺少的是契約沒有要求它整合「本次是否成長、尾巴是否離開、碰撞集合是否排除即將離開的尾格」這組語意，也缺少對應 acceptance scenario。

因此直接缺口首先是 Domain／Advance 規則不完整；use case 切割與沒有完整 tick 契約會放大這個缺口，但不是唯一原因。

### Q5. v2 的成長為什麼可能長度正確，蛇身位置卻錯誤？

v2 先由 `MoveTo()` 移動：

```csharp
_bodyCells.Insert(0, nextHead);
_bodyCells.RemoveAt(_bodyCells.Count - 1);
```

舊尾已被移除。之後食物 handler 才從整張棋盤取得合法空格，呼叫 `Snake.Grow(...)`。這些空格只保證位於棋盤內且未被占用，不保證是剛才離開的尾格，也不保證和目前尾巴相鄰。

所以以下局部條件可能全部成立：

- 食物已消失。
- 長度增加一格。
- 新格沒有和其他物件重疊。
- 新格位於棋盤內。

但新增身體可能出現在遠處，例如主要蛇身在右下角，新身體卻出現在 `(0,0)`。這反映 Snake Aggregate 缺少「身體是依順序相鄰的連續路徑」之 invariant，也缺少完整 tick 最終狀態的驗收。只增加呼叫順序契約並不會自動修復拓樸；必須同時定義成長語意與連續性不變條件。

### Q6. 多個 use case 中途失敗，為什麼會留下部分完成狀態？一定需要 rollback 嗎？

假設依序執行：

```text
AdvanceSnakeStep 完成
→ HandleOrdinaryFoodContact 完成
→ 蛇長增加
→ EvaluateLevelCompletion 完成
→ ReplenishTriggeredObject 發生例外
```

Presenter 的 `catch` 只呼叫 `ShowFailure()`，不會撤銷已完成的移動、食物消失、成長或已發布事件，因此 session 可能停在「食物吃掉了、蛇也變長，但替代食物沒有補出來」的部分完成狀態。

完整 tick 契約不必強制採用資料庫式 rollback，但必須明確選擇並驗收其中一種策略：

- 整個 tick 原子提交，失敗則回復。
- 允許部分提交，將補物件標記為待重試。
- 先完成所有驗證與計畫，再一次提交狀態。
- 明確定義哪些失敗可保留前段結果、哪些不可保留。

v1 也沒有真正的 transaction／rollback；差異只是其「移動到補物件」核心語意集中在私有 `AdvanceOneCell()`，而 UI 正常流程通常在操作完成後才 `Invalidate()`。集中處理降低中間狀態被畫出的機會，但不提供失敗回復能力。

### Q7. 同一 tick 的門切換和蛇移動，順序契約為什麼重要？

v2 Presenter 目前固定：

```text
先 UpdateThirdLevelDoorState
→ 再 AdvanceSnakeStep
```

若門剛好到關閉時間，蛇頭也剛好要通過門：

- 先關門再移動：蛇可能撞門。
- 先移動再關門：蛇可能成功通過，門延後關閉或進入其他狀態。

兩種程式都能自圓其說，但只有產品規則能決定哪一種正確。v1 的 `UpdateGameTime()` 至少集中比較下一個移動、警告和門切換事件時間，再選擇下一個到期事件；即使如此，完全相同時間的優先序仍應由契約明訂。

### Q8. 沒有完整順序契約，是否表示新增需求就要複製 `GamePresenter` 再寫一次？

這是風險之一，但需要分兩種情境：

- 在同一個 WinForms 加入新規則時，不一定複製整個 Presenter，但可能必須同步修改新的 Contact UseCase、`HandleObjectContact()` switch、後續救援／結束判斷、補物件與 Render。漏改一處或排序不同就可能造成規則漂移。
- 新增 Console、Web、Replay、AI 或 headless 入口時，這些入口不能合理重用 WinForms Presenter，便可能各自複製 `Advance → Collision → Contact → Rescue/End → Evaluate → Replenish → Read Model` 的編排。

若 UseCases 提供穩定的完整入口：

```csharp
TickResult result = _advanceGameTickUseCase.Execute(command);
_view.Render(ToViewModel(result));
```

所有 adapter 便可呼叫同一份 workflow，Presenter 不必重新推導商業順序。`TickResult` 也應提供有語意的最終 phase、outcome、碰撞／物件結果與最終 snapshot，而不只是泛用 `IsSuccess`，如此 Presenter 才不需重新比較通關條件。

### Q9. 「沒有完整契約」與通關重複判斷、尾格、拓樸、失敗和多入口問題有何關聯？

應區分三層契約：

| 層級 | 負責內容 | 本次例子 |
|---|---|---|
| Domain 規則／Invariant | 單一商業概念什麼才算正確 | 尾巴離開語意、蛇身必須連續 |
| 局部 Use Case 契約 | 一個操作的輸入、輸出與局部副作用 | 移動、吃食物、救援、補物件 |
| 完整 Tick Workflow 契約 | 局部操作如何排序、互斥、交接、失敗及產生最終結果 | 250ms tick 的完整流程 |

各問題與缺口的關係為：

- 通關重複判斷：直接是缺少權威的 workflow owner 與語意結果交接。
- 尾格誤判：主要是 Domain／Advance 規則缺口，完整 workflow 沒有統整成長與尾巴語意則進一步放大問題。
- 蛇身拓樸錯誤：主要是 Snake invariant 與最終 acceptance 缺口；單有順序仍不足。
- 部分完成狀態：直接是完整 workflow 沒有失敗與提交策略。
- 門與移動先後：直接是完整 workflow 沒有時間事件優先序。
- UI 知道物件商業流程：缺少穩定 application boundary，責任外洩到 Presenter。
- 其他入口重寫：缺少可重用的完整 workflow 入口。

完整 tick use case 不必是一個巨型方法；它可以編排多個小 use case 或 domain operation。重點是有一個明確擁有完整前置條件、順序、互斥分支、失敗政策、最終 postcondition 與 `TickResult` 的邊界。

### Q10. v2 的閃爍到底是怎麼回事？是否每讀一次 `GameSession` 就重畫整個 UI？

不是。`ReadGameplay()` 只讀取目前 session snapshot，不會觸發 repaint。正常 tick 通常在所有遊戲處理完成後只呼叫一次 `RenderCurrentScreen()`，因此問題不是每個小 use case 的中間狀態必然逐段顯示。

實際路徑是：

```text
250ms Timer Tick
→ GamePresenter.OnTickRequested()
→ 完成多個遊戲操作
→ RenderCurrentScreen()
→ GameForm.Render()
```

一次 `GameForm.Render()` 會更新多個獨立控制項：棋盤、進度、關卡／長度 Label、護盾 Panel、規則清單、結果 Panel、按鈕、Timer 與側欄尺寸。最具體的風險是 `RenderRules()` 每次都執行：

```csharp
_rulesList.Controls.Clear();
foreach (RuleHintViewModel ruleHint in viewModel.RuleHints)
{
    _rulesList.Controls.Add(CreateRuleLabel(...));
}
```

即使規則內容沒有變，仍會移除並重建全部 Label，再重新計算 Panel 高度與布局。若 Windows 在控制項移除、加入或重排的時間窗繪製，使用者可能看到規則區短暫空白、文字閃動、背景露出或側欄跳動。

`SuspendLayout()` 主要暫停布局計算，不等於建立整個 Form 的共同後方緩衝；`ResumeLayout(true)` 最後還會觸發布局。v2 明確使用雙緩衝的是 `GameBoardControl` 與 `GameProgressControl`，因此棋盤會在自己的 `OnPaint()` 中一次畫完格子、牆、門、物件和整條蛇，進度條也有自己的緩衝，但它們無法保證所有 Label、Panel、Button 和整個 Form 在同一畫面交換點同步呈現。

### Q11. v1 與 v2 的雙緩衝差異應如何正確描述？

`DoubleBuffered` 是降低閃爍的措施，不是造成閃爍的原因。

v1 將主要棋盤和側欄放在同一個 `GameForm.OnPaint()`／`Graphics` 流程，並對整個 Form 設定 `DoubleBuffered = true`。正常流程先更新遊戲狀態，再由外層呼叫 `Invalidate()`；Windows 稍後以當時的完成狀態重畫整個畫面。這使背景、棋盤、蛇、物件與側欄較接近在同一個後方緩衝完成後一次顯示。

v2 採用多個 WinForms 子控制項；棋盤與進度各自雙緩衝，但規則清單和其他側欄控制項有各自的生命週期與 paint/layout 時機。因此 v2 的問題不是「有 DoubleBuffered 所以閃爍」，而是沒有 v1 那種涵蓋整個主要畫面的單一雙緩衝繪製邊界，並且每個 tick 仍重建部分控制項樹。

需要保留證據邊界：從程式碼可以確認更新架構具有時間性／重畫風險，靜態 screenshot 或 presentation 最終狀態驗證無法證明連續畫面穩定；若沒有實際執行觀察或動態測試，應稱為「閃爍風險」，而不是斷言 v2 一定已發生肉眼可見的閃爍。

## Key Clarifications

- v2 有實際執行順序；缺少的是由上層規格與完整 application boundary 保證的權威順序，而不是程式碼完全沒有順序。
- 小 use case 可以讀取 `GameSession`；尾格問題不是技術上拿不到資料，而是契約沒有要求整合成長、尾巴離開與碰撞集合的完整語意。
- 完整 tick 契約負責局部行為如何組合；Domain invariant 仍需分別定義尾格規則與蛇身連續性。前者不能取代後者。
- `EvaluateLevelCompletionUseCase` 與 Presenter 都比較長度，是重複商業判斷；未來條件改變時可能漂移。
- 完整 tick 契約不等於一定要 rollback，但必須明確定義失敗時的提交／保留／回復策略。
- `ReadGameplay()` 不會重畫 UI；正常 tick 通常最後才 Render 一次。
- v2 的棋盤使用雙緩衝並一次畫完整棋盤，不是把蛇逐節顯示；主要閃爍風險位於多控制項更新、規則清單重建與布局。
- `SuspendLayout()` 不是全畫面雙緩衝，只是延後布局計算。
- 目前只能從程式碼確認 v2 有閃爍風險，不能在沒有執行觀察的情況下宣稱一定發生。

## Decisions Made

- 後續描述 v2 時，使用「Presenter 是目前事實上的 tick workflow owner，但沒有完整且可重用的正式契約」；不再籠統說成「v2 沒有順序」。
- 尾格與成長拓樸問題需分別追溯 Domain 規則／invariant／acceptance 缺口，不將所有問題一律歸因於 use case 拆分。
- 對 UI 僅主張已證實的架構風險：不把 `ReadGameplay()` 說成重畫來源，也不在缺乏動態證據時斷言 v2 必然閃爍。
- 如果未來正式改善 v2，應先決定完整 tick workflow owner 與時間性 presentation contract；若需修改 `Domains/**` 或 `Pattern Network/**`，必須先依專案規則取得明確授權。

## Code Changes

- 新增本 SessionRecord：`SessionRecords/2026-11-03_23-38-clarify-v2-tick-and-rendering-risks.md`。
- 沒有修改應用程式、測試、`Domains/**` 或 `Pattern Network/**`。

## Follow-up Items

- 若要驗證 v2 是否實際閃爍，需要執行 WinForms 並觀察或錄製連續 tick，或建立能偵測控制項重建與中間畫面的動態 UI 測試。
- 若要修正 UI 更新風險，可評估避免每 tick 清空／重建規則控制項、只更新真正變動的值，以及建立更完整的呈現緩衝邊界；本次未實作。
- 若要修正遊戲流程架構，需先定義完整 tick 的輸入粒度、輸出粒度、contract owner、事件優先序、互斥分支、失敗策略與端到端 acceptance；本次未修改受保護規格。
