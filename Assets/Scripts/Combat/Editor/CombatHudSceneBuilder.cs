using MemoryColoseum.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MemoryColoseum.CombatEditor
{
    public static class CombatHudSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Memory Coloseum/Combat/Rebuild Direct-Editable HUD")]
        public static void RebuildSampleSceneHud()
        {
            EnsureDefaultGuardAssets();
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RebuildHudInOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Memory Coloseum/Combat/Create Default Guards")]
        public static void EnsureDefaultGuardAssets()
        {
            const string guardFolder = "Assets/Resources/Combat/Guards";
            Directory.CreateDirectory(guardFolder);

            CombatGuardAsset barrier = EnsureGuardAsset(
                $"{guardFolder}/BarrierGuard.asset",
                "barrier_guard",
                "Barrier Guard",
                GuardStyle.Guard,
                1.5f,
                0f,
                0f,
                true,
                false);

            CombatGuardAsset reduction = EnsureGuardAsset(
                $"{guardFolder}/DamageReductionGuard.asset",
                "damage_reduction_guard",
                "Damage Reduction",
                GuardStyle.DamageReduction,
                3f,
                0.4f,
                0f,
                false,
                false);

            CombatGuardAsset counter = EnsureGuardAsset(
                $"{guardFolder}/CounterGuard.asset",
                "counter_guard",
                "Counter Guard",
                GuardStyle.Counter,
                1f,
                0f,
                0.3f,
                true,
                true);

            CombatGuardAsset[] guards = { barrier, reduction, counter };
            EnsureLoadoutGuards("Assets/Resources/Combat/Loadouts/DebugPlayerLoadout.asset", guards);
            EnsureLoadoutGuards("Assets/Resources/Combat/Loadouts/DebugEnemyLoadout.asset", guards);
            AssetDatabase.SaveAssets();
        }

        public static void RebuildHudInOpenScene()
        {
            DestroyIfExists("Combat Direct HUD");
            EnsureEventSystem();

            Canvas canvas = CreateRootCanvas();
            CombatHudView hudView = canvas.gameObject.AddComponent<CombatHudView>();

            Text playerHp;
            Slider playerHpSlider;
            Text playerGuard;
            Slider playerGuardSlider;
            Text playerAction;
            Slider playerCastSlider;
            Text playerQueued;
            Text playerQueueList;
            Text playerSlots;
            CreateCombatantPanel(canvas.transform, "Player Panel", "Player", new Vector2(24f, -24f), TextAnchor.UpperLeft, out playerHp, out playerHpSlider, out playerGuard, out playerGuardSlider, out playerAction, out playerCastSlider, out playerQueued, out playerQueueList, out playerSlots);

            Text enemyHp;
            Slider enemyHpSlider;
            Text enemyGuard;
            Slider enemyGuardSlider;
            Text enemyAction;
            Slider enemyCastSlider;
            Text enemyQueued;
            Text enemyQueueList;
            Text enemySlots;
            CreateCombatantPanel(canvas.transform, "Enemy Panel", "Enemy", new Vector2(-24f, -24f), TextAnchor.UpperRight, out enemyHp, out enemyHpSlider, out enemyGuard, out enemyGuardSlider, out enemyAction, out enemyCastSlider, out enemyQueued, out enemyQueueList, out enemySlots);

            Text timeText;
            Text statusText;
            CreateStatusPanel(canvas.transform, out timeText, out statusText);

            Text playerFeedbackText;
            Text enemyFeedbackText;
            CreateCombatFeedback(canvas.transform, out playerFeedbackText, out enemyFeedbackText);

            Text loadoutStatus;
            Button loadoutShortcutButton;
            Text loadoutShortcutLabel;
            Button[] slotButtons;
            Text[] slotLabels;
            Image[] slotImages;
            Button[] skillButtons;
            Text[] skillLabels;
            Text equippedGuardLabel;
            Button[] guardButtons;
            Text[] guardLabels;
            Image[] guardImages;
            CreateLoadoutShortcut(canvas.transform, out loadoutShortcutButton, out loadoutShortcutLabel);
            CreateLoadoutPanel(canvas.transform, out loadoutStatus, out slotButtons, out slotLabels, out slotImages, out skillButtons, out skillLabels, out equippedGuardLabel, out guardButtons, out guardLabels, out guardImages);

            Button[] blockButtons;
            Text[] blockLabels;
            Text[] blockChainLabels;
            Image[] blockImages;
            Button guardButton;
            Button restartButton;
            CreateBoardPanel(canvas.transform, out blockButtons, out blockLabels, out blockChainLabels, out blockImages, out guardButton, out restartButton);

            Text logText = CreateLogPanel(canvas.transform);

            SerializedObject serializedHud = new(hudView);
            Set(serializedHud, "playerHpText", playerHp);
            Set(serializedHud, "playerHpSlider", playerHpSlider);
            Set(serializedHud, "playerGuardText", playerGuard);
            Set(serializedHud, "playerGuardSlider", playerGuardSlider);
            Set(serializedHud, "playerActionText", playerAction);
            Set(serializedHud, "playerCastSlider", playerCastSlider);
            Set(serializedHud, "playerQueuedText", playerQueued);
            Set(serializedHud, "playerQueueListText", playerQueueList);
            Set(serializedHud, "playerSlotsText", playerSlots);
            Set(serializedHud, "enemyHpText", enemyHp);
            Set(serializedHud, "enemyHpSlider", enemyHpSlider);
            Set(serializedHud, "enemyGuardText", enemyGuard);
            Set(serializedHud, "enemyGuardSlider", enemyGuardSlider);
            Set(serializedHud, "enemyActionText", enemyAction);
            Set(serializedHud, "enemyCastSlider", enemyCastSlider);
            Set(serializedHud, "enemyQueuedText", enemyQueued);
            Set(serializedHud, "enemyQueueListText", enemyQueueList);
            Set(serializedHud, "enemySlotsText", enemySlots);
            Set(serializedHud, "timeText", timeText);
            Set(serializedHud, "statusText", statusText);
            Set(serializedHud, "logText", logText);
            Set(serializedHud, "playerFeedbackText", playerFeedbackText);
            Set(serializedHud, "enemyFeedbackText", enemyFeedbackText);
            Set(serializedHud, "loadoutShortcutButton", loadoutShortcutButton);
            Set(serializedHud, "loadoutShortcutLabel", loadoutShortcutLabel);
            Set(serializedHud, "loadoutStatusText", loadoutStatus);
            SetArray(serializedHud, "loadoutSlotButtons", slotButtons);
            SetArray(serializedHud, "loadoutSlotLabels", slotLabels);
            SetArray(serializedHud, "loadoutSlotImages", slotImages);
            SetArray(serializedHud, "ownedSkillButtons", skillButtons);
            SetArray(serializedHud, "ownedSkillLabels", skillLabels);
            Set(serializedHud, "equippedGuardLabel", equippedGuardLabel);
            SetArray(serializedHud, "ownedGuardButtons", guardButtons);
            SetArray(serializedHud, "ownedGuardLabels", guardLabels);
            SetArray(serializedHud, "ownedGuardImages", guardImages);
            SetArray(serializedHud, "blockButtons", blockButtons);
            SetArray(serializedHud, "blockLabels", blockLabels);
            SetArray(serializedHud, "blockChainLabels", blockChainLabels);
            SetArray(serializedHud, "blockImages", blockImages);
            Set(serializedHud, "guardButton", guardButton);
            Set(serializedHud, "restartButton", restartButton);
            serializedHud.ApplyModifiedPropertiesWithoutUndo();

            CombatDebugDriver driver = Object.FindAnyObjectByType<CombatDebugDriver>();
            if (driver != null)
            {
                SerializedObject serializedDriver = new(driver);
                Set(serializedDriver, "hudView", hudView);
                serializedDriver.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Canvas CreateRootCanvas()
        {
            GameObject canvasObject = new("Combat Direct HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateCombatantPanel(
            Transform parent,
            string objectName,
            string title,
            Vector2 anchoredPosition,
            TextAnchor anchor,
            out Text hpText,
            out Slider hpSlider,
            out Text guardText,
            out Slider guardSlider,
            out Text actionText,
            out Slider castSlider,
            out Text queuedText,
            out Text queueListText,
            out Text slotsText)
        {
            RectTransform panel = CreatePanel(parent, objectName, new Vector2(360f, 276f), anchoredPosition, anchor);
            CreateText(panel, title, new Vector2(16f, -14f), new Vector2(328f, 28f), 20, FontStyle.Bold);
            hpText = CreateText(panel, "HP", new Vector2(16f, -50f), new Vector2(328f, 22f), 14, FontStyle.Normal);
            hpSlider = CreateSlider(panel, "HP Slider", new Vector2(16f, -76f), new Vector2(328f, 12f));
            guardText = CreateText(panel, "Guard", new Vector2(16f, -96f), new Vector2(328f, 22f), 14, FontStyle.Normal);
            guardSlider = CreateSlider(panel, "Guard Slider", new Vector2(16f, -122f), new Vector2(328f, 10f));
            actionText = CreateText(panel, "Action", new Vector2(16f, -142f), new Vector2(328f, 20f), 13, FontStyle.Normal);
            castSlider = CreateSlider(panel, "Action Phase Slider", new Vector2(16f, -166f), new Vector2(328f, 10f));
            queuedText = CreateText(panel, "Queued", new Vector2(16f, -184f), new Vector2(328f, 20f), 13, FontStyle.Normal);
            queueListText = CreateText(panel, "Queue: Empty", new Vector2(16f, -206f), new Vector2(328f, 36f), 12, FontStyle.Normal);
            slotsText = CreateText(panel, "Slots", new Vector2(16f, -246f), new Vector2(328f, 20f), 12, FontStyle.Normal);
        }

        private static void CreateStatusPanel(Transform parent, out Text timeText, out Text statusText)
        {
            RectTransform panel = CreatePanel(parent, "Center Status Panel", new Vector2(360f, 110f), new Vector2(0f, -24f), TextAnchor.UpperCenter);
            timeText = CreateText(panel, "Time", new Vector2(16f, -14f), new Vector2(328f, 30f), 20, FontStyle.Bold);
            statusText = CreateText(panel, "Status", new Vector2(16f, -50f), new Vector2(328f, 46f), 13, FontStyle.Normal);
        }

        private static void CreateCombatFeedback(Transform parent, out Text playerFeedbackText, out Text enemyFeedbackText)
        {
            playerFeedbackText = CreateFloatingText(parent, "Player Damage Feedback", new Vector2(-250f, -32f), TextAnchor.MiddleCenter);
            enemyFeedbackText = CreateFloatingText(parent, "Enemy Damage Feedback", new Vector2(250f, -32f), TextAnchor.MiddleCenter);
        }

        private static void CreateLoadoutPanel(
            Transform parent,
            out Text loadoutStatus,
            out Button[] slotButtons,
            out Text[] slotLabels,
            out Image[] slotImages,
            out Button[] skillButtons,
            out Text[] skillLabels,
            out Text equippedGuardLabel,
            out Button[] guardButtons,
            out Text[] guardLabels,
            out Image[] guardImages)
        {
            RectTransform panel = CreatePanel(parent, "Loadout Panel", new Vector2(620f, 236f), new Vector2(0f, -144f), TextAnchor.UpperCenter);
            CreateText(panel, "Player Loadout", new Vector2(16f, -12f), new Vector2(488f, 26f), 18, FontStyle.Bold);
            loadoutStatus = CreateText(panel, "Loadout ready.", new Vector2(16f, -42f), new Vector2(588f, 22f), 12, FontStyle.Normal);

            slotButtons = new Button[3];
            slotLabels = new Text[3];
            slotImages = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                Button button = CreateButton(panel, $"Slot {i + 1}", new Vector2(16f + (i * 196f), -74f), new Vector2(186f, 48f), out Text label, out Image image);
                slotButtons[i] = button;
                slotLabels[i] = label;
                slotImages[i] = image;
            }

            CreateText(panel, "Owned Skills", new Vector2(16f, -126f), new Vector2(488f, 18f), 12, FontStyle.Normal);
            skillButtons = new Button[5];
            skillLabels = new Text[5];
            for (int i = 0; i < skillButtons.Length; i++)
            {
                skillButtons[i] = CreateButton(panel, $"Owned Skill {i + 1}", new Vector2(16f + (i * 116f), -148f), new Vector2(108f, 26f), out skillLabels[i], out _);
            }

            equippedGuardLabel = CreateText(panel, "Guard: Empty", new Vector2(16f, -178f), new Vector2(588f, 18f), 12, FontStyle.Bold);
            guardButtons = new Button[3];
            guardLabels = new Text[3];
            guardImages = new Image[3];
            for (int i = 0; i < guardButtons.Length; i++)
            {
                guardButtons[i] = CreateButton(panel, $"Guard {i + 1}", new Vector2(16f + (i * 196f), -200f), new Vector2(186f, 26f), out guardLabels[i], out guardImages[i]);
            }
        }

        private static void CreateLoadoutShortcut(Transform parent, out Button button, out Text label)
        {
            button = CreateButton(parent, "Loadout Shortcut", new Vector2(24f, -320f), new Vector2(72f, 48f), out label, out Image image);
            image.color = new Color(0.18f, 0.7f, 1f, 1f);
            label.text = "LOAD";
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }

        private static void CreateBoardPanel(
            Transform parent,
            out Button[] blockButtons,
            out Text[] blockLabels,
            out Text[] blockChainLabels,
            out Image[] blockImages,
            out Button guardButton,
            out Button restartButton)
        {
            RectTransform panel = CreateStretchPanel(parent, "Player Block Board Panel", new Vector2(24f, 150f), new Vector2(24f, 286f));
            CreateText(panel, "Player Block Board", new Vector2(16f, -12f), new Vector2(320f, 26f), 18, FontStyle.Bold);

            blockButtons = new Button[10];
            blockLabels = new Text[10];
            blockChainLabels = new Text[10];
            blockImages = new Image[10];
            for (int i = 0; i < blockButtons.Length; i++)
            {
                float x = 16f + (i * 72f);
                blockChainLabels[i] = CreateText(panel, string.Empty, new Vector2(x, -44f), new Vector2(64f, 18f), 12, FontStyle.Bold);
                blockButtons[i] = CreateButton(panel, $"Block {i + 1}", new Vector2(x, -66f), new Vector2(64f, 42f), out blockLabels[i], out blockImages[i]);
            }

            guardButton = CreateButton(panel, "Guard Button", new Vector2(756f, -66f), new Vector2(90f, 42f), out Text guardLabel, out _);
            guardLabel.text = "Guard";
            restartButton = CreateButton(panel, "Restart Button", new Vector2(856f, -66f), new Vector2(90f, 42f), out Text restartLabel, out _);
            restartLabel.text = "Restart";
        }

        private static Text CreateLogPanel(Transform parent)
        {
            RectTransform panel = CreateStretchPanel(parent, "Combat Log Panel", new Vector2(24f, 24f), new Vector2(24f, 136f));
            CreateText(panel, "Combat Log", new Vector2(16f, -10f), new Vector2(300f, 24f), 18, FontStyle.Bold);
            return CreateText(panel, string.Empty, new Vector2(16f, -38f), new Vector2(1100f, 82f), 12, FontStyle.Normal);
        }

        private static RectTransform CreatePanel(Transform parent, string name, Vector2 size, Vector2 anchoredPosition, TextAnchor anchor)
        {
            GameObject gameObject = new(name);
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            SetAnchor(rect, anchor);
            rect.anchoredPosition = anchoredPosition;

            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.09f, 0.12f, 0.88f);
            return rect;
        }

        private static RectTransform CreateStretchPanel(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject gameObject = new(name);
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = new Vector2(-offsetMax.x, offsetMax.y);

            Image image = gameObject.AddComponent<Image>();
            image.color = new Color(0.08f, 0.09f, 0.12f, 0.88f);
            return rect;
        }

        private static Text CreateText(Transform parent, string value, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle style)
        {
            GameObject gameObject = new(value.Length > 0 ? value : "Text");
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = gameObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.raycastTarget = false;
            return text;
        }

        private static Text CreateFloatingText(Transform parent, string name, Vector2 anchoredPosition, TextAnchor alignment)
        {
            GameObject gameObject = new(name);
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(260f, 100f);

            Text text = gameObject.AddComponent<Text>();
            text.text = string.Empty;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 0.32f, 0.28f, 0f);
            text.alignment = alignment;
            text.raycastTarget = false;
            gameObject.SetActive(false);
            return text;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, out Text label, out Image image)
        {
            GameObject gameObject = new(name);
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            image = gameObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.2f, 0.25f, 1f);
            Button button = gameObject.AddComponent<Button>();

            label = CreateText(rect, name, Vector2.zero, size, 12, FontStyle.Bold);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject gameObject = new(name);
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Slider slider = gameObject.AddComponent<Slider>();
            Image background = gameObject.AddComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0.12f);

            GameObject fillObject = new("Fill");
            RectTransform fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.SetParent(rect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fill = fillObject.AddComponent<Image>();
            fill.color = new Color(0.2f, 0.75f, 0.95f, 1f);
            slider.fillRect = fillRect;
            slider.targetGraphic = fill;
            slider.interactable = false;
            return slider;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static void DestroyIfExists(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static CombatGuardAsset EnsureGuardAsset(
            string path,
            string guardId,
            string displayName,
            GuardStyle style,
            float duration,
            float damageReductionRatio,
            float reflectDamageRatio,
            bool nullifiesFirstHit,
            bool reflectsStatusEffects)
        {
            CombatGuardAsset guard = AssetDatabase.LoadAssetAtPath<CombatGuardAsset>(path);
            if (guard == null)
            {
                guard = ScriptableObject.CreateInstance<CombatGuardAsset>();
                AssetDatabase.CreateAsset(guard, path);
            }

            SerializedObject serializedGuard = new(guard);
            serializedGuard.FindProperty("guardId").stringValue = guardId;
            serializedGuard.FindProperty("displayName").stringValue = displayName;
            serializedGuard.FindProperty("style").enumValueIndex = (int)style;
            serializedGuard.FindProperty("duration").floatValue = duration;
            serializedGuard.FindProperty("damageReductionRatio").floatValue = damageReductionRatio;
            serializedGuard.FindProperty("reflectDamageRatio").floatValue = reflectDamageRatio;
            serializedGuard.FindProperty("nullifiesFirstHit").boolValue = nullifiesFirstHit;
            serializedGuard.FindProperty("reflectsStatusEffects").boolValue = reflectsStatusEffects;
            serializedGuard.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(guard);
            return guard;
        }

        private static void EnsureLoadoutGuards(string path, CombatGuardAsset[] guards)
        {
            CombatLoadoutAsset loadout = AssetDatabase.LoadAssetAtPath<CombatLoadoutAsset>(path);
            if (loadout == null)
            {
                return;
            }

            SerializedObject serializedLoadout = new(loadout);
            SerializedProperty ownedGuards = serializedLoadout.FindProperty("ownedGuards");
            ownedGuards.arraySize = guards.Length;
            for (int i = 0; i < guards.Length; i++)
            {
                ownedGuards.GetArrayElementAtIndex(i).objectReferenceValue = guards[i];
            }

            serializedLoadout.FindProperty("equippedGuard").objectReferenceValue = guards[0];
            serializedLoadout.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(loadout);
        }

        private static void SetAnchor(RectTransform rect, TextAnchor anchor)
        {
            if (anchor == TextAnchor.UpperLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                return;
            }

            if (anchor == TextAnchor.UpperRight)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
        }

        private static void Set(SerializedObject serializedObject, string propertyName, Object value)
        {
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void SetArray<T>(SerializedObject serializedObject, string propertyName, T[] values) where T : Object
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
