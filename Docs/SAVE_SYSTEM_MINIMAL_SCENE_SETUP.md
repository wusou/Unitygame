# 存档系统最小场景清单（可直接照抄）

目标：
1. 主菜单可点 `Play / Read / Quit`。
2. 暂停菜单可点 `Resume / Save / MainMenu / Quit`。
3. 读档面板支持分页、双击读档、右键重命名/删除。
4. 存档面板支持空槽直接存、非空确认覆盖、右键重命名/删除。

---

## 1. MainMenu 场景最小对象树

```text
MainMenu
├─ Main Camera
├─ EventSystem
├─ GameSystems
│  ├─ GameManager
│  ├─ InputBindingsManager
│  └─ SaveGameManager   (可不手动放，脚本会自动创建；建议手动放便于检查)
└─ Canvas_MainMenu
   ├─ Btn_Play
   ├─ Btn_Read   (可不手动放，MainMenuUI 会自动复制 Play 生成)
   ├─ Btn_Quit
   └─ MainMenuUIHost (挂 MainMenuUI)
```

### MainMenu 必配

1. `GameManager`：
   - `Level Scenes` 填地图场景名（如 `Map_01`, `Map_02`）
   - `Main Menu Scene Name` = `MainMenu`
2. `InputBindingsManager`：
   - `Input Actions` 指向 `Assets/Input/GameInputActions.inputactions`
3. `MainMenuUIHost/MainMenuUI`：
   - `saveSlotCount` 设为你要的槽位数（如 `12`）
   - 可手动拖 `playButton/readButton/quitButton`
   - 不手动拖也行，脚本会按按钮名/文本自动找

---

## 2. Map_01 场景最小对象树

```text
Map_01
├─ Main Camera
├─ EventSystem
├─ Player (Tag=Player)
├─ Canvas_Game
│  ├─ HUD
│  └─ PauseRoot (默认隐藏)
│     ├─ Btn_Resume
│     ├─ Btn_Save   (可不手动放，PauseMenuController 可自动复制按钮生成)
│     ├─ Btn_MainMenu
│     └─ Btn_Quit
├─ GameSystems
│  ├─ SceneSpawnResolver
│  └─ PauseMenuControllerHost (挂 PauseMenuController)
└─ SpawnPoints
   ├─ Spawn_Default_Map01 (isDefaultSpawn=true)
   └─ Spawn_From_Map02
```

### Map_01 必配

1. `Player` 必须 `Tag=Player`（读档恢复需要）。
2. `PauseMenuControllerHost/PauseMenuController`：
   - `pauseRoot` 绑定到 `Canvas_Game/PauseRoot`
   - `pauseAction` 绑定 `Gameplay/Pause`（不绑也可用 Esc）
   - `saveSlotCount` 与主菜单一致（如都设 `12`）
3. `SceneSpawnResolver`：每个地图场景都要有。

---

## 3. Map_02 场景最小对象树

```text
Map_02
├─ Main Camera
├─ EventSystem
├─ Player (Tag=Player)
├─ Canvas_Game
│  ├─ HUD
│  └─ PauseRoot (默认隐藏)
│     ├─ Btn_Resume
│     ├─ Btn_Save
│     ├─ Btn_MainMenu
│     └─ Btn_Quit
├─ GameSystems
│  ├─ SceneSpawnResolver
│  └─ PauseMenuControllerHost
└─ SpawnPoints
   ├─ Spawn_Default_Map02 (isDefaultSpawn=true)
   └─ Spawn_From_Map01
```

### Map_02 必配

与 `Map_01` 同规则：
1. `Player Tag=Player`
2. `SceneSpawnResolver`
3. `PauseMenuControllerHost`
4. `PauseRoot` 内按钮可手动放，也可部分自动生成

---

## 4. Build Settings 必做

1. 打开 `File -> Build Settings...`
2. 确保加入：
   - `MainMenu`
   - `Map_01`
   - `Map_02`
3. 名字必须和脚本里一致。

---

## 5. 自动识别按钮命名建议

脚本会按按钮“对象名 + 文本”自动匹配。

### 主菜单

1. Play：`Play / Start / Begin / 开始`
2. Read：`Read / Load / Continue / 读档 / 读取 / 继续`
3. Quit：`Quit / Exit / 退出`

### 暂停菜单

1. Resume：`Resume / Continue / 继续`
2. Save：`Save / 保存 / 存档`
3. MainMenu：`MainMenu / 主菜单 / 返回菜单`
4. Quit：`Quit / Exit / 退出`

---

## 6. 运行时操作规则（你要的行为）

1. 主菜单读档：
   - 左键选中槽位
   - 双击非空槽读档并继续游玩
   - 右键非空槽可重命名/删除
2. 暂停菜单存档：
   - 点击 `Save`
   - 点空槽：直接存
   - 点非空槽：弹确认是否覆盖
   - 右键非空槽：重命名/删除

---

## 7. 检查表（逐项打勾）

### A. MainMenu

- [ ] 有 `GameManager`
- [ ] 有 `InputBindingsManager`
- [ ] `MainMenuUI.saveSlotCount` 已设置（例如 12）
- [ ] `Play` 可进入地图
- [ ] `Read` 可打开读档面板
- [ ] `Quit` 可退出

### B. Map_01 / Map_02

- [ ] 玩家对象 `Tag=Player`
- [ ] 每图都有 `SceneSpawnResolver`
- [ ] 每图都有 `PauseMenuControllerHost`
- [ ] `pauseRoot` 已绑定到 `PauseRoot`
- [ ] `PauseMenuController.saveSlotCount` 与主菜单一致
- [ ] 暂停菜单 `Save` 能打开存档面板

### C. 存档行为

- [ ] 存空槽可直接保存
- [ ] 存非空槽会弹覆盖确认
- [ ] 读档双击可进入
- [ ] 右键可删除
- [ ] 右键可重命名

---

## 8. 常见问题

1. `Read/Save` 按钮没显示：
   - 先看脚本是否已挂在正确对象上；
   - 按钮命名不匹配时会自动复制按钮生成，若层级受限请手动拖引用。
2. 双击读档无效：
   - 确认该槽是非空。
3. 读档报场景错误：
   - 目标场景未加入 Build Settings。
4. 读档后角色不恢复：
   - 玩家对象没有 `Tag=Player`。
