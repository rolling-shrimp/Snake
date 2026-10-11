# C# 技術規則

本文件只規範 C# 程式碼的寫法，不定義產品功能、專案架構或特定方法的輸入與輸出。適用範圍由採用本文件的儲存庫指引決定。

## 類別與檔案

- 自行撰寫的類別一律宣告為普通的 `public class Name { }`。類名使用 PascalCase；不強制在類名後加空的 `()`，也不加入 `sealed`、`abstract`、`partial`、`static` 等類別修飾詞。
- 不使用 `record` 宣告資料型別；列舉仍使用 `enum`。
- 每個類別各放一個與類名相同的 `.cs` 檔案，測試輔助類別也一樣。
- 類別可依框架契約繼承基底型別；繼承不改變普通 `public class` 的宣告原則。

## 欄位、常數與名稱

- 實例欄位使用 `private` 與 `_camelCase`，例如 `private object _dependency;`。欄位不宣告 `readonly` 或 `static`，也不額外加入其他欄位修飾詞。
- 編譯期常數可用 `const`，名稱採 `UPPER_SNAKE_CASE`，例如 `private const int MAX_COUNT = 10;`。`const` 是禁止 `static` 欄位的明確例外；不宣告 `static readonly`。
- 區域變數和參數使用 `camelCase`；類別、方法、屬性與列舉名稱使用 PascalCase。
- 建構式收到的外部物件若需保存，參數用 `camelCase`，對應的私人欄位用 `_camelCase`。

## 方法與控制流程

- 一般方法是實例方法，使用普通宣告，例如 `public void Execute()`；不宣告 `static` 方法。只有框架契約確實要求時，才使用 `override`。
- `if`、`else if`、`else`、`for`、`foreach`、`while` 與 `do` 的主體都加 `{}`，即使只有一行。
- 應用程式入口使用 C# 頂層陳述式，避免 `static Program.Main`。
