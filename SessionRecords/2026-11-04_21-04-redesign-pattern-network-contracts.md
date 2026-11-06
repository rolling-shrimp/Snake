# Session Record — 2026-11-04 21:04

## Context
- **Project:** Snake
- **Branch:** v2
- **Participants:** Scott Liu
- **Scope:** 完整閱讀既有 SessionRecords，重新釐清 Pattern Network、Domains、`misfit.yaml`、Use Case 層級、Gate 與 DDD 責任；本次只討論設計，未修改受保護規格或應用程式。

## Summary

本次完整閱讀專案內 15 份既有 SessionRecord，並對照目前 `use_case.md`、`Domains/**`、`Pattern Network/**`、`misfit.yaml`、Gate Harness 與 v2 實作。先前多次分析指出的問題仍成立：完整遊戲步進沒有權威 workflow contract、Snake Aggregate 缺少蛇身拓樸與尾格移動語意、Presentation 只規範最後靜態畫面而沒有時間性 render contract，以及目前 Presenter 成為事實上的 tick orchestration owner。

本次進一步修正了規格格式的責任理解：`frame.yaml` 應描述 Problem Frame 中既有的問題世界事實與共享現象；`domain_spec.yaml` 應作為 Design by Contract，擁有前置條件、不變式、決策、順序、分支、停止條件與後置條件；`technical_spec.yaml` 應描述 Command／Query／Result／DTO／Port 的具體欄位與技術映射；`acceptance.yaml` 則提供可驗證的契約證據。現行格式在這些責任間有大量重複與混用，因此同意建立不永久相容舊格式的 schema v2。

最後也修正了 Gate 與 DDD 的理解。Gate 是驗證閘門，可以執行單元測試、整合測試、property-based test、Roslyn 分析、YAML schema/reference 檢查、WinForms 動態探針或有證據的人工審查；不應要求每個 acceptance scenario 一對一對應測試專案內的單元測試。Domain Aggregate 應以一個擁有狀態與多個業務方法的 rich domain class 維持 invariant；Application UseCases 則以完整 workflow method 協調多個 Aggregate，而不是將每個細小行為都產生為獨立 `UseCase` class 與單一 `Execute`。

## Detailed Q&A Record

### Q1. 目前已知的移動、蛇身、自撞與 UI 問題，根本原因分別是什麼？

問題必須分成三層，不能全部歸因於「缺少上層 Use Case」：

1. **完整 workflow 缺口：** `GamePresenter.OnTickRequested()` 目前實際決定門更新、蛇移動、碰撞／物件分流、護盾／死亡、通關、補物件與 Render 的順序，但沒有一份權威的完整 tick contract、完整 Result 與端到端 acceptance 擁有這個順序。
2. **Snake Domain 規則缺口：** `Domains/Snake/domain_spec.yaml` 只保證 `length == count(body_cells)` 與每格座標合法，沒有保證 `body_cells` 是頭到尾有序且連續的路徑，也沒有定義移動、成長、縮短與安全重設的完整拓樸轉換。
3. **Presentation 時間性缺口：** 正常 tick 通常只在最後呼叫一次 `RenderCurrentScreen()`，不是每讀一次 `GameSession` 就 Render；風險在於一次 `GameForm.Render()` 更新多個獨立控制項，且 `RenderRules()` 每次都清空並重建規則控制項，沒有單一可見提交、穩定控制項樹或不可見中間布局的契約。

尾格自撞誤判的直接原因，是現有規格把 `nextHead ∈ current body_cells` 直接視為 SelfCollision，沒有區分：

```text
未成長：尾巴在同一步離開，碰撞集合應排除舊尾格。
會成長：尾巴不離開，碰撞集合包含完整蛇身。
```

Use Case 切割使這個缺口更容易被局部正確性掩蓋，但不是唯一原因。

### Q2. `frame.yaml`、`domain_spec.yaml`、`technical_spec.yaml` 應如何分工？

最終理解如下：

| Artifact | 責任 | 不應包含 |
| --- | --- | --- |
| `frame.yaml` | Problem Frame、問題世界參與者、共享現象、既有世界事實與 frame concerns | 執行順序、C# 型別、DTO 欄位、UI method |
| `domain_spec.yaml` | Design by Contract：input/output grain、owner、precondition、invariant、decision、workflow order、branch、stop condition、commit policy、postcondition | `csharp_type`、WinForms 操作、重複宣告 technical DTO |
| `technical_spec.yaml` | Command／Query／Result／DTO／Port 的名稱、欄位、型別、錯誤碼與 Domain rule mapping | 重新定義商業規則或另行決定 workflow 順序 |
| `acceptance.yaml` | 可觀察的契約證據與 Given／When／Then 情境 | 用測試實作細節取代需求語意 |

