# Session Record — 2026-10-25 15:48

## Context

- **Project:** Snake
- **Branch:** v2
- **Participants:** Scott Liu
- **Topic:** 分析 v1／v2 規範差異，以及 Pattern Network、Gate、Template 是否讓缺漏規範仍能通過驗證

## Summary

本次延續 v1 與 v2 的執行差異分析，進一步確認問題並不是 v2 完全沒有 Given／When／Then，也不是既有 acceptance scenario 沒有列入 `misfit.yaml`。v2 已有的 95 個 scenario 都能追溯到測試；真正缺少的是尾格碰撞、蛇身拓樸、完整遊戲 tick、動態 UI 更新與實體按鍵映射等關鍵情境。來源規範沒有列出這些情境時，`misfit.yaml` 自然無從索引，測試也不會產生。

v1 沒有出現相同執行問題，不代表 v1 的少量文件比 v2 完整。關鍵差異是 v1 以一次完整遊戲步進作為契約與測試邊界，移動、是否成長、碰撞、尾巴離開、物件效果與狀態更新仍在同一個操作中；v2 將這些責任拆成多個 frame/use case，卻沒有補上擁有完整順序、決策資料與原子結果的 workflow/orchestration contract。

Pattern Network 因此被判定為促成問題的系統性因素：它能有效驗證架構方向、規格結構、引用與既有 scenario 的追溯，但無法發現應有而未寫出的情境。現有 Pattern、Template、Gate 規範與 Gate Harness 都有不同程度的缺口，尤其是缺少跨 frame 組合規則、情境完整性要求、時間性 presentation 契約，以及 `manual_review` 的實際審查證據。

## Detailed Q&A Record

### Q1. v2 的問題是否因為 use case 規格少了 Given／When／Then，或 Given／When／Then 沒列入 `misfit.yaml`？

不是這兩種情況。v2 的 `Domains/**/acceptance.yaml` 已使用 Given／When／Then 結構，現有的 95 個 acceptance scenario ID 也能在 `misfit.yaml` 與測試中找到追溯關係。

問題在於 `misfit.yaml` 只能追蹤已存在的 scenario：

```text
acceptance.yaml 已存在的 scenario
        ↓
misfit.yaml 建立 scenario ID 追蹤
        ↓
測試保留並驗證該 scenario ID
```

它不能反向判斷「還缺少哪些 scenario」。例如，自撞規範有「下一格與既有蛇身重疊時產生 SelfCollision」，卻沒有列出以下邊界情境：

```gherkin
Given 蛇本次不會成長
And 下一個蛇頭格是本次即將離開的尾格
When 執行一次完整遊戲步進
Then 不應產生 SelfCollision
And 蛇頭移入原尾格
And 原尾巴在同一步離開
```

也缺少相反情境：本次成長時尾巴不離開，蛇頭移入尾格仍應視為碰撞。這些 scenario 沒有寫入 acceptance，因此不會有 scenario ID、`misfit.yaml` 項目或測試。

### Q2. 是否可以直接說 v2 的關鍵規範根本沒有列出來？

可以。核心因果關係是：

```text
關鍵需求沒有寫入 Domain／acceptance／presentation 規範
→ 沒有對應 scenario ID
→ misfit.yaml 沒有項目可以收錄
→ 測試沒有相應案例
→ 實作即使出錯，現有測試與 Gate 仍可全部通過
```

目前缺少的關鍵內容包括：

- 非成長移動時，蛇頭可進入即將離開的尾格。
- 成長移動時，尾格不離開，因此仍屬占用格。
- `body_cells` 必須依頭到尾有序且保持連續相鄰。
- 成長格位必須從尾端合法延伸，不能任意選擇棋盤合法空格。
- 一次 250ms tick 中，方向、移動、碰撞、物件效果、長度、死亡／護盾／過關、補物件與狀態更新的完整順序。
- 連續 Render 不得反覆清除重建未改變的 UI，也不得暴露中間布局狀態或產生閃爍。
- WinForms 實體按鍵與 `Direction` 的正式映射，例如 W/A/S/D 或方向鍵。

