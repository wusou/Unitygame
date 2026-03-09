# Demo 项目重构使用文档（新手版）

## 1. 本次重构范围

已恢复并标准化以下模块：

1. 武器系统：武器栏上限、拾取、切换、武器效果扩展接口（冻结/灼烧）
2. 玩家动作：蹲下、蹲走、蹲+跳穿下单向平台
3. 敌人系统：游走/追击范围限制、死亡掉武器、状态效果
4. 交互系统：按 F 统一交互（门、拾取、NPC）
5. 场景切换：门触发切图 + 出生点定位
6. 小地图：探索点亮
7. NPC：对话 + 商店购买
8. 文档：新手可执行步骤与检查表

## 2. 代码结构

```text
Assets/
├─ Input/
│  └─ GameInputActions.inputactions
└─ Scripts/
   ├─ Camera/
   │  └─ CameraFlow.cs
   ├─ Combat/
   │  ├─ WeaponAttackMode.cs
   │  ├─ WeaponDefinition.cs
   │  ├─ WeaponModifier.cs
   │  ├─ ProjectileEffectPayload.cs
   │  └─ Modifiers/
   │     ├─ FreezeWeaponModifier.cs
   │     └─ BurnWeaponModifier.cs
   ├─ Enemies/
   │  ├─ EnemyBase.cs
   │  ├─ EnemyStatusController.cs
   │  ├─ EnemyLootDrop.cs
   │  ├─ MeleeEnemy.cs
   │  ├─ RangedEnemy.cs
   │  ├─ BruteEnemy.cs
   │  └─ DasherEnemy.cs
   ├─ Interactables/
   │  ├─ IInteractable.cs
   │  ├─ WeaponPickup.cs
   │  ├─ DoorTransitionInteractable.cs
   │  └─ SceneTransitionContext.cs
   ├─ MiniMap/
   │  ├─ MiniMapCell.cs
   │  └─ MiniMapExplorationController.cs
   ├─ NPC/
   │  └─ NpcShopInteractable.cs
   ├─ Player/
   │  ├─ PlayerController.cs
   │  ├─ PlayerCombat.cs
   │  ├─ PlayerWeaponInventory.cs
   │  ├─ PlayerInteractor.cs
   │  ├─ PlayerWallet.cs
   │  └─ PlayerHealth.cs
   ├─ Projectiles/
   │  ├─ Arrow.cs
   │  └─ ProjectileOwner.cs
   ├─ UI/
   │  └─ NpcDialoguePanel.cs
   └─ World/
      ├─ SpawnPoint.cs
      ├─ SceneSpawnResolver.cs
      └─ OneWayPlatformAuthoring.cs
```

## 3. 场景树（推荐）

```text
SampleScene
├─ GameSystems
│  ├─ GameManager
│  ├─ InputBindingsManager
│  └─ SceneSpawnResolver
├─ Player
│  ├─ GroundCheck
│  ├─ AttackPoint
│  └─ ArrowSpawn
├─ Enemies
│  ├─ Enemy_Melee_01
│  ├─ Enemy_Ranged_01
│  ├─ Enemy_Brute_01
│  └─ Enemy_Dasher_01
├─ Interactables
│  ├─ Door_To_NextMap
│  ├─ WeaponPickup_Freeze
│  ├─ WeaponPickup_Burn
│  └─ NPC_Shop
├─ Level
│  ├─ Ground
│  ├─ OneWayPlatforms
│  └─ SpawnPoints
│     ├─ Spawn_Default
│     └─ Spawn_DoorA
├─ UI
│  ├─ HUD
│  ├─ NPCDialoguePanel
│  └─ MiniMapRoot
└─ Main Camera
```

## 4. 重点对象配置

### Player

必须组件：

- Rigidbody2D（Dynamic，冻结 Z 旋转）
- Collider2D
- SpriteRenderer
- Animator
- PlayerController
- PlayerCombat
- PlayerWeaponInventory
- PlayerInteractor
- PlayerWallet
- PlayerHealth

子对象：

- GroundCheck：脚底检测点
- AttackPoint：近战判定点
- ArrowSpawn：投射物生成点

### Enemy

每个敌人至少有：

- Rigidbody2D / Collider2D / SpriteRenderer
- 敌人脚本（Melee/Ranged/Brute/Dasher）
- EnemyLootDrop（配置掉落概率与候选武器）

### Door（门）

- Collider2D（建议 Trigger）
- DoorTransitionInteractable
  - Target Scene Name
  - Target Spawn Point Id

### SpawnPoint（出生点）

- SpawnPoint 组件
  - Spawn Id
  - Is Default Spawn

### 单向平台

- Collider2D
- PlatformEffector2D
- OneWayPlatformAuthoring

### NPC

- Collider2D
- NpcShopInteractable
  - Dialogue Lines
  - Enable Shop
  - Shop Items

### 小地图

- MiniMapRoot：挂 MiniMapExplorationController
- 每个格子：挂 MiniMapCell + FogMask

## 5. Input System 配置（完整步骤）

1. 打开 `Assets/Input/GameInputActions.inputactions`
2. 在 `Gameplay` map 确认动作：
- Move
- Jump
- Melee
- Bow
- SwitchWeapon
- Crouch
- Interact
- Pause

3. 推荐按键：
- Move：WASD/方向键
- Jump：Space
- Melee：J / 鼠标左键
- Bow：K / 鼠标右键
- SwitchWeapon：Q
- Crouch：LeftCtrl / 下方向
- Interact：F

4. 组件绑定：
- PlayerController
  - Move Action -> Gameplay/Move
  - Jump Action -> Gameplay/Jump
  - Crouch Action -> Gameplay/Crouch
- PlayerCombat
  - Melee Action -> Gameplay/Melee
  - Bow Action -> Gameplay/Bow
  - Switch Weapon Action -> Gameplay/SwitchWeapon
- PlayerInteractor
  - Interact Action -> Gameplay/Interact

5. 打开 `Edit > Project Settings > Player > Active Input Handling`，选择 `Input System Package (New)` 或 `Both`

## 6. TMP 中文显示

1. 导入中文字体（如 NotoSansSC）
2. `Window > TextMeshPro > Font Asset Creator` 生成字体资产
3. Atlas 模式建议开发期用 Dynamic
4. 把生成字体设置到 UI 的 TMP_Text（如 NPC 对话文本）
5. 在 `Window > TextMeshPro > Settings` 增加 Fallback Font

## 7. 验收顺序

1. 玩家移动/跳跃/蹲走正常
2. 蹲+跳可下穿单向平台
3. 按 F 可拾取、开门、与 NPC 交互
4. 武器上限和切换正常
5. 冻结/灼烧效果生效
6. 敌人巡逻、追击、回拉限制正常
7. 敌人死亡掉落武器
8. 小地图探索点亮正常
9. 中文 UI 无方块
