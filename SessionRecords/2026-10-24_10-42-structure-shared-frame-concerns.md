# Session Record — 2026-10-24 10:42

## Context
- **Project:** Snake
- **Branch:** v2
- **Participants:** Scott Liu

## Summary
本次釐清 Problem Frame 的共用 `frame concern` 與個別 use case 具體檢查之間的責任。原本五份 `frame.yaml` 的 `frame_concerns` 同時混有框架共通概念與 use case 特定條件，因此建立 IDF、CBF、RIF 各四項穩定的共用 concern，並要求同類型的每份 use case YAML 完整具體化這些 concern。

個別 `frame.yaml` 改以 `concern_ref` 引用共用 concern ID，以唯一的 `check_id` 識別具體檢查，再由 `description` 說明該 use case 如何驗證。共用 concern 可以產生一個或多個具體 check，但不可省略或標示不適用。

為改善可讀性與自動驗證，原本表格形式的 `Domains/frame_types.md` 最終由 `Domains/frame_types.yaml` 取代。新目錄可以直接由 PyYAML 載入，並用來驗證五份 use case YAML 的引用與涵蓋範圍。

## Detailed Q&A Record

### Q1. 共用 frame concern 與 use case concern 有什麼差別？

共用 concern 是同一 frame type 下所有 use case 都必須面對的檢查原則，由 frame type 目錄統一擁有。例如所有 IDF 都需要確認資訊完整、資訊正確、顯示範圍正確，而且顯示本身沒有業務副作用。

use case concern 則是將共用原則轉成該情境可驗證的條件。例如：

```yaml
frame_concerns:
  - concern_ref: IDF-CONCERN-001
    check_id: IDF-READY-BOARD-001
    description: "確保待開始畫面同時顯示 10×10 棋盤、安全起點上的 3 格蛇與「開始」按鈕。"
```

`concern_ref` 指向共用規則；`check_id` 與 `description` 表示此 use case 的具體檢查。

### Q2. IDF、CBF、RIF 定義了哪些共用 concern？

IDF：

- `IDF-CONCERN-001` 顯示完整性。
- `IDF-CONCERN-002` 資訊正確性。
- `IDF-CONCERN-003` 顯示範圍。
- `IDF-CONCERN-004` 無業務副作用。

CBF：

- `CBF-CONCERN-001` 命令完整性。
- `CBF-CONCERN-002` 前置條件。
- `CBF-CONCERN-003` 執行唯一性。
- `CBF-CONCERN-004` 結果完整性。

RIF：

- `RIF-CONCERN-001` 觸發完整性。
- `RIF-CONCERN-002` 反應完整性。
- `RIF-CONCERN-003` 反應順序。
- `RIF-CONCERN-004` Domain 不變式。

SWF、RBF、TF、CF 的 concern 尚未討論，狀態保持 `pending`，清單為空，避免提前猜測規則。

### Q3. 每份 `frame.yaml` 如何確保涵蓋共用 concern？

每份 use case YAML 的 `frame_type` 先決定所屬的共用 concern 集合，再由 `frame_concerns[].concern_ref` 逐項引用。規則為：

- 必須涵蓋所屬 frame type 的全部共用 concern。
- 一個共用 concern 可以具體化為多個 check。
- 不可省略共用 concern，也不可標示 `not_applicable`。
- 所有 `check_id` 在目前 Domains 規格中必須唯一。
- 共用分類名稱不再重複寫入個別 YAML，因此移除原本的 `type` 欄位。

三份 IDF YAML 都覆蓋 IDF 四項 concern；第一關狀態顯示額外新增顯示範圍檢查。CBF 四個既有檢查分別具體化四項 CBF concern。RIF 的食物合法位置與蛇長完整性共同具體化 Domain 不變式。

### Q4. 為什麼將 `frame_types.md` 改為 `frame_types.yaml`？

Markdown 表格需要把多項 concern 塞入同一格並使用 `<br>` 排版，閱讀與維護不清楚，也無法由 YAML 工具直接解析。改為 YAML 後，每個 frame type、用途、狀態與 concern 都是獨立欄位及條列項目。

`frame_types.yaml` 包含：

- `classification_rules`：資料夾命名、大小寫、引用欄位、涵蓋政策及不適用政策。
- `frame_types`：七種 frame type 的名稱、用途、concern 狀態與清單。
- IDF、CBF、RIF 的 `concern_status` 為 `defined`，各有四項 concern。
- SWF、RBF、TF、CF 的 `concern_status` 為 `pending`，`frame_concerns` 為空陣列。

共用 concern 的引用流程變成：

```text
frame.yaml 的 frame_type
    → frame_types.yaml 的同名 frame type
    → frame_concerns 中與 concern_ref 相同的 concern_id
    → use case 的 check_id 與 description
```

### Q5. 如何驗證修改結果？

使用 PyYAML 載入 `frame_types.yaml` 與五份 use case `frame.yaml`，確認：

- 七種 frame type 全部存在。
- IDF、CBF、RIF 各有四項共用 concern，共 12 項且 ID 不重複。
- 四種 pending frame 的 concern 清單皆為空。
- 五份 use case YAML 的所有 `concern_ref` 都存在於正確 frame type。
- 每份 use case YAML 完整涵蓋所屬 frame type 的四項 concern。
- 22 個具體 `check_id` 全部唯一。

## Key Clarifications

- `frame_types.yaml` 是共用 concern ID 與語意的唯一擁有者。
- `frame.yaml` 不重新定義 concern 類型，只負責具體化檢查。
- `concern_ref` 是共用規則引用；`check_id` 是 use case 層的唯一檢查識別。
- 共用 concern 與具體 check 是一對多關係，不要求一對一。
- 歷史 SessionRecord 中的 `frame_types.md` 記載保留，不視為目前有效規格。

## Decisions Made

- IDF、CBF、RIF 先各定義四項精簡且普遍適用的共用 concern。
- 每份同類型 use case YAML 必須涵蓋全部共用 concern。
- 不允許 `not_applicable`；若 concern 並非所有 use case 都適用，表示它不應放在共用層。
- 使用 `concern_ref + check_id + description` 作為具體化格式，移除原本的 `concern_id + type + description` 格式。
- 以 `frame_types.yaml` 取代 `frame_types.md`，避免 Markdown 表格與工具驗證限制。

## Code Changes

- 以 `Domains/frame_types.yaml` 取代 `Domains/frame_types.md`，保留七種 frame type 並加入結構化 classification rules。
- 修改三份 IDF `frame.yaml`，加入共用 concern 引用；第一關狀態顯示新增一項顯示範圍 check。
- 修改 CBF `start-first-level/frame.yaml`，將四項檢查對應至 CBF 共用 concern，並明確規定拒絕命令不得留下部分狀態。
- 修改 RIF `activate-first-level-play/frame.yaml`，將五項檢查對應至 RIF 共用 concern，並補充每個有效命令最多反應一次。
- 未修改 C#、DBC 或測試規格；本次只有規格檔變更，因此未執行 .NET 測試。

## Follow-up Items

- 分別討論並定義 SWF、RBF、TF、CF 的共用 concern。
- 若要在後續修改中持續自動執行引用檢查，可將本次臨時 PyYAML 驗證邏輯整理成專案內的驗證腳本或測試。
- `Domains/` 與 `SessionRecords/` 目前尚未加入 Git。
