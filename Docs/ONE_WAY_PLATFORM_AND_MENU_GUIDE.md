# 单向平台与菜单系统完整配置指南（新手版）

本文解决 3 件事：
1. 如何在单向平台上“蹲下并下跳”。
2. 为什么在单向平台上有时不能正常再次跳跃（以及如何修复）。
3. 如何做开始菜单（Start Menu）和暂停菜单（Resume/Quit）。

---

## 1. 本项目里“单向平台不能稳定二次跳”的原因

常见根因有两个：
1. `GroundCheck` 没把单向平台层算进地面层，导致角色站在平台上却被判定“未落地”，跳跃次数不重置。
2. “蹲+跳下穿”逻辑在普通地面也会触发，导致你按跳却没有真正起跳。

本项目已在 `Assets/Scripts/Player/PlayerController.cs` 中修复：
1. 地面检测自动合并 `groundLayer + oneWayPlatformLayer`。
2. 只有“脚下确实是单向平台”时，才执行下穿。
3. 蹲下读取支持 `Crouch Action` 与“下方向输入（S/下箭头/摇杆下）”。

---

## 2. 如何设置单向平台（可从下跳上、可从上掉下）

### 2.1 新建平台对象

1. 在 Hierarchy 里创建平台对象，例如 `OneWayPlatform_01`。
2. 给它加碰撞体：`BoxCollider2D`（或 `TilemapCollider2D`）。
3. 给它加组件：`PlatformEffector2D`。
4. 给它加脚本：`OneWayPlatformAuthoring`。

### 2.2 检查平台组件参数

在 `OneWayPlatform_01` 上确认：
1. `Collider2D -> Used By Effector` 已勾选。
2. `PlatformEffector2D -> Use One Way` 已勾选。
3. `PlatformEffector2D -> Surface Arc` 建议 `170`（默认脚本就是这个值）。
4. `Use Side Friction` 和 `Use Side Bounce` 可不勾。

### 2.3 设置平台 Layer（非常重要）

1. 新建 Layer：`OneWayPlatform`。
2. 把所有单向平台对象的 Layer 设为 `OneWayPlatform`。

### 2.4 玩家对象设置（PlayerController）

在玩家 `Player` 对象的 `PlayerController` 组件里：
1. `Ground Check`：拖一个玩家脚底子物体（建议叫 `GroundCheck`）。
2. `Ground Check Radius`：建议 `0.15 ~ 0.25`。
3. `Ground Layer`：勾选普通地面层（例如 `Ground`、`Default` 中你的地面层）。
4. `One Way Platform Layer`：勾选 `OneWayPlatform`。
5. `Drop Down Duration`：建议 `0.2 ~ 0.35`。

---

## 3. 如何“蹲下并下跳”

### 3.1 当前可用按键

默认支持：
1. 蹲下：`Left Ctrl` / `下箭头` / `S(下方向)`。
2. 跳跃：`Space`。
3. 下穿平台：站在单向平台上时，按住“蹲下”再按“跳跃”。

### 3.2 Input System 正确绑定方式

打开 `Assets/Input/GameInputActions.inputactions`：
1. 在 `Gameplay` Action Map 中确认有 `Crouch` Action（Button）。
2. 绑定建议至少有：
   - `<Keyboard>/leftCtrl`
   - `<Keyboard>/downArrow`
3. 可选：给手柄绑定 `leftStickPress`。
4. `Jump` Action 保持 `<Keyboard>/space`。

然后回到玩家对象 `PlayerController`：
1. `Move Action` 绑定到 `Gameplay/Move`。
2. `Jump Action` 绑定到 `Gameplay/Jump`。
3. `Crouch Action` 绑定到 `Gameplay/Crouch`。

---

## 4. 单向平台上无法再次跳起的排查清单

按顺序检查：
1. 玩家脚底 `GroundCheck` 是否在脚底附近（不要在身体中间或头顶）。
2. `GroundCheckRadius` 是否过小（过小会偶发检测不到地面）。
3. 单向平台 Layer 是否真的是 `OneWayPlatform`。
4. 玩家 `One Way Platform Layer` 是否勾到了 `OneWayPlatform`。
5. 物理碰撞矩阵里（Project Settings -> Physics2D）玩家层和平台层是否可碰撞。
6. 平台 Collider 是否启用、且 `Used By Effector` 已勾选。
7. 平台上是否有多个重叠碰撞器导致检测异常。

