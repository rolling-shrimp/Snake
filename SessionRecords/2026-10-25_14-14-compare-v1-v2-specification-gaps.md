# Session Record — 2026-10-25 14:14

## Context

- **Project:** Snake
- **Current branch:** v2
- **v1 comparison source:** `origin/master`（使用者所稱 v1；本機沒有名為 `v1` 的 Git branch）
- **Scope:** 僅比較 v1 與 v2 的規範內容，並分析規範差異如何容許 v2 的執行期問題發生
- **Participants:** Scott Liu

## Summary

v2 的問題不是規範檔案總量太少。相反地，v2 有 172 個 `Domains` YAML、38 份 acceptance、95 個 scenario，以及完整的 Pattern Network 與 `misfit.yaml` 索引；文件數量遠多於 v1。真正的差異是：v1 雖然只有少量 Markdown，但它把「一次玩家操作或一次 250ms 遊戲步進」當成完整契約，明確串起移動、碰撞、物件效果、關卡判定、補物件與畫面狀態，並另有玩家流程整合測試。v2 則將同一個步進拆成多個獨立 frame/use case，卻沒有再指定一個擁有完整執行順序與原子結果的上層契約。

因此，v2 做到了大量文件、ID 與檔案間的結構可追溯，但沒有完整鎖住跨 use case 的行為語意。蛇尾碰撞、蛇身成長拓樸、動態 UI 更新方式與實體按鍵對應都落在規範縫隙中；現有測試和 Gate 仍可全部通過。

結論不是「規範越少越好」，而是「規範的契約邊界與驗收情境必須對準真實工作流程」。v1 的規範也有過時名稱與 UI 數字不一致等瑕疵；v1 運作正常，是因為關鍵遊戲流程剛好由較完整的操作契約、整合測試及較單純的實作共同守住，而不是因為少量文件本身比較可靠。

## Compared Specification Sets

### v1 (`origin/master`)

- `use_case.md`
- `technical_spec.md`
- `exceptional_spec.md`
- `structure_spec.md`
- `design_by_contract.md`
- `test_spec.md`
- `ui_spec.md`
- `assets/`

上述 Markdown 約 711 行。`assets/` 包含三份新版 SVG 待開始畫面、舊版 PNG 與生成提示。

### v2（目前工作樹）

- `Domains/`：172 個 YAML
- `misfit.yaml`
- `Pattern Network/`：38 個來源檔，包含 pattern、gate、驗證 harness、範本及方案檔
- 38 份 acceptance，合計 95 個 scenario ID

## Detailed Q&A Record

### Q1. 為什麼 v2 的規範更多，運行仍會出現問題？

因為 v2 主要強化了「分解、命名、引用與可追溯性」，但沒有同步補上「跨 frame 的完整工作流程契約」。

v1 的 `design_by_contract.md` 將 `AdvanceFirstLevelStep()`、`AdvanceSecondLevelStep()`、`AdvanceThirdLevelStep()` 定義為一次完整且已到期的遊戲步進。每個契約負責移動一格、處理碰撞或物件效果、改變長度、判斷死亡／護盾／過關，再更新補充物與狀態。`test_spec.md` 除了 T-01 至 T-17 一對一契約測試，還有「玩家流程整合測試」，驗證方向輸入、步進、狀態與物件維護的串接。

v2 將相同流程拆成多個 use case，例如：

- `advance-snake-step`
- `handle-ordinary-food-contact`
- `handle-special-food-contact`
- 各種碰撞 handler
- `evaluate-level-completion`
- `rescue-with-shield`／`end-current-level-play`
- `replenish-triggered-object`
- `update-level-status-read-model`
- Presenter／畫面 Render

各自的輸入輸出看似完整，但沒有一份上層規範明確擁有「一次 250ms tick」的完整順序、資料傳遞、互斥條件及最後只能公開完整結果的責任。結果是實作端必須自行拼裝這些碎片；單一碎片符合規格，不代表整體遊戲步進正確。

這是 v2 最核心的規範差異：**輸入粒度從一次完整遊戲步進，變成多個局部事實；但沒有新增完整流程的契約擁有者。**

### Q2. 蛇頭未撞蛇身卻被判定 Game Over，規範缺少什麼？

缺少兩類精確規則。

第一，`Domains/Snake/domain_spec.yaml` 的 `SNAKE-MOVE-004` 規定「下一個蛇頭格位存在於目前 `body_cells` 就產生 SelfCollision」。它沒有區分：

- 一般移動時，尾巴會在同一步離開，蛇頭移入該尾格應可合法；
- 吃食物成長時，尾巴不離開，移入尾格仍應算碰撞。

`handle-self-collision` 的 acceptance 也只有「重疊」與「不重疊」，沒有「移入即將離開的尾格」這個邊界情境。`advance-snake-step` 的 acceptance 只有跨界、第三關撞牆、尚未到期／非 Playing 三項，也沒有自撞或尾格情境。因此實作直接用全部 `body_cells` 判定碰撞，仍可以被現有規範與測試接受。

