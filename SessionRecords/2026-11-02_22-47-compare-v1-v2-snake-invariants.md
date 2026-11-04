# Session Record — 2026-11-02 22:47

## Context

- **Project:** Snake
- **Branch:** v2
- **v1 comparison source:** `origin/master`
- **Participants:** Scott Liu

## Summary

本次先載入 `SessionRecords/2026-10-25_14-14-compare-v1-v2-specification-gaps.md`，恢復先前對 v1 與 v2 規格缺口的分析脈絡，再針對 Snake invariant、尾格碰撞與成長拓樸進行逐檔查證。

查證結果確認：v2 的 Snake aggregate invariant 定義在 `Domains/Snake/domain_spec.yaml`，目前只保證 `length == count(body_cells)` 與每個蛇身格位都是 Board 合法座標。雖然 `body_cells` 型別為 `OrderedList<Board.GridPosition>`，但規格沒有要求相鄰 body cell 必須形成連續路徑，也沒有要求成長格只能從尾端合法延伸。

v1 並沒有更強的正式蛇身拓樸 invariant。v1 沒出現相同問題，主要是完整 tick 契約與整體式實作共同保留了正確語意：移動前就知道下一格是否有食物，非成長移動會排除即將離開的尾格，成長時保留原尾，藍莓需要的額外格則從尾巴相鄰位置延伸。v2 把移動與食物效果拆成獨立 Use Case，卻沒有上層規格擁有兩者間的資料與順序契約，因此現有弱 invariant 容許不連續蛇身。

## Detailed Q&A Record

### Q1. 上次 v1／v2 規格比較留下了哪些與 Snake 有關的問題？

已載入先前 SessionRecord。先前結論指出，v2 的主要問題不是文件數量不足，而是一次 250ms 遊戲步進被拆成 `advance-snake-step`、各物件 contact handler、碰撞 handler、通關判定、補物件及 read model 更新等多個 frame，卻沒有完整 tick 的上層 orchestration contract。

與 Snake 直接相關的既有缺口包括：

- 自撞規則沒有區分「非成長時可進入即將離開的尾格」與「成長時尾格仍被占用」。
- Snake invariant 沒有規定 body cell 的頭尾順序語意、相鄰性及成長位置。
- acceptance 主要驗證長度與局部結果，沒有驗證蛇身拓樸或完整 tick 前後狀態。
- 若要修改 `Domains/**` 或 `Pattern Network/**`，必須先列出確切檔案、修改原因、預計內容與影響，取得使用者針對當次變更的明確同意。

### Q2. v2 的 Snake invariant 定義在哪裡？實際規定了什麼？

定義位置為 `Domains/Snake/domain_spec.yaml`。

`body_cells` 欄位宣告為：

```yaml
- name: body_cells
  path: snake.body_cells
  type: "OrderedList<Board.GridPosition>"
  required: true
```

`length` 是衍生值：

```yaml
- name: length
  path: snake.length
  formula: "count(snake.body_cells)"
```

正式 invariant 只有：

```yaml
- rule_id: SNAKE-INV-001
  description: "蛇長等於 body_cells 數量，且不得小於 0。"

- rule_id: SNAKE-INV-002
  description: "保存的每個蛇身格位都必須是 Board 認可的合法座標。"
```

成長規則 `SNAKE-LENGTH-001` 只要求正的 delta 增加對應格數，沒有規定新增格必須接在尾巴後面。`OrderedList` 只表示集合有順序，不能自行推導相鄰格必須連續。

自撞規則 `SNAKE-MOVE-004` 的條件是「下一個蛇頭格位存在於目前 body_cells」，沒有排除非成長移動時即將離開的最後一格，因此也容許將合法的尾格移動判成 SelfCollision。

### Q3. v1 與 v2 的規格粒度有何差異？

v1 在 `origin/master:design_by_contract.md` 以完整步進為合約邊界：

```markdown
### UC-04 推進第一關：AdvanceFirstLevelStep()

- 蛇沿目前方向前進一格。
- 蛇頭進入普通食物格時增加 1 格並移除食物。
- 蛇頭撞到自身時將本輪結果設為死亡。
- 蛇長達標時立即通關。
```

第二、三關的 `AdvanceSecondLevelStep()` 與 `AdvanceThirdLevelStep()` 同樣在一次操作內擁有移動、物件效果、碰撞、死亡與通關語意。`origin/master:test_spec.md` 的 T-04、T-07、T-10 及玩家流程整合表也以完整步進串接作為驗收範圍。

v2 則將流程拆成：

```text
advance-snake-step
→ handle-ordinary-food-contact / handle-special-food-contact
→ evaluate-level-completion
→ replenish-triggered-object
```

`Domains/Game/rbf/advance-snake-step/domain_spec.yaml` 還要求該 Use Case 只發布後續結果事實，「不越權執行其他 Use Case」。食物 handler 只要求移除食物及長度增加，沒有規定它必須取得哪一個舊尾格、如何延續剛完成的移動，或新增 body cell 必須位於何處。

此外，`use_case.md` 的主要遊戲規則在兩版間沒有實質改變；差異主要發生在 v1 `design_by_contract.md`／`test_spec.md` 被 v2 的大量 frame YAML 取代後，完整 tick 契約沒有等價保留下來。v2 的 `use_case.md` 仍提到已被移除的 `design_by_contract.md` 與 `test_spec.md`，屬於需留意的過時引用。

### Q4. 兩版程式碼如何處理移動、尾格碰撞與成長？

#### v1：在同一個 `AdvanceOneCell()` 內處理

`origin/master:Snake.WinForms/SnakeGame.cs` 先取得下一格物件並判斷是否成長：

