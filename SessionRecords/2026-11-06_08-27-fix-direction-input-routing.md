# Session Record — 2026-11-06 08:27

## Context

- **Project:** Snake (`C:\Users\wbcwb\Desktop\SDD_Coding\snake\Snake`)
- **Branch:** `v2`
- **Participants:** Scott Liu、Codex
- **Related comparison sources:** `C:\Users\wbcwb\Desktop\SDD_Coding\snake\v1`、`C:\Users\wbcwb\Desktop\SDD_Coding\snake\v2`

## Summary

本次工作承接前一份紀錄已定位的 WinForms 方向鍵斷點：方向鍵在一般 `KeyDown` 前被 `ProcessDialogKey` 當成控制項導覽鍵消耗，因此 `DirectionRequested` 沒有產生，後續 Application Workflow 與下一個 Tick 自然也收不到新方向。

規格結構已補上獨立的 Presentation Workflow `handle-direction-input`，明確區分「實體鍵盤訊息轉成 Presentation 事件」與「Application Workflow 接受方向命令」。Pattern Network 的 WinForms 準則、禁止事項、Gate、validator 與 C# checker 也同步加入 `ProcessCmdKey`、焦點無關、單一路徑派送及未宣告按鍵交回 WinForms 的契約。產品程式改由 `GameForm.ProcessCmdKey` 處理四個方向鍵，並由測試及實際視窗證明下一個到期 Tick 會使用已提交方向。

最後比較獨立的 v1、v2 原始碼後，釐清 v1 正常並不是「焦點剛好沒有停在開始按鈕」。v1 的開始按鈕只是 Form 上的繪圖矩形，根本不是可取得焦點的 WinForms `Button`；v2 才引入真正的 Button、Panel 與棋盤控制項樹，卻仍只依賴 `KeyDown`，因而暴露控制項導覽鍵的訊息路由問題。

## Detailed Q&A Record

### Q1. 問題是不是因為 Use Case 只描述「蛇接收命令轉向」，沒有描述玩家按實體方向鍵？

方向大致正確，但需要分清契約層級。原有證據能證明 `DirectionRequested` 或方向命令一旦送達，Presenter、Application Service、queued direction 與下一個 Tick 都能正常執行；它沒有充分證明 Windows 實體方向鍵在任意子控制項取得焦點時，一定能穿過 WinForms 預處理並建立該事件。

實體 `Up`、`Down`、`Left`、`Right` 不應直接成為 Domain 或 Application Workflow 的輸入欄位。適當切分為：

```text
實體方向鍵訊息
→ Presentation Workflow：handle-direction-input
→ DirectionRequested
→ Application Workflow：set-snake-direction
→ queued direction 提交
→ 下一個到期 ProcessGameTick 使用新方向
```

因此遺漏的是 Presentation 邊界與跨 Workflow journey 的契約／證據，不是把 WinForms 的 `Keys` 型別塞進 Domain。

### Q2. Domains、Pattern Network 與 `misfit.yaml` 如何補足這條路徑？

Domains 新增 `Domains/Game/presentation/workflows/handle-direction-input/`：

- `workflow.yaml` 定義輸入為 `WinFormsCommandKeyMessage`、輸出為 `DirectionRequestedEventArgs`。
- 交付階段固定為 `process_cmd_key_before_control_navigation`。
- 焦點範圍涵蓋 Form 與所有 descendant controls。
- 四個方向鍵各自只派送一次；未宣告輸入交回 WinForms。
- `acceptance.yaml` 同時要求結構 Gate 與實際 WinForms 訊息預處理證據。

`Domains/Game/journeys/play-game/acceptance.yaml` 補上「實體方向鍵 → Presentation Workflow → set direction → 下一個 Tick」的端到端情境。`Domains/Game/workflows/set-snake-direction/frame.yaml` 則保留層級邊界：原始 KeyData、焦點與 WinForms 路由不進入 Application Workflow。

Pattern Network 的 WinForms 節點加入：