第二，Snake invariant 目前只規定：

- `length` 等於 `body_cells` 數量；
- 每個格位必須是 Board 的合法座標。

它沒有規定蛇身是有方向順序的連續路徑，也沒有規定相鄰 body cell 必須在棋盤上正交相鄰（跨界時依 Board 政策判斷），更沒有規定成長格只能從尾端延伸。`SNAKE-LENGTH-001` 只要求正的 delta 增加對應格數。於是「在任意合法空格補出新的 body cell」可以滿足數量與合法座標規則，卻形成不連續蛇身，進一步造成看似莫名的格位顏色與碰撞結果。

### Q3. 關卡進度與遊戲規則 UI 閃爍，規範缺少什麼？

v2 的 presentation YAML 能描述畫面上必須／禁止有哪些元素、畫面轉換、Domain 關聯與驗收資產，但它描述的是「渲染完成後長什麼樣」，沒有描述連續遊戲期間「如何更新」。缺少的動態 presentation 契約包括：

- 未變更的規則區控制項不得每 250ms 清除重建；
- 只更新有變化的棋盤或狀態區域；
- 連續 Render 不得暴露中間布局狀態；
- 控制項識別與布局在相同畫面狀態下保持穩定；
- Timer 的啟停責任與 Render 分離；
- 使用雙緩衝或等效機制保證視覺更新不閃爍。

`PresentationContractValidator` 目前驗證畫面集合、引用、required/forbidden 衝突、transition、護盾變體、資產路徑與圖片尺寸等靜態條件，不會觀察 WinForms 控制項生命週期、連續 repaint 或閃爍。因此 presentation 驗證通過，不能證明動態 UI 更新正確。

v1 的 `ui_spec.md` 與 assets 也沒有明寫「不得閃爍」。v1 沒發生此問題，主要是原本使用較單純、整體雙緩衝的繪製方式。v2 改成會重建控制項的 UI 架構後，風險改變了，規範卻沒有新增相應的時間性／更新性契約。這不是 v1 UI 規範比較完整，而是 v1 實作架構讓該缺口較不容易顯現。

### Q4. 按 D 向右時，為什麼左右格都會改變？規範能阻止嗎？

目前規範不能完整區分正常與異常改變。

正常情況下，一次向右移動會同時改變：右方格成為新蛇頭、原蛇頭格變成身體、尾格恢復棋盤底色，所以不只一格改變是合理的。若變色格與頭、身、尾無關，或吃食物後在遠端出現蛇身格，則與前述「蛇身拓樸與成長位置未受 invariant 約束」有關。

v2 acceptance 只驗證長度增加指定數量，沒有驗證：

- body cell 的順序與相鄰性；
- 新增 body cell 必須延伸自原尾巴；
- 一次普通移動只改變新頭、舊頭語意與舊尾；
- 畫面差異必須與 Domain 前後狀態一致。

因此數量測試能通過，但遠端格位被加入蛇身或整個棋盤重新 repaint 仍不會違反現有驗收條件。

### Q5. WASD 與方向鍵的規範差異是什麼？

v1 的 `use_case.md`、`design_by_contract.md` 與 `test_spec.md` 都以鍵盤「方向鍵」描述輸入；T-03 覆蓋上、下、左、右、反方向及無效方向。

v2 的 `set-snake-direction` 規範只說來源是「WinForms 方向鍵事件」或「方向鍵 key down」，Domain input 則已抽象成 `Direction`。它沒有列出實體按鍵到方向的映射，例如：

- `Keys.Up` → `Up`
- `Keys.W` 是否也可 → `Up`
- `Keys.D` 是否可 → `Right`

其 acceptance 只具體測 Right 狀態下輸入 Up，以及反向 Left／非 Playing 的 no-op；沒有四方向實體按鍵矩陣，也沒有未授權按鍵情境。所以程式額外接受 WASD，不會被 Domain acceptance 或 Pattern Gate 判為錯誤。

如果產品要求是「蛇頭上下左右使用 WASD」，v2 應在 WinForms adapter／presentation input 契約中明列 W、A、S、D；若要求僅方向鍵，也應明列其他按鍵不改變方向。只定義抽象 `Direction` 不足以約束實際鍵盤行為。

### Q6. `misfit.yaml` 與 Pattern Network 為什麼沒有抓到這些問題？

`misfit.yaml` 的目的，是集中列出 acceptance scenario 與 Pattern Gate 的追溯關係。檢查結果顯示：95 個 `SCN-*` 都能在測試專案找到對應 ID，但「每個 ID 有測試」只證明既有情境被實作，不能證明重要情境沒有漏寫。

Pattern Network 現有 Gate 主要能自動驗證：

- 專案與參考方向；
- Command／Query／Result 形狀與命名；
- WinForms、UseCases、Entities 的依賴邊界；
- presentation YAML 的結構、引用與資產條件。

