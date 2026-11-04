# Session Record — 2026-11-03 22:55

## Context

- **Project:** Snake
- **Branch:** v2
- **v1 comparison source:** `origin/master`
- **Working directory:** `C:\Users\wbcwb\Desktop\SDD_Coding\snake\Snake`
- **Participants:** Scott Liu
- **Scope:** 釐清 v2「自行拼接 use case」的真正含義、目前 Presenter orchestration 為何看似合理但仍有風險，以及尾格自撞誤判與規格缺口的關係。

## Summary

今天的討論首先修正了「自行拼接」的理解：問題不是 use case 物件是否由外部注入，也不是 v2 依賴 `GameSession` 而 v1 不依賴狀態；真正差異是完整遊戲流程的決策責任放在哪裡。v1 由較粗粒度的 `AdvanceFirst/Second/ThirdLevelStep()`／`SnakeGame.AdvanceOneCell()` 擁有移動、碰撞、物件效果、死亡／通關與補物件的順序；v2 則由 WinForms `GamePresenter.OnTickRequested()` 讀取 `GameSession`，再選擇並依序呼叫多個細粒度 use case。

討論也確認：v2 目前的 happy path 順序本身合理，問題不是「這段流程一定錯」，而是正確性主要存在於 Presenter 實作，沒有一份完整 tick 契約、完整 Result 與端到端 acceptance 將順序、互斥條件及失敗一致性鎖定。這會帶來通關判斷重複、局部 use case 的時間耦合、中途失敗留下部分狀態、同時事件優先順序不明，以及其他入口必須重做 orchestration 等風險。

最後以尾格自撞為具體例子釐清：`GameSession` 並不是過期副本；`AdvanceSnakeStepUseCase` 在移動前讀到尾巴仍位於舊位置是正確的。錯誤在於碰撞規則直接以「下一格是否存在於目前全部 `body_cells`」判定自撞，沒有將「未成長時尾巴會在同一步離開」納入計算。v2 規格沒有定義這項例外，acceptance 也沒有相應情境，因此 `BodyCells.Contains(nextHead)` 的實作仍能符合現有規格字面與測試。

## Detailed Q&A Record

### Q1. 「v2 自己拼接 use case」是否是指依賴外部帶進來的物件，判斷要執行哪個 use case？

不是。需要區分兩個概念：

1. **依賴建立／注入：** 誰建立或提供 `AdvanceSnakeStepUseCase`、`EvaluateLevelCompletionUseCase` 等物件。
2. **流程編排：** 何時呼叫哪個 use case、先後順序、哪些分支互斥，以及前一步結果如何傳給下一步。

「自行拼接」指的是第二項。即使所有 use case 都由建構式注入，只要沒有完整 workflow 契約，呼叫順序仍需要實作者自行決定。

目前 v2 甚至不是由外部 DI container 提供全部 use case；`GamePresenter.BuildRoundUseCases()` 會自己建立許多 use case。但這不是核心問題，核心是 `GamePresenter.OnTickRequested()` 擁有如下控制流：

```text
更新第三關門狀態
→ AdvanceSnakeStepUseCase
→ ReadGameplay
→ 若碰撞，選擇 Collision Handler
→ RescueWithShield／EndCurrentLevelPlay
→ 若無碰撞，找蛇頭接觸物件
→ 依物件種類選擇 Contact Handler
→ EvaluateLevelCompletion
→ 再次 ReadGameplay
→ 若達標，EndCurrentLevelPlay
→ 若仍為 Playing，ReplenishTriggeredObject
→ RenderCurrentScreen
```

因此「拼接」是指 Presenter 將多個平行、細粒度 use case 組成一次完整 tick，而不是單純接收依賴物件。

### Q2. v1 是否只接收上層 use case，而 v2 接收第一關開始下面的小 use case？

這個方向大致正確，但例子需要修正。`StartFirstLevel()` 和一次遊戲 tick 是不同玩家操作；本次比較的 v1 上層入口是：

```text
AdvanceFirstLevelStep()
AdvanceSecondLevelStep()
AdvanceThirdLevelStep()
```

