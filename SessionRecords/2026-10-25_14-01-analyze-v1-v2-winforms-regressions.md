# Session Record — 2026-10-25 14:01

## Context
- **Project:** Snake
- **Branch:** v2
- **Participants:** Scott Liu

## Summary

本次比較 v1 與 v2 的 WinForms 執行差異，針對蛇身碰撞誤判、關卡進度與規則區閃爍、向右移動時左右格同時改變，以及 WASD 鍵盤映射四項現象進行唯讀診斷。

Git 證據顯示，專案沒有名為 `v1` 的 branch；舊版是 checkout 在 `C:\Users\wbcwb\Desktop\SDD_Coding\snake\v1` worktree 的 `master`。`master` 與 `v2` HEAD 中的 `Snake.WinForms/GameForm.cs`、`Snake.WinForms/SnakeGame.cs` blob 完全相同，因此問題不是已提交的 v2 branch commit 所造成，而是目前 v2 工作目錄中尚未提交的 Entities／UseCases／Presenter／ViewModel／Control 重構。

目前 v2 測試結果為 114/114 通過，但既有測試沒有覆蓋移入即將離開的尾格、蛇身成長後的連續性、重複 Render 的閃爍，以及實際鍵盤映射，因此無法阻止這些執行期回歸。

## Detailed Q&A Record

### Q1. 為什麼 v1 運行正常，而目前 v2 會在蛇頭沒有撞到蛇身時判定 Game Over？

v1 在每一步移動前會先判斷蛇頭下一格是否有食物。沒有成長時，尾巴會在同一步離開，所以自撞檢查刻意排除最後一個尾巴格：

```csharp
bool grows = objectAtNext == GameObjectType.Strawberry
    || objectAtNext == GameObjectType.Blueberry;
int bodyCountToCheck = _snake.Count - (grows ? 0 : 1);
```

位置：`C:\Users\wbcwb\Desktop\SDD_Coding\snake\v1\Snake.WinForms\SnakeGame.cs:664-666`。

目前 v2 的 `Snake.UseCases/AdvanceSnakeStepUseCase.cs:39` 則直接執行：

```csharp
if (_session.Snake.BodyCells.Contains(movement.Position))
```

這會把本次移動後即將離開的尾巴也當成碰撞目標。但 `Snake.Entities/Snake.cs:55-63` 的 `MoveTo` 本身仍採「前端插入新頭、後端移除尾巴」，因此出現語意矛盾：移動規則會移除尾巴，碰撞規則卻不允許蛇頭進入該尾巴格。

最小情境是 U 形蛇身：頭 `(2,2)`、身體 `(2,3)`、`(1,3)`、尾巴 `(1,2)`。蛇向左轉時，下一格 `(1,2)` 是即將離開的尾格；v1 允許移動，目前 v2 則產生 `SelfCollisionDomainEvent` 並進入失敗流程。

另外，v2 的食物成長流程有獨立但相關的資料語意問題：

- `HandleOrdinaryFoodContactUseCase.cs:45-53`
- `HandleSpecialFoodContactUseCase.cs:36-44`

兩者使用 `Board.GetLegalSpawnCells(...).Take(lengthDelta)` 取得整個棋盤上的任意合法空格，再傳給 `Snake.Grow` 追加到蛇身尾端。這只能保證格位不重複，不能保證新增格位與尾巴相鄰，可能形成不連續的蛇身，進一步造成畫面與碰撞看起來不合理。

受保護規格 `Domains/Snake/domain_spec.yaml:144-147` 目前也將規則寫成「下一個蛇頭格位存在於目前全部 `body_cells` 就產生 SelfCollision」，沒有描述非成長移動時尾巴會同時離開。若要正式恢復 v1 語意，需要先決定並取得明確同意後才能修改該受保護規格。

### Q2. 為什麼關卡進度與遊戲規則區會持續閃爍？

目前 v2 每 250 毫秒執行一次完整畫面 Render：

1. `GamePresenter.OnTickRequested` 推進遊戲。
2. `GamePresenter.RenderCurrentScreen` 建立新的完整 `GameViewModel`。
3. `GameForm.Render` 重新套用棋盤、狀態、規則、結果、按鈕與計時器。

每次 Render 都會執行下列操作：

- `GameForm.cs:88-92`：停止並重新啟動遊戲 Timer。
- `GameProgressControl.cs:18-23`：即使進度沒有改變，仍呼叫 `Invalidate()`。
- `GameForm.cs:283-309`：先執行 `_rulesList.Controls.Clear()`，再重新建立全部規則 Label。
- `GameForm.cs:95-100`：重新設定側欄控制項尺寸並執行 `ResumeLayout(true)`。

目前只有棋盤與進度自訂 Control 設定 `DoubleBuffered = true`；Form、FlowLayoutPanel、規則區與卡片 Panel 沒有統一的雙緩衝繪製。因而使用者會看見規則控制項被移除、重建與重新排版的過程。

v1 的 `GameForm` 在 `GameForm.cs:46` 對整個 Form 啟用 `DoubleBuffered = true`，並在單一 `OnPaint` 中一次繪製棋盤與側欄。即使畫面被重新繪製，中間狀態也由整體緩衝遮蔽，所以不容易閃爍。

