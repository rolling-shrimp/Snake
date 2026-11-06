# Session Record — 2026-11-06 09:03

## Context

- **Project:** Snake (`C:\Users\wbcwb\Desktop\SDD_Coding\snake\Snake`)
- **Branch:** `v2`
- **Participants:** Scott Liu、Codex

## Summary

本次以初學者角度說明目前專案的兩套規格語言：`Pattern Network` 規範所有同類產品專案的架構形狀；`Domains` 規範這一款貪吃蛇實際必須遵守的產品行為。兩者分工不同，Pattern Network 不決定蛇如何移動，Domain／Workflow 規格也不應自行改寫共用的分層與依賴規則。

`PatternNetworkValidator` 明確要求每個 pattern node 都必須包含 `doctrine.yaml`、`prohibitions.yaml`、`gate.yaml` 與 `template.cs.j2`。本次依其內容將四個必要 artifact 分別解讀為正向準則、禁止事項、檢測方式與產碼起始形狀；但專案原始規格沒有把這個組合正式命名為「四形式模式語言」，該名稱只是教學歸納。教學以 UseCases node 為實例，說明完整 Application Workflow、禁止葉節點 Use Case class、Gate evidence 與可包含多個 operation 的 Application Service template 如何互相對應。

同時校正「Use Case 有五個同目錄規格檔」的理解：目前一個 Application Use Case 目錄實際有 `frame.yaml`、`workflow.yaml`、`technical_spec.yaml`、`acceptance.yaml` 四個檔案；第五種契約是由多個 Use Case 共用、位於 Aggregate 根目錄的 `domain_spec.yaml`。本次用 `set-snake-direction`、`process-game-tick`、Snake Aggregate、`render-game-screen` 與 `handle-direction-input` 逐一示範各規格應放的內容。

## Detailed Q&A Record

### Q1. Pattern Network 和 Domains 的根本差別是什麼？

Pattern Network 是共用的架構模式語言，回答「Entities、UseCases、WinForms 專案應該如何分工、相依與命名」。例如全域依賴方向固定為：

```text
WinForms → UseCases → Entities
```

Domains 是單一產品的行為契約，回答「這款貪吃蛇的方向輸入、移動、碰撞、成長、死亡、通關和畫面應如何運作」。因此：

- 「UseCases 不得參考 WinForms」屬於 Pattern Network。
- 「蛇向右時不能直接反轉向左」屬於 Snake Aggregate contract。
- 「Tick 先處理門事件，再規劃蛇身，再檢查碰撞」屬於 Application Workflow。
- 「`SetSnakeDirectionCommand.Direction` 是必填 enum」屬於 technical contract。

### Q2. Pattern Network 的四形式分別代表什麼？

嚴格來說，專案明文定義的是「每個 pattern node 必須具有四個檔案」，沒有明文定義「四形式模式語言」這個術語。下表是依四個檔案的實際內容所做的教學歸納，不是另一份正式 metadata。

| 形式 | 角色 | 核心問題 |
|---|---|---|
| `doctrine.yaml` | 正向設計準則 | 正確的節點應該長什麼樣？ |
| `prohibitions.yaml` | 明確禁止事項 | 哪些錯誤設計不能出現？ |
| `gate.yaml` | 檢測與證據入口 | 用什麼 checker、模式與參數判斷？ |
| `template.cs.j2` | 產碼起始形狀 | 新程式碼預設從什麼結構開始？ |

UseCases 的 `doctrine.yaml` 宣告：公開入口必須從一個外部事件執行到完整且可見的最終結果；一個內聚的 Application Service 可以擁有多個 Workflow Method，不要求一個 method 一個 class。

`prohibitions.yaml` 從反面禁止：不得把碰撞、食物、補物件等內部 step 各自公開為單一 `Execute()` Use Case class，再讓 Presenter 排列順序。

`gate.yaml` 透過 `application_workflow_contracts` 等 checker 引用 doctrine／prohibition rule ID。`mode: blocking` 代表失敗會擋下；`mode: audit` 代表留下審查結果但不一定阻斷。Gate 是閘門及證據聚合，不是 acceptance 與單元測試的一對一清單。

