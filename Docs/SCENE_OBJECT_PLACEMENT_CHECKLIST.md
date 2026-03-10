# 场景对象挂载检查表（打勾版）

## A. 全局只挂一次（MainMenu）

- [ ] `MainMenu` 场景有 `GameSystems/GameManager`。
- [ ] `GameManager.levelScenes` 已填：`Map_01`、`Map_02`。
- [ ] `GameManager.mainMenuSceneName = MainMenu`。
- [ ] `GameSystems/InputBindingsManager` 已挂。
- [ ] `InputBindingsManager.inputActions` 指向 `Assets/Input/GameInputActions.inputactions`。
- [ ] `InputBindingsManager.dontDestroyOnLoad` 已勾选。

## B. 每个地图场景都要有

### B1. Map_01

- [ ] `Map_01/GameSystems/SceneSpawnResolver` 已挂。
- [ ] `Map_01/GameSystems/PauseMenuControllerHost` 已挂 `PauseMenuController`。
- [ ] `PauseMenuController.pauseRoot` 已绑定到 `Canvas_Game/PauseRoot`。
- [ ] `PauseMenuController.pauseAction` 已绑定 `Gameplay/Pause`（或确认 Esc fallback 可用）。

### B2. Map_02

- [ ] `Map_02/GameSystems/SceneSpawnResolver` 已挂。
- [ ] `Map_02/GameSystems/PauseMenuControllerHost` 已挂 `PauseMenuController`。
- [ ] `PauseMenuController.pauseRoot` 已绑定到 `Canvas_Game/PauseRoot`。
- [ ] `PauseMenuController.pauseAction` 已绑定 `Gameplay/Pause`。

## C. SpawnPoint 配置

### C1. Map_01

- [ ] `Spawn_Default_Map01` 已挂 `SpawnPoint`。
- [ ] `Spawn_Default_Map01.isDefaultSpawn = true`。
- [ ] `Spawn_From_Map02` 已挂 `SpawnPoint`。
- [ ] `Spawn_From_Map02.spawnId` 已填写且唯一。

### C2. Map_02

- [ ] `Spawn_Default_Map02` 已挂 `SpawnPoint`。
- [ ] `Spawn_Default_Map02.isDefaultSpawn = true`。
- [ ] `Spawn_From_Map01` 已挂 `SpawnPoint`。
- [ ] `Spawn_From_Map01.spawnId = from_map01`（示例）。

## D. Door 对象（按需）

### D1. Map_01 -> Map_02

- [ ] `Door_To_NextMap` 在 `Map_01`。
- [ ] 门对象有 `Collider2D`。
- [ ] `Collider2D.isTrigger` 已勾。
- [ ] 挂了 `DoorTransitionInteractable`。
- [ ] `targetSceneName = Map_02`。
- [ ] `targetSpawnPointId = from_map01`（与 Map_02 对应 SpawnPoint 一致）。

### D2. Map_02 -> Map_01（如果要回去）

- [ ] 有 `Door_To_Map01`。
- [ ] `targetSceneName = Map_01`。
- [ ] `targetSpawnPointId` 对应 Map_01 的入口 SpawnPoint。

## E. WeaponPickup 对象（按需）

- [ ] `WeaponPickup_Freeze` 已挂 `WeaponPickup`。
- [ ] `WeaponPickup_Freeze.weaponDefinition = wpn_freeze.asset`。
- [ ] `WeaponPickup_Burn` 已挂 `WeaponPickup`。
- [ ] `WeaponPickup_Burn.weaponDefinition = wpn_burn.asset`。
- [ ] `WeaponPickup_Common` 已挂 `WeaponPickup`。
- [ ] `WeaponPickup_Common.weaponDefinition = wpn_sword.asset`（或你的 common 武器）。
- [ ] 每个拾取物都有 `Collider2D`。
- [ ] `autoPickupOnTrigger` 按你需求设置。

## F. NPC_Shop（按需）

- [ ] `NPC_Shop` 对象有 `Collider2D`。
- [ ] 挂了 `NpcShopInteractable`。
- [ ] `enableShop` 已勾。
- [ ] `shopItems` 已填武器和价格。
- [ ] `dialogueLines` 已填对白。
- [ ] 场景中有 `NpcDialoguePanel`（建议）。

## G. Interactable Layer（强烈建议统一）

- [ ] 已创建 Layer：`Interactable`。
- [ ] 门、拾取物、NPC 都在 `Interactable` 层。
- [ ] `PlayerInteractor.interactableLayer` 只勾 `Interactable`。

## H. Build Settings

- [ ] `MainMenu` 已加入 Scenes In Build。
- [ ] `Map_01` 已加入 Scenes In Build。
- [ ] `Map_02` 已加入 Scenes In Build。
- [ ] 场景名与 Inspector 里填写完全一致。

## I. 运行验证

- [ ] MainMenu 点击 Play 能进 Map_01。
- [ ] Map_01 门能把玩家送到 Map_02。
- [ ] Map_02 返回门能把玩家送回 Map_01（如已配置）。
- [ ] Esc 能打开暂停菜单。
- [ ] Resume 能恢复，MainMenu 能回菜单，Quit 能退出。
- [ ] Freeze/Burn/Common 拾取正常。
- [ ] NPC 商店可交互并能购买武器。
