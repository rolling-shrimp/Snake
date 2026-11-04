# Session Record — 2026-11-02 22:05

## Context

- **Project:** Snake
- **Branch:** v2
- **v1 comparison source:** `origin/master`
- **Working directory:** `C:\Users\wbcwb\Desktop\SDD_Coding\snake\Snake`
- **Participants:** Scott Liu
- **Scope:** 載入既有 v1／v2 規格差異紀錄，進一步以實際程式碼與規格追溯比較兩版一次遊戲 tick 的 orchestration、契約粒度及責任歸屬。

## Summary

本次先載入 `SessionRecords/2026-10-25_14-14-compare-v1-v2-specification-gaps.md`，恢復先前結論：v2 的核心風險不是規格檔案不足，而是一次 250ms 遊戲步進被拆成多個局部 use case，卻沒有一份上層 workflow 契約擁有完整順序、資料傳遞、互斥分支與最終結果。

接著實際比對 v1 `origin/master` 與目前 v2 程式碼。v1 的公開 `AdvanceFirstLevelStep()`、`AdvanceSecondLevelStep()`、`AdvanceThirdLevelStep()` 契約都把一次到期步進視為完整狀態轉換，核心邏輯集中在 `SnakeGame.AdvanceOneCell()`：移動、邊界、碰撞、物件效果、護盾／死亡、通關與補物件依固定順序完成。v2 則由 `GamePresenter.OnTickRequested()` 逐一呼叫 `AdvanceSnakeStepUseCase`、各種 contact／collision handler、`EvaluateLevelCompletionUseCase`、`RescueWithShieldUseCase`／`EndCurrentLevelPlayUseCase`、`ReplenishTriggeredObjectUseCase`，最後再更新狀態 read model 並 Render。

最終判定是：差異確實來自契約粒度，但不是 Markdown 對 YAML、Design by Contract 對「五份檔案」或規格數量本身。v1 的契約單位接近完整玩家操作；v2 的每組結構化檔案只完整描述一個局部 frame，並常明確要求「不越權執行其他 Use Case」。由於沒有另立完整 tick orchestration use case，應由 UseCases 層擁有的 application orchestration 實際落到 WinForms Presenter。

## Detailed Q&A Record

### Q1. 載入先前 SessionRecord 後，應承接哪些脈絡？

載入的紀錄是：

`SessionRecords/2026-10-25_14-14-compare-v1-v2-specification-gaps.md`

先前已建立的主要結論如下：

- v1 比較基準採 `origin/master`；v2 採目前工作樹。
- v2 有大量 `Domains` YAML、acceptance scenarios、Pattern Network 與 `misfit.yaml`，問題不是規格數量太少。
- v1 將「一次玩家操作或一次到期遊戲步進」視為較完整的契約；v2 將同一流程拆成許多局部 use case。
- v2 沒有一份上層規格明確擁有完整 tick 的執行順序、資料粒度、互斥條件及最終結果。
- 已辨識的規格缺口包括：
  - 完整遊戲 tick 的 orchestration contract。
  - 尾格碰撞規則。
  - 蛇身拓樸 invariant。
  - 跨 frame acceptance。
  - 動態 UI 更新契約。
  - 實體按鍵映射。
  - 可執行的 semantic Gate。
- 先前工作只進行唯讀比較；若要修改 `Domains/**` 或 `Pattern Network/**`，仍須先列出確切檔案與內容並取得當次明確同意。

### Q2. v2 的實作究竟如何自行拼接 use case？v1 與 v2 的程式碼差在哪裡？

#### 2.1 兩版高階呼叫鏈

