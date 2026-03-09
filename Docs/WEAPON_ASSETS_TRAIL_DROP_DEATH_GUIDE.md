# 武器素材 + 远程轨迹 + 掉落范围 + 角色死亡动画配置指南

项目路径：`F:\Documents\Playground\demo`

## 1. 已提供的临时武器素材

已生成在：`Assets/Art/TempWeapons`

- `icon_sword_temp.png`
- `icon_bow_temp.png`
- `icon_staff_temp.png`
- `icon_relic_temp.png`
- `projectile_arrow_temp.png`

用途建议：

- `icon_*`：拖到 `WeaponDefinition.Icon`
- `projectile_arrow_temp.png`：用于 Arrow 预制体的 Sprite

## 2. 在 Unity 中正确导入素材

1. 在 Project 面板选中上述 PNG。
2. Inspector 设置：
   - `Texture Type` = `Sprite (2D and UI)`
   - `Sprite Mode` = `Single`
   - `Pixels Per Unit` = 100（或按你项目）
3. 点击 `Apply`。

## 3. 如何让远程武器显示箭矢轨迹

你现在的 `Arrow.cs` 已支持 TrailRenderer 自动生成。

脚本位置：

- `Assets/Scripts/Projectiles/Arrow.cs`

关键参数（在 Arrow 预制体 Inspector 可调）：

- `Enable Trail`：是否开启轨迹
- `Trail Time`：轨迹保留时长（建议 0.1~0.25）
- `Trail Start Width` / `Trail End Width`：轨迹粗细
- `Trail Start Color` / `Trail End Color`：轨迹颜色渐变

建议初始值：

- Trail Time = `0.15`
- Start Width = `0.08`
- End Width = `0.02`

## 4. 箭击中敌人后消失

已实现：

- 箭命中后立刻停止移动
- 关闭碰撞体，避免重复命中
- 关闭轨迹发射
- `Destroy(gameObject, destroyDelayOnHit)`

可调参数：

- `Destroy Delay On Hit`（默认 `0.02`）

如果你希望“命中后停留一瞬再消失”，把它调到 `0.15~0.3`。

## 5. 敌人死亡掉落“物品范围”如何配置

脚本位置：

- `Assets/Scripts/Enemies/EnemyLootDrop.cs`

现在可配置项：

1. 概率
- `Weapon Drop Chance`：本次死亡是否触发掉落

2. 掉落池范围（掉什么）
- `Weapon Candidates`：可掉落武器列表

3. 数量范围（掉几个）
- `Min Drop Count`
- `Max Drop Count`
- `Avoid Duplicate In One Death`：一次死亡是否避免重复物品

4. 空间范围（掉在哪）
- `Drop Offset X Range`：左右散布范围
- `Drop Offset Y Range`：上下散布范围

5. 拾取行为
- `Auto Pickup Dropped Weapon`：掉落后是否碰撞即自动拾取

示例配置（适合测试）：

- Drop Chance = `1`
- Min Drop Count = `1`
- Max Drop Count = `2`
- X Range = `(-1.2, 1.2)`
- Y Range = `(0, 0.5)`

## 6. 配置角色死亡后播放几秒动画再消失

脚本位置：

- `Assets/Scripts/Player/PlayerHealth.cs`

已支持参数：

- `Animator`：角色 Animator
- `Death Trigger`：死亡触发器（默认 `Die`）
- `Death Animation Duration`：播放动画秒数
- `Disappear Delay`：隐藏后额外等待秒数
- `Hide After Death`：是否隐藏角色
- `Respawn After Death`：是否自动重生

建议配置：

- Death Trigger = `Die`
- Death Animation Duration = `2.0`
- Hide After Death = `true`
- Respawn After Death = `true`

Animator 里要做的事：

1. 添加参数 `Trigger: Die`
2. 从 `Any State` 或主状态到 `Death` 动画状态
3. 过渡条件为 `Die`
4. `Death` 动画长度建议与 `Death Animation Duration` 接近

## 7. 不可丢弃武器设置

脚本：`WeaponDefinition`

在每个武器资产上设置：

- `Can Drop = false`：不可丢弃
- `Can Drop = true`：可丢弃

按 `G` 丢弃时，若不可丢弃会被拒绝并输出提示。

## 8. 快速验收清单

- [ ] 箭发射时看得到轨迹
- [ ] 箭击中敌人后立刻消失
- [ ] 敌人死亡掉落在你配置的散布范围内
- [ ] 死亡动画播放 N 秒后角色隐藏
- [ ] 角色按设定自动重生（或不重生）
- [ ] 不可丢弃武器无法被 G 丢弃

## 9. 背包窗格放到屏幕中下部（适中）

已新增脚本：`Assets/Scripts/UI/BackpackPanelLayout.cs`

使用步骤：

1. 在 Canvas 下找到你的背包 Panel（例如 `BackpackPanel`）。
2. 挂上 `BackpackPanelLayout`。
3. 保持默认参数即可：
   - `Anchored Position = (0, 36)`
   - `Panel Size = (760, 220)`
4. 点击组件右上角菜单或右键执行 `Apply Backpack Layout`。
5. 运行后背包会固定在屏幕中下部。

如果面板太大或太小：

- 调 `Panel Size`，例如 `680x200` 或 `820x240`。
- 位置太高/太低调 `Anchored Position.y`。

## 10. 如何使用武器素材（从导入到生效）

素材路径：`Assets/Art/TempWeapons`

1. 选中 `icon_*.png` 与 `projectile_arrow_temp.png`。
2. Inspector 设置：
   - `Texture Type = Sprite (2D and UI)`
   - `Sprite Mode = Single`
   - 点击 `Apply`
3. 新建或打开武器资产（`WeaponDefinition`）：
   - `Icon` 拖入 `icon_*.png`
   - 远程武器把 `Projectile Prefab` 指向你的箭预制体
4. 打开箭预制体：
   - `SpriteRenderer.sprite` 设为 `projectile_arrow_temp.png`
   - 保证挂了 `Arrow.cs`
5. 运行测试：
   - 拾取武器后，背包 UI 槽位会显示对应 Icon
   - 远程攻击会发射该箭精灵