對 Entity invariant、Query 不修改狀態、Passive View、Presenter 結果處理、UI 不做商業決策等語意性規則，gate YAML 使用 `checker: manual_review`。Validator 只把 `manual_review` 視為合法 checker 名稱，沒有自動執行真正的語意審查。因此 Gate 全部通過，只代表結構符合 Pattern Network，不能證明碰撞數學、蛇身拓樸、跨 use case 順序或連續 UI 更新正確。

`misfit.yaml` 也沒有「實作狀態」或「需求覆蓋完整性」的判斷能力。它是索引，不是能推導遺漏需求的模型。

### Q7. 為什麼 v1 規範檔案較少，反而沒有 v2 的問題？

主要有四個原因：

1. **契約邊界更接近使用者動作。** 一次 Timer tick 由一個 `Advance...Step()` 契約完整擁有，不需由 Presenter 拼裝大量局部 use case。
2. **有跨契約玩家流程測試。** v1 不只測單一契約，也驗證方向、移動、狀態與物件維護的串接。
3. **T-03/T-04/T-07/T-10 的情境較聚焦在可見行為。** 特別是方向輸入與各關完整步進，不只驗證資料結構或命名。
4. **實作架構較單純。** 單一遊戲狀態與整體繪製降低了跨層協調與控制項生命週期錯誤的機會。

但不能推論「規範少所以品質好」。v1 規範本身也有問題：

- `structure_spec.md` 標題／內容仍殘留其他專案名稱，且部分結構描述過時；
- `ui_spec.md` 與新版 SVG 的第一、二關目標／百分比，和 `use_case.md` 的 3/20、15% 規則不一致；
- 靜態 assets 無法定義或驗證連續繪製不閃爍。

因此 v1 正常是「關鍵契約與實作碰巧對齊」，不是「所有 v1 規範都正確」。v2 的文件增加後，局部一致性提升，但整體工作流程的語意被分散，形成新的漏接風險。

## Specification Gaps Identified in v2

1. **完整遊戲 tick 的 orchestration contract**：定義一次 250ms 步進從方向套用、移動、碰撞、物件效果、成長／縮短、護盾／死亡、過關、補物件到 read model 更新的唯一順序與原子結果。
2. **尾格碰撞規則**：明確區分非成長時可進入即將離開尾格，以及成長時尾格仍被占用。
3. **蛇身拓樸 invariant**：body cells 有頭到尾的順序，連續格位必須依 Board 政策相鄰，成長只能從尾端合法延伸。
4. **跨 frame acceptance**：加入完整 tick、接觸食物並成長、自撞、移入離開尾格、護盾救援、死亡與過關的端到端情境。
5. **動態 UI 更新契約**：規範控制項穩定性、差異更新、Timer 所有權、雙緩衝與不可見中間布局狀態。
6. **實體按鍵映射**：明列 WASD、方向鍵或兩者的支援範圍，並測四方向、反向與未授權鍵。
7. **驗收完整性與語意 Gate**：除了 scenario ID 存在，還要檢查必要邊界情境與完整玩家流程；`manual_review` 項目需要實際審查流程或可執行的檢查器。

## Verification Evidence

- `Snake.Test`：114 通過、0 失敗。
- Pattern Network Gate tests：33 通過、0 失敗。
- `validate-network`：PASS。
- `check-code`：PASS。
- `validate-presentations`：PASS。
- 95 個 `Domains/**/acceptance.yaml` scenario ID 均可在 `Snake.Test` 找到。

上述結果反而證明目前自動驗證的盲點：它們可確認既有規範與結構被追溯，不能發現未被規範的尾格語意、蛇身拓樸、完整 tick 順序、UI 閃爍或實體按鍵映射。

## Decisions Made

- v1 比較基準採 `origin/master`；v2 採目前工作樹的 `Domains/`、`misfit.yaml` 與 `Pattern Network/`。
- 將問題判定為契約粒度與語意覆蓋缺口，而非規範檔案數量不足。
- 本次只做唯讀比對與建立 SessionRecord，不修改應用程式、測試或受保護規格。
- 若後續要修改 `Domains/**` 或 `Pattern Network/**`，必須先依專案規則列出確切檔案、修改內容與影響，取得使用者針對當次變更的明確同意。

## Code Changes

- 新增本 SessionRecord。
- 未修改程式碼、測試、`Domains/**`、`Pattern Network/**`、`misfit.yaml` 或其他規範來源。

## Follow-up Items

- 先決定正式控制方式是 WASD、方向鍵，或兩者都支援。
- 設計一份完整 tick 的 workflow/orchestration 規範，指定跨 use case 的順序、資料粒度與契約擁有者。
- 補齊 Snake 尾格碰撞與 body topology invariant，以及相對應 acceptance。
- 補上連續 Render／不閃爍的 presentation 時間性契約與可執行驗收方式。
- 將現有 `manual_review` 規則轉成明確的人工作業紀錄或可執行的 semantic checker。
- 任何受保護規格變更開始前，先向使用者取得明確授權。