v1 UI 大致只需呼叫：

```csharp
_game.AdvanceSecondLevelStep();
```

這個較粗粒度入口下方由 `SnakeGame.AdvanceOneCell()` 完成：

```text
移動
→ 邊界與碰撞
→ 物件效果
→ 護盾或死亡
→ 通關判定
→ 補物件
```

其中多數是同一個 `SnakeGame` 裡的私有實作步驟，不需要 UI 分別選擇公開 use case。

v2 則沒有類似 `AdvanceCurrentGameTickUseCase` 的完整入口。Presenter 直接編排：

```text
AdvanceSnakeStepUseCase
HandleOrdinaryFoodContactUseCase
HandleSpecialFoodContactUseCase
HandleFireContactUseCase
HandleBombContactUseCase
各種 Collision Handler
RescueWithShieldUseCase
EvaluateLevelCompletionUseCase
EndCurrentLevelPlayUseCase
ReplenishTriggeredObjectUseCase
UpdateLevelStatusReadModelUseCase
```

所以較精確的對比是：

> v1 的 UI 呼叫較粗粒度的完整步進入口，其下多為私有步驟；v2 的 UI Presenter 直接呼叫多個公開細粒度 use case，並自行補上完整步進的順序與分支。

### Q3. 是否因為 v2 沒有小 use case 的執行順序，所以 AI 只能憑直覺拼接？

這是最接近的理解，但「憑直覺」應改成「依局部規格推論」。v2 並非完全沒有順序線索，例如：

- 食物效果完成後要判斷通關。
- 致死事件要等待護盾決策。
- 補物件要等物件效果、護盾、通關／死亡判定完成。
- 關卡結束後不得補物件。

這些規格可以推導出局部順序：

```text
食物效果 → 通關判定 → 補物件
```

```text
死亡事件 → 護盾決策 → 結束關卡
```

但沒有一份完整規格回答：

- 門更新與移動同時到期時誰先？
- 碰撞與物件接觸是否互斥？
- 碰撞後是否還要執行通關判定？
- `EvaluateLevelCompletion` 發布 LevelPassed 後，誰呼叫 `EndCurrentLevelPlay`？
- 補物件 trigger 如何可靠傳遞？
- Domain Event ID 與版本由誰提供？
- 任一步失敗時，前面狀態是否保留？
- 何時可以 Render？
- 哪個 Result 代表整個 tick 完整成功？

因此 AI 或人類工程師都必須根據 trigger、precondition、postcondition、事件名稱與自然語言，補出一個合理但不唯一的完整流程。

### Q4. v2 目前的流程看起來也很合理，實際會有什麼問題？

v2 目前正常路徑確實合理：

```text
移動
→ 碰撞則處理死亡
→ 無碰撞則處理物件
→ 判斷通關
→ 若仍在 Playing，補物件
→ Render
```

問題不是 happy path 荒謬，而是完整正確性缺少單一契約擁有。主要風險如下。

#### 4.1 通關規則重複

Presenter 先呼叫：

```csharp
_evaluateLevelCompletionUseCase.Execute(
    new EvaluateLevelCompletionCommand());
```

但沒有使用其語意 Result 或 LevelPassed 事件作為下一步依據，而是再讀狀態並自己比較：

```csharp
GameplayReadModel updatedSnapshot =
    _session.ReadGameplay();

if (updatedSnapshot.CurrentLength
    >= updatedSnapshot.TargetLength)
{
    _endCurrentLevelPlayUseCase.Execute(...);
}
```

所以 `EvaluateLevelCompletionUseCase` 和 Presenter 同時知道通關規則。未來若第三關增加「必須通過出口」等條件，只修改 Evaluate 不夠，Presenter 也必須同步修改，否則兩邊可能得出不同結果。

#### 4.2 小 use case 分開後，完整步進語意可能遺漏

移動碰撞與食物成長分屬不同 use case，如果沒有完整 tick 契約共同定義「本次是否成長、尾巴是否離開、哪些格位最後仍被占用」，各 use case 可能只符合自己的局部規則，卻產生錯誤的完整蛇身。