「員工資訊具有唯一識別資料」只有在它是企業世界中本來存在的事實時才屬於 world fact；若是系統必須保證不可重複，則應是 Domain invariant。現行 `frame.yaml` 中的 `required_behavior`、`reaction_order`、按鈕顯示結果等內容應分別遷移至 Domain Contract 或 Presentation Contract。

### Q3. Use Case 應如何建立層級？

同意採三層模型，而且 Use Case 層級與 CBF／RBF／IDF 等 frame type 是兩個正交概念，不新增「Workflow」作為第八種 frame type：

| 層級 | 作用 | 是否為公開應用入口 |
| --- | --- | --- |
| Journey／Lifecycle | 描述跨時間的玩家流程、重複關係與狀態生命週期 | 否 |
| Workflow Use Case | 一個外部事件到完整且可見的最終結果 | 是 |
| Workflow Step／Domain Operation | Workflow 內部步驟或 Aggregate 業務行為 | 通常否 |

第一關開始不應是一個永遠包住所有後續移動的長期呼叫。它負責：

```text
StartFirstLevel
→ 驗證 Ready
→ 建立初始物件
→ 啟用移動排程
→ 完整切換為 Playing
→ 回傳單一 StartLevelResult
```

後續每個到期移動事件再分別呼叫完整的 `ProcessGameTick` workflow。`PlayFirstLevel` 可以是 Lifecycle 規格，組合 `StartFirstLevel`、重複的 `ProcessGameTick` 與 `EndLevel`，但不是一個長期不返回的 `Execute`。

### Q4. 為什麼建議將小 Use Case 降為 Workflow Step 或 Domain Operation？

目前多個細小行為都被建立成同一層的公開類別，例如：

```text
AdvanceSnakeStepUseCase
HandleSelfCollisionUseCase
HandleOrdinaryFoodContactUseCase
EvaluateLevelCompletionUseCase
ReplenishTriggeredObjectUseCase
```

Presenter 因而必須自行選擇類別、決定順序並重複部分商業判斷。最終同意：只有「離開完整 workflow 後仍具有獨立業務價值」的操作才保留為公開 Workflow Use Case；其餘重新分類為內部 step 或 Domain operation。

一次完整 tick 建議由 UseCases 層的 workflow 擁有：

```text
ProcessGameTick
→ 驗證 Playing 與到期狀態
→ 決定同時時間事件優先序
→ 讀取下一格物件並建立 MovementPlan
→ 依 growth delta 決定 tail_will_vacate
→ 計算移動後仍被占用的碰撞集合
→ 一次提交合法蛇身轉換或產生碰撞結果
→ 套用物件效果
→ 處理護盾、死亡與通關互斥分支
→ 仍在 Playing 時補物件
→ 回傳完整 GameTickResult 與最終 snapshot
```

`GameTickResult` 不應只包含 `IsSuccess`，還應有例如 `NotDue`、`Advanced`、`ShieldRescued`、`LevelPassed`、`GameOver`、`GameCompleted` 等語意結果，避免 Presenter 再次計算通關或死亡條件。

### Q5. 「Snake 一個 class、method 代表行為」是否符合 DDD？

這個方向對 Snake Aggregate 是正確的。Domain class 應同時擁有狀態與維持 invariant 的業務方法，例如：

```csharp
public class Snake
{
    public void ChangeDirection(Direction direction) { }
    public SnakeMovementPlan PlanMovement(GridPosition nextHead, int growthDelta) { }
    public SnakeMovementResult Move(SnakeMovementPlan movementPlan) { }
    public LengthChangeResult ChangeLength(int delta) { }
    public void RestoreAtSafeStart(List<GridPosition> safeBodyCells) { }
}
```

但不能把 Food、Shield、Level、Game 與 UI 全部塞進 `Snake`，因為這些是其他 Aggregate 或外層責任。跨 Aggregate 的完整玩家操作應由 Application Service／Workflow Service 協調，例如：

```csharp
public class GameplayApplicationService
{
    public StartLevelResult StartLevel(StartLevelCommand command) { }
    public GameTickResult ProcessTick(GameTickCommand command) { }
}
```

因此正確模型是：

```text
Domain Aggregate class
    擁有自己的狀態與多個業務方法。

Application Service class
    擁有一組內聚的完整 workflow method。

Presenter
    每個 UI 事件只呼叫一個完整 workflow method，再呈現語意 Result。
```

不是「每個細小 Use Case 一個 class」，也不是「所有遊戲行為全部放進 Snake class」。

