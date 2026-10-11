# 貪吃蛇專案指引

- 遊戲規則、玩家流程與可見結果以 [use_case.md](use_case.md) 為準；畫面排版、圖案與已確認的預覽以 [ui_spec.md](ui_spec.md) 為準。
- 新計畫的 use case 輸入、輸出與狀態保證以 [design_by_contract.md](design_by_contract.md) 為準；測試案例與驗收以 [test_spec.md](test_spec.md) 為準。
- 新增或修改 C# 程式時，遵守 [technical_spec.md](technical_spec.md) 的 C# 技術規則與 [structure_spec.md](structure_spec.md) 的專案結構規則。
- 本機 PowerShell 設為 `AllSigned`；啟動命令僅對該次 PowerShell 程序使用 `Bypass`，不修改系統執行原則。
- 使用 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File 'C:\Users\wbcwb\Scripts\Start-WhackAMoleCodex.ps1' -Game Snake` 開啟 codex-clean 工作階段。