```text
v1
Frame Timer
  → SnakeGame.UpdateGameTime
    → AdvanceFirst/Second/ThirdLevelStep
      → AdvanceOneCell
        → 套用方向與邊界政策
        → 判定蛇身／牆／門碰撞
        → 移動與成長
        → 套用物件效果
        → 護盾或死亡
        → 通關判定
        → 補物件
  → 依最終 phase 顯示結果或下一關
  → UpdateLevelStatus
  → Invalidate

v2
Game Timer
  → GamePresenter.OnTickRequested
    → UpdateThirdLevelDoorState
    → AdvanceSnakeStepUseCase
    → ReadGameplay
    → collision handler 或 object contact handler
    → RescueWithShieldUseCase / EndCurrentLevelPlayUseCase
    → EvaluateLevelCompletionUseCase
    → 再次 ReadGameplay
    → EndCurrentLevelPlayUseCase
    → ReplenishTriggeredObjectUseCase
    → RenderCurrentScreen
      → UpdateLevelStatusReadModelUseCase
      → IGameView.Render
```

#### 2.2 v1：完整步進集中在 `SnakeGame.AdvanceOneCell()`

來源：`origin/master:Snake.WinForms/SnakeGame.cs`

三關的公開步進入口只負責檢查關卡、phase 與到期時間，再進入共同的完整步進：

```csharp
public void AdvanceSecondLevelStep()
{
    if (_level != 2
        || _phase != GamePhase.Playing
        || GetElapsedMilliseconds() < _nextMoveAtMilliseconds)
    {
        return;
    }

    _nextMoveAtMilliseconds += MOVE_INTERVAL_MILLISECONDS;
    AdvanceOneCell();

    Verify(
        nameof(AdvanceSecondLevelStep),
        VerifyCurrentBoard(),
        "第二關移動後物件、蛇與棋盤仍符合合法格位規則");
}
```

`AdvanceOneCell()` 在同一條核心流程內知道下一格是否有食物、是否成長、尾巴是否離開，並按順序處理碰撞、物件、通關及補物件：

```csharp
private void AdvanceOneCell()
{
    Point nextHead = GetNextPoint(_snake[0]);

    if (_level < 3)
    {
        nextHead = new Point(
            (nextHead.X + BOARD_SIZE) % BOARD_SIZE,
            (nextHead.Y + BOARD_SIZE) % BOARD_SIZE);
    }
    else if (IsWall(nextHead))
    {
        TriggerDeath(DeathCause.WallCollision);
        return;
    }

    int doorIndex = GetDoorIndex(nextHead);
    if (doorIndex >= 0 && !_doorsOpen[doorIndex])
    {
        TriggerDeath(DeathCause.ClosedDoor);
        return;
    }

    GameObjectType? objectAtNext =
        _objects.TryGetValue(nextHead, out GameObjectType foundObject)
            ? foundObject
            : null;

    bool grows =
        objectAtNext == GameObjectType.Strawberry
        || objectAtNext == GameObjectType.Blueberry;

    int bodyCountToCheck = _snake.Count - (grows ? 0 : 1);
    for (int index = 0; index < bodyCountToCheck; index++)
    {
        if (_snake[index] == nextHead)
        {
            TriggerDeath(DeathCause.BodyCollision);
            return;
        }
    }

    int lengthBeforeEvent = _snakeLength;
    _snake.Insert(0, nextHead);

    if (!grows)
    {
        _snake.RemoveAt(_snake.Count - 1);
    }
    else if (objectAtNext == GameObjectType.Blueberry && !ExtendTail())
    {
        throw new OutputContractViolationException(
            contractMethod,
            "藍莓接觸後蛇身須立即增加 2 格",
            DescribeState());
    }

    if (objectAtNext.HasValue)
    {
        _objects.Remove(nextHead);
        ApplyObject(objectAtNext.Value, lengthBeforeEvent);
    }

    RefreshPendingDoorClosures();

    if (_phase == GamePhase.Playing && _snakeLength >= TargetLength)
    {
        CompleteCurrentLevel();
        return;
    }

    if (_phase == GamePhase.Playing)
    {
        MaintainLevelObjects();
    }
}
```

死亡與護盾分流仍由同一個 `SnakeGame` 核心狀態擁有者處理：