### Q6. 現在 Pattern Network 的 UseCases 設定是否有問題？

是。現有 `Pattern Network/patterns/use-cases/doctrine.yaml` 規定公開入口一律為 `Execute`，`PN-UC-002` 要求單一 Execute，`template.cs.j2` 又固定產生：

```csharp
public class SomeUseCase
{
    public SomeResult Execute(SomeCommand request) { }
}
```

這會系統性推導出「一 Use Case class＋一 Execute＋一 Command＋一 Result」。這不是 DDD 的必要規則，而是目前 Pattern Network 強制採用的 command handler 模式。少數獨立 workflow 可以採這種形式，但不應套用到碰撞、成長、縮短、通關判斷與補物件等 Domain 行為或內部 workflow step。

Pattern Network 後續應調整：

- Entities 明確要求 Aggregate class 擁有多個領域方法並維持 invariant。
- UseCases 改成 Application Workflow owner；一個 Application Service 可以有多個內聚的公開 workflow method。
- 不再要求每個 Use Case 都是獨立 class。
- 不再要求所有公開入口都命名為 `Execute`。
- Technical spec 應映射到 `OwnerClass.MethodName`。
- Presenter 每個事件只能呼叫一個完整 workflow，不得自行編排多個業務 step。
- 現有 `WinForms → UseCases → Entities` 相依方向可以保留；需要重做的是責任粒度、Doctrine、Template 與 Gate。

### Q7. Gate 與 acceptance 的正確關係是什麼？

修正先前錯誤理解：Gate 是驗證閘門，不等於要求每個 acceptance scenario 在 `{Solution}.Test` 中有一個一對一單元測試。

`acceptance.yaml` 定義要證明的可觀察行為；Gate 選擇適合的 checker 取得證據。可用 checker 包括：

| 驗證目標 | 適合的 checker |
| --- | --- |
| Snake 移動與成長 invariant | Domain unit test／property-based test |
| 完整 tick 順序與分支 | Workflow integration test |
| Command／Result 符合 technical spec | Reflection／Roslyn checker |
| YAML ID 與 reference 完整 | Schema/reference validator |
| Presenter 沒有商業決策 | Roslyn semantic checker |
| UI 不重建未變更控制項 | WinForms runtime instrumentation |
| 無可見中間畫面或閃爍 | Dynamic UI/render probe |
| 無法自動判斷的架構規則 | 有 reviewer、status、evidence 的 manual review |

一個 acceptance scenario 可以由多個 checker 證明；一個整合測試也可以同時證明多個 scenario。Gate 應執行適當 checker 並回報證據是否完整，而不是把 acceptance 固定轉成測試專案內的單元測試。

### Q8. `misfit.yaml` 應如何重新定位？

目前 `misfit.yaml` 將所有 acceptance scenario 與 Pattern Gate check 再抄一次，並錯誤規定 `required_unit_tests` 與 `{solution_name}.Test`。這只能索引已經想到的 scenario，無法證明需求完整，也會造成來源與索引漂移。

最終同意採半自動模式：

```text
acceptance.yaml / domain_spec.yaml / technical_spec.yaml / gate.yaml
    規格與 Gate 的來源。

misfit.yaml
    人工維護專案特有風險、自訂 checker、manual review 與 residual misfit。

verification-manifest.generated.yaml
    Gate 掃描規格後自動產生的契約索引、所需證據、checker 與覆蓋狀態。

verification-report.json 或 Markdown
    每次 Gate 執行後的 PASS／FAIL／PENDING 結果。
```

`misfit.yaml` 應移除：

- `required_unit_tests`。
- `test_project_name_formula`。
- 所有 acceptance 必須一對一對應 `{solution}.Test` 的規則。
- Scenario title／description 的重複副本。
- Pattern Gate check 的重複副本。

Manual review 若沒有 reviewer、status 與 evidence，必須是 PENDING 或 FAIL，不能只因 `manual_review` 是合法 checker 名稱就讓整體 Gate PASS。

### Q9. UI 統一渲染應如何建立契約？

應新增 `render-game-screen` presentation workflow，至少規範：

- 一個完整 workflow result 最多造成一次畫面提交。
- Renderer 只接收完整且已提交的 `GameScreenViewModel`。
- 同一畫面狀態下，未變更區域與控制項身分保持穩定。
- 不得先清空規則區再逐一重建相同內容。
- 更新期間不得向玩家顯示空白或半完成布局。
- Render 不得啟停 Timer、觸發 Use Case 或進行商業判斷。
- Timer/event-source lifecycle 與 Renderer 分離。
- 雙緩衝、差異更新等 WinForms 實作策略放入 technical contract，不作為 Domain 規則。
- Acceptance 應包含連續 tick、相同規則重複 render、護盾 rescue 與畫面 transition。