`template.cs.j2` 使用 Jinja 變數與迴圈產生 `ApplicationService` 起始骨架；其中 `for operation in operations` 表示同一 service 可以有多個公開 Workflow Method。Template 不定義蛇的業務規則，只提供符合 Pattern 的程式形狀。

四形式的關係可濃縮為：

```text
Doctrine：公開入口應是完整 Workflow
    ↓
Prohibition：禁止把內部 step 公開成葉節點 Use Case class
    ↓
Gate：檢查產品程式有沒有違反上述規則
    ↓
Template：建立符合上述規則的 ApplicationService 起始骨架
```

### Q3. Use Case 是四個規格檔還是五個？

目前專案中，一個 Application Use Case 目錄實際只有四個檔案。例如：

```text
Domains/Game/workflows/set-snake-direction/
├─ frame.yaml
├─ workflow.yaml
├─ technical_spec.yaml
└─ acceptance.yaml
```

完整產品契約還會引用 Aggregate 的第五種規格：

```text
Domains/Snake/domain_spec.yaml
Domains/Game/domain_spec.yaml
```

`domain_spec.yaml` 不屬於某一個 Use Case，也不應複製到每個 Workflow 目錄；它由所有會操作該 Aggregate 的 Workflow 共用。

五種規格的速記如下：

| 規格 | 回答的問題 |
|---|---|
| `frame.yaml` | 為什麼有這個問題？問題世界有哪些角色、事實與風險？ |
| `domain_spec.yaml` | Aggregate 哪些狀態、行為與不變式永遠必須成立？ |
| `workflow.yaml` | 一次完整外部事件的順序、分支、停止與提交政策是什麼？ |
| `technical_spec.yaml` | 程式具體接收／回傳哪些 model、欄位、型別與 Port？ |
| `acceptance.yaml` | 用哪些 Given／When／Then 情境及證據證明契約成立？ |

### Q4. `frame.yaml` 的具體格式與責任是什麼？

`frame.yaml` 的主要欄位為：

```yaml
schema_version: 3
spec_kind: problem_frame
id: ...
frame:
  bounded_context: ...
  frame_type: ...
  requirement: ...
problem_domains: ...
shared_phenomena: ...
world_facts: ...
frame_concerns: ...
```

- `requirement`：以使用者／系統結果描述需求，不寫 C# method。
- `problem_domains`：問題世界中的參與者或領域，不等於要求建立相同數量的 class。
- `shared_phenomena`：不同問題領域間可觀察的事件或資料交換。
- `world_facts`：現實、框架或既有世界中必須承認的事實。
- `frame_concerns`：需求跨越多個領域時容易失敗、必須特別處理的風險。

`set-snake-direction/frame.yaml` 的實例包括：WinForms 可能在一般 `KeyDown` 前把方向鍵當控制項導覽鍵消耗，這是 world fact；方向鍵必須在導覽前交給 input adapter，且一個訊息只能產生一個 `DirectionRequested`，這些是 frame concerns。

同一句「員工有唯一識別資料」必須依意圖分類：描述現實可辨識性可作 world fact；要求系統永遠維持 `EmployeeId` 唯一是 Aggregate invariant；要求 API 欄位名稱、型別與必填則是 technical spec。

### Q5. `domain_spec.yaml` 為什麼才是 Aggregate 的 Design by Contract？

Aggregate contract 的主要結構為：

```yaml
schema_version: 3
spec_kind: aggregate_contract
id: ...
aggregate: ...
state: ...
value_objects: ...
enums: ...
relations: ...
invariants: ...
behaviors: ...
```

Snake 的 `domain_spec.yaml` 規定：

- `body_cells` 由頭到尾排序。
- 每一格必須唯一。
- 每對相鄰格必須連續。
- 下一方向不得與目前方向直接相反。
- 所有移動、成長、縮短、護盾與重設後都必須重新滿足不變式。

