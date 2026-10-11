# 貪吃蛇遊戲錯誤與例外規範

## 適用範圍

本文件配合 [design_by_contract.md](design_by_contract.md) 的 17 份合約，規範錯誤分類、例外契約與 UI 呼叫入口的 `try/catch` 行為。遊戲規則與玩家可見結果以 [use_case.md](use_case.md) 為準；畫面依 [ui_spec.md](ui_spec.md) 執行。遊戲尚未實作，例外類別的程式碼與 UI 呼叫結構留待後續設計。

每個 use case 的 UI 呼叫入口負責捕捉及處理合約例外。遊戲合約負責驗證 input、階段與遊戲狀態；畫面合約在變更畫面前驗證 input 與前置階段。每份合約也須在正常完成或回傳前檢查自己的 output 與後置保證；違反時以 `OutputContractViolationException` 回報。

例外不得把正常遊戲結果改成錯誤。通關、Game Over、護盾保命、未到移動時點、直接反向輸入被忽略、無合法物件格時等待，以及合約明定忽略的不適用操作，均依遊戲規則正常處理。

## 錯誤分類

| 類型 | 情境 | 表達方式 | UI 處理 |
| --- | --- | --- | --- |
| 正常遊戲結果或不適用操作 | 通關、死亡、護盾保命；未到移動／門更新時點；直接反向輸入；非遊玩階段的方向、開始或重玩要求；沒有合法物件格。 | 不擲例外。依合約更新遊戲狀態，或忽略操作／等待合法條件。 | 依最新狀態更新畫面，不顯示錯誤；不得接續前置階段尚未成立的畫面合約。死亡與通關依各自結果畫面呈現。 |
| 非法方向 | `SetDirection(direction)` 收到上、下、左、右以外的值。直接反向是合法但被忽略的操作，不屬於非法方向。 | `ArgumentOutOfRangeException`；驗證先於方向或遊戲狀態變更。 | 在方向輸入入口提示「方向輸入無效」，保留原遊戲狀態與畫面，並記錄合約方法及呼叫方缺陷。 |
| 非法死亡原因 | `RescueWithShield(cause)` 收到蛇身、牆、關閉的門、炸彈或火焰致命減長以外的原因。 | `ArgumentOutOfRangeException`；驗證先於扣除護盾或調整物件。 | 在死亡處理入口提示「死亡原因無效」，保留原遊戲狀態與畫面，並記錄合約方法及呼叫方缺陷。 |
| 非法關卡編號 | `UpdateLevelStatus(levelNumber)` 收到 1、2、3 以外的整數，例如 0 或 4。 | `ArgumentOutOfRangeException`；驗證先於讀取狀態或修改畫面。 | 在狀態更新入口提示「關卡編號無效」，保留原遊戲狀態與畫面，並記錄合約方法及呼叫方缺陷。 |
| 合約前置階段違約 | 在錯誤階段呼叫有階段限制的畫面合約或保命合約，例如第一關未通關就顯示第二關待開始畫面，未死亡或沒有護盾卻呼叫保命，未死亡卻顯示 Game Over，或第三關尚未通關卻顯示成功畫面。 | `InvalidOperationException`；階段驗證先於遊戲或畫面變更。 | 在該操作入口提示「目前無法執行此操作」，保留原遊戲狀態與畫面，並記錄合約方法及呼叫方缺陷。開始、方向、移動、門更新、補物件或重玩在合約明定的不適用階段仍屬正常忽略，不擲此例外。 |
| 遊戲未初始化 | `UpdateLevelStatus(levelNumber)` 在新遊戲尚未準備時被呼叫。 | `InvalidOperationException`；不得讀取不存在的關卡狀態或改動畫面。 | 在狀態更新入口提示「遊戲尚未準備」，保留現有畫面，並記錄呼叫方缺陷。 |
| Output／後置條件違約 | 合約方法完成後，回傳狀態、遊戲狀態或實際畫面不符合 [design_by_contract.md](design_by_contract.md) 的保證，例如達標後仍繼續移動、保命後蛇長未恢復、物件出現在牆格，或畫面顯示錯誤的進度。 | `OutputContractViolationException`；由擁有該合約的方法在正常回傳或完成前檢查並擲出，屬已辨識的實作缺陷。 | 先停止蛇移動、門計時及玩家輸入，再提示「遊戲發生錯誤，本輪已停止」；記錄合約方法、違反的保證與實際結果，保留目前資料及畫面供檢查，不宣稱狀態已回復。 |
| 未知程式或環境故障 | 合約未列出的例外，包括遊戲時鐘、亂數來源或畫面更新失敗。 | 保留原例外型別與發生的合約方法；不一律改包成 `OutputContractViolationException`。 | 停止蛇移動、門計時及玩家輸入；提示「遊戲發生錯誤，本輪已停止」，保留目前資料與畫面供診斷，不自動重玩，也不假設狀態已回復。 |