## Key Clarifications

- Frame type 回答「這是什麼問題形態」；Journey／Workflow／Step 回答「契約責任粒度」，兩者不可混成同一分類。
- `frame.yaml` 的 world facts 只能記錄問題世界既有事實，不能放 required behavior、workflow order 或技術模型。
- `domain_spec.yaml` 是 Design by Contract；`technical_spec.yaml` 是技術介面與欄位表示。
- `StartFirstLevel` 只負責 Ready 到 Playing 的完整轉換，不包住所有後續 tick。
- 完整 workflow contract 不等於巨型方法；它可以委派給 Domain method 與內部 step，但完整順序只有一個 owner。
- Snake Domain class 應擁有方向、移動、碰撞集合、成長、縮短與安全重設的 invariant 行為。
- Food、Shield、Level 與 Game 的跨 Aggregate 流程不應全部塞進 Snake class，應由 Application Workflow 協調。
- Gate 可以使用任何適合的 checker；acceptance 不要求一對一單元測試。
- 現有 Pattern Network 的一 Use Case class／單一 Execute 模式過度強制，確實是造成局部 Use Case 爆炸的系統性因素。
- `misfit.yaml` 應記錄真正的專案特有缺口與人工證據，自動索引與覆蓋狀態改由 Gate 產生。
- UI 問題不是每次 `ReadGameplay()` 都會 Render；真正風險是單次 Render 內多控制項重建、重新布局與缺少統一可見提交。

## Decisions Made

- 同意建立不永久向下相容舊格式的 schema v2，再分批遷移所有 Domains 規格。
- 同意將沒有獨立業務價值的細小 Use Case 降為 workflow step 或 Domain operation。
- 同意 `misfit.yaml` 採半自動模型：Gate 產生規格索引、checker 與覆蓋報告；人工只維護 residual misfit、custom checker 與 manual review evidence。
- 同意 Gate 不限制為單元測試；不同 acceptance 應依證據類型選擇 unit、integration、property、static analysis、runtime UI 或 manual checker。
- 同意 Domain Aggregate 採 rich domain model，一個 Aggregate class 可以有多個維持 invariant 的業務方法。
- 同意 Application 層採內聚的 Workflow／Application Service method，而不是每個細小 Use Case 都產生獨立 class 與單一 `Execute`。
- 同意保留 `WinForms → UseCases → Entities` 相依方向，但重做 UseCases／Entities／WinForms 的 Doctrine、Template 與 Gate 責任。
- 同意將完整 tick、Snake topology／tail semantics 與 presentation render workflow 視為三個需要分別補齊、再互相引用的契約。
- 本次同意的是設計方向，不構成修改 `Domains/**` 或 `Pattern Network/**` 的當次檔案授權；開始修改前仍須列出確切路徑、內容與影響並取得明確同意。

## Code Changes

- 新增本 SessionRecord：`SessionRecords/2026-11-04_21-04-redesign-pattern-network-contracts.md`。
- 未修改應用程式碼、測試、`Domains/**`、`Pattern Network/**`、`misfit.yaml` 或其他既有規格。
- 本次為唯讀分析與設計討論，未執行建置或測試。

## Follow-up Items

- 先設計 schema v2 的正式欄位：Frame、Domain Contract、Technical Contract、Acceptance、Presentation Workflow 與驗證證據格式。
- 定義 Journey／Workflow／Step／Domain Operation 的識別、引用與 Gate 規則。
- 重新設計 Pattern Network 的 Entities、UseCases 與 WinForms doctrine、prohibitions、templates 與 checkers，移除一 Use Case class／單一 Execute 的硬性假設。
- 規劃 `validate-specs` Gate：schema、reference、workflow completeness、orphan step、invariant evidence、Presenter orchestration 與 dynamic presentation checkers。
- 重新設計 `misfit.yaml`、`verification-manifest.generated.yaml` 與 verification report 的責任邊界。
- 以 `start-first-level` 與 `process-game-tick` 作為兩個 schema v2 垂直試點。
- 在 Snake Aggregate 補上 body order、uniqueness、Board-policy adjacency、移動／成長／縮短、departing-tail collision 與 rescue reset invariants。
- 建立 `render-game-screen` presentation workflow 與動態 UI acceptance。
- 真正開始規格修改前，列出所有受保護檔案的確切路徑、修改原因、預計內容與影響，取得使用者當次明確授權。