`QueueDirection`、`PlanMovement`、`CommitMovement` 等 behaviors 定義 Aggregate 可以接受的領域操作。尤其 `PlanMovement` 規定一般移動時舊尾格會離開，自撞集合必須排除該尾格；成長時舊尾格保留，自撞集合必須包含完整既有蛇身。

這些規則不只服務某一個 Use Case，而是任何修改 Snake 的流程都必須遵守，因此由 Aggregate 擁有。

### Q6. `workflow.yaml` 應該寫什麼？

Application Workflow 的常見結構為：

```yaml
schema_version: 3
spec_kind: workflow_contract
id: ...
workflow: ...
frame_ref: ...
input: ...
output: ...
domain_refs: ...
preconditions: ...
decisions: ...
steps: ...
branches: ...
stop_conditions: ...
commit_policy: ...
postconditions: ...
failures: ...
no_op: ...
```

`input.grain` 與 `output.grain` 描述一次呼叫的語意資料單位。例如 `SetSnakeDirection` 一次接收一個方向命令，回傳一個完整方向結果與目前畫面；不能回傳半成品，再讓 Presenter 呼叫其他 Use Case 補齊。

`steps` 是完整執行順序的權威來源。`ProcessGameTick` 目前明確規定收集／排序到期事件、門事件、方向選擇、移動計畫、碰撞、物件效果、死亡或通關、補物件、停止條件及最後一次原子提交。這解決了先前沒有人擁有蛇移動完整順序的問題。

`commit_policy` 規定交易邊界，例如完整 Tick 使用 `all_or_nothing`，中間狀態不能被 UI 或其他 Workflow 觀察。`failure` 表示契約無法完成；`no_op` 表示操作有效但在當下條件下不需改變狀態，例如非 Playing 時忽略方向。

### Q7. `technical_spec.yaml` 和 `workflow.yaml` 有什麼差別？

`workflow.yaml` 使用業務語意描述「做什麼、按什麼順序、何時提交」；`technical_spec.yaml` 描述「程式介面具體長什麼樣」。

technical contract 常見結構：

```yaml
schema_version: 3
spec_kind: technical_contract
id: ...
operation: ...
workflow_ref: ...
input:
  model: ...
  fields: ...
  grain: ...
output:
  model: ...
  fields: ...
  grain: ...
types: ...
ports: ...
error_mappings: ...
domain_mappings: ...
implementation_binding: ...
```

`SetSnakeDirection` 的 technical spec 指定輸入 model 為 `SetSnakeDirectionCommand`、`Direction` 欄位必填且只能是四個 enum 值；輸出為 `SetSnakeDirectionResult`，包含 `IsSuccess`、`Outcome`、failure 資訊、完整 Screen 與固定為 `Unchanged` 的 RuntimeDirective。

`implementation_binding.class_count_constraint: none`、`method_name_constraint: none` 再次說明：規格定義邏輯操作，不要求一個 Use Case 一個 class，也不強迫方法名叫 `Execute`。

### Q8. `acceptance.yaml` 是否等於每個 scenario 都要有一個單元測試？

不是。acceptance contract 的主要結構為：

```yaml
schema_version: 3
spec_kind: acceptance_contract
id: ...
contract_ref: ...
scenarios:
  - id: ...
    title: ...
    given: ...
    when: ...
    then: ...
    evidence: ...
evidence_requirements: ...
```

`given` 是操作前狀態，`when` 是單一事件／操作，`then` 是可觀察結果。`evidence` 表示需要的證據種類，例如 behavioral、invariant、runtime UI、temporal、static 或 manual review。

一個測試套件或 checker 可以同時證明多個 scenario；同一個 scenario 也可能需要多種證據。Acceptance 定義「必須證明什麼」，Gate 定義「由哪些檢測器與證據進行閘門判斷」。

### Q9. 目前專案的四個原始問題分別應由哪份規格擁有？

