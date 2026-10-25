# Session Record — 2026-10-23 21:51

## Context
- **Project:** Snake
- **Branch:** v2
- **Participants:** Scott Liu

## Summary
本次將 Snake 專案的自然語言 use case 整理成 Problem Frame／SDD 規格骨架。最初建立了 `Domains/Game/domain_spec.md`，收錄 `use_case.md`「遊戲說明」的九條規則；後續釐清 frame type 應由單一 `frame_types.md` 定義，而不是建立七個空的類型目錄。

「第一關開始前」的五個編號步驟最終各自視為一個新的 use case，不沿用或對應根目錄既有 UC-01／UC-02。每個 use case 依主要問題關注點歸類，這一階段只建立 `frame.yaml`，暫不撰寫 requirements、machine spec、domain contract 或 acceptance 規格。

完成後共有三個 IDF、一個 CBF 與一個 RIF。五份 YAML 均通過 PyYAML 解析，必要欄位完整，21 個 frame concern ID 沒有重複；本次沒有修改 C# 程式碼。

## Detailed Q&A Record

### Q1. `Domains` 與 Game Domain 最初要如何建立？

在專案根目錄建立 `Domains`，並以 `Domains/Game` 表示遊戲 Domain。`Domains/Game/domain_spec.md` 使用 `# Game Domain Spec` 作為標題，從根目錄 `use_case.md` 複製「遊戲說明」小節的九條規則；不建立頂層 `Domains/domain_spec.md`。

最初曾把 CBF、SWF、IDF、RBF、TF、RIF、CF 建成 `Domains/frame_types/` 下的七個空目錄。這個理解後來被使用者修正，因此這些空目錄已移除。

### Q2. `frame_types` 正確的用途是什麼？

`frame_types` 是 use case 的分類字典，不是七個空目錄。最終建立 `Domains/frame_types.md`，定義：

| 代碼 | 名稱 | 主要用途 |
| --- | --- | --- |
| CBF | Commanded Behaviour Frame | 接收命令並執行業務行為。 |
| SWF | Simple Workpiece Frame | 直接更改狀態或資料。 |
| IDF | Information Display Frame | 顯示資料或狀態。 |
| RBF | Required Behaviour Frame | 指定條件成立時執行必要行為。 |
| TF | Transform Frame | 維護讀取模型或轉換輸入／輸出資料。 |
| RIF | React Innovation Frame | 業務行為完成後執行後續反應，以維持 Domain 資訊完整。 |
| CF | Connection Frame | 規格化獨立程序之間的通訊與事件傳遞。 |

分類規則是一個 use case 使用一個資料夾，並依主要問題關注點選擇一種 frame type。

### Q3. 「第一關開始前」要使用什麼 use case 粒度？

使用者明確要求一個 use case 一個資料夾，因此不採用既有 DBC 將第 1–3 步合併為 UC-01、第 4–5 步合併為 UC-02 的粒度。五個原文步驟各自建立新資料夾，也不設定或追溯既有 UC 編號：

```text
Domains/Game/
├── idf/
│   ├── show-first-level-ready-board/frame.yaml
│   ├── show-first-level-status/frame.yaml
│   └── show-first-level-rules/frame.yaml
├── cbf/
│   └── start-first-level/frame.yaml
└── rif/
    └── activate-first-level-play/frame.yaml
```

### Q4. 五個 use case 為什麼這樣分類？

- 顯示 10×10 棋盤、安全起點 3 格蛇與「開始」按鈕：IDF，主要關注顯示完整性、空間正確性及待開始階段一致性。
- 顯示第一關、`3/20` 與 15%：IDF，主要關注狀態值與進度計算的正確投影。
- 顯示普通食物 +1、邊界穿越及撞身死亡提示：IDF，主要關注規則完整、正確且不混入其他關卡規則。
- 玩家點擊「開始」：CBF，主要關注命令辨識、接受條件、防止重複命令及後續反應交接。
- 隱藏按鈕、產生普通食物並開始自動移動：RIF，主要關注開始命令完成後，Playing 狀態、食物與移動時序是否完整一致。

第 4、5 項刻意分離：CBF 負責「命令是否被正確接受」，RIF 負責「命令接受後的完整 Domain 反應」。

### Q5. `frame.yaml` 在這套規格中的責任是什麼？

每份 `frame.yaml` 使用 `bounded_context: snake-game`、`aggregate: game`，並包含 `use_case`、`frame_type`、`world_facts` 與 `frame_concerns`。

`world_facts` 記錄問題世界中必須成立的業務事實、狀態、時間與觸發條件；`frame_concerns` 則記錄該類 frame 在實作與驗收時最需要防止的不一致或失配風險。這些檔案屬於問題側宣告，目前不定義 API、DTO、C# 方法或既有程式對應關係。

## Key Clarifications

- `frame_types.md` 是分類定義文件，不是 frame type 空目錄的集合。
- 一個 frame type 可以包含多個 use case，但每一個 use case 必須有自己的資料夾。
- frame type 表示 use case 的主要問題視角，不是應用程式分層或類別分類。
- 五個新 use case 不使用既有 UC-01／UC-02 編號，也不修改根目錄 DBC 與測試規格。
- 本階段只建立 `frame.yaml`；其他四種規格檔由使用者日後重新決定。

## Decisions Made

- Bounded Context 使用 `snake-game`，Domain／Aggregate 使用 `game`。
- 資料夾的 frame type 代碼使用小寫，`frame.yaml` 的 `frame_type` 使用大寫。
- 「第一關開始前」建立三個 IDF、一個 CBF、一個 RIF。
- `start-first-level` 成功後接續 `activate-first-level-play`，但不引入正式 API、DTO 或跨程序事件。
- 保留 `Domains/Game/domain_spec.md`，不修改現有 C#、`design_by_contract.md` 或 `test_spec.md`。

## Code Changes

- 建立 `Domains/Game/domain_spec.md`，收錄九條遊戲說明。
- 建立 `Domains/frame_types.md`，定義七種 Problem Frame 與分類規則。
- 移除誤建的 `Domains/frame_types/` 及其七個空目錄。
- 建立五個 use case 的 `frame.yaml`：三個 IDF、一個 CBF、一個 RIF。
- 未建立 `requirements.md`、`machine-spec.yaml`、`domain-spec.yaml` 或 `acceptance.yaml`。
- 未修改 C# 程式碼；因只有規格文件變更，未執行 .NET 測試。

## Follow-up Items

- 使用者日後重新決定五個 use case 的 requirements、machine spec、domain contract 與 acceptance 格式及內容。
- 在開始程式實作前，確認新 Problem Frame 規格是否要取代、補充或獨立於根目錄現有 DBC／測試規格。
- `Domains/` 與 `SessionRecords/` 目前尚未加入 Git。