```csharp
private void TriggerDeath(DeathCause cause, int? lengthBeforeEvent = null)
{
    _lastDeathCause = cause;
    _lengthBeforeDeath = lengthBeforeEvent ?? _snakeLength;

    if (_level >= 2 && _shieldCount > 0)
    {
        _phase = GamePhase.DeathPending;
        RescueWithShield(cause);
        return;
    }

    _failedLevel = _level;
    _finalLength = _snakeLength;
    _phase = GamePhase.DeathPending;
    _clock.Stop();
}
```

UI 端的 `GameForm.OnFrameTimerTick()` 主要是在完整遊戲更新之後，依 phase 切換畫面並更新狀態：

```csharp
private void OnFrameTimerTick(object? sender, EventArgs eventArgs)
{
    int priorLevel = _game.Level;

    ExecuteUiOperation(contractMethod, () =>
    {
        _game.UpdateGameTime();

        if (_game.Phase == GamePhase.DeathPending)
        {
            _game.ShowGameOverScreen();
        }
        else if (_game.Phase == GamePhase.SuccessPending)
        {
            _game.ShowThirdLevelSuccessScreen();
        }
        else if (_game.Level == 2 && priorLevel == 1)
        {
            _game.ShowSecondLevelReadyScreen();
        }
        else if (_game.Level == 3 && priorLevel == 2)
        {
            _game.ShowThirdLevelReadyScreen();
        }

        _game.UpdateLevelStatus(_game.Level);
    });
}
```

v1 並不是具有真正 rollback 的交易，但移動到補物件的核心語意集中在一個私有操作，UI 只在外層操作完成後 `Invalidate()`。

#### 2.3 v2：`GamePresenter.OnTickRequested()` 成為實際 orchestration owner

來源：`Snake.WinForms/GamePresenter.cs:541-586`

```csharp
private void OnTickRequested(object? sender, GameTickEventArgs eventArgs)
{
    try
    {
        UpdateThirdLevelDoorState(eventArgs.ElapsedMilliseconds);

        _advanceSnakeStepUseCase.Execute(
            new AdvanceSnakeStepCommand(eventArgs.ElapsedMilliseconds));

        GameplayReadModel snapshot = _session.ReadGameplay();
        if (snapshot.Phase != GameplayPhase.Playing)
        {
            RenderCurrentScreen();
            return;
        }

        if (snapshot.LastMovementCollision is not null)
        {
            HandleMovementCollision(
                snapshot.LastMovementCollision,
                snapshot.CurrentLength);
            RenderCurrentScreen();
            return;
        }

        GameObjectStateDTO? contactedObject = HandleObjectContact(snapshot);

        _evaluateLevelCompletionUseCase.Execute(
            new EvaluateLevelCompletionCommand());

        GameplayReadModel updatedSnapshot = _session.ReadGameplay();
        if (updatedSnapshot.CurrentLength >= updatedSnapshot.TargetLength)
        {
            _endCurrentLevelPlayUseCase.Execute(
                new EndCurrentLevelPlayCommand(
                    LevelOutcome.Passed,
                    null));
        }

        if (contactedObject is not null
            && _session.ReadGameplay().Phase == GameplayPhase.Playing)
        {
            _replenishTriggeredObjectUseCase.Execute(
                new ReplenishTriggeredObjectCommand(
                    contactedObject.Type,
                    contactedObject.ItemId));
        }

        RenderCurrentScreen();
    }
    catch (Exception exception)
    {
        _view.ShowFailure($"更新遊戲失敗：{exception.Message}");
    }
}
```

`HandleObjectContact()` 由 Presenter 決定物件種類對應哪個 use case，以及炸彈／致命火焰何時進入護盾或死亡流程：

```csharp
switch (contactedObject.Type)
{
    case GameObjectType.Strawberry:
        _handleOrdinaryFoodContactUseCase.Execute(...);
        break;
    case GameObjectType.SpecialFood:
        _handleSpecialFoodContactUseCase.Execute(...);
        break;
    case GameObjectType.Shield:
        _handleShieldContactUseCase.Execute(...);
        break;
    case GameObjectType.Fire:
        _handleFireContactUseCase.Execute(...);
        break;
    case GameObjectType.Bomb:
        _handleBombContactUseCase.Execute(...);
        break;
}

if (contactedObject.Type == GameObjectType.Bomb)
{
    RescueOrEnd(DeathCause.Bomb, preHitLength);
}
else if (contactedObject.Type == GameObjectType.Fire
    && preHitLength <= 2)
{
    RescueOrEnd(DeathCause.FatalFire, preHitLength);
}
```

