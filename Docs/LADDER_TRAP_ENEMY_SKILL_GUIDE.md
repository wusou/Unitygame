# LADDER_TRAP_ENEMY_SKILL_GUIDE

## 1. 本次新增脚本总览

```text
Assets/Scripts
├─ Player
│  └─ PlayerController.cs               (新增梯子攀爬逻辑)
├─ World
│  ├─ LadderAuthoring.cs                (梯子碰撞器一键配置)
│  └─ Traps
│     ├─ TrapContext.cs                 (陷阱效果上下文)
│     ├─ TrapEffect.cs                  (陷阱效果扩展接口)
│     ├─ TrapDefinition.cs              (陷阱定义)
│     ├─ TrapZone.cs                    (陷阱触发区)
│     └─ DamageTrapEffect.cs            (掉血陷阱示例)
└─ Enemies
   ├─ EnemyBase.cs                      (接入技能控制器)
   └─ Skills
      ├─ EnemySkillContext.cs           (技能上下文)
      ├─ EnemySkillEffect.cs            (技能效果扩展接口)
      ├─ EnemySkillDefinition.cs        (技能定义)
      ├─ EnemySkillController.cs        (技能施放控制器)
      └─ EnemyDamageSkillEffect.cs      (掉血技能示例)
```

## 2. 可爬梯子地形

### 2.1 梯子对象怎么建
1. 在场景中创建一个梯子对象（可以是空物体或带 Sprite 的物体）。
2. 给梯子加 `BoxCollider2D`（覆盖整个可攀爬区域）。
3. 给梯子加 `LadderAuthoring`。
4. `LadderAuthoring.forceTrigger` 保持勾选（会自动把碰撞器设为 Trigger）。

### 2.2 玩家怎么配置
1. 选中 Player。
2. 在 `PlayerController` 中设置：
- `Ladder Layer`：建议单独建一个 `Ladder` 层并赋给梯子。
- `Climb Speed`：上下攀爬速度。
- `Horizontal While Climbing Multiplier`：爬梯时水平移动比例（建议 0.3~0.6）。
3. 运行后测试：
- 进入梯子触发区后，按 `W/S`（或上下）开始爬。
- 在梯子上按跳跃键会跳离梯子。

## 3. 陷阱地形系统（可扩展接口）

### 3.1 先创建陷阱效果资产（示例：掉血）
1. 右键 Project -> `Create/Demo/World/Trap Effects/Damage Trap Effect`。
2. 配置参数：
- `Damage On Enter`：进入触发时伤害。
- `Damage On Stay`：停留每次 Tick 伤害（配合 TrapZone 的 Tick 间隔）。
- `Damage On Exit`：离开触发时伤害。
- `Damage Player / Damage Enemy`：影响对象。

### 3.2 创建陷阱定义
1. 右键 Project -> `Create/Demo/World/Trap Definition`。
2. 在 `Effects` 列表里添加上一步的 `DamageTrapEffect` 资产。

### 3.3 场景中创建陷阱地形对象
1. 新建对象 `Trap_Spikes`（示例名）。
2. 加 `BoxCollider2D`（覆盖陷阱区域），勾 `Is Trigger`。
3. 加 `TrapZone` 组件。
4. `TrapZone` 参数建议：
- `Trap Definition`：拖入 `TrapDefinition`。
- `Trigger On Enter`：勾选（踩到立刻触发）。
- `Trigger On Stay`：可选（持续伤害场景勾选）。
- `Stay Tick Interval`：持续触发间隔，例如 `0.5`。
- `Affect Player`：勾选。
- `Affect Enemy`：按需。

### 3.4 自定义陷阱效果接口（给组员用）
继承 `TrapEffect`，重写任意方法：
- `OnEnter(TrapContext context)`
- `OnStay(TrapContext context)`
- `OnExit(TrapContext context)`

示例（伪代码）：
```csharp
public class SlowTrapEffect : TrapEffect
{
    public override void OnEnter(TrapContext context)
    {
        // 这里写减速、击退、加状态等逻辑
    }
}
```

## 4. 敌人技能系统（可扩展接口）

### 4.1 创建技能效果资产（示例：掉血）
1. 右键 Project -> `Create/Demo/Enemies/Skill Effects/Damage Skill Effect`。
2. 配置：
- `Damage`：技能伤害。
- `Apply Knockback`：是否附带击退。
- `Knockback Velocity`：击退速度。

### 4.2 创建技能定义资产
1. 右键 Project -> `Create/Demo/Enemies/Skill Definition`。
2. 设置：
- `Cooldown`：冷却。
- `Min Distance / Max Distance`：触发距离。
- `Usable In Chase / Attack / Patrol`：在什么状态可用。
- `Effects`：加入 `EnemyDamageSkillEffect`。

### 4.3 给敌人挂技能控制器
1. 选中敌人 Prefab（如 `MeleeEnemy`、`RangedEnemy`）。
2. 添加 `EnemySkillController`。
3. 在 `Skill Definitions` 列表里拖入 `EnemySkillDefinition`。
4. 可调：
- `Global Cooldown`：全局技能间隔。
- `Stop Horizontal Velocity On Cast`：施法时停水平速度。

`EnemyBase` 已接入，敌人在追击/攻击阶段会优先尝试施放技能（满足距离和冷却时）。

### 4.4 自定义敌人技能接口（给组员用）
继承 `EnemySkillEffect`，重写：
- `Execute(EnemySkillContext context)`

示例（伪代码）：
```csharp
public class EnemyBurnSkillEffect : EnemySkillEffect
{
    public override void Execute(EnemySkillContext context)
    {
        // 给玩家添加燃烧、减速、召唤弹幕等
    }
}
```

## 5. 快速测试清单

1. 玩家进入梯子区域后能上下爬，按跳跃可跳离。
2. 玩家踩到 `TrapZone` 后立即掉血（HUD 血量减少）。
3. 敌人进入技能触发距离后，按技能定义的冷却和状态施放技能。
4. `dotnet build demo.sln` 无报错（已通过）。
