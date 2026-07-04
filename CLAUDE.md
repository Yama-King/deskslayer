# DeskSlayer — Claude Code 專案指南

## 語言規則

**一律使用繁體中文回覆**，包含說明、提問、commit message 以外的所有溝通內容（commit message 維持英文，見下方版本控制章節的規範）。

## 專案概述

`DeskSlayer` 是一款桌面陪伴類打字戰鬥遊戲。玩家日常打字時，遊戲在背景累積能量並觸發角色攻擊，PvE 討伐怪物為主，非 PvP。

**這是求職作品集專案**，一個月內完成開發到上架，目標對齊業界遊戲公司技術規範。開發過程（commit 習慣、架構決策）都要能作為面試談資，因此程式碼與流程需刻意採用業界最貼近實務的做法，不是能跑就好。

## 技術棧與環境

- **引擎**：Unity 6.3 LTS
- **渲染**：Universal Render Pipeline (URP)，**2D 專案**（Universal 2D 範本，非 3D）
- **平台**：Windows PC，優先桌面版，上架 itch.io（下載版 exe，不做 WebGL——WebGL 無法支援 Global Keyboard Hook 的背景鍵盤監聽機制）
- **語言**：C#

## 架構鐵律

- **絕對避免 God Class**：任何類別若職責超過一個，先拆分再實作。
- **高內聚低耦合**：模組間透過介面或事件溝通，避免直接互相參考具體類別。
- **大量使用 `ScriptableObject`** 管理數據（敵人數值、能量規則、音效設定等），讓數據跟邏輯分離，可調參不用重新編譯。
- **2D 專屬技術選型**：
  - 碰撞判定用 `Collider2D` + `Rigidbody2D`（不要用 3D 物理 API）
  - 角色動畫用 Sprite 序列幀動畫（Sprite-based Animation）驅動 `Animator Controller`，非 3D 骨架動畫
  - 場景與鏡頭採 2D 正交（Orthographic）
- **UI / 數值動畫一律用 `DOTween`** 實作（轉場、數值跳動、Tweening），不要手刻 Coroutine 做插值。
- **複雜邏輯與角色動作用 Unity `Animator` 狀態機**控制，保持狀態切換清晰、可視化。
- **預留外部 API 串接介面**（天氣 API 或 Firebase 全球排行榜），即使 W1-W2 還沒實作，架構上要預留擴充點（例如介面或抽象類別）。

## 核心機制

- **Global Keyboard Hook**：**技術路線已定案**——手刻 Win32 `SetWindowsHookEx`（`WH_KEYBOARD_LL`），透過 P/Invoke 呼叫 `user32.dll`，不使用第三方套件（如 SharpHook）或 `RegisterHotKey`。需另開 STA 執行緒處理 Windows 訊息迴圈，並用 `ConcurrentQueue` 等機制將按鍵事件安全傳回 Unity 主執行緒，不可阻塞主執行緒。這是專案技術深度最高的一塊，注意全域鍵盤監聽行為可能被防毒軟體誤判為 Keylogger，README 需誠實說明用途。必須能在遊戲視窗未聚焦時，仍正確攔截並回應全域鍵盤輸入。
- **核心迴圈**：打字 (Input) → 累積能量 → 觸發動作 (Output)。**攻擊機制已定案**：雙武器模式，不做打字對錯判定（背景常駐軟體無目標文字可比對）。
  - **輕攻擊**：每次偵測到有效按鍵（可列印字元）→ 立即觸發一次小攻擊，傷害低、頻率高
  - **重攻擊**：累積按鍵次數/密度達閾值 → 觸發一次大攻擊，傷害高、需蓄力
  - W2 階段將加入時間窗口式蓄力（依打字節奏/頻率驅動蓄力速度），W1 先做基礎累積觸發即可
  - 起始武器：小刀（輕）+ 重劍（重），透過 `WeaponDataSO` 為基礎的 `LightWeaponSO` / `HeavyWeaponSO` 管理