Presenter 因此實際決定：

- 門狀態更新與蛇移動的先後順序。
- 碰撞優先於物件接觸。
- 碰撞分支是否立即結束本次 tick。
- 物件種類對應哪個 contact handler。
- 炸彈與致命火焰的死亡原因。
- 何時執行護盾保命或結束關卡。
- 何時執行通關判定。
- 通關後何時執行 `EndCurrentLevelPlayUseCase`。
- 關卡仍為 Playing 時才補物件。
- 最後應 Render 哪個畫面。

#### 2.4 各階段責任比較

| 階段 | v1 | v2 |
| --- | --- | --- |
| 移動與邊界 | `SnakeGame.AdvanceOneCell()` | `AdvanceSnakeStepUseCase` |
| 蛇身／牆／門碰撞 | `AdvanceOneCell()` 直接判斷 | Advance 留下 `LastMovementCollision`，Presenter 再呼叫碰撞 handler |
| 食物與物件效果 | `ApplyObject()` | Presenter switch 到各 Contact UseCase |
| 護盾／死亡 | `TriggerDeath()` | Presenter `RescueOrEnd()`，再由 `RescueWithShieldUseCase` 分流 |
| 通關 | `AdvanceOneCell()` 直接檢查並完成 | Presenter 呼叫 Evaluate，再自行比較長度並呼叫 End |
| 補物件 | `AdvanceOneCell()` 在確認仍為 Playing 後呼叫 | Presenter 再次讀取 phase 後呼叫 Replenish |
| 狀態更新 | GameForm 在步進完成後更新 | `RenderPlaying()` 每次產生新的事件 ID 與版本後更新 Read Model |
| Render | `ExecuteUiOperation()` 完成後 `Invalidate()` | Presenter 最後呼叫 `RenderCurrentScreen()` |

#### 2.5 具體行為差異一：尾格碰撞

v1 在移動前就知道下一格是否有食物：

```csharp
bool grows =
    objectAtNext == GameObjectType.Strawberry
    || objectAtNext == GameObjectType.Blueberry;

int bodyCountToCheck = _snake.Count - (grows ? 0 : 1);
```

因此：

- 未成長時，尾巴會在同一步離開，自撞檢查排除尾格。
- 成長時，尾巴保留，全部蛇身仍需納入碰撞判定。

v2 `AdvanceSnakeStepUseCase` 在物件接觸 use case 之前執行，直接用完整現有蛇身判斷：

```csharp
if (_session.Snake.BodyCells.Contains(movement.Position))
{
    _session.RecordMovementFact(
        new SelfCollisionDomainEvent(movement.Position));
    return Succeed();
}
```

此時 Advance use case 沒有整個 tick 的物件效果語意，因此無法區分「尾巴本次將離開」與「成長時尾巴保留」。

#### 2.6 具體行為差異二：蛇身成長拓樸

v1 普通食物成長時保留原尾巴；藍莓額外增加的一格由 `ExtendTail()` 從尾端相鄰格尋找合法位置，因此實作至少試圖維持尾端延伸。

v2 的 `Snake.MoveTo()` 一律先移除尾巴：

```csharp
public void MoveTo(GridPosition nextHead)
{
    _bodyCells.Insert(0, nextHead);
    _bodyCells.RemoveAt(_bodyCells.Count - 1);
    _currentDirection = NextDirection;
    _queuedDirection = null;
}
```

接著 `HandleOrdinaryFoodContactUseCase`／`HandleSpecialFoodContactUseCase` 才從整張棋盤的合法空格取得新增蛇身：

```csharp
List<GridPosition> legalCells = _session.Board.GetLegalSpawnCells(
    _session.Snake.BodyCells,
    _session.GetObjectOccupiedCells());

_session.Snake.Grow(legalCells.Take(lengthDelta).ToList());
```