`OutputContractViolationException` 尚未實作；新版本實作時使用直接繼承 `Exception` 的專案例外類別。診斷內容須能指出違約的合約方法、違反的保證與實際結果，不增加遊戲 output 或 DTO 欄位。未來若需增加例外種類，先確認觸發條件、狀態保證與 UI 處理方式，再修改本文件及對應合約／測試。

## Output／後置條件違約

- 檢查範圍以 `design_by_contract.md` 已定義的 output 與後置保證為準，包括回傳狀態的語意、執行後的遊戲狀態及畫面；不增加 DTO 欄位或未約定的遊戲限制。`void` 合約仍須檢查執行後的狀態或畫面。
- 檢查責任屬於擁有該合約的方法；在正常回傳或完成前檢查。正常忽略及正常等待的路徑也須符合該合約保證。
- 發現違約時擲出 `OutputContractViolationException`。呼叫方不得補值、修正錯誤狀態、回傳假結果或接續下一個畫面合約來掩蓋違約。
- Input／前置階段違約要求原狀不變；Output／後置條件違約可能已完成部分變更，不沿用原狀不變保證。UI 捕捉後停止本輪，不自動回復、不自動重玩。
- 遊戲時鐘、亂數來源或 UI 元件的例外若非明確的合約後置條件違約，保留原例外作為未知故障，不轉成 `OutputContractViolationException`。

## 每個合約的 `try/catch` 邊界

表中的邊界是 UI 發起一次 use case 的操作入口，不要求建立特定控制器、包裝類別或 DTO。入口須涵蓋該次操作直接呼叫及其合約流程內串接的其他合約；每份合約均須在擁有者內檢查自己的 output／後置條件。

| 合約 | UI 呼叫入口 | Input／前置階段違約與正常忽略 |
| --- | --- | --- |
| `ShowFirstLevelReadyScreen()` | 新遊戲初始畫面入口。 | 階段違約：`InvalidOperationException`。 |
| `StartFirstLevel()` | 第一關「開始」按鈕入口；重玩流程不呼叫此合約。 | 不適用階段正常忽略。 |
| `SetDirection(direction)` | 鍵盤方向鍵入口。 | 非法方向：`ArgumentOutOfRangeException`；直接反向或非遊玩階段正常忽略。 |
| `AdvanceFirstLevelStep()` | 第一關移動計時入口。 | 非第一關遊玩階段或移動尚未到期時正常不移動。 |
| `ShowSecondLevelReadyScreen()` | 第一關通關後的第二關待開始畫面入口。 | 階段違約：`InvalidOperationException`。 |
| `StartSecondLevel()` | 第二關「開始」按鈕入口。 | 不適用階段正常忽略。 |
| `AdvanceSecondLevelStep()` | 第二關移動計時入口；若觸發死亡，涵蓋後續保命或死亡結果流程。 | 非第二關遊玩階段或移動尚未到期時正常不移動。 |
| `ShowThirdLevelReadyScreen()` | 第二關通關後的第三關待開始畫面入口。 | 階段違約：`InvalidOperationException`。 |
| `StartThirdLevel()` | 第三關「開始」按鈕入口。 | 不適用階段正常忽略。 |
| `AdvanceThirdLevelStep()` | 第三關移動計時入口；若觸發死亡或通關，涵蓋相應結果流程。 | 非第三關遊玩階段或移動尚未到期時正常不移動。 |
| `UpdateThirdLevelDoor()` | 第三關門計時入口。 | 非第三關遊玩階段或門更新時點未到時正常忽略。 |
| `RescueWithShield(cause)` | 第二、三關死亡事件處理入口；可能由移動或門碰撞流程呼叫。 | 非法死亡原因：`ArgumentOutOfRangeException`；階段不符或保命次數為 0：`InvalidOperationException`。 |
| `UpdateLevelStatus(levelNumber)` | 發起狀態顯示或狀態更新的畫面入口。 | 非法關卡編號：`ArgumentOutOfRangeException`；遊戲未初始化：`InvalidOperationException`。 |
| `MaintainLevelObjects()` | 開始、物件消耗、火焰／炸彈觸發或保命流程中的物件產生入口。 | 尚未開始、已結束或不需補物件時正常不產生；無合法空格時正常等待。 |
| `ShowGameOverScreen()` | 無護盾死亡後的失敗結果畫面入口。 | 尚未進入死亡結束階段：`InvalidOperationException`。 |
| `ShowThirdLevelSuccessScreen()` | 第三關達標後的闖關成功畫面入口。 | 尚未完成第三關：`InvalidOperationException`。 |
| `RestartGame()` | Game Over 或第三關成功畫面的「重玩」按鈕入口。 | 遊戲仍在進行時正常忽略。 |