- **戰鬥基礎**：**判定精細度已定案為數值/機率判定**（非即時 Hitbox/Hurtbox 碰撞），呼應 CPU 佔用率極低的核心要求。透過 `ICombatResolver` 介面實作，角色攻擊力 vs 敵人防禦力套用命中率/傷害公式運算，具體數值公式待後續設計階段細談。介面設計需保留未來可替換成即時判定的彈性。
- **W2 延伸規劃（暫不在 W1 範圍）**：`PlayStyleAnalyzer` 模組，統計玩家打字節奏/輕重攻擊觸發比例，映射成「戰鬥風格」分類（例如敏捷型/重砲型），做為特色賣點。W1 階段不用實作，僅需在 Global Keyboard Hook 設計時保留按鍵時間戳記可被後續模組讀取的擴充性，不要為了這個未來需求過度設計 W1 的程式碼。可選延伸：統計 Backspace 修正頻率作為額外風格維度（謹慎型 vs 衝動型），非必做。
- **功能鍵排除**：Ctrl/Shift/Alt 等修飾鍵**刻意不**納入攻擊觸發（`ToUnicode` 轉換會自然過濾掉），僅可列印字元算有效輸入，避免日常操作（如 Ctrl+C）意外觸發遊戲反應。這是確認過的設計決策，不是待修的 bug，不要主動「修正」這個行為。
- **待驗證技術風險（建議提早 Spike，排在 W1/W2 之間）**：桌面陪伴視窗需要無邊框、背景透明、貼齊螢幕邊緣的效果，Unity 不原生支援透明背景視窗，需額外透過 Win32 API（`SetLayeredWindowAttributes`、`WS_EX_LAYERED`）處理，風險等級與 Global Keyboard Hook 相近。完整整合排在 W3，但建議先花 1-2 小時做最小可行性驗證，確認技術路線可行，避免留到 W3 才發現卡關。

## 業界規範教學原則（給我，也給你自己）

因為程式碼與開發流程都要作為面試談資：

- Commit message 用 **Conventional Commits** 格式（`feat:` / `fix:` / `refactor:` / `chore:` / `docs:`），英文撰寫，祈使語氣，標題行 50 字內。
- 需要說明「為什麼這樣改」時才寫 commit body，純環境設定類不用寫 body。
- 遇到架構決策時，主動說明「這是不是業界慣例」，值得我在面試時提起的點要明確點出來。
- 程式碼需要有專業、清楚的註解，隨時假設面試官會直接打開來看。
- 遵循 W1→W4 的四週敏捷排程節奏（見下方），不要跳著做超出當週範圍的功能，避免功能蔓延（feature creep）。

## 四週排程（目前進度請詢問我，不要假設）

- **W1**：底層引擎與核心循環（Global Keyboard Hook、數值/機率戰鬥判定與打擊演出、I-frame（冷卻式無敵幀，非碰撞觸發）、基礎 ScriptableObject 架構）
- **W2**：視覺回饋與果汁感（Animator 狀態機、DOTween、Audio Manager）
- **W3**：系統選單與優化（Game State Machine、Save/Load、CPU 佔用率優化——背景常駐是生存關鍵）
- **W4**：封裝測試與作品集包裝（Bug Fixing、itch.io 上架、README/架構圖/展示影片）

## 版本控制

- 這個檔案（`CLAUDE.md`）本身要 commit 進 Git，讓規則跟著專案版本走。
- 修改前確認目前分支與 working tree 乾淨，避免把未完成的改動跟新功能混在一起 commit。
- 大型重構或新系統開發前，建議提示我先開 feature branch，不要直接在 main 上做。
- **學習目標**：我正在透過這個專案實際練習 Git 版控實務，不只是求「能動就好」。遇到適合的時機（開發新功能、修 bug、想平行處理多個任務），**主動建議並帶著我實際操作** `branch`、`worktree` 等功能，不要為了省事全部都在 `main` 上直接處理。解釋操作時比照「業界規範教學原則」，順便講清楚這是不是業界常見用法、為什麼要這樣做。