---

## 5. 开始菜单应该用 Scene 吗？

建议：**是，用独立 Scene。**

推荐结构：
1. `MainMenu.unity`：开始菜单场景。
2. `SampleScene.unity`（或你的关卡场景）：实际游戏场景。

这样最清晰，后续扩展（设置、存档、教程）也更容易。

---

## 6. 开始菜单（Play / Quit）完整步骤

### 6.1 新建场景

1. 在 `Assets/Scenes` 下创建：
   - `MainMenu.unity`
   - `SampleScene.unity`（或你已有的主关卡）
2. 打开 `File -> Build Settings...`，把两个场景都加入 `Scenes In Build`。
3. 建议把 `MainMenu` 放到列表第一个（索引 0）。

### 6.2 创建 MainMenu 场景对象

`MainMenu` 场景建议树：

```text
MainMenu
├─ Main Camera
├─ EventSystem (InputSystemUIInputModule)
├─ Systems
│  └─ GameManager (挂 GameManager 脚本)
└─ Canvas_MainMenu
   └─ Panel
      ├─ Title (TMP_Text)
      ├─ Btn_Play (Button)
      │  └─ Label (TMP_Text)
      └─ Btn_Quit (Button)
         └─ Label (TMP_Text)
```

### 6.3 挂载脚本并绑定按钮

1. 在 `Canvas_MainMenu`（或单独空物体）挂 `MainMenuUI` 脚本。
2. `MainMenuUI.submitAction` 可绑定到 `UI/Submit`（可选，不绑也能点击按钮）。
3. `Btn_Play -> OnClick()` 绑定 `MainMenuUI.PlayGame()`。
4. `Btn_Quit -> OnClick()` 绑定 `MainMenuUI.QuitGame()`。

### 6.4 GameManager 场景名配置

在 `Systems/GameManager` 组件中：
1. `Main Menu Scene Name` 填 `MainMenu`。
2. `Level Scenes` 至少填一个，例如 `SampleScene`。
3. 场景名要和 Build Settings 里的场景名一致（大小写也建议一致）。

---

## 7. 暂停菜单（Resume / MainMenu / Quit）完整步骤

### 7.1 在游戏场景创建 UI

在 `SampleScene`（或你的关卡）创建：

```text
SampleScene
├─ Player
├─ Main Camera
├─ EventSystem (InputSystemUIInputModule)
├─ Systems
│  └─ PauseMenuControllerHost (挂 PauseMenuController)
└─ Canvas_Game
   ├─ HUD...
   └─ PauseRoot (默认 SetActive=false)
      └─ Panel
         ├─ Title (TMP_Text)
         ├─ Btn_Resume (Button)
         ├─ Btn_MainMenu (Button)
         └─ Btn_Quit (Button)
```

### 7.2 绑定 PauseMenuController

在 `PauseMenuControllerHost` 上：
1. 挂 `PauseMenuController` 脚本。
2. `Pause Root` 拖到 `Canvas_Game/PauseRoot`。
3. `Pause Hint Text` 可拖 HUD 上提示文本（可选）。
4. `Pause Action` 绑定 `Gameplay/Pause`（默认 `Esc`）。

### 7.3 绑定按钮事件

1. `Btn_Resume -> OnClick()` 绑定 `PauseMenuController.Resume()`。
2. `Btn_MainMenu -> OnClick()` 绑定 `PauseMenuController.BackToMainMenu()`。
3. `Btn_Quit -> OnClick()` 绑定 `PauseMenuController.QuitGame()`。

运行时逻辑：
1. 按 `Esc` 打开/关闭暂停菜单。
2. 暂停时 `Time.timeScale = 0`。
3. `Resume` 恢复 `Time.timeScale = 1`。
4. `Quit` 在编辑器会停止 Play，打包后会退出程序。

---

## 8. 最后测试流程（建议逐条勾选）

1. 进入 `MainMenu`，点击 `Play` 能进入 `SampleScene`。
2. 在关卡按 `Esc` 能打开暂停面板。
3. 点击 `Resume` 能继续游戏。
4. 点击 `MainMenu` 能回到 `MainMenu`。
5. 站在单向平台上，按住蹲下 + 跳跃，角色能下穿。
6. 从单向平台起跳后，落回平台，跳跃次数能正确重置。
7. 在普通地面按蹲下 + 跳跃，角色应正常起跳（不会错误触发下穿）。
