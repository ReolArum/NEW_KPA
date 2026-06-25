using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemoryColoseum.Combat.Editor
{
    public static class MixamoAnimationTestSceneBuilder
    {
        private const string ModelPath = "Assets/Art/Characters/XBot/X Bot.fbx";
        private const string AnimationFolder = "Assets/Art/Animations/Mixamo";
        private const string ControllerPath = "Assets/Art/Animations/Mixamo/XBot_Mixamo_Test.controller";
        private const string ScenePath = "Assets/Scenes/MixamoAnimationTest.unity";

        [MenuItem("Memory Coloseum/Build Mixamo Animation Test Scene")]
        public static void Build()
        {
            ConfigureHumanoidImports();

            AnimatorController controller = CreateController();
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (modelPrefab == null)
            {
                throw new FileNotFoundException($"Model was not found at {ModelPath}");
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;

            GameObject cameraObject = new("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 1.35f, -4.2f), Quaternion.Euler(12f, 0f, 0f));
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f);

            GameObject lightObject = new("Key Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Reference Floor";
            floor.transform.localScale = new Vector3(1.8f, 1f, 1.8f);

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, scene);
            model.name = "X Bot Mixamo Test";
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));

            Animator animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            MixamoAnimationInputDriver driver = model.GetComponent<MixamoAnimationInputDriver>();
            if (driver == null)
            {
                driver = model.AddComponent<MixamoAnimationInputDriver>();
            }

            SerializedObject serializedDriver = new(driver);
            serializedDriver.FindProperty("animator").objectReferenceValue = animator;
            serializedDriver.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Built Mixamo animation test scene at {ScenePath}. Press 1 Idle, 2 Punch, 3 Kick, 4 Victory, 5 Defeat.");
        }

        private static void ConfigureHumanoidImports()
        {
            string[] paths =
            {
                ModelPath,
                $"{AnimationFolder}/Standing Idle.fbx",
                $"{AnimationFolder}/Punching.fbx",
                $"{AnimationFolder}/Mma Kick.fbx",
                $"{AnimationFolder}/Victory.fbx",
                $"{AnimationFolder}/Defeat.fbx"
            };

            foreach (string path in paths)
            {
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    throw new FileNotFoundException($"FBX importer was not found at {path}");
                }

                bool changed = importer.animationType != ModelImporterAnimationType.Human;
                importer.animationType = ModelImporterAnimationType.Human;

                if (path != ModelPath && importer.materialImportMode != ModelImporterMaterialImportMode.None)
                {
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static AnimatorController CreateController()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

            AddState(stateMachine, "Idle", $"{AnimationFolder}/Standing Idle.fbx", true);
            AddState(stateMachine, "Punch", $"{AnimationFolder}/Punching.fbx", false);
            AddState(stateMachine, "Kick", $"{AnimationFolder}/Mma Kick.fbx", false);
            AddState(stateMachine, "Victory", $"{AnimationFolder}/Victory.fbx", false);
            AddState(stateMachine, "Defeat", $"{AnimationFolder}/Defeat.fbx", false);

            return controller;
        }

        private static void AddState(AnimatorStateMachine stateMachine, string stateName, string assetPath, bool isDefault)
        {
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", System.StringComparison.Ordinal));

            if (clip == null)
            {
                throw new FileNotFoundException($"Animation clip was not found in {assetPath}");
            }

            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clip;

            if (isDefault)
            {
                stateMachine.defaultState = state;
            }
        }
    }
}