- `doctrine.yaml`：宣告方向／導覽鍵必須在控制項導覽前交付，且不受子控制項焦點影響。
- `prohibitions.yaml`：禁止只依靠 `KeyDown`，也禁止 `ProcessCmdKey` 與其他鍵盤事件形成平行派送路徑。
- `gate.yaml`：`V3-PN-UI-GATE-004` 由 `winforms_boundaries` checker 檢查實作結構。
- `CSharpGateInspector`：驗證 `ProcessCmdKey` 簽章、四個映射、單次事件派送、方向鍵回傳 `true`、其餘輸入委派 `base.ProcessCmdKey`，以及不存在平行派送。
- `PresentationContractValidator`：驗證 Presentation Workflow 的輸入／輸出粒度、交付階段、焦點範圍及 acceptance evidence 宣告。

`misfit.yaml` 新增／更新 `SNAKE-MISFIT-ARROW-KEY-DELIVERY` 與 `SNAKE-REVIEW-DIRECTION-INPUT-DELIVERY`。Gate 不是 acceptance 對單元測試的一對一對照；這次證據包含靜態 checker、WinForms 訊息測試、Application Service 時序測試與實際 UI 操作。

### Q3. 程式如何正式修正？

`Snake.WinForms/GameForm.cs` 不再使用 Form `KeyDown` 作為方向派送入口，改為唯一的 `ProcessCmdKey` 路徑：

```csharp
protected override bool ProcessCmdKey(ref Message message, Keys keyData)
{
    Direction? direction = (keyData & Keys.KeyCode) switch
    {
        Keys.Up => Direction.Up,
        Keys.Down => Direction.Down,
        Keys.Left => Direction.Left,
        Keys.Right => Direction.Right,
        _ => null
    };

    if (direction is null)
    {
        return base.ProcessCmdKey(ref message, keyData);
    }

    DirectionRequested?.Invoke(this, new DirectionRequestedEventArgs(direction.Value));
    return true;
}
```

這個位置早於控制項導覽處理，因此即使焦點位於開始按鈕或其他子控制項，方向鍵仍會先轉成遊戲事件。方向鍵處理後回傳 `true`，避免再被導覽或重複派送；其他按鍵維持 WinForms 原有行為。

### Q4. 測試如何證明不是只測 `DirectionRequested` 之後？

`Snake.Test/GameFormRenderingTests.cs` 使用可見的 `GameForm`，讓真正的開始按鈕取得焦點，再對按鈕呼叫 WinForms `PreProcessMessage`。測試涵蓋 Up、Down、Left、Right，驗證每個實體訊息都恰好產生一次對應的 `DirectionRequested`。

`Snake.Test/SnakeGameApplicationServiceTests.cs` 的 `ProcessGameTick_ConsumesQueuedDirectionOnNextDueMovement` 另外證明：先接受 `Down`、提交 queued direction，再執行 250ms 到期 Tick 後，`CurrentDirection` 變成 `Down` 且 queued direction 清空。

本次重新驗證結果：

- `dotnet test Snake.sln --no-restore`：36/36 通過。
- `PatternNetwork.Gates.Tests`：79/79 通過。
- Gate 測試仍出現 `NU1900`，原因是無法取得 NuGet 弱點資料；沒有造成建置或測試失敗。
- `misfit.yaml` 記錄的實際視窗證據顯示：開始第一關後送出 `Down`，後續畫面由水平移動轉為垂直向下移動。

### Q5. v1 沒發生問題，是否純粹因為焦點沒有停在開始按鈕？v2 是否相同？

不是。v1 的「開始按鈕」不是 WinForms `Button`：

```csharp
private Rectangle _startButton = new Rectangle(...);
```

它只是 Form 自繪畫面上的命中區域。Form 自己接收 `MouseDown`，判斷座標位於矩形後開始遊戲；v1 的 `GameForm` 也沒有加入 Button、Panel 等子控制項。因此正常路徑是 Form 同時擁有滑鼠與鍵盤輸入，不存在開始 Button 取得焦點後把方向鍵拿去做控制項導覽的情況。

獨立的 v2 目錄則不同：

- `_startButton` 是真正的 WinForms `Button`。
- `CreateActionButton` 設定 `TabStop = true`。
- 畫面包含 `TableLayoutPanel`、`FlowLayoutPanel`、`GameBoardControl` 等子控制項。
- 只設定 `KeyPreview = true` 與 Form `KeyDown`。
- 開始事件只送出 `StartRequested`，沒有 `Focus()`、`Select()`、`ActiveControl` 或 `ProcessCmdKey` 處理。

