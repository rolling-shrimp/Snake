# Session Record — 2026-10-24 22:20

## Context
- **Project:** Snake
- **Branch:** v2
- **Participants:** Scott Liu

## Summary

本次將既有可接受的舊 UI 調整為只呈現 `use_case.md` 明確要求的內容，移除 `assets/new ui/` 與 `assets/old ui/` 的雙軌結構，改由 `assets/` 根目錄保存三關待開始畫面的 HTML 可維護來源與對應 PNG 快照。

為讓 UI 規範成為 Domain 需求的一部分，在 `Domains/Game/presentation/` 建立八個正式畫面狀態及其轉換。Presentation 僅以 `reference_only` 關聯既有 frame、domain 與 technical contract，不新增 UI Aggregate，也不允許既有 Domain 反向依賴 presentation。

Pattern Network Gate 新增 `validate-presentations` 命令，自動驗證畫面清單、關聯鏈、轉換、護盾保命 state variant、必要／禁止元素、預覽資產位置與尺寸，以及反向依賴。實作採測試先行，並以實際 Chrome headless 渲染驗證三份 HTML。

## Detailed Q&A Record

### Q1. 舊 UI、照片與 HTML 應如何整理？

- 刪除 `assets/new ui/` 的 SVG 與 `assets/old ui/` 的舊目錄結構。
- 把調整後的三關預覽統一放到 `assets/level-1.*`、`level-2.*`、`level-3.*`。
- 每一關同時有 HTML 與 1536×1024 PNG；HTML 是可維護來源，PNG 是畫面快照。
- 歷史圖片生成資訊改放 `assets/legacy-generation-prompts.json`，只供追溯，不作為規格來源。
- 預覽只包含 use case 已定義的棋盤、蛇、進度、規則提示、按鈕、牆與門等內容。

### Q2. UI 規範如何納入 Domains？

建立 `Domains/Game/presentation/presentation_catalog.yaml` 與八個畫面契約：

1. `first-level-ready`
2. `first-level-playing`
3. `second-level-ready`
4. `second-level-playing`
5. `third-level-ready`
6. `third-level-playing`
7. `game-over`
8. `game-completed`

每條 `presentation_relations` 都包含：

- `target_use_case`
- `target_specs.frame/domain/technical`
- `frame_check_refs`
- `domain_rule_refs`
- `technical_contract_refs`
- `ownership: reference_only`

關聯方向固定為 presentation 指向既有契約；Aggregate 的 `domain_relations` 不反向引用 presentation。

### Q3. 畫面流程有哪些關鍵決定？

- 第一、二關過關後進入下一關待開始畫面。
- Game Over 不顯示棋盤，只顯示失敗結果卡與重玩按鈕。
- 第三關完成後保留凍結的第三關最後棋盤，顯示成功結果與重玩按鈕。
- Game Over 與完成畫面按下重玩後，直接進入第一關遊玩，不回到待開始畫面。
- 第二、三關護盾保命是遊玩畫面的 state variant 與 self-transition；保命後不要求再次按開始。
- 每條畫面轉換都以 `relation_ref` 指向同畫面的既有 use case relation，並由 Gate 驗證對應 use case 正確。

### Q4. 如何驗證 presentation 規格？

新增命令：

```powershell
dotnet run --project "Pattern Network/harness/PatternNetwork.Gates/PatternNetwork.Gates.csproj" -- validate-presentations --domains Domains --project-root .
```

退出碼：

- `0`：全部通過。
- `1`：規格或關聯違規。
- `2`：命令、路徑或 YAML 設定錯誤。

Gate 會檢查八個畫面、十三條轉換、全域唯一 ID、spec 路徑、use case 名稱、frame/domain/technical trace、`reference_only`、shield rescue variant、visibility 衝突、三組資產及 1536×1024 PNG、以及 Domain 反向依賴。

## Key Clarifications

- `use_case.md` 是遊戲規則、玩家流程與可見結果的需求來源。
- `Domains/Game/presentation/` 是畫面狀態、必要／禁止元素、轉換及契約關聯的機器可驗證來源。
- `ui_spec.md` 是人工閱讀索引；HTML／PNG 是視覺驗收資料，不反向產生需求。
- Presentation 是組合層，不是 Aggregate，也不新增 frame/domain/technical/acceptance 四件組。
- 本階段未修改 `misfit.yaml`，既有未索引 use case 留待下一階段盤點。

## Decisions Made

- UI 規格採八個正式畫面狀態，護盾保命不另建第九個畫面。
- 只有三個待開始狀態提供靜態 HTML／PNG 預覽。
- 所有 presentation 關聯固定使用 `reference_only`。
- Game Over 採純結果卡；完成畫面採凍結第三關棋盤。
- 重玩直接進入第一關遊玩。
- 以 Pattern Network 的既有 C# Gate／YamlDotNet 基礎擴充驗證，不另增驗證工具鏈。

## Code Changes

- `assets/`：新增三關 HTML、PNG、共用 CSS／JavaScript 與歷史提示；移除新舊 UI 子目錄。
- `Domains/Game/presentation/`：新增 catalog 與八個 presentation YAML 契約。
- `Pattern Network/harness/PatternNetwork.Gates/`：新增 presentation validator、結果型別及 CLI 命令。
- `Pattern Network/harness/PatternNetwork.Gates.Tests/`：新增正向與負向驗證器測試、CLI 退出碼測試。
- `AGENTS.md`、`use_case.md`、`ui_spec.md`、`design_by_contract.md`、`test_spec.md`、`exceptional_spec.md`、`structure_spec.md`：補上 UI 規格來源與維護責任。

## Verification

- Pattern Network Gate tests：33/33 通過。
- `validate-presentations`：PASS。
- Snake solution tests：14/14 通過。
- Chrome headless：三關各渲染 100 格棋盤、3 格蛇與 1 個開始按鈕。
- PNG：三張皆通過 1536×1024 尺寸驗證。
- NuGet 僅出現無法讀取弱點資訊來源的 `NU1900` 警告，不影響建置與測試。

## Follow-up Items

- 補齊根目錄 `misfit.yaml`，使所有 Domain use case 與實作狀態可追溯。
- 僅依 `Domains/` 與 `Pattern Network/` 規格，逐一測試先行實作尚未完成的 use case。
- 保留目前工作樹其他既有修改，不在後續 slice 中順手重構無關程式。
