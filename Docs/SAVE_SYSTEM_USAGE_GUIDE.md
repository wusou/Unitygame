# 存档系统使用说明

## 已实现功能

1. 多存档槽（默认 12 槽，可改）。
2. 主菜单新增读档入口。
3. 读档面板分页显示。
4. 左键选择、双击读档。
5. 右键弹出菜单：重命名 / 删除。
6. 暂停面板新增存档入口。
7. 存档模式：
   - 选空槽：直接保存。
   - 选非空槽：弹确认是否覆盖。
   - 右键：重命名 / 删除。

---

## 代码位置

1. 存档核心：`Assets/Scripts/Systems/SaveGameManager.cs`
2. 存档列表面板：`Assets/Scripts/UI/SaveSlotBrowserPanel.cs`
3. 槽位点击组件：`Assets/Scripts/UI/SaveSlotRowItem.cs`
4. 主菜单接入：`Assets/Scripts/UI/MainMenuUI.cs`
5. 暂停菜单接入：`Assets/Scripts/UI/PauseMenuController.cs`

---

## 槽位数量怎么改

可在任一入口改：
1. `MainMenuUI.saveSlotCount`
2. `PauseMenuController.saveSlotCount`

建议两个值保持一致，例如都设为 `12`。

---

## 主菜单如何触发读档

`MainMenuUI` 会自动寻找按钮（按按钮名或文本匹配）：
1. `read / load / continue / 读档 / 读取 / 继续`

如果没找到，会自动复制 `Play` 按钮生成一个 `读档` 按钮。

---

## 暂停菜单如何触发存档

`PauseMenuController` 会自动寻找按钮（按按钮名或文本匹配）：
1. `save / 保存 / 存档`

如果没找到，会自动复制 `Resume` 或 `MainMenu` 按钮生成一个 `存档` 按钮。

---

## 存档包含哪些内容

1. 当前场景名。
2. 玩家位置。
3. 玩家生命值。
4. 玩家金币。
5. 玩家加成（伤害/速度 bonus）。
6. 武器背包（武器列表 + 当前选中槽）。
7. 运行局状态（RunCount / Difficulty / CurrentLevelIndex / 尸体数据）。

---

## 读档流程说明

1. 主菜单点“读档”打开面板。
2. 双击某个非空槽位。
3. 系统切场景并恢复玩家状态。

注意：
1. 目标场景必须在 `Build Settings` 里。
2. 场景内玩家对象要有 `Tag = Player`。

---

## 右键菜单说明

在读档面板或存档面板，对非空槽右键：
1. 重命名：弹输入框，确认后写入槽位名。
2. 删除：弹确认框，确认后删除该槽位文件。

---

## 存档文件位置

运行后会在 `Application.persistentDataPath/SaveSlots` 下生成：
1. `slot_01.json`
2. `slot_02.json`
3. ...

---

## 常见问题

1. 点读档没反应：
   - 先确认是“双击”，不是单击。
2. 读档提示场景无效：
   - 该存档对应场景未加入 Build Settings。
3. 暂停里没有存档按钮：
   - 按钮名/文本不匹配；或让脚本自动复制按钮（已内置）。
4. 读档后位置不对：
   - 检查场景里是否有其他脚本每帧强制改玩家位置。