#### 4.3 中途失敗留下部分狀態

如果一次 tick 已完成移動、吃食物、增加長度，但補物件時拋出例外，Presenter 的 catch 只會顯示失敗並停止 Timer，不會撤銷已完成的移動、食物消耗與長度變化。

#### 4.4 同時事件的優先順序未被完整規範

目前第三關 Presenter 固定執行：

```text
先 UpdateThirdLevelDoorState
→ 再 AdvanceSnakeStep
```

若門切換與蛇穿門在同一個 tick，先關門或先移動可能產生不同結果。現有完整 tick 規格沒有決定這個優先順序。

#### 4.5 新物件或其他入口需要重做流程

新增毒蘑菇等物件時，Presenter 的 switch 與後續分支也要修改。若未來增加 Console、Web、Replay runner 或 headless simulation，它們若只取得目前的細粒度 use case，也必須重寫同一套 Advance／Collision／Contact／Evaluate／Replenish orchestration。

最終結論是：

> v2 的問題不是目前拼出的順序不合理，而是這個順序的正確性主要依賴目前這份 Presenter 實作，而不是由完整契約、完整 Result 與完整 acceptance 共同保證。

### Q5. 「Presenter 需要知道草莓、火焰、炸彈、通關與補物件的商業順序」是否只是因為都依賴 `GameSession`？

不是。需要區分：

- `GameSession` 回答「目前狀態是什麼」。
- 商業流程 owner 決定「知道這些狀態後，下一步做什麼」。

例如 `GameSession` 可以提供：

```text
目前蛇長 19
目標長度 20
蛇頭位置 (5,5)
(5,5) 有草莓
Game 仍為 Playing
```

但還需要有人決定：

```text
先處理草莓
→ 蛇長變成 20
→ 判斷通關
→ 結束關卡
→ 因為已不再 Playing，所以不補草莓
→ 顯示下一關
```

v1 由 `SnakeGame.AdvanceOneCell()` 主動做這些判斷。v2 也不是被動；它由 Presenter 讀取 `GameSession` 後主動判斷：

```csharp
switch (contactedObject.Type)
{
    case GameObjectType.Strawberry:
        _handleOrdinaryFoodContactUseCase.Execute(...);
        break;
    case GameObjectType.Fire:
        _handleFireContactUseCase.Execute(...);
        break;
    case GameObjectType.Bomb:
        _handleBombContactUseCase.Execute(...);
        break;
}
```

Presenter 還知道：

- 炸彈一定進入死亡處理。
- 火焰在受擊前長度小於等於 2 時致死。
- 通關後不得補物件。
- 關卡仍為 Playing 時才補物件。

所以差異不是：

```text
v2 依賴 GameSession，v1 主動判斷
```

而是：

```text
v1：SnakeGame 主動判斷並擁有完整步進。
v2：Presenter 讀取 GameSession 後，主動選擇多個 use case 與執行順序。
```

使用 `GameSession` 本身沒有錯。理想 v2 仍可由完整 `AdvanceGameTickUseCase` 使用 `GameSession`，但 Presenter 只需呼叫一次完整入口並 Render Result。

### Q6. 「未成長時，可以進入本次即將離開的尾格」是什麼意思？

假設蛇彎成 2×2 的 U 形，蛇身由頭到尾為：

```text
H(2,2) → B1(2,3) → B2(1,3) → T(1,2)
```

棋盤外觀：

```text
T  H
B2 B1
```

蛇頭向左，下一格是 `(1,2)`，正好是目前尾巴位置。

未成長的一次移動是：

```text
加入新蛇頭
→ 移除舊尾巴
```

移動前：

```text
[H(2,2), B1(2,3), B2(1,3), T(1,2)]
```

移動後：

```text
[H'(1,2), H(2,2), B1(2,3), B2(1,3)]
```

舊尾巴 `(1,2)` 在同一個 tick 離開，新蛇頭使用它騰出的格子，最後沒有兩段蛇身重疊，所以應屬合法移動。