### Q3. 為什麼按下 D 向右時，蛇頭右邊及左邊的格子都會改變？

若改變的是新蛇頭左側的舊蛇頭格，這是正常的蛇身狀態轉換：

1. 右側空格成為新蛇頭。
2. 原蛇頭格變成普通蛇身，臉部、邊距或外觀會改變。
3. 最後一個尾巴格離開蛇身並恢復棋盤底色。

`GameBoardControl.Render` 每次都呼叫 `Invalidate()`，接著 `OnPaint` 先重畫 100 格棋盤，再重畫整條蛇，因此頭、舊頭與尾巴的變化會同時出現。若使用者看到的是與頭、身、尾都無關的空格閃動，則屬於上一題的全畫面重新排版／重畫問題；若發生在吃食物後，也可能是任意合法空格被錯誤加入蛇身的成長問題。

### Q4. v1 與 v2 的方向按鍵有何差異？

v1 的 `GameForm.OnGameKeyDown` 只映射四個方向鍵：`Keys.Up`、`Keys.Down`、`Keys.Left`、`Keys.Right`。

目前 v2 的 `Snake.WinForms/GameForm.cs:487-493` 則明確支援兩組輸入：

```csharp
Keys.Up or Keys.W => Direction.Up,
Keys.Down or Keys.S => Direction.Down,
Keys.Left or Keys.A => Direction.Left,
Keys.Right or Keys.D => Direction.Right,
```

所以 D 會向右是目前程式刻意加入的映射，不是方向判斷自行發生錯誤。不過 `use_case.md:24` 只寫「使用鍵盤方向鍵控制上、下、左、右」，沒有明確定義 WASD。若需求是 WASD，v2 已支援 WASD 及方向鍵；若需求是只允許方向鍵，這就是未被規格授權的行為差異。

### Q5. 為什麼 114 個測試全部通過，執行時仍有上述問題？

測試覆蓋的是靜態結果或一般路徑，沒有覆蓋回歸情境：

- `AdvanceSnakeStepAcceptanceTests.Execute_DoesNotCommitSelfCollisionMovement` 只測蛇頭撞到非尾巴的身體，沒有測移入即將離開的尾格。
- `HandleOrdinaryFoodContactAcceptanceTests` 與 `HandleSpecialFoodContactAcceptanceTests` 只驗證蛇長變成 4 或 5，沒有驗證蛇身格位相鄰或拓撲連續。
- `GamePresenterTests.TickRequested_MovesSnakeOneCellAndRendersUpdatedBoard` 只驗證新蛇頭座標，沒有驗證舊頭與尾巴的完整轉換。
- `GameFormRenderingTests` 只做單次 `DrawToBitmap` 靜態驗證，無法觀察連續 Render 閃爍。
- 沒有測試 WinForms 的 WASD／方向鍵實際映射。

執行命令：

```powershell
dotnet test .\Snake.Test\Snake.Test.csproj --no-restore --nologo --verbosity minimal
```

結果：114 通過、0 失敗、0 略過。

## Key Clarifications

- Git 中沒有 `v1` branch；使用者所稱 v1 是 `master` branch 的相鄰 worktree。
- `master` 與 `v2` HEAD 沒有 WinForms 程式差異；目前問題存在於 v2 worktree 尚未提交的重構。
- 尾巴格是否應算自撞不能只看 `Contains`；必須同時考慮本次移動是否成長及尾巴是否離開。
- 向右移動時舊蛇頭格與尾巴格改變通常是正常行為；無關空格閃動或吃食物後不連續格位則是不同問題。
- v2 支援方向鍵與 WASD；v1 只支援方向鍵。

## Decisions Made

- 本次只進行唯讀診斷，不修改程式碼或受保護規格。
- 比較基準採 v1/master 完整版本對目前 v2 工作樹，因為實際執行問題來自尚未提交的重構，而不是 v2 HEAD。
- 將尾巴碰撞判斷、蛇身成長拓撲、UI 重複 Render 與鍵盤映射視為四個應分開驗證的契約問題。

## Code Changes

- 新增本 SessionRecord；沒有修改應用程式、測試或受保護規格。

## Follow-up Items

- 確認非成長移動時是否正式採用 v1 的「可進入即將離開尾格」規則；若需要同步 `Domains/Snake/domain_spec.yaml`，先取得使用者針對該受保護檔案的明確同意。
- 修正自撞判斷，並新增「移入尾格合法、成長時移入尾格仍碰撞」的回歸測試。
- 修正普通與特殊食物成長方式，確保新增蛇身格位與尾端保持連續，並新增拓撲測試。
- 將遊戲 tick 的棋盤更新與靜態側欄更新分離；只有狀態真正改變時才更新進度及規則，避免每 250 毫秒清空控制項。
- 補上連續 Render、進度不變時不重畫規則，以及 WinForms 鍵盤映射測試。
- 明確決定控制契約是只使用方向鍵、只使用 WASD，或兩者都支援，並在非受保護／受保護規格的授權範圍內同步。