### Q3. 為什麼 v1 沒有這些問題？v1 與 v2 的關鍵差異在哪裡？

v1 的 `design_by_contract.md` 以 `AdvanceFirstLevelStep()`、`AdvanceSecondLevelStep()`、`AdvanceThirdLevelStep()` 表示一次完整且已到期的遊戲步進。一次呼叫能同時掌握下一格物件、本次是否成長、尾巴是否離開、碰撞、物件效果、死亡／過關與後續狀態。

這使自撞判斷能在同一個操作中採用正確語意：

```text
本次不成長 → 尾巴同時離開 → 自撞檢查排除最後一個尾格
本次會成長 → 尾巴不離開 → 自撞檢查包含尾格
```

v2 則把完整 tick 拆成 `advance-snake-step`、各種 collision/contact handler、`evaluate-level-completion`、`replenish-triggered-object`、`update-level-status-read-model` 與 Render。碰撞決策和食物成長位於不同 use case，卻沒有上層契約保證決策時已取得 `will_grow` 與 `tail_will_vacate` 等必要事實。

其他差異如下：

| 面向 | v1 | v2 |
|---|---|---|
| 輸入粒度 | 一次完整遊戲 tick | 多個局部 command/event |
| 輸出粒度 | 完整且一致的遊戲狀態 | 多個局部 result |
| 契約擁有者 | `Advance...Step()`／單一遊戲流程 | 沒有完整 tick 的唯一擁有者 |
| 狀態更新 | 同一流程中完成 | 分散於多個 use case |
| 測試重點 | 單一契約加玩家流程整合測試 | 局部 acceptance scenario 追蹤 |
| UI 更新 | 整體雙緩衝繪製，控制項生命週期較單純 | Presenter、ViewModel、Control 樹每 tick 重新 Render |

v1 規範並非完美：其結構文件有過時內容，UI 規格與資產也有數字不一致；尾格語意與不閃爍要求也不一定全部明文寫出。v1 運作正常，部分原因是較粗粒度的契約與較單純的實作架構保留了完整決策資訊，並不是「規範越少越好」。

### Q4. Pattern Network 是否有缺陷，也是導致問題的原因之一？

是，但它是「沒有攔下問題的系統性原因」，不是直接產生每一個程式錯誤的唯一根因。

目前 Pattern Network 擅長驗證：

- WinForms → UseCases → Entities 的依賴方向。
- Command／Query／Result 的結構與命名。
- Pattern、Domain、acceptance 與 presentation 檔案引用。
- 已存在 scenario ID 到測試的追溯。
- Presentation 的 required／forbidden elements、transition、資產路徑與尺寸。

但它缺少：

1. **完整 workflow/orchestration 驗證**：沒有要求拆開的 frame 必須由一個契約擁有者依確定順序組合。
2. **跨 frame 決策資料檢查**：無法發現碰撞判斷缺少 `will_grow`、`tail_will_vacate` 等必要輸入。
3. **情境完整性檢查**：能確認已有 scenario 被測試，不能確認正向、反向、邊界、衝突、no-op 與 invariant preservation 情境是否齊全。
4. **時間性 presentation 檢查**：只驗證完成後的靜態畫面，不驗證連續 repaint、控制項重建、Timer 所有權與閃爍。
5. **實體 input adapter 契約**：抽象 `Direction` 正確，不代表 W/A/S/D 或方向鍵映射已被定義與驗證。
6. **有效的 manual review 門檻**：`checker: manual_review` 目前主要被當成合法 checker 名稱，沒有審查人、證據、狀態或未完成時的阻擋行為。

因此 Pattern Network 目前較接近「結構與追溯驗證器」，還不是「業務行為完整性驗證器」。

### Q5. 是否代表 Pattern Network 的 Gate 規範不足，Template 本身也有缺陷？

是，但應區分 Pattern、Template、Gate 規範與 Gate Harness 四層：

