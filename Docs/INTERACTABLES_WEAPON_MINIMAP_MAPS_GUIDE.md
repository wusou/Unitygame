# Interactables / 武器 / 小地图 / 多地图 实操指南

本文对应项目路径：`F:\Documents\Playground\demo`

## 1. 你新增得到的功能

- 背包支持 1~4 号槽切换
- 按 `G` 丢弃当前武器
- 武器可设置“不可丢弃”
- 保留按 `Q` 循环切换武器
- 可继续使用 `F` 与门、NPC、武器拾取物交互

## 2. 如何使用 Interactables（可交互物）

## 2.1 统一规则

项目里所有可交互物都实现了 `IInteractable` 接口，玩家通过 `PlayerInteractor` 在范围内按 `F` 触发最近对象。

你要保证两件事：

1. 玩家上有 `PlayerInteractor`
2. 可交互对象在 `PlayerInteractor.interactableLayer` 可检测的层里

## 2.2 武器拾取物（WeaponPickup）

对象配置：

- GameObject：`WeaponPickup_*`
- 组件：
  - `Collider2D`（`Is Trigger` 勾选）
  - `WeaponPickup`

`WeaponPickup` 参数：

- `Weapon Definition`：要拾取的武器资产
- `Auto Pickup On Trigger`：
  - `true`：碰到自动捡
  - `false`：必须按 `F`

## 2.3 门交互（DoorTransitionInteractable）

对象配置：

- GameObject：`Door_*`
- 组件：
  - `Collider2D`（建议 Trigger）
  - `DoorTransitionInteractable`

参数：

- `Target Scene Name`：目标场景名（例如 `Map_Forest`）
- `Target Spawn Point Id`：目标出生点 ID（例如 `entry_from_town`）

交互：玩家靠近门按 `F` 切图。

## 2.4 NPC 交互（NpcShopInteractable）

对象配置：

- GameObject：`NPC_Shop_*`
- 组件：
  - `Collider2D`
  - `NpcShopInteractable`

参数：

- `Dialogue Lines`：对话内容
- `Enable Shop`：是否启用商店
- `Shop Items`：可购买武器与价格

## 3. 如何制作武器（给策划/美术/程序协作）

## 3.1 新建武器资产

1. Project 面板右键
2. `Create > Demo > Combat > Weapon Definition`
3. 命名，例如 `WPN_FreezeBow`

## 3.2 填基础字段

- `Weapon Id`：唯一 ID
- `Display Name`：显示名
- `Description`：描述
- `Icon`：图标
- `Can Drop`：是否可丢弃（关键）

## 3.3 填战斗字段

- `Attack Mode`：
  - `Melee` 近战
  - `Projectile` 投射
- `Base Damage`：基础伤害
- `Range`：范围（近战半径）
- `Cooldown`：冷却
- `Projectile Prefab`：投射武器需要

## 3.4 挂武器效果（可选）

在 `Modifiers` 列表添加效果资产：

- `FreezeWeaponModifier`：减速冻结
- `BurnWeaponModifier`：持续灼烧

如果你们要扩展新效果：继承 `WeaponModifier`，重写 `OnHit` 或 `OnProjectileSpawn`。

## 4. 如何使用武器（玩家侧）

- 攻击：
  - 近战：`J`（或绑定的 `Melee`）
  - 远程：`K`（或绑定的 `Bow`）
- 切换：
  - `Q`：循环切换
  - `1/2/3/4`：直接切到指定槽位
- 丢弃：
  - `G`：丢弃当前武器
  - 若该武器 `Can Drop = false`，会提示不可丢弃

注意：如果背包槽位数小于 4，则按超出范围的数字键不会生效。

## 5. 如何使用小地图

## 5.1 场景对象结构

```text
Canvas 或 UIRoot
└─ MiniMapRoot (挂 MiniMapExplorationController)
   ├─ Cell_01 (挂 MiniMapCell)
   │  └─ FogMask
   ├─ Cell_02 (挂 MiniMapCell)
   │  └─ FogMask
   └─ Cell_03 (挂 MiniMapCell)
      └─ FogMask
```

## 5.2 参数

`MiniMapExplorationController`：

- `Player`：拖入玩家
- `Reveal Radius`：玩家靠近多远点亮格子
- `Refresh Interval`：检测间隔

`MiniMapCell`：

- `Fog Mask`：未探索遮罩对象
- `Discovered On Start`：开局是否已点亮

## 5.3 运行逻辑

玩家进入范围后，`MiniMapCell` 会把 `FogMask` 关闭，实现“探索后点亮”。

## 6. 如何制作不同地图（是的，通常用不同 Scene）

结论：通常就是创建多个 Scene，每个 Scene 一张地图。

推荐流程：

1. `Assets/Scenes` 下创建新场景，例如：
   - `Map_Town.scene`
   - `Map_Forest.scene`
2. 每个场景都放：
   - `Player`（或可复用统一玩家生成逻辑）
   - `SceneSpawnResolver`
   - 多个 `SpawnPoint`
3. 在 `Build Settings` 把这些 Scene 全部加入
4. 在门对象上配置 `DoorTransitionInteractable.Target Scene Name`
5. 用 `Target Spawn Point Id` 指定进门后出生点

## 7. 如何通过门切换地图（完整）

以从 `Map_Town` 进 `Map_Forest` 为例：

1. 在 `Map_Forest` 建 `SpawnPoint`：
   - `Spawn Id = entry_from_town`
2. 在 `Map_Town` 门对象挂 `DoorTransitionInteractable`：
   - `Target Scene Name = Map_Forest`
   - `Target Spawn Point Id = entry_from_town`
3. 玩家靠近门按 `F`
4. 进入 `Map_Forest` 后 `SceneSpawnResolver` 根据 ID 把玩家放到对应出生点

## 8. 输入绑定清单（建议）

- `Interact` -> `F`
- `SwitchWeapon` -> `Q`
- `WeaponSlot1~4` -> `1~4`
- `DropWeapon` -> `G`

如果你用手柄，也可在 `GameInputActions.inputactions` 中给这些动作加手柄绑定。

## 9. 常见问题

## 9.1 为什么按 1~4 没反应？

- 背包没有对应数量武器
- `PlayerWeaponInventory.maxWeaponSlots` 太小
- 玩家当前没有 `PlayerWeaponInventory` 组件

## 9.2 为什么 G 不能丢？

- 当前没有武器
- 当前武器 `Can Drop = false`

## 9.3 门切图失败？

- 目标场景没进 Build Settings
- `Target Scene Name` 拼写与场景名不一致
- 目标场景没放 `SceneSpawnResolver` 或 `SpawnPoint`