| 原始問題 | 契約位置 |
|---|---|
| 蛇移動時完整執行順序無人負責 | `Game/workflows/process-game-tick/workflow.yaml` |
| 蛇身可能不連續 | `Snake/domain_spec.yaml` invariants |
| 舊尾巴離開卻被誤判自撞 | Snake `PlanMovement` behavior，加上 Tick 的 tail-collision decision |
| UI 各自渲染、造成閃爍 | `Game/presentation/workflows/render-game-screen/workflow.yaml` |
| 實體方向鍵被 WinForms 導覽處理吃掉 | `handle-direction-input/workflow.yaml` 與其 acceptance |
| Command／Result 的欄位與 enum | 對應 Use Case 的 `technical_spec.yaml` |

Presentation Workflow 目前只有 `workflow.yaml` 與 `acceptance.yaml`，因為它描述 UI input adapter 或 renderer，不是包含 Aggregate orchestration 的 Application Use Case。

### Q10. 「四形式模式語言」是專案明文定義，還是教學解讀？

專案中沒有找到「四形式」、「four forms」或「四形式模式語言」的原始定義。可直接證明的事實位於 `Pattern Network/harness/PatternNetwork.Gates/PatternNetworkValidator.cs`：validator 對每個 pattern directory 組合四個固定路徑，並分別呼叫 `EnsureRequiredFile`，因此四個 artifact 是機器強制結構。

「Doctrine 是正向、Prohibitions 是反向、Gate 是檢測、Template 是產碼」可由各檔案內容支持；但把四者合稱「四形式模式語言」是 Codex 為了教學所做的分類。更嚴謹的表述應是：「目前專案每個 Pattern Node 由四個必要 artifact 表達。」

同樣地，專案沒有中央檔案明文宣告「每個 Use Case 有五個同目錄規格檔」。可觀察到的實際結構是 Application Workflow 目錄有四個檔案，而 Aggregate `domain_spec.yaml` 位於領域根目錄並由多個 Workflow 共用；「五種契約視角」也是教學組合，不是五個同目錄檔案的機器規則。

## Key Clarifications

- Pattern Network 管共用架構形狀；Domains 管本產品的實際業務行為。
- 每個 Pattern Node 必須具備四個 artifact 是 validator 明確強制的事實；「四形式模式語言」不是專案明文術語，而是教學歸納。
- 一個 Application Use Case 目錄目前有四個檔案；`domain_spec.yaml` 是多個 Use Case 共用的 Aggregate contract，不應複製進每個目錄。
- `frame.yaml` 的 world fact 描述既存事實；需要系統永遠維持的規則應由 `domain_spec.yaml` invariant 擁有。
- `workflow.yaml` 擁有跨 Aggregate 的完整順序、分支、停止與原子提交政策。
- `technical_spec.yaml` 擁有具體 model、欄位、型別、Port 與 mapping，不應用來保存業務順序。
- `acceptance.yaml` 定義必須被證明的情境與 evidence 類型，不要求 scenario 與單元測試一對一。
- DDD 不代表每個 Use Case 必須是一個獨立 class；重要的是 Aggregate 與完整 Application Workflow 的契約擁有者。

## Decisions Made

- 後續理解與撰寫規格時，採用「Frame 問題世界、Aggregate 永久不變式、Workflow 完整順序、Technical 介面形狀、Acceptance 證據」的分類方式。
- 初學範本先以 `set-snake-direction` 的 `frame_type`／`problem_domains.role` 格式為準，不混用 `process-game-tick/frame.yaml` 目前存在的 `type`／`kind` 變體。
- 本次只進行唯讀教學與現況分析，不調整受保護規格。

## Code Changes

None.

## Follow-up Items

- 可選擇 `start-current-level` 作為下一個練習，依序自行判斷 requirement、world facts、Aggregate invariants、Workflow steps、technical models 與 acceptance scenarios。
- 若要統一 `process-game-tick/frame.yaml` 與其他 Frame 的欄位格式，需先列出確切受保護檔案、預計修改與影響，再取得當次明確同意。
- 後續新增 Use Case 時，先確認它是否真的是「一個外部事件到完整最終結果」，避免再次把內部 step 建成公開葉節點 Use Case。