`Board.GetLegalSpawnCells()` 從 `(0,0)` 開始逐列掃描所有未占用合法格，而不是從蛇尾搜尋相鄰位置。因此新增格位可能離蛇尾很遠。

目前 `Domains/Snake/domain_spec.yaml` 只規定：

- `SNAKE-INV-001`：蛇長等於 `body_cells` 數量。
- `SNAKE-INV-002`：每個蛇身格位是 Board 合法座標。
- `SNAKE-LENGTH-001`：正的 delta 增加對應蛇身格數。

它沒有規定相鄰 body cell 必須連續，也沒有規定成長只能延伸自尾端。因此「遠端合法格」仍可通過局部 invariant 與 acceptance。

#### 2.7 具體行為差異三：狀態投影事件由 Presenter 人工補足

`update-level-status-read-model` 規格要求來源是已提交且可排序的 Domain Event；目前 `GamePresenter.RenderPlaying()` 每次 Render 都自行產生：

```csharp
_statusVersion++;
_updateLevelStatusUseCase.Execute(
    new UpdateLevelStatusReadModelCommand(
        Guid.NewGuid().ToString(),
        _statusVersion));
```

這不是從既有 Domain Event 取得真實 event ID/version，而是 Presenter 為了符合投影介面自行製造識別與版本，也是外層自行補接規格碎片的例子。

#### 2.8 v2 正常路徑並非每個 use case 都立即 Render

需要避免誤解：目前 `OnTickRequested()` 正常路徑通常只在最後呼叫一次 `RenderCurrentScreen()`，碰撞分支也在處理死亡／護盾後才 Render。因此問題不是畫面必然逐段顯示每個中間狀態。

真正缺少的是涵蓋多個 `Execute()` 的完整 tick result、原子提交或 rollback。若前段已移動／吃食物，後段 Evaluate、End、Replenish 或 Render 前失敗，先前對共享 `SnakeGameSession` 的修改仍然存在；catch 只會呼叫 `ShowFailure()` 並停止 Timer，沒有回復整個 tick。

### Q3. 差異是否就是 v1 Design by Contract 與 v2 多份結構化規格檔案造成的？

答案是「契約粒度造成了差異，但不是檔案格式或檔案數量本身」。

#### 3.1 v1 規格的契約單位

`origin/master:design_by_contract.md` 明確定義：

- 第 7 行：以一個原子玩家操作或明確畫面狀態作為一項 use case。
- UC-04 `AdvanceFirstLevelStep()`：一次步進包含移動、跨界、普通食物、自撞、通關、停止與補物件條件。
- UC-07 `AdvanceSecondLevelStep()`：一次步進包含移動、所有物件效果、死亡、護盾分流、通關與後續狀態。
- UC-10 `AdvanceThirdLevelStep()`：一次步進另包含牆、門與炸彈。
- 第 240–249 行另外列出合約間的呼叫流程及失敗時不得繼續相依合約的原則。

`origin/master:test_spec.md` 也以 T-04、T-07、T-10 驗收完整步進，並在「玩家流程整合測試」驗證方向、步進、結果畫面、狀態更新與補物件的串接。

v1 實作者接到的核心要求接近：

> 實作一次完整到期步進，完成後必須得到完整且一致的本關狀態。

#### 3.2 v2 每個 use case 目錄實際有四份本地規格

例如 `Domains/Game/rbf/advance-snake-step/` 包含：

1. `frame.yaml`：需求、trigger、required condition、required behavior 與 frame concern。
2. `domain_spec.yaml`：input/output、precondition、invariant、process、postcondition、no-op，以及 Aggregate rule references。
3. `technical_spec.yaml`：Command、Result、欄位與 domain-to-technical mapping。
4. `acceptance.yaml`：scenario、given/when/then 與 traceability。

`domain_spec.yaml` 另引用 `Domains/Game/domain_spec.yaml`、`Domains/Snake/domain_spec.yaml`、`Domains/Board/domain_spec.yaml` 等 Aggregate 規格；引用數量依 use case 而變，不是固定第五份。`misfit.yaml` 則集中索引 acceptance scenarios，presentation YAML 另外描述畫面狀態與 transition。

