# 新增武器效果示例：击退（Knockback）

你现在可以直接在项目里使用击退效果。

## 已新增脚本

- `Assets/Scripts/Combat/Modifiers/KnockbackWeaponModifier.cs`
- `Assets/Scripts/Enemies/EnemyBase.cs`（新增 `ApplyKnockback` 支持）

## Unity 内使用步骤

1. 在 Project 面板右键：
   - `Create > Demo > Combat > Modifiers > Knockback`
2. 命名：`Knockback_Strong`（示例）
3. 在该资产上设置参数：
   - `Horizontal Velocity`：水平击退速度（如 6）
   - `Vertical Velocity`：垂直抬升（如 1.2）
   - `Stun Duration`：硬直时长（如 0.18）
4. 打开你的武器资产 `WeaponDefinition`。
5. 在 `Modifiers` 列表里添加 `Knockback_Strong`。
6. 运行游戏，命中敌人即可看到击退。

## 推荐参数模板

### 轻击退（近战小武器）
- Horizontal Velocity = `4.5`
- Vertical Velocity = `0.8`
- Stun Duration = `0.10`

### 中击退（标准）
- Horizontal Velocity = `6.0`
- Vertical Velocity = `1.2`
- Stun Duration = `0.18`

### 强击退（重武器）
- Horizontal Velocity = `8.5`
- Vertical Velocity = `1.6`
- Stun Duration = `0.28`

## 进阶扩展方式

如果你要新增其他效果（吸血、减防、眩晕、连锁闪电），都用同一套路：

1. 新建脚本继承 `WeaponModifier`
2. 在 `OnHit` 里写效果逻辑
3. `CreateAssetMenu` 暴露为可创建资产
4. 在 `WeaponDefinition.Modifiers` 挂上去

示例骨架：

```csharp
public class NewEffectModifier : WeaponModifier
{
    public override void OnHit(GameObject attacker, GameObject target, WeaponDefinition weapon)
    {
        // 写你的效果逻辑
    }
}
```