v2 現在先執行：

```csharp
if (_session.Snake.BodyCells.Contains(
    movement.Position))
{
    _session.RecordMovementFact(
        new SelfCollisionDomainEvent(
            movement.Position));
    return Succeed();
}
```

因為移動前尾巴仍在 `BodyCells`，`Contains(nextHead)` 回傳 `true`，程式直接回報 SelfCollision，沒有執行後面的：

```csharp
_session.Snake.MoveTo(movement.Position);
```

但 `MoveTo()` 原本會：

```csharp
_bodyCells.Insert(0, nextHead);
_bodyCells.RemoveAt(_bodyCells.Count - 1);
```

也就是移除尾巴。v2 的錯誤是用移動前的全部蛇身判斷碰撞，沒有排除本次即將離開的尾巴。

### Q7. `GameSession` 顯示的是尾巴移動前位置，所以 use case 只知道舊 `GameSession` 嗎？

不是。`GameSession` 不是過期副本，而是目前有效的共享遊戲狀態。

一次正常執行可表示為：

```text
移動前：GameSession = S0
→ AdvanceSnakeStepUseCase 修改 Session
移動後：GameSession = S1
→ Contact UseCase 修改 Session
物件效果後：GameSession = S2
→ Presenter ReadGameplay()
讀到目前最新 S2
```

問題發生在第一次碰撞判斷：此時遊戲本來就尚未移動，所以尾巴仍位於舊格是正確的。演算法應使用移動前狀態「推算」哪些格位在移動後仍會被占用，而不是把全部移動前格位都視為碰撞。

正確概念是：

```text
讀取 S0
→ 規劃本次移動
→ 知道尾巴是否將離開
→ 建立移動後仍被占用的碰撞集合
→ 驗證碰撞
→ 通過後提交 MoveTo，產生 S1
```

目前 v2 是：

```text
讀取 S0
→ nextHead 存在於 S0.BodyCells
→ 立即 SelfCollision
→ return
→ MoveTo 未執行
→ Session 保持 S0
```

所以不是 Session 太舊，而是碰撞判斷沒有建模即將發生的尾巴移除。

### Q8. 這是否是因為 v1 先處理資料才改圖，而 v2 沒有？

不是。兩版都是先處理遊戲資料，再更新畫面。

v1：

```text
UpdateGameTime
→ AdvanceOneCell
→ 更新完整遊戲狀態
→ Invalidate
→ Windows 稍後 OnPaint
```

v2：

```text
AdvanceSnakeStepUseCase
→ Contact／Collision UseCase
→ Evaluate／End／Replenish
→ RenderCurrentScreen
→ View.Render
```

尾格誤判發生在 Render 前，是遊戲狀態計算問題，與 `Invalidate()` 或畫圖時機無關。

v1 也使用移動前資料判斷碰撞，但它先計算本次是否成長：

```csharp
bool grows =
    objectAtNext == GameObjectType.Strawberry
    || objectAtNext == GameObjectType.Blueberry;

int bodyCountToCheck =
    _snake.Count - (grows ? 0 : 1);
```

未成長時，檢查數量減一，排除最後一格尾巴；成長時尾巴不離開，才檢查全部蛇身。兩版差異不在先畫或後畫，而在碰撞演算法是否把本次狀態轉換納入計算。

### Q9. 最終理解：v2 誤判自撞，是因為 use case 規格沒有規範尾巴離開嗎？

是，這個理解正確。更精確地說：

> v2 並非完全沒有自撞規格，而是現有自撞規格不完整；它把「下一格存在於目前 `body_cells`」直接視為 SelfCollision，沒有區分未成長時尾巴會在同一步離開。

目前規則近似：

```text
nextHead ∈ currentBodyCells
→ SelfCollision
```

完整規則應近似：

```text
如果本次尾巴會離開：
    collisionCells = bodyCells 排除尾巴
否則：
    collisionCells = 全部 bodyCells

如果 nextHead ∈ collisionCells：
    SelfCollision
```