所以舊 v2 的確具有同樣風險，但不能簡化為「焦點一直留在開始按鈕」。開始後按鈕會被隱藏，WinForms 可能把焦點轉給其他子控制項；先前 trace 也曾在焦點位於 `GameBoardControl` 時看到方向鍵被 `ProcessDialogKey` 消耗。根因是控制項樹與鍵盤預處理路徑改變，而不是單一按鈕的偶然焦點狀態。

## Key Clarifications

- v1 的開始按鈕是自繪 `Rectangle`，不是可取得焦點的 Button；「v1 只是剛好沒把焦點放在開始按鈕」並不正確。
- v2 引入真正的控制項樹後，`KeyPreview = true` 仍無法攔截已在導覽鍵預處理階段被消耗的方向鍵。
- 問題不只可能發生在開始按鈕；任何進入 WinForms 控制項導覽路徑的焦點狀態都可能受影響。
- 實體鍵盤路由屬於 Presentation Workflow；Application Workflow 從語意化的方向命令開始，Domain 不應依賴 WinForms 型別。
- Gate 是閘門及證據聚合，不要求每個 acceptance 各自對應一個單元測試。
- 修正位置是 UI input adapter 的 `ProcessCmdKey`，不是 Presenter、Application Service 或 Domain 的方向邏輯。

## Decisions Made

- 以 `handle-direction-input` 作為實體方向鍵訊息的 Presentation 契約擁有者。
- `ProcessCmdKey` 是四個方向鍵建立 `DirectionRequested` 的唯一產品路徑，不保留平行 `KeyDown` 派送。
- 宣告方向鍵處理後回傳 `true`；未宣告輸入交回 `base.ProcessCmdKey`。
- 端到端證據必須同時涵蓋實際 WinForms 預處理、任意子控制項焦點、單次派送與下一個 Tick 消費方向。
- v1、獨立 v2 目錄只用於唯讀比較，本次沒有修改。

## Code Changes

- `Domains/Game/presentation/workflows/handle-direction-input/workflow.yaml`：新增 Presentation 方向輸入 Workflow。
- `Domains/Game/presentation/workflows/handle-direction-input/acceptance.yaml`：新增四方向鍵、焦點無關、未宣告輸入與 runtime evidence 規格。
- `Domains/Game/journeys/play-game/acceptance.yaml`：加入實體鍵盤到下一個 Tick 的端到端情境。
- `Domains/Game/workflows/set-snake-direction/frame.yaml`：補上 WinForms 預處理世界事實與 Presentation/Application 邊界。
- `Pattern Network/patterns/winforms/{doctrine,gate,prohibitions}.yaml`、`template.cs.j2`：加入 `ProcessCmdKey` 單一路徑契約及產碼模板。
- `Pattern Network/harness/PatternNetwork.Gates/CSharpGateInspector.cs`：實作 WinForms 方向輸入 code gate。
- `Pattern Network/harness/PatternNetwork.Gates/PresentationContractValidator.cs`：實作 Presentation Workflow schema／語意驗證。
- `Pattern Network/harness/PatternNetwork.Gates.Tests/CodeGateRunnerTests.cs`、`PresentationContractValidatorTests.cs`：加入正反例 regression tests。
- `Snake.WinForms/GameForm.cs`：正式以 `ProcessCmdKey` 派送四個方向鍵。
- `Snake.Test/GameFormRenderingTests.cs`：新增有焦點 Button 的真實訊息預處理測試。
- `Snake.Test/SnakeGameApplicationServiceTests.cs`：驗證 queued direction 由下一個到期 Tick 消費。
- `misfit.yaml`：記錄方向鍵交付 misfit、review、證據與 resolved 狀態。
- `C:\Users\wbcwb\Desktop\SDD_Coding\snake\v1`、`C:\Users\wbcwb\Desktop\SDD_Coding\snake\v2`：無修改。

## Follow-up Items

- 完成仍為 pending 的 `SNAKE-REVIEW-UI-FLICKER` 實機渲染檢查。
- 提交前分組檢視工作樹；目前仍包含前幾階段大量未提交的 schema、Domain、Application Workflow 與 Gate 變更。
- 若需要對獨立 v1、v2 做百分之百的執行期對照，可分別加入暫時 trace 或執行 UI 自動化；本次結論來自原始碼結構比較，兩個目錄均未修改。
- 網路可用時重新取得 NuGet 弱點資料，以排除 `NU1900` 警告。