#### 3.3 四份檔案都在深化同一個局部 frame

`advance-snake-step/domain_spec.yaml` 的 process 是：

```yaml
process:
  - action: "確認觸發事實與 Playing／movement_active。"
  - action: "每個有效 interval 只前進一格並產生接觸或碰撞事實。"
  - action: "發布後續流程需要的結果事實，但不越權執行其他 Use Case。"
```

普通／特殊食物、碰撞與通關判定的 RBF 規格也採相同結構：完成自己的局部效果，發布後續所需事實，但不執行其他 use case。

RIF 規格則各自保證單一 reaction 的跨 Domain 行為，例如：

- `rescue-with-shield`：死亡事件已有護盾時，消耗護盾並安全恢復。
- `end-current-level-play`：收到 LevelPassed 或無法保命死亡事件後停止遊玩並記錄結果。
- `replenish-triggered-object`：物件效果、護盾、通關／死亡判定完成後，仍為 Playing 才補物件。

但沒有另一個規格負責說明：

- 哪個應用層入口擁有一次完整 tick。
- Advance 的接觸／碰撞事實如何可靠觸發下一步。
- 多種事實同時存在時的優先順序與互斥條件。
- Evaluate 發布 LevelPassed 後由誰呼叫 End。
- Rescue 與 End 如何確保只走一條分支。
- Replenish 如何證明前置的護盾／通關／死亡判定已全部完成。
- Read Model 與 Render 何時才可看見整個 tick 的最終狀態。
- 任一步失敗時是否回復整個 tick。

#### 3.4 acceptance 文字與實際測試仍存在追溯落差

`handle-ordinary-food-contact/acceptance.yaml` 的成功情境寫道：

> 食物消失，蛇長精確增加 1，後續通關判定與補充流程各觸發一次。

但 `Snake.Test/HandleOrdinaryFoodContactAcceptanceTests.cs` 對應測試只驗證：

- `Result.IsSuccess == true`。
- 食物已消失。
- 蛇長由 3 變為 4。
- 發布一個 `FoodConsumedDomainEvent`。
- 發布一個 `SnakeLengthChangedDomainEvent`。

它沒有驗證 `EvaluateLevelCompletionUseCase` 與 `ReplenishTriggeredObjectUseCase` 各執行一次。Presenter integration tests 確實覆蓋部分完整流程，但這些測試沒有對應一份完整 tick acceptance contract，而是在驗證目前 Presenter 已選定的拼接方式。

#### 3.5 Pattern Network 的責任規則與目前程式碼落點

`Pattern Network/patterns/use-cases/doctrine.yaml` 將 `application_orchestration` 列為 UseCases 責任。

`Pattern Network/patterns/winforms/doctrine.yaml` 則規定：

- WinForms 負責 Form、View、Presenter、ViewModel、輸入事件映射與 Composition Root。
- WinForms 排除 `application_business_decision`。
- Presenter 應呼叫 Use Case、處理 Result 並轉換為 ViewModel。

目前 `GamePresenter.OnTickRequested()` 實際進行碰撞優先序、物件分流、致死判定、通關判定、補充時機等 application orchestration／business decision。這表示完整 tick use case 缺席後，其責任落到了外層 Presenter。

相關規則多為 `manual` enforcement；Gate 通過只證明結構檢查沒有違規，不能證明人工語意責任真的經過審查。

## Key Clarifications

