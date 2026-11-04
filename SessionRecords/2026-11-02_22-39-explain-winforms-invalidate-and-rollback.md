# Session Record — 2026-11-02 22:39

## Context

- **Project:** Snake
- **Branch:** v2
- **v1 reference:** `origin/master`
- **Working directory:** `C:\Users\wbcwb\Desktop\SDD_Coding\snake\Snake`
- **Participants:** Scott Liu
- **Topic:** 解釋 v1 一次遊戲步進的集中處理、rollback 限制、UI 外層與 WinForms `Invalidate()`。

## Summary

本次針對以下說法進行獨立釐清：

> v1 並不是真正具有 rollback 的資料庫交易，但「移動到補物件」的語意順序集中於一個私有操作，畫面也只在外層操作完成後 `Invalidate()`。

核心結論是：這句話在區分「正常流程的程式碼順序集中」與「失敗時可整批回復的交易原子性」。v1 的 `SnakeGame.AdvanceOneCell()` 集中處理一次移動造成的碰撞、物件效果、護盾／死亡、通關與補物件；但如果處理到一半發生例外，程式沒有快照、transaction 或 rollback 可以撤銷前面已完成的狀態修改。

句中的「外層」是包住 `SnakeGame` 遊戲邏輯的 WinForms UI 事件處理，也就是 `GameForm.OnFrameTimerTick()` 與 `ExecuteUiOperation()`。`Invalidate()` 不會修改遊戲資料，也不是同步立刻完成繪圖；它只是將 Form 標記為需要重畫，讓 Windows 訊息迴圈稍後觸發 `OnPaint()`，依當時的遊戲狀態重新繪製畫面。

## Detailed Q&A Record

### Q1. 「v1 並不是真正具有 rollback 的資料庫交易」是什麼意思？

這是在說：v1 雖然把一次遊戲步進的主要處理順序放在同一個方法中，但沒有真正的交易或狀態回復機制。

假設一次步進依序發生：

```text
蛇已移動
→ 食物已刪除
→ 蛇長已增加
→ 補物件時發生例外
```

如果最後一步失敗，v1 沒有以下機制：

```csharp
try
{
    BeginTransaction();
    AdvanceOneCell();
    Commit();
}
catch
{
    Rollback();
}
```

也沒有先保存完整遊戲狀態快照，再於失敗時恢復。因此可能出現：

- 蛇已經移動。
- 舊食物已經消失。
- 蛇長已經增加。
- 新食物卻還沒有補上。

前面的修改不會因後面的例外自動撤銷。

因此 v1 具有的是：

> 正常成功路徑的語意順序集中，容易看見一次步進的完整處理流程。

而不是：

> 任一步驟失敗時，整個步進的狀態都能自動恢復。

這裡以「資料庫交易」作類比，是為了說明 rollback／atomicity 的差異；Snake v1 的遊戲步進本身不是資料庫操作。

### Q2. 「移動到補物件」是什麼？

「移動到補物件」不是指把某一個物件從 A 點搬到 B 點，而是指一次蛇移動所引發的整段業務流程範圍：

```text
套用已排隊的方向
→ 計算下一個蛇頭位置
→ 套用邊界政策
→ 判斷牆壁、門或蛇身碰撞
→ 移動蛇頭與尾巴
→ 處理接觸到的食物／護盾／火焰／炸彈
→ 判斷護盾保命或死亡
→ 判斷是否通關
→ 若仍在遊玩，補回被消耗或觸發的物件
```

v1 這段流程主要集中在 `origin/master:Snake.WinForms/SnakeGame.cs` 的私有方法 `AdvanceOneCell()`：

```csharp
private void AdvanceOneCell()
{
    // 計算下一個蛇頭位置。
    // 判斷牆、門與蛇身碰撞。
    // 移動蛇。
    // ApplyObject：套用食物、護盾、火焰或炸彈效果。
    // 判斷死亡／保命與是否通關。

    if (_phase == GamePhase.Playing)
    {
        MaintainLevelObjects();
    }
}
```

所以更精確的說法是：

> 從蛇開始處理本次移動，到這次移動造成的碰撞、物件效果、死亡／通關判定與物件補充全部結束。

### Q3. 「畫面只在外層操作完成後」的「外層」是什麼？

「外層」是 WinForms UI 層，也就是包住 `SnakeGame` 遊戲邏輯呼叫的 `GameForm`。

大致呼叫結構如下：

```text
GameForm.OnFrameTimerTick
  → GameForm.ExecuteUiOperation
    → SnakeGame.UpdateGameTime
      → SnakeGame.AdvanceFirst/Second/ThirdLevelStep
        → SnakeGame.AdvanceOneCell
    → 依最後 phase 顯示結果或下一關畫面
    → SnakeGame.UpdateLevelStatus
    → VerifyVisibleContract
    → Invalidate
```

責任可以分成：

| 層次 | 主要責任 |
| --- | --- |
| 內層 `SnakeGame` | 修改蛇、物件、關卡、護盾、死亡與通關等遊戲狀態。 |
| 外層 `GameForm` | 接收 Timer／鍵盤／滑鼠事件、呼叫遊戲邏輯、處理 UI 錯誤並要求 WinForms 重畫。 |

v1 `origin/master:Snake.WinForms/GameForm.cs` 的正常成功路徑可簡化為：

