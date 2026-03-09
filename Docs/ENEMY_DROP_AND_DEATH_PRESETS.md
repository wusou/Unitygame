# 敌人掉落与死亡动画参数模板（可直接抄）

适用项目：`F:\Documents\Playground\demo`

对应脚本：

- 敌人掉落：`Assets/Scripts/Enemies/EnemyLootDrop.cs`
- 玩家死亡动画：`Assets/Scripts/Player/PlayerHealth.cs`
- 箭矢轨迹：`Assets/Scripts/Projectiles/Arrow.cs`

---

## 1. 敌人掉落参数模板

## 1.1 普通怪（稳定掉 0~1，偏保守）

适用：小兵、基础巡逻怪。

`EnemyLootDrop` 推荐：

- `Weapon Drop Chance` = `0.20`
- `Min Drop Count` = `1`
- `Max Drop Count` = `1`
- `Avoid Duplicate In One Death` = `true`
- `Drop Offset X Range` = `(-0.5, 0.5)`
- `Drop Offset Y Range` = `(0.0, 0.25)`
- `Auto Pickup Dropped Weapon` = `false`

掉落池建议（Weapon Candidates）：

- 常见武器 2~3 把
- 不放稀有武器

## 1.2 精英怪（掉落更明显）

适用：血量高、技能特殊的精英怪。

`EnemyLootDrop` 推荐：

- `Weapon Drop Chance` = `0.65`
- `Min Drop Count` = `1`
- `Max Drop Count` = `2`
- `Avoid Duplicate In One Death` = `true`
- `Drop Offset X Range` = `(-0.9, 0.9)`
- `Drop Offset Y Range` = `(0.0, 0.35)`
- `Auto Pickup Dropped Weapon` = `false`

掉落池建议：

- 常见武器 + 1 把稀有武器

## 1.3 Boss（高确定性奖励）

适用：关底/Boss。

`EnemyLootDrop` 推荐：

- `Weapon Drop Chance` = `1.0`
- `Min Drop Count` = `2`
- `Max Drop Count` = `3`
- `Avoid Duplicate In One Death` = `true`
- `Drop Offset X Range` = `(-1.5, 1.5)`
- `Drop Offset Y Range` = `(0.1, 0.6)`
- `Auto Pickup Dropped Weapon` = `false`

掉落池建议：

- 稀有/特效武器优先
- 可加 1 把不可丢弃的剧情武器（`Can Drop=false`）

---

## 2. 如何配置“掉落范围”（新手步骤）

1. 选中敌人对象（例如 `Enemy_Brute_01`）。
2. 确保挂了 `EnemyLootDrop`。
3. 在 `Weapon Candidates` 填入武器资产列表。
4. 按敌人类型套用上面的参数模板。
5. 点 Play 测试：
   - 敌人死亡后是否在你设定的 X/Y 范围内掉落
   - 掉落数量是否在 `Min~Max` 之间

如果掉落“挤在一起”不好看：

- 增大 `Drop Offset X Range`
- 略增 `Drop Offset Y Range`

---

## 3. 玩家死亡动画模板（几秒后消失）

脚本：`PlayerHealth`

## 3.1 标准死亡（2秒）

- `Death Trigger` = `Die`
- `Death Animation Duration` = `2.0`
- `Disappear Delay` = `0.0`
- `Hide After Death` = `true`
- `Respawn After Death` = `true`

## 3.2 慢节奏死亡（3.5秒）

- `Death Trigger` = `Die`
- `Death Animation Duration` = `3.5`
- `Disappear Delay` = `0.3`
- `Hide After Death` = `true`
- `Respawn After Death` = `true`

## 3.3 调试模式（不重生，只看动画）

- `Death Trigger` = `Die`
- `Death Animation Duration` = `2.0`
- `Hide After Death` = `false`
- `Respawn After Death` = `false`

---

## 4. Arrow 轨迹视觉模板

脚本：`Arrow`

## 4.1 轻量轨迹（推荐）

- `Enable Trail` = `true`
- `Trail Time` = `0.15`
- `Trail Start Width` = `0.08`
- `Trail End Width` = `0.02`
- `Destroy Delay On Hit` = `0.02`

## 4.2 强烈轨迹（法术箭）

- `Enable Trail` = `true`
- `Trail Time` = `0.25`
- `Trail Start Width` = `0.12`
- `Trail End Width` = `0.03`
- `Destroy Delay On Hit` = `0.08`

---

## 5. 一键验收清单

- [ ] 普通怪掉率明显低于精英怪
- [ ] Boss 必掉或高概率掉落
- [ ] 掉落物不会重叠在同一点（有散布）
- [ ] 玩家死亡会先播动画再隐藏
- [ ] 隐藏后按设定重生
- [ ] 箭命中后不会穿透多个敌人
- [ ] 箭飞行中有可见轨迹

---

## 6. 小提醒（避免常见坑）

1. `Weapon Candidates` 里不要留空元素（Null），否则会出现这次没掉到任何有效物品。
2. `Min Drop Count` 不要大于掉落池有效武器数量（在 `Avoid Duplicate In One Death=true` 时尤其要注意）。
3. 如果箭轨迹不显示，检查 Arrow 预制体是否被极短生命周期提前销毁。
4. 如果死亡动画不触发，优先检查 Animator 中是否存在 `Die` Trigger 参数和对应状态机过渡。
