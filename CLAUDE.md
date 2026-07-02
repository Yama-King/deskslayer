# DeskSlayer — Claude Code 專案指南

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

- **Global Keyboard Hook**：必須能在遊戲視窗未聚焦時，仍正確攔截並回應全域鍵盤輸入。這是專案最難、優先度最高的底層系統。
- **核心迴圈**：打字 (Input) → 累積能量 → 觸發動作 (Output)。**具體攻擊方式尚未定案**，W1 設計 Hitbox/Hurtbox 系統時請保留彈性（例如用抽象的「觸發攻擊事件」介面），不要預設寫死成特定攻擊形式，待後續 GDD 明確後再收斂實作。
- **戰鬥基礎**：目前規劃 Hitbox/Hurtbox 碰撞判定 + 無敵幀（I-frame）邏輯作為起點，但**判定精細度尚未定案**——不確定最終會是即時動作遊戲等級的精準判定，還是更輕量的抽象化判定（例如純數值/機率觸發）。W1 實作時請先做成可替換的模組（例如獨立的 `ICombatResolver` 介面），避免整個戰鬥系統綁死在特定判定方式上。

## 業界規範教學原則（給我，也給你自己）

因為程式碼與開發流程都要作為面試談資：

- Commit message 用 **Conventional Commits** 格式（`feat:` / `fix:` / `refactor:` / `chore:` / `docs:`），英文撰寫，祈使語氣，標題行 50 字內。
- 需要說明「為什麼這樣改」時才寫 commit body，純環境設定類不用寫 body。
- 遇到架構決策時，主動說明「這是不是業界慣例」，值得我在面試時提起的點要明確點出來。
- 程式碼需要有專業、清楚的註解，隨時假設面試官會直接打開來看。
- 遵循 W1→W4 的四週敏捷排程節奏（見下方），不要跳著做超出當週範圍的功能，避免功能蔓延（feature creep）。

## 四週排程（目前進度請詢問我，不要假設）

- **W1**：底層引擎與核心循環（Global Keyboard Hook、Hitbox/Hurtbox、I-frame、基礎 ScriptableObject 架構）
- **W2**：視覺回饋與果汁感（Animator 狀態機、DOTween、Audio Manager）
- **W3**：系統選單與優化（Game State Machine、Save/Load、CPU 佔用率優化——背景常駐是生存關鍵）
- **W4**：封裝測試與作品集包裝（Bug Fixing、itch.io 上架、README/架構圖/展示影片）

## 版本控制

- 這個檔案（`CLAUDE.md`）本身要 commit 進 Git，讓規則跟著專案版本走。
- 修改前確認目前分支與 working tree 乾淨，避免把未完成的改動跟新功能混在一起 commit。
- 大型重構或新系統開發前，建議提示我先開 feature branch，不要直接在 main 上做。
