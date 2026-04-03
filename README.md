# 疾风行者（极简跑酷版）

当前项目已收敛为 `SwiftRunner` 跑酷玩法：一直前冲、踩踏处决、滑铲变道、连击计分、一命通关。

## 项目树
```text
Assets/
├─ Resources/
│  └─ Input/
│     └─ SwiftRunnerInput.inputactions
├─ Scenes/
│  └─ SwiftRunner_Playable.unity
├─ Scripts/
│  └─ Runner/
│     ├─ SwiftRunnerSceneInstaller.cs
│     ├─ SwiftRunnerGameController.cs
│     ├─ SwiftRunnerPlayerController.cs
│     ├─ SwiftRunnerCameraFollow.cs
│     ├─ SwiftRunnerEnemy.cs
│     ├─ SwiftRunnerObstacle.cs
│     ├─ SwiftRunnerHud.cs
│     ├─ SwiftRunnerHudAuthoring.cs
│     ├─ SwiftRunnerFinishGateAuthoring.cs
│     ├─ SwiftRunnerGroupAuthoring.cs
│     ├─ SwiftRunnerVisualFactory.cs
│     ├─ SwiftRunnerWorld.cs
│     └─ Editor/
│        └─ SwiftRunnerSceneAuthoring.cs
├─ Art/
│  └─ ...
└─ TextMesh Pro/
   └─ ...
```

## Scene Tree
场景：`Assets/Scenes/SwiftRunner_Playable.unity`