表中每個入口也須捕捉該次完整流程內被呼叫合約的例外。例如第二關移動可能先處理火焰，再呼叫 `RescueWithShield(cause)`、更新狀態並補物件；第二關移動入口須處理此鏈上的 `ArgumentOutOfRangeException`、`InvalidOperationException`、`OutputContractViolationException` 及未知故障。

### 捕捉順序與責任

- UI 操作入口按流程捕捉已定義的 `ArgumentOutOfRangeException` 與 `InvalidOperationException`，並依違約合約顯示對應提示；沒有該類違約的入口不必機械式加入無用途的捕捉。
- 所有 17 份合約的 UI 呼叫入口都須捕捉 `OutputContractViolationException`，並在最後以 `catch (Exception)` 處理未知故障。`OutputContractViolationException` 必須列在一般 `Exception` 捕捉之前。
- 內層不得以空 `catch` 吞掉例外後讓外層視為成功。例外須傳遞到能停止遊戲並呈現錯誤的 UI 操作入口；診斷時保留原例外及觸發合約脈絡。
- 相同 .NET 例外型別可能來自不同原因。只有在合約明定的 input 驗證或階段檢查處擲出的例外才分類為相應的合約違約；其他來源依未知故障處理。

## 處理保證

- Input 驗證與前置階段檢查先於遊戲或畫面變更。`ArgumentOutOfRangeException` 與合約指定的 `InvalidOperationException` 必須保留原遊戲資料與原畫面。
- 捕捉 input／前置階段違約後，提示並終止該次操作流程；不得以預設方向、替代關卡編號、假狀態或部分結果呼叫後續合約。
- 正常通關與 Game Over 是玩家可見的遊戲結果，不是系統錯誤；不得因此顯示例外通知。
- Output／後置條件違約或未知故障時，先停止自動移動、門計時與玩家輸入，再顯示系統錯誤。保留現有資料與畫面供診斷，不自動重玩、不假設已完成的部分變更已回復。
- 錯誤通知不增加遊戲分數、關卡結果或遊戲規則；不可用 `Game Over` 取代系統錯誤，也不可宣稱玩家已正常死亡或通關。

## 驗收情境

- `SetDirection` 的四個方向值可接受；非法方向在改變行進方向前擲出 `ArgumentOutOfRangeException`。直接反向及非遊玩階段輸入不顯示錯誤、不改變狀態。
- `UpdateLevelStatus` 的關卡 1、3 可接受；0、4 在讀取狀態或更新畫面前擲出 `ArgumentOutOfRangeException`。未初始化時擲出 `InvalidOperationException`。
- `RescueWithShield` 對允許的五種死亡原因執行保命；非法原因在扣除保命或移動物件前擲出 `ArgumentOutOfRangeException`。沒有護盾或階段不符時擲出 `InvalidOperationException`，保命與物件均不變。
- 第一關未通關、第二關未通關、未死亡或第三關未成功時，分別呼叫受階段限制的畫面／保命合約，須在任何畫面或狀態改變前擲出 `InvalidOperationException`。
- 在不適用階段要求開始、重玩、方向輸入或移動，以及在未到時點更新第三關門時，依合約正常忽略，不顯示錯誤、不接續不成立的畫面流程。
- 暫無合法空格時物件等待；玩家死亡、護盾保命或通關依規則正常處理；上述情境均不可錯誤地轉為例外。
- 對每一份合約安排可觀察的 output／後置條件違約時，擁有該合約的方法在回傳或正常完成前擲出 `OutputContractViolationException`；入口停止計時與輸入、記錄方法與違反保證，不呼叫後續畫面合約，也不宣稱遊戲狀態已復原。
- 在時鐘、亂數或畫面更新入口注入未列出的例外時，UI 保留原例外型別並停止本輪；不得將未知故障誤分類為正常 Game Over、`OutputContractViolationException` 或已回復狀態。
- 各正常結果畫面與重玩流程仍依 [design_by_contract.md](design_by_contract.md) 執行；例外通知不改寫遊戲結果。
