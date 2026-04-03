#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SwiftRunner.Editor
{
    public static class SwiftRunnerSceneAuthoring
    {
        private const string ScenePath = "Assets/Scenes/SwiftRunner_Playable.unity";
        private const string PrefabFolderPath = "Assets/Prefabs/SwiftRunner/SceneGroups";
        private static readonly string[] LegacyGeneratedAssetPaths =
        {
            $"{PrefabFolderPath}/UI/RunnerHudCanvas/Combo.prefab",
            $"{PrefabFolderPath}/UI/RunnerHudCanvas/Phase.prefab",
            $"{PrefabFolderPath}/UI/RunnerHudCanvas/Status.prefab",
            $"{PrefabFolderPath}/UI/RunnerHudCanvas/Hint.prefab",
        };

        private static readonly PrefabExportDefinition[] ExportDefinitions =
        {
            new PrefabExportDefinition("Environment/Backdrop", "Environment/Backdrop"),
            new PrefabExportDefinition("Environment/Track", "Environment/Track"),
            new PrefabExportDefinition("Environment/Labels", "Environment/Labels"),
            new PrefabExportDefinition("Gameplay/Systems", "Gameplay/Systems"),
            new PrefabExportDefinition("Gameplay/Player", "Gameplay/Player"),
            new PrefabExportDefinition("Gameplay/CameraRig", "Gameplay/CameraRig"),
            new PrefabExportDefinition("Encounters/WallSection/Hazards", "Encounters/WallSection/Hazards"),
            new PrefabExportDefinition("Encounters/WallSection/Enemies", "Encounters/WallSection/Enemies"),
            new PrefabExportDefinition("Encounters/WallSection", "Encounters/WallSection"),
            new PrefabExportDefinition("Encounters/SlumSection/Obstacles", "Encounters/SlumSection/Obstacles"),
            new PrefabExportDefinition("Encounters/SlumSection/Enemies", "Encounters/SlumSection/Enemies"),
            new PrefabExportDefinition("Encounters/SlumSection", "Encounters/SlumSection"),
            new PrefabExportDefinition("Encounters/MarketSection/Obstacles", "Encounters/MarketSection/Obstacles"),
            new PrefabExportDefinition("Encounters/MarketSection/Enemies", "Encounters/MarketSection/Enemies"),
            new PrefabExportDefinition("Encounters/MarketSection", "Encounters/MarketSection"),
            new PrefabExportDefinition("UI/RunnerHudCanvas/TopLeftPanel/Combo", "UI/RunnerHudCanvas/TopLeftPanel/Combo"),
            new PrefabExportDefinition("UI/RunnerHudCanvas/TopLeftPanel/Phase", "UI/RunnerHudCanvas/TopLeftPanel/Phase"),
            new PrefabExportDefinition("UI/RunnerHudCanvas/TopLeftPanel", "UI/RunnerHudCanvas/TopLeftPanel"),
            new PrefabExportDefinition("UI/RunnerHudCanvas/TopCenterPanel/Status", "UI/RunnerHudCanvas/TopCenterPanel/Status"),
            new PrefabExportDefinition("UI/RunnerHudCanvas/TopCenterPanel", "UI/RunnerHudCanvas/TopCenterPanel"),
            new PrefabExportDefinition("UI/RunnerHudCanvas/BottomHintPanel/Hint", "UI/RunnerHudCanvas/BottomHintPanel/Hint"),
            new PrefabExportDefinition("UI/RunnerHudCanvas/BottomHintPanel", "UI/RunnerHudCanvas/BottomHintPanel"),
            new PrefabExportDefinition("UI/RunnerHudCanvas", "UI/RunnerHudCanvas"),
            new PrefabExportDefinition("Finish/FinishGate/GateFrame", "Finish/FinishGate/GateFrame"),
            new PrefabExportDefinition("Finish/FinishGate/FinishBanner", "Finish/FinishGate/FinishBanner"),
            new PrefabExportDefinition("Finish/FinishGate/TriggerVolume", "Finish/FinishGate/TriggerVolume"),
            new PrefabExportDefinition("Finish/FinishGate", "Finish/FinishGate"),
            new PrefabExportDefinition("Environment", "Environment"),
            new PrefabExportDefinition("Gameplay", "Gameplay"),
            new PrefabExportDefinition("Encounters", "Encounters"),
            new PrefabExportDefinition("UI", "UI"),
            new PrefabExportDefinition("Finish", "Finish"),
        };

        [MenuItem("Tools/SwiftRunner/Rebuild Playable Scene")]
        public static void RebuildPlayableScene()
        {
            var scene = OpenOrCreateScene();
            SwiftRunnerSceneInstaller.RebuildAuthoredScene(scene);
            CreateOrUpdateGroupPrefabs(scene);
            DeleteLegacyGeneratedAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SwiftRunner] Rebuilt preplaced scene: {ScenePath}");
        }

        [MenuItem("Tools/SwiftRunner/Update Group Prefabs")]
        public static void UpdateGroupPrefabs()
        {
            var scene = OpenOrCreateScene();
            CreateOrUpdateGroupPrefabs(scene);
            DeleteLegacyGeneratedAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SwiftRunner] Updated group prefabs for scene: {ScenePath}");
        }

        private static Scene OpenOrCreateScene()
        {
            if (File.Exists(ScenePath))
            {
                return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            return scene;
        }

        private static void CreateOrUpdateGroupPrefabs(Scene scene)
        {
            EnsureFolder(PrefabFolderPath);

            var bootstrap = FindRoot(scene, "SwiftRunnerBootstrap");
            if (bootstrap == null)
            {
                Debug.LogWarning("[SwiftRunner] SwiftRunnerBootstrap not found; skipped prefab export.");
                return;
            }

            for (var index = 0; index < ExportDefinitions.Length; index++)
            {
                var exportDefinition = ExportDefinitions[index];
                var groupTransform = bootstrap.transform.Find(exportDefinition.SceneRelativePath);
                if (groupTransform == null)
                {
                    continue;
                }

                var prefabPath = $"{PrefabFolderPath}/{exportDefinition.PrefabRelativePath}.prefab";
                EnsureFolder(Path.GetDirectoryName(prefabPath)?.Replace('\\', '/') ?? PrefabFolderPath);
                PrefabUtility.SaveAsPrefabAssetAndConnect(
                    groupTransform.gameObject,
                    prefabPath,
                    InteractionMode.AutomatedAction);
            }
        }

        private static void DeleteLegacyGeneratedAssets()
        {
            for (var index = 0; index < LegacyGeneratedAssetPaths.Length; index++)
            {
                var assetPath = LegacyGeneratedAssetPaths[index];
                if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) == null)
                {
                    continue;
                }

                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private static GameObject FindRoot(Scene scene, string rootName)
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

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var segments = assetPath.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }

        private readonly struct PrefabExportDefinition
        {
            public PrefabExportDefinition(string sceneRelativePath, string prefabRelativePath)
            {
                SceneRelativePath = sceneRelativePath;
                PrefabRelativePath = prefabRelativePath;
            }

            public string SceneRelativePath { get; }
            public string PrefabRelativePath { get; }
        }
    }
}
#endif
