#if UNITY_EDITOR
using System;
using System.Linq;
using MyoGuess.MainMenu;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyoGuess.EditorTools
{
    public static class MirrorStrikeMainMenuBuilder
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string RootName = "MirrorStrikeMenu";
        private static readonly Color Cyan = new Color(0f, 1f, 0.86f, 1f);
        private static readonly Color Magenta = new Color(1f, 0f, 0.82f, 1f);

        [InitializeOnLoadMethod]
        private static void BuildOnceWhenImported()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            EditorApplication.delayCall += TryAutoBuild;
        }

        private static void OnActiveSceneChanged(Scene previous, Scene next)
        {
            TryAutoBuild();
        }

        private static void TryAutoBuild()
        {
            const string sessionKey = "MyoGuess.MirrorStrikeMainMenu.AutoBuild.v1";
            if (SessionState.GetBool(sessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != ScenePath)
            {
                return;
            }

            SessionState.SetBool(sessionKey, true);
            if (activeScene.GetRootGameObjects().Any(go => go.name == RootName))
            {
                return;
            }

            BuildAndSave();
        }

        [MenuItem("Tools/Myo Guess/Build Mirror Strike Main Menu")]
        public static void BuildFromMenu()
        {
            BuildAndSave();
            Selection.activeGameObject = GameObject.Find(RootName);
        }

        public static void BuildBatch()
        {
            BuildAndSave();
            EditorApplication.Exit(0);
        }

        private static void BuildAndSave()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            Scene scene = activeScene.path == ScenePath
                ? activeScene
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject previous = scene.GetRootGameObjects().FirstOrDefault(go => go.name == RootName);
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous);
            }

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.006f, 0.004f, 0.018f, 1f);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Mirror Strike menu");

            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1920f, 1080f);
            canvasRect.position = new Vector3(0f, 1.62f, 3f);
            canvasRect.rotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * 0.00172f;

            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            root.AddComponent<GraphicRaycaster>();

            MirrorStrikeBackdrop backdrop = CreateStretch<MirrorStrikeBackdrop>("NeonBackdrop", canvasRect);
            backdrop.raycastTarget = false;

            CanvasGroup content = CreateStretch<CanvasGroup>("Content", canvasRect);
            RectTransform contentRect = content.GetComponent<RectTransform>();

            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font == null)
            {
                throw new InvalidOperationException("TextMesh Pro Essentials não foi encontrado.");
            }

            CreateText("Eyebrow", contentRect, font, "META QUEST 3  ·  GESTURE ARENA",
                17f, FontStyles.Bold, Cyan, new Vector2(0f, 218f), new Vector2(700f, 35f), 12f);
            CreateText("Mirror", contentRect, font, "MIRROR",
                59f, FontStyles.Bold, Color.white, new Vector2(0f, 153f), new Vector2(700f, 80f), -2f);
            CreateText("Strike", contentRect, font, "STRIKE",
                59f, FontStyles.Bold, Magenta, new Vector2(0f, 88f), new Vector2(700f, 80f), -2f);
            CreateText("Subtitle", contentRect, font,
                "Gestos virão em sua direção — copie-os para marcar pontos",
                17f, FontStyles.Normal, new Color(0.63f, 0.57f, 0.68f, 1f),
                new Vector2(0f, 22f), new Vector2(900f, 42f), 1.1f);

            Button startButton = CreateStartButton(contentRect, font);

            GameObject gestureObject = CreateUIObject("GestureStrip", contentRect);
            RectTransform gestureRect = gestureObject.GetComponent<RectTransform>();
            SetCentered(gestureRect, new Vector2(0f, -184f), new Vector2(310f, 70f));
            MirrorStrikeGestureStrip gestureStrip = gestureObject.AddComponent<MirrorStrikeGestureStrip>();
            gestureStrip.raycastTarget = false;

            MirrorStrikeMainMenuController controller = root.AddComponent<MirrorStrikeMainMenuController>();
            controller.Configure(startButton, content);

            EnsureDesktopEventSystem();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Myo Guess] MainMenu recriado com o tema Mirror Strike.");
        }

        private static Button CreateStartButton(RectTransform parent, TMP_FontAsset font)
        {
            GameObject buttonObject = CreateUIObject("StartButton", parent);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            SetCentered(buttonRect, new Vector2(0f, -82f), new Vector2(195f, 62f));

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.06f, 0.025f, 0.16f, 0.93f);
            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.72f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.72f, 1f, 0.97f, 1f);
            colors.pressedColor = new Color(0.95f, 0.55f, 1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            TextMeshProUGUI label = CreateText("Label", buttonRect, font, "INICIAR",
                18f, FontStyles.Bold, Cyan, Vector2.zero, new Vector2(195f, 62f), 7f);
            label.raycastTarget = false;
            return button;
        }

        private static TextMeshProUGUI CreateText(string name, RectTransform parent, TMP_FontAsset font,
            string value, float size, FontStyles style, Color color, Vector2 position, Vector2 dimensions,
            float characterSpacing)
        {
            GameObject textObject = CreateUIObject(name, parent);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetCentered(rect, position, dimensions);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.characterSpacing = characterSpacing;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static T CreateStretch<T>(string name, RectTransform parent) where T : Component
        {
            GameObject gameObject = CreateUIObject(name, parent);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return gameObject.AddComponent<T>();
        }

        private static GameObject CreateUIObject(string name, RectTransform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            gameObject.layer = LayerMask.NameToLayer("UI");
            return gameObject;
        }

        private static void SetCentered(RectTransform rect, Vector2 position, Vector2 dimensions)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
        }

        private static void EnsureDesktopEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }
    }
}
#endif
