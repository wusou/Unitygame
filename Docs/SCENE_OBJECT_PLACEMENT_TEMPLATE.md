# 场景对象挂载模板（MainMenu / Map_01 / Map_02）

本文专门回答这批对象怎么挂：
1. `GameManager`
2. `InputBindingsManager`
3. `SceneSpawnResolver`
4. `PauseMenuControllerHost`
5. `Door_To_NextMap`
6. `WeaponPickup_Freeze`
7. `WeaponPickup_Burn`
8. `WeaponPickup_Common`
9. `NPC_Shop`

---

## 1. 先看结论（必须/按需）

1. 全项目只挂一次：
   - `GameManager`
   - `InputBindingsManager`
2. 每个可游玩地图场景都要挂：
   - `SceneSpawnResolver`
   - `PauseMenuControllerHost`
3. 按玩法需要挂（不是每图都要）：
   - `Door_To_NextMap`
   - `WeaponPickup_Freeze`
   - `WeaponPickup_Burn`
   - `WeaponPickup_Common`
   - `NPC_Shop`

---

## 2. 推荐场景树（可直接照着建）

### 2.1 MainMenu 场景

```text
MainMenu
├─ Main Camera
├─ EventSystem
├─ GameSystems
│  ├─ GameManager
│  └─ InputBindingsManager
└─ Canvas_MainMenu
   ├─ Title
   ├─ Btn_Play
   └─ Btn_Quit
```

### 2.2 Map_01 场景

```text
Map_01
├─ Main Camera
├─ EventSystem
├─ Player
├─ Canvas_Game
│  ├─ HUD
│  └─ PauseRoot (默认隐藏)
├─ GameSystems
│  ├─ SceneSpawnResolver
│  └─ PauseMenuControllerHost
├─ SpawnPoints
│  ├─ Spawn_Default_Map01
│  └─ Spawn_From_Map02
└─ Interactables
   ├─ Door_To_NextMap
   ├─ WeaponPickup_Freeze
   └─ NPC_Shop
```

### 2.3 Map_02 场景

```text
Map_02
├─ Main Camera
├─ EventSystem
├─ Player
├─ Canvas_Game
│  ├─ HUD
│  └─ PauseRoot (默认隐藏)
├─ GameSystems
│  ├─ SceneSpawnResolver
│  └─ PauseMenuControllerHost
├─ SpawnPoints
│  ├─ Spawn_Default_Map02
│  └─ Spawn_From_Map01
└─ Interactables
   ├─ Door_To_Map01
   ├─ WeaponPickup_Burn
   └─ WeaponPickup_Common
```

---

## 3. 对象逐个详细配置

## 3.1 GameSystems 下对象

### 3.1.1 GameManager（只放 1 次）

放置场景：`MainMenu`。

挂载方法：
1. 在 `MainMenu` 新建空物体 `GameSystems`。
2. 在 `GameSystems` 下新建空物体 `GameManager`。
3. 挂脚本 `GameManager`。

Inspector 配置：
1. `Level Scenes` 填你地图场景名，例如：
   - `Map_01`
   - `Map_02`
2. `Level Names` 可填中文关卡名，数量与 `Level Scenes` 对齐。
3. `Main Menu Scene Name` 填 `MainMenu`。
4. `Victory Scene Name` 填你的结算场景名（没有就先留默认但建议创建）。

注意：
1. 其他 scene 不要再手动放第二个 `GameManager`。
2. 脚本虽会去重，但会多创建再销毁，增加混乱。

### 3.1.2 InputBindingsManager（只放 1 次）

放置场景：`MainMenu`（与 `GameManager` 同场景）。

挂载方法：
1. 在 `GameSystems` 下新建空物体 `InputBindingsManager`。
2. 挂脚本 `InputBindingsManager`。

Inspector 配置：
1. `Input Actions` 绑定：`Assets/Input/GameInputActions.inputactions`。
2. `Dont Destroy On Load` 勾选。

### 3.1.3 SceneSpawnResolver（每个地图 scene 都要）

放置场景：`Map_01`、`Map_02`。

挂载方法：
1. 每个地图的 `GameSystems` 下新建 `SceneSpawnResolver`。
2. 挂脚本 `SceneSpawnResolver`。

配套对象（必须）：
1. 每个地图都要有 `SpawnPoints` 组。
2. 每个地图至少 1 个 `SpawnPoint` 对象勾 `Is Default Spawn`。
3. 通过门跳图时，要有对应 `spawnId` 给门填。

### 3.1.4 PauseMenuControllerHost（每个地图 scene 都要）

放置场景：`Map_01`、`Map_02`。

挂载方法：
1. 每个地图 `GameSystems` 下新建 `PauseMenuControllerHost`。
2. 挂脚本 `PauseMenuController`。

Inspector 配置：
1. `Pause Action` 绑定 `Gameplay/Pause`（默认 Esc）。
2. `Pause Root` 绑定到 `Canvas_Game/PauseRoot`。
3. `Pause Hint Text` 可选，绑定到 HUD 里提示文本。

PauseRoot 按钮绑定：
1. `Btn_Resume` -> `PauseMenuController.Resume()`
2. `Btn_MainMenu` -> `PauseMenuController.BackToMainMenu()`
3. `Btn_Quit` -> `PauseMenuController.QuitGame()`