- v2 每個 use case 目錄本地是四份結構化規格，不是固定五份；它們會引用數量不定的 Aggregate domain specs。
- 問題不在 Markdown 或 YAML，也不在文件數量，而在完整 tick 是否有明確契約擁有者。
- v1 也不是具有 rollback 的真正原子交易；它的優勢是完整核心步進語意集中且契約邊界接近玩家操作。
- v2 各局部 use case 的輸入輸出可以個別正確，但跨 use case 的順序、互斥、失敗處理與最終公開狀態仍可能未受規範。
- v2 正常流程通常只在 tick 最後 Render 一次，不是每個 use case 都立即 Render；缺口是完整 tick 沒有原子 Result 或 rollback 保證。
- v2 `GamePresenter` 是目前實際的拼接位置，並承擔原本 Pattern Network 宣告應由 UseCases 層擁有的 application orchestration。
- v2 蛇身遠端成長不是因為長度計算錯誤，而是 `Grow()` 只要求合法不重疊格位，Aggregate invariant 與 acceptance 沒有要求尾端連續拓樸。
- `update-level-status-read-model` 規格要求已提交 Domain Event，但目前 Presenter 每次 Render 自行建立 GUID 與遞增版本，屬於實作自行補接事件來源。

## Decisions Made

- v1 程式碼與規格比較基準維持 `origin/master`；v2 使用目前 `v2` 工作樹。
- 將兩版主要差異定義為「完整步進契約」與「局部 frame 契約」的粒度差異，而不是規格格式或數量差異。
- 將 `GamePresenter.OnTickRequested()` 判定為目前 v2 的實際 tick orchestration owner。
- 將缺少完整 tick workflow/use case 視為導致 Presenter 自行拼裝、尾格語意斷裂、成長拓樸缺口及事件來源人工補接的共同設計原因。
- 本次維持唯讀分析；不修改 `Domains/**`、`Pattern Network/**`、應用程式或測試。

## Code Changes

- 新增 `SessionRecords/2026-11-02_22-05-compare-v1-v2-tick-orchestration.md`，記錄本次完整 Q&A、程式碼比較、規格追溯與最終判定。
- 未修改應用程式碼、測試、`Domains/**`、`Pattern Network/**`、`misfit.yaml` 或其他既有規格。

## Verification Evidence

- 以 `git show origin/master:<path>` 讀取 v1 `Snake.WinForms/SnakeGame.cs`、`Snake.WinForms/GameForm.cs`、`design_by_contract.md`、`test_spec.md` 與 `use_case.md`。
- 直接讀取目前 v2 `Snake.WinForms/GamePresenter.cs`、`Snake.WinForms/GameForm.cs`、相關 `Snake.UseCases`、`Snake.Entities/Snake.cs`、`Snake.Entities/Board.cs` 與 acceptance tests。
- 直接讀取 v2 `Domains/Game/**` frame/domain/technical/acceptance 規格、`Domains/Snake/domain_spec.yaml`、presentation YAML、`misfit.yaml` 與 Pattern Network doctrine。
- `.understand-anything/knowledge-graph.json` 不存在，因此沒有使用既有 knowledge graph；本次呼叫鏈由實際原始碼、Git tree 與規格引用重建。
- 本次為唯讀分析與文件建立，未執行應用程式測試。

## Follow-up Items

- 若要修正規格，先定義一份完整 tick workflow/orchestration contract，明確指定 input grain、output grain、contract owner、固定順序、互斥分支及原子公開結果。
- 決定該 workflow 應是新的 UseCases 層入口，或由既有局部 use cases 改為其內部步驟／事件 reaction；避免繼續由 Presenter 擁有商業流程順序。
- 補齊尾格碰撞規則：未成長時可進入即將離開的尾格，成長時尾巴保留並納入碰撞。
- 補齊蛇身拓樸 invariant：`body_cells` 必須由頭到尾有序且連續，成長只能從尾端合法延伸。
- 補一份跨 frame acceptance，覆蓋移動、物件效果、護盾／死亡、通關、補物件、read model 與 Render 的完整 tick。
- 驗證 acceptance 所稱「後續流程各觸發一次」是否有對應可執行測試，而不是只驗證局部 Domain Event 數量。
- 將 Pattern Network 中 UseCases orchestration 與 WinForms 禁止 business decision 的 manual rules 落實成人工審查紀錄或 semantic checker。
- 任何 `Domains/**` 或 `Pattern Network/**` 變更前，依專案規則列出確切檔案、修改內容及影響，並取得使用者針對當次變更的明確同意。