```text
SwiftRunnerBootstrap [Transform, SwiftRunnerSceneInstaller]
├─ Environment [Transform, SwiftRunnerGroupAuthoring]
│  ├─ Backdrop [Transform, SwiftRunnerGroupAuthoring]
│  │  ├─ Sky [Transform, SpriteRenderer]
│  │  ├─ CloudsFar_0..7 [Transform, SpriteRenderer]
│  │  ├─ CloudsNear_0..8 [Transform, SpriteRenderer]
│  │  ├─ Mountains_0..7 [Transform, SpriteRenderer]
│  │  ├─ FarWall [Transform, SpriteRenderer]
│  │  ├─ SlumBackdrop [Transform, SpriteRenderer]
│  │  ├─ MarketBackdrop [Transform, SpriteRenderer]
│  │  └─ LaneGuide_0..2 [Transform, SpriteRenderer]
│  ├─ Track [Transform, SwiftRunnerGroupAuthoring]
│  │  ├─ Ground_0..2 [Transform, SpriteRenderer]
│  │  ├─ GroundTrim_0..2 [Transform, SpriteRenderer]
│  │  └─ LaneGlow_0..2 [Transform, SpriteRenderer]
│  └─ Labels [Transform, SwiftRunnerGroupAuthoring]
│     ├─ WallLabel [RectTransform, MeshRenderer, TextMeshPro, MeshFilter]
│     ├─ SlumLabel [RectTransform, MeshRenderer, TextMeshPro, MeshFilter]
│     └─ MarketLabel [RectTransform, MeshRenderer, TextMeshPro, MeshFilter]
├─ Gameplay [Transform, SwiftRunnerGroupAuthoring]
│  ├─ Systems [Transform, SwiftRunnerGroupAuthoring, SwiftRunnerGameController]
│  ├─ Player [Transform, SwiftRunnerGroupAuthoring]
│  │  └─ RunnerPlayer [Transform, SpriteRenderer, PlayerInput, SwiftRunnerPlayerController]
│  │     ├─ Shadow [Transform, SpriteRenderer]
│  │     ├─ FocusGlow [Transform, SpriteRenderer]
│  │     └─ AfterImage [Transform, SpriteRenderer]
│  └─ CameraRig [Transform, SwiftRunnerGroupAuthoring]
│     └─ MainCamera [Transform, Camera, SwiftRunnerCameraFollow]
├─ Encounters [Transform, SwiftRunnerGroupAuthoring]
│  ├─ WallSection [Transform, SwiftRunnerGroupAuthoring]
│  │  ├─ Hazards [Transform, SwiftRunnerGroupAuthoring]
│  │  │  ├─ Water_15 [Transform, SwiftRunnerObstacle]
│  │  │  ├─ Water_23 [Transform, SwiftRunnerObstacle]
│  │  │  ├─ Water_32 [Transform, SwiftRunnerObstacle]
│  │  │  └─ WallCrest [Transform, SwiftRunnerObstacle]
│  │  └─ Enemies [Transform, SwiftRunnerGroupAuthoring]
│  │     ├─ WallGuard_A/B/C/D [Transform, SwiftRunnerEnemy]
│  │     └─ Scout_Left/Right/Center [Transform, SwiftRunnerEnemy]
│  ├─ SlumSection [Transform, SwiftRunnerGroupAuthoring]
│  │  ├─ Obstacles [Transform, SwiftRunnerGroupAuthoring]
│  │  │  ├─ PushCart_A/B/C [Transform, SwiftRunnerObstacle]
│  │  │  ├─ Stall_A/B [Transform, SwiftRunnerObstacle]
│  │  │  └─ LowRack_A/B/C [Transform, SwiftRunnerObstacle]
│  │  └─ Enemies [Transform, SwiftRunnerGroupAuthoring]
│  │     ├─ KnifeGuard_A0/A/B/B2/C/D [Transform, SwiftRunnerEnemy]
│  └─ MarketSection [Transform, SwiftRunnerGroupAuthoring]
│     ├─ Obstacles [Transform, SwiftRunnerGroupAuthoring]
│     │  ├─ Stall_B [Transform, SwiftRunnerObstacle]
│     │  └─ LowRack_D [Transform, SwiftRunnerObstacle]
│     └─ Enemies [Transform, SwiftRunnerGroupAuthoring]
│        └─ MarketWall_A..O [Transform, SwiftRunnerEnemy]
├─ UI [Transform, SwiftRunnerGroupAuthoring]
│  └─ RunnerHudCanvas [RectTransform, Canvas, CanvasScaler, GraphicRaycaster, SwiftRunnerHud, SwiftRunnerHudAuthoring]
│     ├─ TopLeftPanel [RectTransform]
│     │  ├─ Combo [RectTransform, CanvasRenderer, TextMeshProUGUI]
│     │  └─ Phase [RectTransform, CanvasRenderer, TextMeshProUGUI]
│     ├─ TopCenterPanel [RectTransform]
│     │  ├─ Status [RectTransform, CanvasRenderer, TextMeshProUGUI]
│     │  └─ Controls [RectTransform, CanvasRenderer, TextMeshProUGUI]
│     ├─ TopRightPanel [RectTransform]
│     │  ├─ PursuitLabel [RectTransform, CanvasRenderer, TextMeshProUGUI]
│     │  └─ PursuitBar [RectTransform]
│     └─ BottomHintPanel [RectTransform]
│        └─ Hint [RectTransform, CanvasRenderer, TextMeshProUGUI]
└─ Finish [Transform, SwiftRunnerGroupAuthoring]
   └─ FinishGate [Transform, SwiftRunnerGroupAuthoring, SwiftRunnerFinishGateAuthoring]
      ├─ GateFrame [Transform, SpriteRenderer]
      ├─ FinishBanner [Transform, SpriteRenderer]
      └─ TriggerVolume [Transform, SpriteRenderer, SwiftRunnerObstacle]
```

## 现状
- 旧的战斗、背包、任务、迷你地图、加密、TMP 示例脚本已删除。
- 场景已重建为当前 `SwiftRunner` 玩法树。
- 运行逻辑集中在 `Assets/Scripts/Runner/`。

## 玩法摘要
- 玩家手动左右移动，停下过久会死。
- `Space` 起跳，空中再按一次 `Space` 是二段跳。
- `V` 在空中是下踩，在地面是滑铲。
- `Shift` 按住加速，`Ctrl` 切换常驻加速。
- 评分只看 Combo。

## 输入配置
- 当前使用 `Unity Input System`。
- 输入资源：`Assets/Resources/Input/SwiftRunnerInput.inputactions`
- 玩家对象挂载了 `PlayerInput`，默认 Action Map 是 `Runner`。
- 输入资源现在包含两个 Action Map：`Runner` 和 `UI`。
- 要改键：在 Unity 中双击 `SwiftRunnerInput.inputactions`，修改 `Runner` 下的 `Move`、`Jump`、`Stomp`、`SprintHold`、`SprintToggle`，以及 `UI` 下的 `Navigate`、`Submit`、`Cancel`、`Pause` 绑定即可。
- HUD 会根据当前活动控制方案自动显示一套提示：键盘或手柄，不再同时显示两套。
