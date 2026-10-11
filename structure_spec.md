# 打地鼠遊戲專案結構

## 狀態與依據

本文件描述**新計畫、尚未實作**的目標結構。現有 C# 程式僅供辨識待取代的檔案與專案參考，不是新架構、類別命名或資料型別的範本。遊戲需求見 [use_case.md](use_case.md)，畫面配置見 [ui_spec.md](ui_spec.md)，輸入與輸出保證見 [design_by_contract.md](design_by_contract.md)，錯誤處理見 [exceptional_spec.md](exceptional_spec.md)，C# 寫法見 [technical_spec.md](technical_spec.md)。

## 目標結構

新計畫使用一個 WinForms 遊戲主專案和一個獨立測試專案，不建立 Core／Application 分層專案。WinForms 主專案採平坦檔案結構，參考 `SnakeEatFood2`：表單、遊戲規則、操作入口與必要的資料型別放在主專案根目錄；框架產生的資源與設定可使用框架慣用目錄。不為每個 use case、DTO 或責任層額外建立資料夾。自行撰寫的類別仍遵守 `technical_spec.md` 的一類一檔規則。

| 責任 | 放置位置與界線 |
| --- | --- |
| 畫面與輸入 | WinForms 主專案；接收按鈕與目標圖案輪廓點擊、更新畫面、呈現錯誤，不自行實作計分規則。 |
| 遊戲規則與狀態 | WinForms 主專案；處理計時、目標、命中、分數、關卡與重玩，不依賴 WinForms 控制項。 |
| UI 與遊戲之間的操作介面 | WinForms 主專案；讓畫面執行契約所需操作並取得結果。是否使用控制器、DTO 或其他型別，由後續介面設計決定。 |
| 自動測試 | 獨立測試專案；以新契約驗證遊戲行為、邊界與錯誤處理。 |

目前的 `WhackAMole.Core` 與其專案參考在新架構實作時移除或取代；不把現有類別逐一搬入主專案作為預設方案。新類別與檔案依契約需要設計，避免沿用舊型別只為維持相容。

## 後續實作與驗收

只有在另行要求程式實作時，才調整 solution、專案參考、類別與測試。本次文件整理不修改 C#。實作驗收須建置方案並執行測試；檢查遊戲只有一個 WinForms 主專案、畫面不自行計分、規則不依賴控制項，並驗證 [design_by_contract.md](design_by_contract.md) 的命中、時間、分數門檻、重玩及錯誤情境。另依 `use_case.md` 手動確認畫面。