```text
Pattern 定義缺少完整 workflow 與組合責任
→ Template 沒有要求作者填寫完整流程、決策資料與必要邊界
→ 產生的規範格式完整但語意可能不完整
→ Gate 只檢查結構、引用與既有情境追溯
→ 不完整規範仍可 PASS
```

Template 的問題不是 YAML 格式錯誤，而是缺少能迫使作者回答下列問題的欄位或檢核提示：

- 這個 frame 屬於哪個完整玩家 workflow？
- 完整流程的 contract owner 是誰？
- 前後有哪些步驟，順序與終止條件是什麼？
- 哪些步驟必須原子完成，不得公開中間狀態？
- 做出決策需要哪些輸入事實？
- 修改哪些狀態，操作後必須維持哪些 invariant？
- 正向、反向、邊界、衝突、no-op 與跨 frame 情境是否齊全？
- UI 或 Timer 相關行為是否需要時間性驗收？
- 抽象輸入如何映射至實體平台事件？

不過 Template 無法自行發明「貪吃蛇非成長時可以進入即將離開尾格」這類業務規則。合理責任分工應是：Domain 作者提供正確語意；Template 強迫作者思考必要類型；Gate 拒絕覆蓋或證據不完整的規範；workflow pattern 保護跨 frame 組合；測試證明實作符合規範。

## Key Clarifications

- v2 不是缺少 Given／When／Then 格式，而是缺少正確粒度與必要邊界的 Given／When／Then。
- `misfit.yaml` 沒有漏抄既有 scenario；它無法發現來源 acceptance 根本沒有寫出的 scenario。
- v1 正常不代表 v1 規範完整或規範越少越好；完整 tick 邊界、整合測試與單純實作共同降低了錯誤。
- v2 的主要設計風險是將需要共同決策的資料拆散，卻沒有 workflow contract owner。
- Pattern Network 有效保護架構和追溯，但目前無法保證業務語意與情境集合完整。
- `manual_review` 若沒有實際證據與阻擋狀態，不應被視為已通過的 semantic gate。
- Template 的缺陷是引導與強制項目不足，不是它應該自動推導所有領域知識。

## Decisions Made

- 將 v2 問題分為三層：來源規範漏寫、Pattern Network 未攔截、實作依不完整規範產生錯誤行為。
- 不以增加更多獨立 frame 作為主要修正；優先補上完整 workflow/orchestration contract 與跨 frame acceptance。
- Pattern Network 後續若強化，應同時處理 Pattern、Template、Gate 規範及 Gate Harness，而不是只增加 `misfit.yaml` 條目。
- `manual_review` 應有明確狀態及證據；未完成時應為 PENDING 或 FAIL，而不是整體 PASS。

## Code Changes

- 本次分析前，依使用者指示將當時工作樹全部 490 個檔案變更提交並推送至 `origin/v2`。
- Commit：`0ddaf8b1f1e77e72fc5791d4e8560358be5d199c`（`feat: restructure snake game around domain use cases`）。
- 驗證結果：Snake 測試 114/114 通過；Pattern Network Gate 測試 33/33 通過。
- 新增本 Session Record；沒有修改 `Domains/**`、`Pattern Network/**` 或應用程式碼。

## Follow-up Items

- 設計完整 gameplay-tick workflow/orchestration Pattern，明確定義 input grain、output grain、contract owner、步驟順序、決策資料與原子性。
- 擴充 Template，要求列出 invariant preservation、正反向、邊界、衝突、no-op、跨 frame 與時間性情境。
- 擴充 Gate，檢查重要業務規則是否有必要 scenario 類型，而不只檢查 scenario ID 是否存在。
- 將 `manual_review` 改為需要審查人、狀態與證據；未完成時不得 PASS。
- 增加動態 presentation 契約及 WinForms 實體按鍵映射契約。
- 若要修改受保護的 `Domains/**` 或 `Pattern Network/**`，必須先列出確切檔案、修改內容與影響，取得使用者當次明確同意。
