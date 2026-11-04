# Session Record — 2026-11-02 22:48

## Context

- **Project:** Snake
- **Branch:** v2
- **Participants:** Scott Liu

## Summary

本次釐清為何 v1 在沒有明寫「不得閃爍」的情況下較不容易出現 UI 閃爍，以及這是否代表規格越粗，AI 越能自行補足缺漏。結論是：主要差異不在規格粗細本身，而在 v1 與 v2 採用的 UI 更新架構不同，且 v2 新增了控制項生命週期、重新布局與連續 repaint 等時間性風險，規格與 `PresentationContractValidator` 卻仍主要驗證完成後的靜態畫面。

進一步比對 v1 的程式碼、`ui_spec.md`、`technical_spec.md`、`test_spec.md`、`use_case.md` 與已追蹤紀錄後，確認 v1 的「整體繪製＋雙緩衝」並非規格明文要求，而是實作者在規格未指定繪製方式時採用的技術設計。這個選擇降低了閃爍風險，但不能據此斷言 AI 當時是為了補足「不得閃爍」需求而有意選擇它。

## Detailed Q&A Record

### Q1. v1 的規範較粗，是否讓 AI 能自行補足缺漏；v2 規範太細，反而使 AI 被約束而漏掉未規範的閃爍問題？

這個理解有一部分正確，但不能把原因簡化為「v2 規範太細」。較完整的因果關係是：

```text
v2 UI 架構改變
→ 引入控制項清除、重建、重新布局與連續 repaint 等新風險
→ presentation 契約仍主要描述更新完成後的靜態狀態
→ PresentationContractValidator 無法觀察更新期間的中間狀態
→ AI 以明文契約及可驗證條件作為主要完成證據
→ 靜態驗證通過，但閃爍仍可能存在
```

v1 規範也沒有完整描述時間性 UI 行為；其缺口一直存在。只是 v1 將主要畫面放在同一個 Form 的繪圖流程中，沒有在每個遊戲 tick 清除並重建規則區等子控制項，因此較少進入容易暴露空白、中間布局或不同步 repaint 的路徑。

AI 在規格較粗時確實有較多設計空間，可能依一般 WinForms 經驗選擇雙緩衝或整體繪製；但這只是可能的實作選擇，不能當成可靠契約。規格沒有要求的行為，即使某次 AI 實作碰巧做對，下一次重構仍可能消失。

因此解法不是把 v2 規範寫粗，而是讓規範涵蓋新架構帶來的時間性條件，例如：

- 遊戲進行中的一般畫面更新不得重建主要控制項樹。
- 更新期間不得向使用者呈現空白或不完整的中間狀態。
- 繪圖區應使用適當的雙緩衝。
- repaint 應在單一 UI 更新週期內完成。
- 連續移動、狀態更新與護盾切換不得產生可見閃爍。

### Q2. 什麼是「較單純的整體繪製與雙緩衝」？這是在規範沒有要求的情況下自行做出的嗎？

v1 的整體繪製是把 `GameForm` 當成一張主要畫布，而不是把關卡名稱、規則列、狀態區與按鈕全部做成會個別建立、布局及 repaint 的 WinForms 子控制項：

```text
GameForm.OnPaint()
  └─ DrawScene()
      ├─ DrawBoard()    繪製棋盤、蛇與物件
      └─ DrawSidebar()  繪製關卡、進度、規則與按鈕
```

v1 的按鈕也是畫在 Form 上的矩形，再以滑鼠座標判斷是否點中，而不是一般的 `Button` 控制項。程式證據位於：

- `C:\Users\wbcwb\Desktop\SDD_Coding\snake\v1\Snake.WinForms\GameForm.cs:46`：對整個 Form 設定 `DoubleBuffered = true`。
- `C:\Users\wbcwb\Desktop\SDD_Coding\snake\v1\Snake.WinForms\GameForm.cs:80`：覆寫單一 `OnPaint`。
- `DrawScene()` 在同一繪圖流程依序呼叫 `DrawBoard()` 與 `DrawSidebar()`。

雙緩衝的概念是先在不可見的後方緩衝區完成繪圖，再把完成結果一次呈現：

```text
沒有雙緩衝：
清除舊畫面 → 畫背景 → 畫棋盤 → 畫蛇 → 畫側欄
使用者可能看到中間步驟

使用雙緩衝：
在後方緩衝區完成整張畫面
→ 完成後一次顯示
```

Form 的雙緩衝不一定會自動解決所有子控制項的閃爍；v1 效果較直接，是因為主要 UI 本來就由該 Form 自己繪製。v2 改用多個 `Label`、`Panel`、`FlowLayoutPanel`、`Button` 與自訂 Control 後，每個控制項都有自己的生命週期、布局與繪製行為，僅對棋盤或進度 Control 啟用雙緩衝，無法等同於 v1 的整體呈現。

檢查 v1 `master` 中的 `ui_spec.md`、`technical_spec.md`、`test_spec.md`、`use_case.md` 與生成紀錄，未找到下列明文要求：

- 必須設定 `DoubleBuffered`。
- 必須由單一 `OnPaint` 繪製完整畫面。
- 禁止使用多個 WinForms 控制項。
- 一般畫面更新不得閃爍。
- 更新時不得重建控制項。

因此可以判定，整體繪製與雙緩衝是 v1 實作者／AI 在規格沒有指定繪製架構時自行選擇的技術設計。但程式碼只能證明做了這項選擇，不能證明當時的主觀理由。它可能是為了精確還原預覽座標、降低實作複雜度，也可能只是採用了自訂 WinForms 繪圖的常見做法；不能直接說 AI 明確發現了「不得閃爍」的規格缺口並主動補上。

## Key Clarifications

- v1 沒有閃爍不代表 v1 規範已經包含「不得閃爍」契約。
- v1 的單一 Form 繪製與雙緩衝是實作選擇，不是從 `ui_spec.md` 直接推導出的明文要求。
- v1 較不容易閃爍，是因為主要畫面在同一個繪圖緩衝中完成，控制項生命週期較單純。
- v2 的規範詳細度不是直接根因；真正缺少的是與新 UI 架構相配的時間性／更新性契約和動態驗證。
- AI 可以在未規範的空間內做出良好技術選擇，但不能依賴這種偶然選擇取代正式契約。
- 靜態 presentation 驗證通過，只能證明已描述的靜態條件成立，不能證明連續 Render、repaint 或控制項更新過程正確。

## Decisions Made

- 後續描述 v1 時，應使用「v1 的實作架構碰巧降低了未規範的閃爍風險」，不應表述成「v1 規範要求雙緩衝」或「AI 一定主動補足了不得閃爍需求」。
- 不以降低 v2 規範詳細度作為解法；若要修補缺口，應補充時間性 presentation 契約與相應的動態驗證。

## Code Changes

- 新增本 SessionRecord。
- 沒有修改應用程式、測試、`Domains/**` 或 `Pattern Network/**`。

## Follow-up Items

- 若要正式處理 v2 閃爍，先決定時間性 UI 契約的來源與擁有者。
- 若需要修改 `Domains/Game/presentation/**` 或 `Pattern Network/**`，必須先依專案規則列出確切檔案、修改內容與影響，取得使用者針對當次變更的明確同意。
- 可再設計能觀察連續 Render、控制項重建與中間畫面暴露的 WinForms 動態測試；本次未實作。
