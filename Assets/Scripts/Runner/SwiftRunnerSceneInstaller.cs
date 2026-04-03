using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SwiftRunner
{
    public enum SwiftRunnerSceneMode
    {
        RuntimeGenerated = 0,
        AuthoredBindings = 1,
    }

    public sealed class SwiftRunnerSceneInstaller : MonoBehaviour
    {
        private static readonly HashSet<string> SupportedSceneNames = new()
        {
            "Map_01",
            "Map_01_XY",
            "debug",
        };

        private const string BootstrapObjectName = "SwiftRunnerBootstrap";
        private const string RuntimeInstallerName = "SwiftRunnerSceneInstaller";
        private const float FinishLineX = 194f;
        private const float StartX = 2f;
        private const string SkyBackdropSpritePath = "Assets/Art/Backgrounds/Backgrounds/backgroundColorDesert.png";
        private const string DesertBackdropSpritePath = "Assets/Art/Backgrounds/Backgrounds/backgroundDesert.png";
        private const string CastleBackdropSpritePath = "Assets/Art/Backgrounds/Backgrounds/backgroundCastles.png";
        private const string MountainsSpritePath = "Assets/Art/Backgrounds/Backgrounds/Elements/mountains.png";
        private const string HillsSpritePath = "Assets/Art/Backgrounds/Backgrounds/Elements/hillsLarge.png";
        private const string CloudFarSpritePath = "Assets/Art/Backgrounds/Backgrounds/Elements/cloudLayerB2.png";
        private const string CloudNearSpritePath = "Assets/Art/Backgrounds/Backgrounds/Elements/cloudLayer1.png";
        private const string GroundBaseSpritePath = "Assets/Art/Backgrounds/Backgrounds/Elements/groundLayer1.png";
        private const string GroundTrimSpritePath = "Assets/Art/Backgrounds/Backgrounds/Elements/groundLayer2.png";
        private const string PlayerIdleSpritePath = "Assets/Art/Sprites/Characters/Characters/Player/character_yellow_idle.png";
        private const string PlayerWalkASpritePath = "Assets/Art/Sprites/Characters/Characters/Player/character_yellow_walk_a.png";
        private const string PlayerWalkBSpritePath = "Assets/Art/Sprites/Characters/Characters/Player/character_yellow_walk_b.png";
        private const string PlayerJumpSpritePath = "Assets/Art/Sprites/Characters/Characters/Player/character_yellow_jump.png";
        private const string PlayerDuckSpritePath = "Assets/Art/Sprites/Characters/Characters/Player/character_yellow_duck.png";
        private const string PlayerHitSpritePath = "Assets/Art/Sprites/Characters/Characters/Player/character_yellow_hit.png";
        private const string GuardSpritePath = "Assets/Art/Sprites/Characters/Characters/Player/character_beige_idle.png";
        private const string ScoutSpritePath = "Assets/Art/Sprites/Characters/Characters/Player/character_purple_idle.png";
        private const string MarketGuardSpritePath = "Assets/Art/Sprites/Characters/Characters/Player/character_green_idle.png";
        private const string CartSpritePath = "Assets/Art/Tilesets/Tiles/block_planks.png";
        private const string StallSpritePath = "Assets/Art/Tilesets/Tiles/bridge_logs.png";
        private const string RackSpritePath = "Assets/Art/Tilesets/Tiles/fence.png";
        private const string WaterSpritePath = "Assets/Art/Tilesets/Tiles/block_blue.png";
        private const string WallSpritePath = "Assets/Art/Tilesets/Tiles/bricks_grey.png";
        private const string GateFrameSpritePath = "Assets/Art/Tilesets/Tiles/brick_brown.png";
        private const string BannerSpritePath = "Assets/Art/Tilesets/Tiles/flag_yellow_a.png";
        private const string LandingMarkerSpritePath = "Assets/Art/UI/PNG/Extra/Default/button_round_line.png";
        private static readonly float[] DefaultLaneCenters = { -2.6f, 0f, 2.6f };

        [SerializeField] private SwiftRunnerSceneMode sceneMode = SwiftRunnerSceneMode.RuntimeGenerated;

        public static bool ShouldBuild(Scene scene)
        {
            return scene.IsValid() &&
                   scene.isLoaded &&
                   SupportedSceneNames.Contains(scene.name);
        }

        public static void EnsureInstalled(Scene scene)
        {
            var existing = FindObjectOfType<SwiftRunnerSceneInstaller>();
            if (existing != null)
            {
                return;
            }

            var installerObject = new GameObject(RuntimeInstallerName);
            SceneManager.MoveGameObjectToScene(installerObject, scene);
            var installer = installerObject.AddComponent<SwiftRunnerSceneInstaller>();
            installer.sceneMode = SwiftRunnerSceneMode.RuntimeGenerated;
        }

        public static void RebuildAuthoredScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            var existingBootstrap = FindRootByName(scene, BootstrapObjectName);
            if (existingBootstrap != null)
            {
                DestroyImmediate(existingBootstrap);
            }

            var bootstrap = new GameObject(BootstrapObjectName);
            SceneManager.MoveGameObjectToScene(bootstrap, scene);
            var installer = bootstrap.AddComponent<SwiftRunnerSceneInstaller>();
            installer.sceneMode = SwiftRunnerSceneMode.AuthoredBindings;
            CleanupSceneRoots(scene, bootstrap);
            DestroyChildrenImmediate(bootstrap.transform);
            BuildSceneContent(bootstrap.transform);
        }

        private void Awake()
        {
            if (sceneMode == SwiftRunnerSceneMode.AuthoredBindings)
            {
                BindAuthoredScene();
                return;
            }

            BuildRuntimeScene();
        }

        private void BuildRuntimeScene()
        {
            CleanupSceneRoots(SceneManager.GetActiveScene(), gameObject);
            DestroyChildrenImmediate(transform);
            BuildSceneContent(transform);

            Debug.Log($"[SwiftRunner] Built runtime scene for {SceneManager.GetActiveScene().name}.", this);
        }

        private void BindAuthoredScene()
        {
            var controller = GetComponentInChildren<SwiftRunnerGameController>(true);
            var player = GetComponentInChildren<SwiftRunnerPlayerController>(true);
            var cameraFollow = GetComponentInChildren<SwiftRunnerCameraFollow>(true);
            var hud = GetComponentInChildren<SwiftRunnerHud>(true);
            if (controller == null || player == null || cameraFollow == null || hud == null)
            {
                Debug.LogWarning("[SwiftRunner] Authored scene is missing required runner objects.", this);
                return;
            }

            controller.ResetRuntimeState();
            controller.Configure((float[])DefaultLaneCenters.Clone(), StartX, FinishLineX);
            hud.Initialize();
            player.Initialize(controller, startLaneIndex: ResolveNearestLaneIndex(player.transform.position.y));
            cameraFollow.Bind(player);
            controller.BindPlayer(player);
            controller.BindCamera(cameraFollow);
            controller.BindHud(hud);

            var enemies = GetComponentsInChildren<SwiftRunnerEnemy>(true);
            for (var index = 0; index < enemies.Length; index++)
            {
                var enemy = enemies[index];
                enemy.Configure(controller, ResolveNearestLaneIndex(enemy.transform.position.y), enemy.transform.position.x);
                controller.RegisterEnemy(enemy);
            }

            var obstacles = GetComponentsInChildren<SwiftRunnerObstacle>(true);
            for (var index = 0; index < obstacles.Length; index++)
            {
                var obstacle = obstacles[index];
                obstacle.BindController(controller);
                controller.RegisterObstacle(obstacle);
            }
        }

        private static void BuildSceneContent(Transform parent)
        {
            var laneCenters = (float[])DefaultLaneCenters.Clone();

            var environmentRoot = CreateGroup("Environment", parent, "Top Level", 0f, FinishLineX, "场景环境、车道和氛围表现");
            var backdropRoot = CreateGroup("Backdrop", environmentRoot, "Visual Layer", 0f, FinishLineX, "远景背景与视觉衬底");
            var trackRoot = CreateGroup("Track", environmentRoot, "Visual Layer", 0f, FinishLineX, "可跑酷地面与车道本体");
            var labelRoot = CreateGroup("Labels", environmentRoot, "Visual Layer", 0f, FinishLineX, "关卡段落标签");

            var gameplayRoot = CreateGroup("Gameplay", parent, "Top Level", StartX, FinishLineX, "玩家、相机与核心系统");
            var systemsRoot = CreateGroup("Systems", gameplayRoot, "System Root", StartX, FinishLineX, "关卡控制器与状态机");
            var playerRoot = CreateGroup("Player", gameplayRoot, "Actor Root", StartX, FinishLineX, "玩家角色与拖影");
            var cameraRoot = CreateGroup("CameraRig", gameplayRoot, "Camera Root", StartX, FinishLineX, "主相机跟随");

            var encountersRoot = CreateGroup("Encounters", parent, "Top Level", 15f, 191f, "敌人、障碍与节奏段落");
            var wallRoot = CreateGroup("WallSection", encountersRoot, "Section", 15f, 54f, "城墙起跳段");
            var slumRoot = CreateGroup("SlumSection", encountersRoot, "Section", 59f, 118f, "贫民窟三车道段");
            var marketRoot = CreateGroup("MarketSection", encountersRoot, "Section", 120f, 191f, "集市冲刺段");

            var uiRoot = CreateGroup("UI", parent, "Top Level", StartX, FinishLineX, "HUD 和提示面板");
            var finishRoot = CreateGroup("Finish", parent, "Top Level", FinishLineX - 4f, FinishLineX + 4f, "终点门与结算触发");

            var controller = systemsRoot.gameObject.AddComponent<SwiftRunnerGameController>();
            controller.Configure(laneCenters, StartX, FinishLineX);

            BuildBackdrop(backdropRoot, laneCenters);
            BuildTrack(trackRoot, laneCenters);
            BuildTrackLabels(labelRoot);

            var player = BuildPlayer(playerRoot, controller, laneCenters);
            var cameraFollow = BuildCamera(cameraRoot, player);
            var hud = BuildHud(uiRoot);

            controller.ResetRuntimeState();
            controller.BindPlayer(player);
            controller.BindCamera(cameraFollow);
            controller.BindHud(hud);

            BuildWallSection(wallRoot, controller, laneCenters);
            BuildSlumSection(slumRoot, controller, laneCenters);
            BuildMarketSection(marketRoot, controller, laneCenters);
            BuildFinishGate(finishRoot, controller);
        }

        private static GameObject FindRootByName(Scene scene, string rootName)
        {
            var roots = scene.GetRootGameObjects();
            for (var index = 0; index < roots.Length; index++)
            {
                if (roots[index] != null && roots[index].name == rootName)
                {
                    return roots[index];
                }
            }

            return null;
        }

        private static void CleanupSceneRoots(Scene scene, GameObject exceptRoot)
        {
            var roots = scene.GetRootGameObjects();
            for (var index = 0; index < roots.Length; index++)
            {
                var root = roots[index];
                if (root == null || root == exceptRoot)
                {
                    continue;
                }

                DestroyObject(root);
            }
        }

        private static void DestroyChildrenImmediate(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                DestroyObject(parent.GetChild(index).gameObject);
            }
        }

        private static void DestroyObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static int ResolveNearestLaneIndex(float worldY)
        {
            var bestIndex = 0;
            var bestDistance = float.MaxValue;
            for (var index = 0; index < DefaultLaneCenters.Length; index++)
            {
                var distance = Mathf.Abs(worldY - DefaultLaneCenters[index]);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestIndex = index;
            }

            return bestIndex;
        }

        private static SwiftRunnerPlayerController BuildPlayer(Transform parent, SwiftRunnerGameController controller, IReadOnlyList<float> laneCenters)
        {
            var playerObject = SwiftRunnerVisualFactory.CreateSpriteObject(
                "RunnerPlayer",
                parent,
                new Vector3(StartX, laneCenters[1], 0f),
                new Vector2(1.65f, 1.65f),
                PlayerIdleSpritePath,
                Color.white,
                sortingOrder: 30);

            SwiftRunnerVisualFactory.CreateBlock(
                "Shadow",
                playerObject.transform,
                new Vector3(0f, -0.9f, 0f),
                new Vector2(0.95f, 0.18f),
                new Color(0.14f, 0.08f, 0.05f, 0.35f),
                sortingOrder: 24);

            SwiftRunnerVisualFactory.CreateBlock(
                "FocusGlow",
                playerObject.transform,
                new Vector3(0f, -0.05f, 0f),
                new Vector2(1.2f, 1.55f),
                new Color(1f, 0.9f, 0.4f, 0.09f),
                sortingOrder: 23);

            var trail = SwiftRunnerVisualFactory.CreateSpriteObject(
                "AfterImage",
                playerObject.transform,
                new Vector3(-0.52f, -0.02f, 0f),
                new Vector2(1.5f, 1.5f),
                PlayerIdleSpritePath,
                new Color(1f, 0.82f, 0.18f, 0.18f),
                sortingOrder: 25,
                fallbackToBlock: false);

            var landingMarker = SwiftRunnerVisualFactory.CreateSpriteObject(
                "LandingMarker",
                playerObject.transform,
                new Vector3(0f, -0.92f, 0f),
                new Vector2(1.05f, 0.26f),
                LandingMarkerSpritePath,
                new Color(0.98f, 0.96f, 0.62f, 0.65f),
                sortingOrder: 22,
                fallbackToBlock: true);

            var rigidbody = playerObject.AddComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = playerObject.AddComponent<CapsuleCollider2D>();
            collider.isTrigger = true;
            collider.direction = CapsuleDirection2D.Vertical;
            collider.offset = new Vector2(0f, 0.02f);
            collider.size = new Vector2(0.82f, 1.42f);

            playerObject.AddComponent<PlayerInput>();

            var controllerComponent = playerObject.AddComponent<SwiftRunnerPlayerController>();
            controllerComponent.ConfigureVisuals(
                playerObject.GetComponent<SpriteRenderer>(),
                trail.GetComponent<SpriteRenderer>(),
                landingMarker.GetComponent<SpriteRenderer>(),
                SwiftRunnerVisualFactory.LoadSprite(PlayerIdleSpritePath),
                SwiftRunnerVisualFactory.LoadSprite(PlayerWalkASpritePath),
                SwiftRunnerVisualFactory.LoadSprite(PlayerWalkBSpritePath),
                SwiftRunnerVisualFactory.LoadSprite(PlayerJumpSpritePath),
                SwiftRunnerVisualFactory.LoadSprite(PlayerDuckSpritePath),
                SwiftRunnerVisualFactory.LoadSprite(PlayerHitSpritePath));
            controllerComponent.ConfigurePhysics(rigidbody, collider);
            controllerComponent.Initialize(controller, startLaneIndex: 1);
            return controllerComponent;
        }

        private static SwiftRunnerCameraFollow BuildCamera(Transform parent, SwiftRunnerPlayerController player)
        {
            var cameraObject = new GameObject("MainCamera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";

            var cameraComponent = cameraObject.AddComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = 6.6f;
            cameraComponent.backgroundColor = new Color(0.91f, 0.84f, 0.69f, 1f);
            cameraComponent.clearFlags = CameraClearFlags.SolidColor;

            var follow = cameraObject.AddComponent<SwiftRunnerCameraFollow>();
            follow.Bind(player);
            return follow;
        }

        private static SwiftRunnerHud BuildHud(Transform parent)
        {
            var canvasObject = new GameObject("RunnerHudCanvas");
            canvasObject.transform.SetParent(parent, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();

            var hud = canvasObject.AddComponent<SwiftRunnerHud>();
            hud.Initialize();

            var hudAuthoring = canvasObject.AddComponent<SwiftRunnerHudAuthoring>();
            hudAuthoring.Configure(
                canvasObject.transform.Find("TopLeftPanel") as RectTransform,
                canvasObject.transform.Find("TopCenterPanel") as RectTransform,
                canvasObject.transform.Find("BottomHintPanel") as RectTransform,
                canvasObject.transform.Find("TopLeftPanel/Combo")?.GetComponent<TextMeshProUGUI>(),
                canvasObject.transform.Find("TopLeftPanel/Phase")?.GetComponent<TextMeshProUGUI>(),
                canvasObject.transform.Find("TopCenterPanel/Status")?.GetComponent<TextMeshProUGUI>(),
                canvasObject.transform.Find("BottomHintPanel/Hint")?.GetComponent<TextMeshProUGUI>());
            return hud;
        }

        private static void BuildBackdrop(Transform parent, IReadOnlyList<float> laneCenters)
        {
            SwiftRunnerVisualFactory.CreateSpriteObject(
                "Sky",
                parent,
                new Vector3(96f, 7.8f, 0f),
                new Vector2(228f, 22f),
                SkyBackdropSpritePath,
                Color.white,
                -90);

            CreateRepeatedSpriteStrip("CloudsFar", parent, CloudFarSpritePath, 96f, 10.4f, 236f, 30f, 3.2f, new Color(1f, 1f, 1f, 0.38f), -72);
            CreateRepeatedSpriteStrip("CloudsNear", parent, CloudNearSpritePath, 96f, 8.8f, 230f, 28f, 4.2f, new Color(1f, 1f, 1f, 0.52f), -69);
            CreateRepeatedSpriteStrip("Mountains", parent, MountainsSpritePath, 96f, 4.5f, 232f, 29f, 8.2f, new Color(0.9f, 0.84f, 0.72f, 0.8f), -48);

            SwiftRunnerVisualFactory.CreateSpriteObject(
                "FarWall",
                parent,
                new Vector3(27f, 2.85f, 0f),
                new Vector2(58f, 11.8f),
                CastleBackdropSpritePath,
                Color.white,
                -26);

            SwiftRunnerVisualFactory.CreateSpriteObject(
                "SlumBackdrop",
                parent,
                new Vector3(84f, 1.65f, 0f),
                new Vector2(84f, 6.6f),
                HillsSpritePath,
                new Color(0.89f, 0.83f, 0.72f, 0.88f),
                -22);

            SwiftRunnerVisualFactory.CreateSpriteObject(
                "MarketBackdrop",
                parent,
                new Vector3(153f, 1.9f, 0f),
                new Vector2(94f, 9.4f),
                DesertBackdropSpritePath,
                new Color(1f, 1f, 1f, 0.82f),
                -20);

            for (var index = 0; index < laneCenters.Count; index++)
            {
                SwiftRunnerVisualFactory.CreateBlock(
                    $"LaneGuide_{index}",
                    parent,
                    new Vector3(96f, laneCenters[index] - 0.95f, 0f),
                    new Vector2(220f, 0.08f),
                    new Color(1f, 1f, 1f, 0.12f),
                    -5);
            }
        }

        private static void BuildTrack(Transform parent, IReadOnlyList<float> laneCenters)
        {
            for (var index = 0; index < laneCenters.Count; index++)
            {
                SwiftRunnerVisualFactory.CreateSpriteObject(
                    $"Ground_{index}",
                    parent,
                    new Vector3(96f, laneCenters[index] - 1.5f, 0f),
                    new Vector2(220f, 2.4f),
                    GroundBaseSpritePath,
                    Color.white,
                    0);

                SwiftRunnerVisualFactory.CreateSpriteObject(
                    $"GroundTrim_{index}",
                    parent,
                    new Vector3(96f, laneCenters[index] - 0.96f, 0f),
                    new Vector2(220f, 1.05f),
                    GroundTrimSpritePath,
                    new Color(1f, 1f, 1f, 0.9f),
                    1);

                SwiftRunnerVisualFactory.CreateBlock(
                    $"LaneGlow_{index}",
                    parent,
                    new Vector3(96f, laneCenters[index] - 0.9f, 0f),
                    new Vector2(220f, 0.12f),
                    new Color(1f, 0.92f, 0.62f, 0.14f),
                    2);
            }
        }

        private static void BuildTrackLabels(Transform parent)
        {
            SwiftRunnerVisualFactory.CreateWorldLabel("WallLabel", parent, new Vector3(18f, 5f, 0f), "城墙突围", new Color(0.94f, 0.92f, 0.82f, 1f), 6);
            SwiftRunnerVisualFactory.CreateWorldLabel("SlumLabel", parent, new Vector3(80f, 5f, 0f), "贫民窟三车道", new Color(0.95f, 0.94f, 0.84f, 1f), 6);
            SwiftRunnerVisualFactory.CreateWorldLabel("MarketLabel", parent, new Vector3(150f, 5f, 0f), "集市终点冲刺", new Color(0.98f, 0.93f, 0.86f, 1f), 6);
        }

        private static void BuildWallSection(Transform parent, SwiftRunnerGameController controller, IReadOnlyList<float> laneCenters)
        {
            var hazardRoot = CreateGroup("Hazards", parent);
            var enemyRoot = CreateGroup("Enemies", parent);

            // The opening jump should be reliable, so the first gap is shorter and the first guard lands later.
            CreateWater(hazardRoot, controller, laneCenters, 15f, 3.1f);
            CreateEnemy(enemyRoot, controller, 19.6f, 1, laneCenters, "WallGuard_A");
            CreateWater(hazardRoot, controller, laneCenters, 23f, 3.4f);
            CreateEnemy(enemyRoot, controller, 26.5f, 1, laneCenters, "WallGuard_B");
            CreateWater(hazardRoot, controller, laneCenters, 32f, 4f);
            CreateEnemy(enemyRoot, controller, 35.2f, 1, laneCenters, "WallGuard_C");
            CreateEnemy(enemyRoot, controller, 38.1f, 1, laneCenters, "WallGuard_D");

            CreateKillWall(hazardRoot, controller, 40.8f, 1.4f, 2.5f, "WallCrest");
            CreateEnemy(enemyRoot, controller, 44.5f, 0, laneCenters, "Scout_Left");
            CreateEnemy(enemyRoot, controller, 48f, 2, laneCenters, "Scout_Right");
            CreateEnemy(enemyRoot, controller, 51f, 1, laneCenters, "Scout_Center");
        }

        private static void BuildSlumSection(Transform parent, SwiftRunnerGameController controller, IReadOnlyList<float> laneCenters)
        {
            var obstacleRoot = CreateGroup("Obstacles", parent);
            var enemyRoot = CreateGroup("Enemies", parent);

            CreateSlowObstacle(obstacleRoot, controller, 59f, 0, laneCenters, "PushCart_A");
            CreateEnemy(enemyRoot, controller, 63f, 1, laneCenters, "KnifeGuard_A0");
            CreateSlowObstacle(obstacleRoot, controller, 67f, 2, laneCenters, "PushCart_B");
            CreateLowBarrier(obstacleRoot, controller, 72f, 1, laneCenters, "LowRack_A");
            CreateEnemy(enemyRoot, controller, 77f, 2, laneCenters, "KnifeGuard_A");
            CreateSlowObstacle(obstacleRoot, controller, 82f, 1, laneCenters, "Stall_A");
            CreateEnemy(enemyRoot, controller, 88f, 0, laneCenters, "KnifeGuard_B");
            CreateLowBarrier(obstacleRoot, controller, 94f, 2, laneCenters, "LowRack_B");
            CreateEnemy(enemyRoot, controller, 98f, 1, laneCenters, "KnifeGuard_B2");
            CreateSlowObstacle(obstacleRoot, controller, 102f, 0, laneCenters, "PushCart_C");
            CreateEnemy(enemyRoot, controller, 108f, 1, laneCenters, "KnifeGuard_C");
            CreateEnemy(enemyRoot, controller, 112.5f, 2, laneCenters, "KnifeGuard_D");
            CreateLowBarrier(obstacleRoot, controller, 116.5f, 0, laneCenters, "LowRack_C");
        }

        private static void BuildMarketSection(Transform parent, SwiftRunnerGameController controller, IReadOnlyList<float> laneCenters)
        {
            var obstacleRoot = CreateGroup("Obstacles", parent);
            var enemyRoot = CreateGroup("Enemies", parent);

            CreateEnemy(enemyRoot, controller, 120f, 1, laneCenters, "MarketWall_A");
            CreateEnemy(enemyRoot, controller, 125f, 2, laneCenters, "MarketWall_B");
            CreateEnemy(enemyRoot, controller, 130f, 0, laneCenters, "MarketWall_C");
            CreateEnemy(enemyRoot, controller, 135f, 1, laneCenters, "MarketWall_D");
            CreateSlowObstacle(obstacleRoot, controller, 140f, 2, laneCenters, "Stall_B");
            CreateEnemy(enemyRoot, controller, 144f, 0, laneCenters, "MarketWall_E");
            CreateEnemy(enemyRoot, controller, 148f, 2, laneCenters, "MarketWall_F");
            CreateEnemy(enemyRoot, controller, 152f, 1, laneCenters, "MarketWall_G");
            CreateEnemy(enemyRoot, controller, 155f, 0, laneCenters, "MarketWall_G2");
            CreateLowBarrier(obstacleRoot, controller, 157f, 0, laneCenters, "LowRack_D");
            CreateEnemy(enemyRoot, controller, 162f, 2, laneCenters, "MarketWall_H");
            CreateEnemy(enemyRoot, controller, 166f, 1, laneCenters, "MarketWall_I");
            CreateEnemy(enemyRoot, controller, 170f, 0, laneCenters, "MarketWall_J");
            CreateEnemy(enemyRoot, controller, 172.8f, 2, laneCenters, "MarketWall_J2");
            CreateEnemy(enemyRoot, controller, 175f, 1, laneCenters, "MarketWall_K");
            CreateEnemy(enemyRoot, controller, 180f, 2, laneCenters, "MarketWall_L");
            CreateEnemy(enemyRoot, controller, 182.5f, 0, laneCenters, "MarketWall_L2");
            CreateEnemy(enemyRoot, controller, 184.5f, 1, laneCenters, "MarketWall_M");
            CreateEnemy(enemyRoot, controller, 188f, 2, laneCenters, "MarketWall_N");
            CreateEnemy(enemyRoot, controller, 191f, 0, laneCenters, "MarketWall_O");
        }

        private static void BuildFinishGate(Transform parent, SwiftRunnerGameController controller)
        {
            var gateRoot = CreateGroup("FinishGate", parent);
            var gateFrame = SwiftRunnerVisualFactory.CreateTexturedBlock(
                "GateFrame",
                gateRoot,
                new Vector3(FinishLineX, 0f, 0f),
                new Vector2(1.3f, 8.5f),
                GateFrameSpritePath,
                new Color(0.9f, 0.82f, 0.64f, 1f),
                5,
                new Color(0.77f, 0.55f, 0.2f, 1f));
            var finishBanner = SwiftRunnerVisualFactory.CreateTexturedBlock(
                "FinishBanner",
                gateRoot,
                new Vector3(FinishLineX, 2.5f, 0f),
                new Vector2(3.6f, 1.2f),
                BannerSpritePath,
                new Color(1f, 0.95f, 0.56f, 1f),
                6,
                new Color(0.98f, 0.84f, 0.24f, 1f));

            var triggerVolume = SwiftRunnerVisualFactory.CreateBlock(
                "TriggerVolume",
                gateRoot,
                new Vector3(FinishLineX, 0f, 0f),
                new Vector2(1.95f, 8.8f),
                new Color(1f, 0.96f, 0.72f, 0.08f),
                4);

            var trigger = triggerVolume.AddComponent<SwiftRunnerObstacle>();
            trigger.Configure(controller, SwiftRunnerObstacleType.FinishGate, FinishLineX, -1, 0.9f, 0f, 0f);
            trigger.SetContactColliders(CreateBoxTrigger(triggerVolume.transform, "FinishTrigger", Vector2.zero, new Vector2(1.8f, 8.6f)));
            controller.RegisterObstacle(trigger);

            var finishAuthoring = gateRoot.gameObject.AddComponent<SwiftRunnerFinishGateAuthoring>();
            finishAuthoring.Configure(
                gateFrame.transform,
                finishBanner.transform,
                triggerVolume.transform,
                FinishLineX,
                new Vector2(1.3f, 8.5f),
                new Vector2(3.6f, 1.2f),
                new Vector2(1.95f, 8.8f));
        }

        private static void CreateEnemy(Transform parent, SwiftRunnerGameController controller, float x, int laneIndex, IReadOnlyList<float> laneCenters, string objectName)
        {
            var spritePath = ResolveEnemySpritePath(objectName);
            var tint = ResolveEnemyTint(objectName);
            var enemy = SwiftRunnerVisualFactory.CreateEnemyVisual(objectName, parent, new Vector3(x, laneCenters[laneIndex], 0f), spritePath, tint);
            var bodyContact = CreateBoxTrigger(enemy.transform, "BodyTrigger", new Vector2(0f, 0.38f), new Vector2(0.86f, 1.08f));
            var stompContact = CreateBoxTrigger(enemy.transform, "StompTrigger", new Vector2(0f, 0.98f), new Vector2(0.72f, 0.22f));
            var component = enemy.AddComponent<SwiftRunnerEnemy>();
            component.Configure(controller, laneIndex, x);
            component.SetContactColliders(bodyContact, stompContact);
            controller.RegisterEnemy(component);
        }

        private static void CreateSlowObstacle(Transform parent, SwiftRunnerGameController controller, float x, int laneIndex, IReadOnlyList<float> laneCenters, string objectName)
        {
            var spritePath = objectName.Contains("Stall") ? StallSpritePath : CartSpritePath;
            var obstacle = SwiftRunnerVisualFactory.CreateTexturedBlock(
                objectName,
                parent,
                new Vector3(x, laneCenters[laneIndex] - 0.25f, 0f),
                new Vector2(1.35f, 1.1f),
                spritePath,
                Color.white,
                4,
                new Color(0.46f, 0.28f, 0.1f, 1f));
            var component = obstacle.AddComponent<SwiftRunnerObstacle>();
            component.Configure(controller, SwiftRunnerObstacleType.Slowdown, x, laneIndex, 0.82f, 1.15f, 4.4f);
            component.SetContactColliders(CreateBoxTrigger(obstacle.transform, "SlowdownTrigger", new Vector2(0f, 0.1f), new Vector2(1.1f, 0.86f)));
            controller.RegisterObstacle(component);
        }

        private static void CreateLowBarrier(Transform parent, SwiftRunnerGameController controller, float x, int laneIndex, IReadOnlyList<float> laneCenters, string objectName)
        {
            var barrier = SwiftRunnerVisualFactory.CreateTexturedBlock(
                objectName,
                parent,
                new Vector3(x, laneCenters[laneIndex] + 0.1f, 0f),
                new Vector2(1.7f, 0.55f),
                RackSpritePath,
                new Color(0.94f, 0.88f, 0.72f, 1f),
                4,
                new Color(0.63f, 0.48f, 0.24f, 1f));
            var component = barrier.AddComponent<SwiftRunnerObstacle>();
            component.Configure(controller, SwiftRunnerObstacleType.LowBarrier, x, laneIndex, 0.95f, 1.15f, 0f);
            component.SetContactColliders(CreateBoxTrigger(barrier.transform, "LowBarrierTrigger", new Vector2(0f, 0.1f), new Vector2(1.45f, 0.42f)));
            controller.RegisterObstacle(component);
        }

        private static void CreateWater(Transform parent, SwiftRunnerGameController controller, IReadOnlyList<float> laneCenters, float x, float width)
        {
            var water = SwiftRunnerVisualFactory.CreateTexturedBlock(
                $"Water_{x:0}",
                parent,
                new Vector3(x, -4.25f, 0f),
                new Vector2(width, 6f),
                WaterSpritePath,
                new Color(0.48f, 0.72f, 1f, 0.92f),
                2,
                new Color(0.12f, 0.41f, 0.56f, 1f));
            var component = water.AddComponent<SwiftRunnerObstacle>();
            component.Configure(controller, SwiftRunnerObstacleType.Water, x, -1, width * 0.5f, 1.1f, 0f);
            var contactColliders = new Collider2D[laneCenters.Count];
            for (var index = 0; index < laneCenters.Count; index++)
            {
                contactColliders[index] = CreateBoxTrigger(water.transform, $"WaterSurface_{index}", new Vector2(0f, laneCenters[index] + 4.25f), new Vector2(Mathf.Max(0.8f, width - 0.6f), 0.36f));
            }

            component.SetContactColliders(contactColliders);
            controller.RegisterObstacle(component);
        }

        private static void CreateKillWall(Transform parent, SwiftRunnerGameController controller, float x, float width, float clearanceHeight, string objectName)
        {
            var wall = SwiftRunnerVisualFactory.CreateTexturedBlock(
                objectName,
                parent,
                new Vector3(x, 0f, 0f),
                new Vector2(width, 7.5f),
                WallSpritePath,
                new Color(0.86f, 0.86f, 0.9f, 1f),
                3,
                new Color(0.56f, 0.56f, 0.6f, 1f));
            var component = wall.AddComponent<SwiftRunnerObstacle>();
            component.Configure(controller, SwiftRunnerObstacleType.KillWall, x, -1, width * 0.5f, clearanceHeight, 0f);
            component.SetContactColliders(CreateBoxTrigger(wall.transform, "WallTrigger", new Vector2(0f, 0f), new Vector2(Mathf.Max(0.4f, width), 6.8f)));
            controller.RegisterObstacle(component);
        }

        private static BoxCollider2D CreateBoxTrigger(Transform parent, string objectName, Vector2 localOffset, Vector2 size)
        {
            var triggerObject = new GameObject(objectName);
            triggerObject.transform.SetParent(parent, false);
            triggerObject.transform.localPosition = localOffset;
            var collider = triggerObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;
            return collider;
        }

        private static string ResolveEnemySpritePath(string objectName)
        {
            if (objectName.StartsWith("Scout_"))
            {
                return ScoutSpritePath;
            }

            if (objectName.StartsWith("MarketWall_"))
            {
                return MarketGuardSpritePath;
            }

            return GuardSpritePath;
        }

        private static Color ResolveEnemyTint(string objectName)
        {
            if (objectName.StartsWith("Scout_"))
            {
                return new Color(0.96f, 0.62f, 0.88f, 1f);
            }

            if (objectName.StartsWith("MarketWall_"))
            {
                return new Color(0.78f, 0.96f, 0.72f, 1f);
            }

            return new Color(1f, 0.88f, 0.72f, 1f);
        }

        private static void CreateRepeatedSpriteStrip(
            string objectNamePrefix,
            Transform parent,
            string spriteAssetPath,
            float centerX,
            float centerY,
            float totalWidth,
            float segmentWidth,
            float segmentHeight,
            Color color,
            int sortingOrder)
        {
            var count = Mathf.Max(1, Mathf.CeilToInt(totalWidth / segmentWidth));
            var actualWidth = count * segmentWidth;
            var startX = centerX - actualWidth * 0.5f + segmentWidth * 0.5f;

            for (var index = 0; index < count; index++)
            {
                SwiftRunnerVisualFactory.CreateSpriteObject(
                    $"{objectNamePrefix}_{index}",
                    parent,
                    new Vector3(startX + segmentWidth * index, centerY, 0f),
                    new Vector2(segmentWidth, segmentHeight),
                    spriteAssetPath,
                    color,
                    sortingOrder);
            }
        }

        private static Transform CreateGroup(string objectName, Transform parent, string role = null, float worldXMin = float.NaN, float worldXMax = float.NaN, string notes = null)
        {
            var group = new GameObject(objectName);
            group.transform.SetParent(parent, false);
            var authoring = group.AddComponent<SwiftRunnerGroupAuthoring>();
            authoring.Configure(objectName, role ?? objectName, worldXMin, worldXMax, notes);
            return group.transform;
        }
    }
}