---

## 3.2 Interactables 下对象

### 3.2.1 Door_To_NextMap（按需）

放置场景：门所在的源场景。

例子：
1. 在 `Map_01` 放 `Door_To_NextMap`，把玩家送到 `Map_02`。
2. 在 `Map_02` 放 `Door_To_Map01`，把玩家送回 `Map_01`。

挂载方法：
1. 创建门对象并加 `Collider2D`。
2. `Collider2D.isTrigger` 建议勾选（避免挡住角色）。
3. 挂 `DoorTransitionInteractable`。

Inspector 配置（Map_01 -> Map_02）：
1. `Interaction Title` = `进入`（随意）
2. `Target Scene Name` = `Map_02`
3. `Target Spawn Point Id` = `from_map01`

对应目标场景必须有：
1. `Map_02` 的某个 `SpawnPoint.spawnId = from_map01`

### 3.2.2 WeaponPickup_Freeze / Burn / Common（按需）

放置场景：你希望玩家能捡到它们的场景。

挂载方法（每个拾取物都一样）：
1. 新建对象 `WeaponPickup_xxx`。
2. 加 `Collider2D`（脚本会在运行时强制 `isTrigger = true`）。
3. 挂 `WeaponPickup` 脚本。

Inspector 关键项：
1. `Weapon Definition`：
   - `WeaponPickup_Freeze` 绑 `wpn_freeze.asset`
   - `WeaponPickup_Burn` 绑 `wpn_burn.asset`
   - `WeaponPickup_Common` 绑 `wpn_sword.asset` 或你的普通武器
2. `Auto Pickup On Trigger`：
   - 勾选 = 触碰自动拾取
   - 不勾 = 按 F 才拾取
3. `Interaction Title` 可填 `拾取武器`。

显示设置建议：
1. `Icon Renderer` 不填也行，脚本会尝试自动创建。
2. `Auto Create Icon Renderer` 保持勾选。

### 3.2.3 NPC_Shop（按需）

放置场景：有商店 NPC 的场景。

挂载方法：
1. 创建 NPC 对象 `NPC_Shop`。
2. 加 `Collider2D`（建议 trigger）。
3. 挂 `NpcShopInteractable`。

Inspector 配置：
1. `Interaction Title`：如 `交谈`。
2. `Npc Name`：如 `商人`。
3. `Enable Shop`：勾选。
4. `Dialogue Lines`：填几句对白。
5. `Shop Items`：逐项添加 `Weapon + Price`。

可选 UI（建议有）：
1. 在场景中挂一个 `NpcDialoguePanel` 用于显示对白。
2. 不挂也能运行，但只会打印日志。

---

## 4. 交互层设置（必须检查）

`PlayerInteractor` 会根据 `interactableLayer` 搜索可交互对象。

建议做法：
1. 新建 Layer：`Interactable`。
2. 把门、拾取物、NPC 全设到 `Interactable` 层。
3. 玩家 `PlayerInteractor.interactableLayer` 只勾 `Interactable`。

否则现象：
1. 对象看得到但按 F 没反应。
2. 只能偶发交互，稳定性差。

---

## 5. Build Settings 必做项

1. 打开 `File -> Build Settings...`。
2. 把 `MainMenu`、`Map_01`、`Map_02` 都加入 `Scenes In Build`。
3. 确保场景名与脚本里填的名字完全一致。

---

## 6. 哪些 scene 不需要挂这些对象

1. `MainMenu` 不需要：
   - `SceneSpawnResolver`
   - `PauseMenuControllerHost`
   - `Door` / `WeaponPickup` / `NPC_Shop`
2. 纯展示场景（如 `VictoryScreen`）通常也不需要上述交互对象。
3. 可游玩地图 scene 才需要：
   - `SceneSpawnResolver`
   - `PauseMenuControllerHost`
   - 以及你想放的交互对象。

---

## 7. 最小可运行配置（建议先跑通）

1. `MainMenu`：只放 `GameManager + InputBindingsManager`。
2. `Map_01`：放 `SceneSpawnResolver + PauseMenuControllerHost + Door_To_NextMap + 1个Spawn_Default`。
3. `Map_02`：放 `SceneSpawnResolver + PauseMenuControllerHost + 1个Spawn_Default + 1个Spawn_From_Map01`。
4. 跑通后再加 `WeaponPickup_*` 和 `NPC_Shop`。

---

## 8. 快速排错

1. 按 F 门没反应：
   - 检查门是否在 `interactableLayer` 内。
   - 检查 `Target Scene Name` 是否在 Build Settings。
2. 传送后出生点不对：
   - 检查 `Target Spawn Point Id` 与目标场景 `spawnId` 是否完全一致。
3. 暂停菜单不弹：
   - 检查 `Pause Root` 是否绑定。
   - 检查 `Pause Action` 是否绑定到 `Gameplay/Pause`。
4. 拾取物碰到没反应：
   - 检查 `weaponDefinition` 是否为空。
   - 检查玩家是否有 `PlayerWeaponInventory`（你的项目通常运行时会自动补）。
5. NPC 能交互但不显示对话框：
   - 场景里补 `NpcDialoguePanel`。