```csharp
bool grows = objectAtNext == GameObjectType.Strawberry
    || objectAtNext == GameObjectType.Blueberry;
int bodyCountToCheck = _snake.Count - (grows ? 0 : 1);
```

- 不成長時，尾格將在同一步離開，因此碰撞檢查排除最後一格。
- 成長時，尾巴不離開，因此碰撞檢查涵蓋完整蛇身。

移動時先插入新頭；只有不成長時才刪除舊尾：

```csharp
_snake.Insert(0, nextHead);
if (!grows)
{
    _snake.RemoveAt(_snake.Count - 1);
}
```

草莓因此自然保留舊尾並增加一格。藍莓還需要增加第二格時，`ExtendTail()` 只從目前尾巴的上下左右相鄰格挑選合法位置，所以不會把新蛇身放到遠端。

#### v2：先正常移動刪尾，再由獨立 handler 從全棋盤補格

`Snake.UseCases/AdvanceSnakeStepUseCase.cs` 直接使用全部 `BodyCells` 判定碰撞：

```csharp
if (_session.Snake.BodyCells.Contains(movement.Position))
{
    // SelfCollision
}
```

`Snake.Entities/Snake.cs` 的 `MoveTo()` 一律插入新頭並移除尾巴：

```csharp
_bodyCells.Insert(0, nextHead);
_bodyCells.RemoveAt(_bodyCells.Count - 1);
```

之後 `Snake.WinForms/GamePresenter.cs` 才根據新快照呼叫食物 contact handler。`HandleOrdinaryFoodContactUseCase` 與 `HandleSpecialFoodContactUseCase` 使用 `Board.GetLegalSpawnCells()` 取得整個棋盤上所有未占用格，再以 `Take(lengthDelta)` 交給 `Snake.Grow()`。

`Board.GetLegalSpawnCells()` 只按 `(0,0)` 起的 row-major 順序列出未占用格；`Snake.Grow()` 只驗證新增格不與現有蛇身或彼此重疊，沒有驗證與目前尾巴相鄰。

具體例子：

```text
移動前：(3,1), (2,1), (1,1)，右方 (4,1) 有草莓

v1 結果：(4,1), (3,1), (2,1), (1,1)

v2 移動後：(4,1), (3,1), (2,1)
v2 成長後可能成為：(4,1), (3,1), (2,1), (0,0)
```

v2 結果長度為 4，所有座標也合法，所以符合現有 `SNAKE-INV-001` 與 `SNAKE-INV-002`，但蛇身已不連續。

### Q5. 為什麼 v2 現有 acceptance 與測試沒有抓到？

`Domains/Game/rbf/handle-ordinary-food-contact/acceptance.yaml` 只要求食物消失、蛇長精確增加 1，以及後續流程各觸發一次。對應的 `Snake.Test/HandleOrdinaryFoodContactAcceptanceTests.cs` 只驗證：

```csharp
Assert.Empty(session.Food.ActiveItems);
Assert.Equal(4, session.Snake.Length);
```

測試沒有驗證：

- `body_cells` 的相鄰性。
- 新增格是否緊接原尾巴。
- 一次完整 tick 後的蛇身形狀。
- 非成長時移入即將離開尾格的合法情境。
- 成長時相同尾格仍被占用而應判碰撞的對照情境。

因此目前規格、程式碼與測試彼此是局部一致的，但共同遺漏了蛇身拓樸與完整 tick 的語意。

## Key Clarifications

- v1 沒有更完整的正式蛇身拓樸 invariant；不能把 v1 正常歸因於規格明確要求了連續性。
- v1 正常的直接原因，是完整 tick 契約與 `AdvanceOneCell()` 實作在移動前就知道是否成長，並據此正確處理尾格碰撞與尾巴保留。
- v2 的 `OrderedList<Board.GridPosition>` 不等於「連續路徑」；順序、合法座標與拓樸相鄰是不同約束。
- v2 不連續蛇身不是單一 mapper 錯誤，而是完整 tick 被拆開後，跨 Use Case 的輸入粒度、輸出粒度與契約擁有者沒有被定義。
- `SNAKE-MOVE-004` 同時存在尾格語意缺口；目前程式碼會把非成長時進入即將離開的尾格判為 SelfCollision。

## Decisions Made

- v1 程式碼與規格比較基準維持 `origin/master`；v2 使用目前 `v2` 分支工作樹。
- 將問題定性為 v2 契約粒度與蛇身拓樸語意缺口，而不是單純缺少 `List` 驗證或規格文件數量不足。
- 本次只進行唯讀規格、Git 與程式碼查證，不修改應用程式、測試或受保護規格。
- 若後續修正 `Domains/**`，必須先列出確切檔案、修改原因、預計內容及影響，取得使用者針對當次變更的明確同意。

## Code Changes

- 新增本 SessionRecord：`SessionRecords/2026-11-02_22-47-compare-v1-v2-snake-invariants.md`。
- 未修改應用程式碼、測試、`Domains/**`、`Pattern Network/**`、`misfit.yaml` 或其他規格來源。

## Follow-up Items

- 若要修正規格，先定義完整 tick 的 workflow／orchestration contract 與契約擁有者。
- 在 Snake invariant 補上 body cell 頭尾順序、依 Board 邊界政策判定的相鄰性，以及成長只能合法延伸蛇尾的規則。
- 明確區分非成長時可進入即將離開的尾格，以及成長時尾格仍被占用的碰撞語意。
- 增加完整 tick、普通／特殊食物成長後拓樸、尾格移動對照組與護盾重設後連續路徑的 acceptance。
- 修正 v2 `use_case.md` 對已刪除 `design_by_contract.md` 與 `test_spec.md` 的過時引用時，需先確認新的唯一契約來源。
- 本次未執行測試，因工作內容為唯讀規格與兩版本程式碼比較，沒有實作變更。