規格缺口存在於三個層次：

1. **Snake Domain 規則：** 沒有規定尾巴離開時，碰撞集合必須排除舊尾格。
2. **`advance-snake-step`：** 沒有要求先規劃本次是否成長、尾巴是否離開及移動後占用集合。
3. **Acceptance：** 沒有「未成長時進入即將離開尾格」與相反成長情境。

因此實作直接使用：

```csharp
BodyCells.Contains(nextHead)
```

仍符合現有規格字面，相關局部測試也可以通過。

use case 切割不是唯一原因；即使單一大方法也可能寫錯。直接原因是碰撞規則與 acceptance 漏掉尾巴離開語意。切割的影響是：移動碰撞與食物成長分屬不同 use case，又沒有完整 tick 契約共同計算最終占用格位，使這項遺漏更難被其他局部規格與測試發現。

## Key Clarifications

- 「自行拼接」指流程編排，不是依賴注入或物件由外部傳入。
- v1 的比較入口是 `AdvanceFirst/Second/ThirdLevelStep()`，不是 `StartFirstLevel()`。
- v2 不是完全沒有順序，而是只有局部先後條件，缺少完整 tick 的唯一 workflow owner。
- v2 目前 happy path 合理；風險在於完整順序只由 Presenter 實作承載，未被完整契約、Result 與 acceptance 鎖定。
- `GameSession` 只提供目前狀態；「根據狀態下一步做什麼」是流程決策責任。
- v1 與 v2 都會主動判斷；v1 的完整判斷集中在 `SnakeGame`，v2 的跨 use case 判斷落到 Presenter。
- `GameSession` 不是舊副本；尾格誤判是因為碰撞演算法沒有推算本次移動後的占用集合。
- 尾格問題發生在資料計算階段，與 v1 `Invalidate()` 或 v2 Render 時機無關。
- 「小 use case 拿不到資訊」是過度簡化；技術上 `AdvanceSnakeStepUseCase` 持有 Session，真正缺少的是規格與契約責任要求它整合尾巴離開／成長語意。
- 尾格誤判的直接原因是 Domain／use case／acceptance 未規範未成長時尾巴離開的例外；use case 切割使缺口更容易被局部正確性掩蓋。

## Decisions Made

- 將「v2 自行拼接」的最終定義確定為：Presenter 讀取共享狀態後，自行選擇細粒度 use case、執行順序與互斥分支。
- 不將使用 `GameSession` 本身視為問題；問題是 Presenter 以 Session 事實作出 application business decisions。
- 承認 v2 現有順序在 happy path 下合理，但判定其完整正確性缺少上層契約擁有者。
- 將「use case 資訊不足」修正為「局部契約沒有要求整合完整 tick 所需的尾巴離開／成長語意」。
- 將尾格誤判的直接根因判定為規格與 acceptance 缺口，而不是 GameSession 過期、畫面更新時機或 use case 切割本身。
- 本次只進行概念釐清與建立 SessionRecord，不修改應用程式、測試或受保護規格。

## Code Changes

- 新增 `SessionRecords/2026-11-03_22-55-clarify-v2-orchestration-tail-collision.md`，記錄 11/03 的完整問答與最終澄清。
- 未修改應用程式碼、測試、`Domains/**`、`Pattern Network/**`、`misfit.yaml` 或其他既有規格。

## Follow-up Items

- 若要修正尾格規格，先依專案規則列出要修改的 `Domains/**` 確切檔案、規則內容、acceptance 情境與程式影響，取得使用者當次明確同意。
- 後續可設計完整 tick workflow contract，定義事件優先順序、碰撞與物件互斥、尾巴離開／成長計畫、死亡／通關分支、補物件時機與最終 TickResult。
- 可新增尾格 acceptance：未成長時進入即將離開的尾格應合法；若存在尾巴不離開的成長模型，對應情境應判定 SelfCollision。
- 可補完整 Presenter／workflow integration acceptance，避免局部 use case 測試通過卻漏掉跨 use case 語意。