```csharp
private void OnFrameTimerTick(object? sender, EventArgs eventArgs)
{
    ExecuteUiOperation(contractMethod, () =>
    {
        _game.UpdateGameTime();
        HandleResultOrNextLevelScreen();
        _game.UpdateLevelStatus(_game.Level);
    });
}

private void ExecuteUiOperation(
    string contractMethod,
    Action operation)
{
    operation();
    VerifyVisibleContract();
    Invalidate();
}
```

這裡的「外層操作完成」是指傳入 `ExecuteUiOperation()` 的 `operation` 已完成遊戲更新、phase 分流與狀態更新，接著才要求重畫。

需要保留一個例外：這只是在描述正常成功路徑。v1 的錯誤處理也可能呼叫 `Invalidate()`，讓畫面顯示錯誤狀態。因此「只在外層操作完成後」不是所有程式分支的絕對規則。

### Q4. WinForms `Invalidate()` 在做什麼？

`Invalidate()` 是 WinForms 的重畫請求。它的意思不是：

> 立即同步把整個畫面繪製完成。

而是：

> 將控制項或 Form 的區域標記為無效，表示這個區域需要重新繪製。

Windows 訊息迴圈之後會安排 Paint 訊息，最後呼叫 `OnPaint()`：

```csharp
protected override void OnPaint(PaintEventArgs eventArgs)
{
    base.OnPaint(eventArgs);
    DrawScene(eventArgs.Graphics);
}
```

正常流程可理解為：

```text
修改遊戲狀態
→ 呼叫 Invalidate()
→ Form 被標記為需要重畫
→ 目前事件處理結束
→ Windows 稍後觸發 OnPaint()
→ OnPaint 讀取當時的遊戲狀態並繪製
```

`Invalidate()` 本身：

- 不會移動蛇。
- 不會修改蛇長、物件或關卡資料。
- 不會執行死亡或通關判定。
- 不保證呼叫當下同步完成重畫。
- 只通知 WinForms 某個畫面區域需要重新繪製。

Windows 也可能合併短時間內多次 `Invalidate()`，以一次 Paint 處理，減少重複繪圖。

### Q5. 為什麼先完成操作再 `Invalidate()`，比較不容易顯示中間狀態？

正常成功路徑是：

```text
蛇移動
→ 處理食物效果
→ 改變蛇長
→ 判斷死亡或通關
→ 補物件
→ 完成狀態更新
→ Invalidate
→ OnPaint 讀取完成後的狀態
```

而不是：

```text
蛇移動 → 重畫一次
吃食物 → 重畫一次
增加蛇長 → 重畫一次
補食物 → 再重畫一次
```

因此正常情況下，`OnPaint()` 較可能讀到本次操作完成後的狀態，而不是每個小步驟的中間狀態。

但這仍不是交易保證。若 `AdvanceOneCell()` 中途發生例外：

- 先前的狀態修改可能已經存在。
- 錯誤處理可能另外要求重畫。
- 畫面仍可能顯示部分修改後的失敗狀態。

所以「最後才 `Invalidate()`」只能降低正常流程暴露中間狀態的機會，不能取代 rollback。

### Q6. 原句應如何改寫得更精確？

建議改成：

> v1 將一次移動所引發的碰撞、物件效果、死亡／通關判定與物件補充集中在 `AdvanceOneCell()` 的正常流程中；UI 層通常等這段操作完成後才呼叫 `Invalidate()`，讓 WinForms 稍後依當時的遊戲狀態重新繪製。這降低了正常流程顯示中間狀態的機會，但不提供失敗時的狀態 rollback。

這個版本明確區分：

- 「集中於一個方法」是程式碼與責任配置。
- 「通常最後才重畫」是正常 UI 更新時機。
- 「沒有 rollback」是失敗時的狀態一致性限制。

## Key Clarifications

- 「移動到補物件」是一次蛇移動引發的完整處理範圍，不是搬移某個遊戲物件。
- 「外層」是 `GameForm` 的 WinForms UI 事件處理，相對於內層 `SnakeGame` 遊戲邏輯。
- `Invalidate()` 是標記需要重畫，不是同步立即完成繪製，也不會修改遊戲資料。
- `OnPaint()` 通常在目前事件處理結束後由 Windows 訊息迴圈觸發。
- 正常流程最後才 `Invalidate()`，只會降低中間狀態被畫出的機會，不會提供 transaction、snapshot 或 rollback。
- v1 的「集中」代表順序容易追蹤，不代表整段操作具有失敗時的原子回復能力。
- 「只在外層操作完成後」應限縮為正常成功路徑；錯誤處理也可能呼叫 `Invalidate()`。

## Decisions Made

- 將原句的「移動到補物件」重新表述為「一次移動所引發的碰撞、物件效果、死亡／通關判定與物件補充」。
- 將「只有在外層操作完成後」修正為「UI 層通常在正常操作完成後才呼叫 `Invalidate()`」，避免形成所有分支皆如此的絕對敘述。
- 明確區分程式碼順序集中、UI 重畫時機與 rollback／atomicity，避免把三者視為同一件事。

## Code Changes

- 新增 `SessionRecords/2026-11-02_22-39-explain-winforms-invalidate-and-rollback.md`，獨立記錄本次教學型 Q&A。
- 未修改應用程式碼、測試、`Domains/**`、`Pattern Network/**` 或既有 SessionRecord。

## Follow-up Items

- None.
