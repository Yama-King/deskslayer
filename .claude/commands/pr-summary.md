---
description: 依目前分支相對 main 的變更，生成規範的英文 PR 標題與簡介，供使用者手動開 PR 使用
---
請執行以下步驟，產出可以直接複製貼上到 GitHub PR 頁面的內容：

1. 執行 `git log main..HEAD --oneline` 確認目前分支相對 main 的完整 commit 歷史，
   並執行 `git diff main...HEAD --stat` 確認實際變更的檔案範圍。

2. 產出一個英文 PR 標題，格式比照 Conventional Commits 的 type 前綴（feat/fix/refactor 等），
   簡潔描述這條分支「做了什麼」，控制在一行之內，不要超過 72 字元。

3. 產出一份英文 PR 內文（Description），使用以下結構：

## Summary
（2-4 條 bullet points，概述這條分支的核心變更與目的）

## Changes
（列出主要新增/修改的檔案與各自職責，可依 commit 歷史整理，不要逐行列出 diff）

## Testing
（列出這條分支開發過程中實際驗證過的情境與結果，特別標註任何用 Unity Play Mode 或
Unity MCP 直接驗證過的測項，而不只是程式碼邏輯上看起來正確）

## Notes
（列出任何值得 reviewer 特別注意的架構決策、取捨、或已知的範疇邊界——例如某個功能
刻意留給下一條分支處理——避免 reviewer 誤以為是遺漏）

4. 標題與內文都輸出成可以直接複製的純文字區塊，不要加多餘的說明文字包住它們。
5. 這次只負責生成內容，不要執行任何 `gh pr create` 或其他會實際建立 PR 的指令，
   PR 由我自己手動建立。
