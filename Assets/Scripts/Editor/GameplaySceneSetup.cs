using System;
using System.Linq;
using MyoGuess.Gameplay;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameplaySceneSetup
{
    [MenuItem("Myo Guess/Configurar gameplay")]
    public static void Configure()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/Gameplay.unity")
            throw new InvalidOperationException("Abra Assets/Scenes/Gameplay.unity fora do Play Mode.");

        var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform Find(string name) => transforms.Single(t => t.name == name);
        var manager = Find("GameplayManager").gameObject;
        var controller = manager.GetComponent<GestureRoundController>();
        var canvas = Find("Gameplay").GetComponent<Canvas>();
        if (controller == null || canvas == null) throw new InvalidOperationException("Gameplay incompleto.");
        var font = Find("ScoreText").GetComponent<TMP_Text>().font;
        var source = manager.GetComponent<HeadsetGestureSource>() ?? Undo.AddComponent<HeadsetGestureSource>(manager);
        var fbg = manager.GetComponent<FbgGestureSource>() ?? Undo.AddComponent<FbgGestureSource>(manager);
        var panel = manager.GetComponent<GameOverPanel>() ?? Undo.AddComponent<GameOverPanel>(manager);

        var poseNames = new[] { "StopPoseRight", "PointerPoseRight", "RockPoseRight", "TripodPoseRight", "ThumbsUpRight", "RestPoseRight" };
        var gestureIds = new[] { GestureType.OpenHand, GestureType.Pointer, GestureType.Rock, GestureType.Tripod, GestureType.ThumbsUp, GestureType.Rest };
        var spriteNames = new[] { "open", "pointer", "rock", "tripod", "ok", "rest" };
        var sourceSettings = new SerializedObject(source);
        var bindings = sourceSettings.FindProperty("bindings");
        bindings.arraySize = poseNames.Length;
        for (int i = 0; i < poseNames.Length; i++)
        {
            var state = Find(poseNames[i]).GetComponent<ActiveStateGroup>();
            if (state == null) throw new InvalidOperationException("Pose sem ActiveStateGroup: " + poseNames[i]);
            bindings.GetArrayElementAtIndex(i).FindPropertyRelative("gesture").enumValueIndex = (int)gestureIds[i];
            bindings.GetArrayElementAtIndex(i).FindPropertyRelative("activeState").objectReferenceValue = state;
        }
        sourceSettings.ApplyModifiedProperties();

        var pointable = canvas.GetComponent<PointableCanvas>() ?? Undo.AddComponent<PointableCanvas>(canvas.gameObject);
        pointable.InjectCanvas(canvas);
        EditorUtility.SetDirty(pointable);
        canvas.worldCamera = Camera.main;

        var overlay = canvas.transform.Find("GameOverOverlay") as RectTransform;
        if (overlay == null)
        {
            overlay = Rect("GameOverOverlay", canvas.transform, new Vector2(760, 470), Vector2.zero);
            var background = overlay.gameObject.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.055f, 0.015f, 0.09f, 0.98f);
            background.raycastTarget = true;
            var outline = overlay.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.65f, 0.15f, 1f, 0.9f);
            outline.effectDistance = new Vector2(2, -2);
            Text("Title", overlay, font, "GAME OVER", 52, new Vector2(660, 70), new Vector2(0, 155), new Color(0.94f, 0.45f, 1f));
            Text("Subtitle", overlay, font, "SUAS VIDAS ACABARAM", 20, new Vector2(660, 34), new Vector2(0, 101), Color.white);
            Text("FinalScore", overlay, font, "SEU SCORE\n0", 32, new Vector2(290, 105), new Vector2(-155, 12), Color.white);
            Text("HighScore", overlay, font, "RECORDE\n0", 32, new Vector2(290, 105), new Vector2(155, 12), new Color(0f, 1f, 0.86f));
            Button("RestartButton", overlay, font, "JOGAR NOVAMENTE", new Vector2(0, -102), new Color(0.42f, 0.05f, 0.65f));
            Button("MenuButton", overlay, font, "SAIR PARA O MENU", new Vector2(0, -174), new Color(0.1f, 0.16f, 0.2f));

            var plane = overlay.gameObject.AddComponent<PlaneSurface>();
            plane.InjectAllPlaneSurface(PlaneSurface.NormalFacing.Backward, false);
            var bounds = overlay.gameObject.AddComponent<BoundsClipper>();
            bounds.Size = new Vector3(760, 470, 1);
            var clipped = overlay.gameObject.AddComponent<ClippedPlaneSurface>();
            clipped.InjectAllClippedPlaneSurface(plane, new IBoundsClipper[] { bounds });
            var ray = overlay.gameObject.AddComponent<RayInteractable>();
            ray.InjectAllRayInteractable(clipped);
            ray.InjectOptionalPointableElement(pointable);
        }
        var panelSettings = new SerializedObject(panel);
        panelSettings.FindProperty("panelRoot").objectReferenceValue = overlay.gameObject;
        panelSettings.FindProperty("scoreText").objectReferenceValue = overlay.Find("FinalScore").GetComponent<TMP_Text>();
        panelSettings.FindProperty("highScoreText").objectReferenceValue = overlay.Find("HighScore").GetComponent<TMP_Text>();
        panelSettings.FindProperty("restartButton").objectReferenceValue = overlay.Find("RestartButton").GetComponent<UnityEngine.UI.Button>();
        panelSettings.FindProperty("menuButton").objectReferenceValue = overlay.Find("MenuButton").GetComponent<UnityEngine.UI.Button>();
        panelSettings.ApplyModifiedProperties();
        overlay.gameObject.SetActive(false);

        var feedback = canvas.transform.Find("RoundFeedback")?.GetComponent<TMP_Text>();
        if (feedback == null)
            feedback = Text("RoundFeedback", canvas.transform, font, "COPIE O GESTO", 24, new Vector2(760, 48), new Vector2(0, -215), Color.white);
        var settings = new SerializedObject(controller);
        settings.FindProperty("inputMode").enumValueIndex = (int)GestureInputMode.Headset;
        settings.FindProperty("headsetSource").objectReferenceValue = source;
        settings.FindProperty("fbgSource").objectReferenceValue = fbg;
        settings.FindProperty("scoreText").objectReferenceValue = Find("ScoreText").GetComponent<TMP_Text>();
        settings.FindProperty("feedbackText").objectReferenceValue = feedback;
        settings.FindProperty("gameOverPanel").objectReferenceValue = panel;
        settings.FindProperty("startingLives").intValue = 3;
        settings.FindProperty("roundDuration").floatValue = 5f;
        settings.FindProperty("evaluationLeadTime").floatValue = 0.1f;
        var lives = settings.FindProperty("lifeImages");
        lives.arraySize = 3;
        for (int i = 0; i < 3; i++) lives.GetArrayElementAtIndex(i).objectReferenceValue = Find("LivesGroup").GetChild(i).GetComponent<UnityEngine.UI.Image>();
        var gestures = settings.FindProperty("gestures");
        gestures.arraySize = gestureIds.Length;
        for (int i = 0; i < gestureIds.Length; i++)
        {
            gestures.GetArrayElementAtIndex(i).FindPropertyRelative("gesture").enumValueIndex = (int)gestureIds[i];
            gestures.GetArrayElementAtIndex(i).FindPropertyRelative("image").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/" + spriteNames[i] + ".png");
        }
        settings.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = manager;
        Debug.Log("Gameplay configurado: óculos, 3 vidas, score e painel de game over.");
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Configurar gameplay");
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, float size, Vector2 dimensions, Vector2 position, Color color)
    {
        var text = Rect(name, parent, dimensions, position).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static void Button(string name, Transform parent, TMP_FontAsset font, string label, Vector2 position, Color color)
    {
        var rect = Rect(name, parent, new Vector2(570, 58), position);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color;
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.75f, 1f, 1f);
        colors.pressedColor = new Color(0.5f, 0.65f, 0.8f);
        button.colors = colors;
        Text("Label", rect, font, label, 24, new Vector2(550, 54), Vector2.zero, Color.white);
    }
}
