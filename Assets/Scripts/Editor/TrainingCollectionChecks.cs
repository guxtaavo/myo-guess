using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using MyoGuess.Gameplay;
using MyoGuess.Training;
using Oculus.Interaction.Locomotion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TrainingCollectionChecks
{
    [MenuItem("Myo Guess/Validar coleta do treinamento")]
    public static void RunFromMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Valide fora do Play Mode.");
        int count = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            count++;
        }
        string[] ReadLines(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        string directory = Path.Combine(Application.dataPath, "../Logs/TrainingCollectionChecks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var originalCulture = CultureInfo.CurrentCulture;
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Training.unity");
        GameObject fakePoses = null;
        TrainingCsvWriter recording = null;
        TrainingInterfaceController controller = null;
        var privateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        void Set(string name, object value) => typeof(TrainingInterfaceController).GetField(name, privateInstance).SetValue(controller, value);
        object Call(string name, params object[] arguments) => typeof(TrainingInterfaceController).GetMethod(name, privateInstance).Invoke(controller, arguments);
        try
        {
            Check(TrainingInterfaceController.CollectionDirectory == Path.GetFullPath(Path.Combine(Application.dataPath, "..", "data_collect")),
                "No Editor, os CSVs devem ficar em data_collect na raiz do projeto.");
            // Arquivos: número persistente, lacunas, cultura e leitura enquanto o gravador está aberto.
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
            var timestamp = new DateTimeOffset(2026, 10, 15, 13, 0, 0, TimeSpan.FromHours(-3));
            string firstPath;
            using (var writer = TrainingCsvWriter.Create(directory))
            {
                firstPath = writer.FilePath;
                Check(Path.GetFileName(firstPath) == "COLETA_1.csv", "Primeira coleta deve usar COLETA_1.csv.");
                writer.WriteSample(timestamp, 1.25, 2, 3, TrainingInterfaceController.TrainingPhase.Gesture, GestureType.OpenHand, GestureType.Pointer);
                writer.Flush();
                var lines = ReadLines(firstPath);
                Check(lines.Length == 2 && lines[0] == TrainingCsvWriter.Header, "Cabeçalho e amostra devem estar disponíveis após Flush.");
                Check(lines[1] == "2026-10-15T16:00:00.000Z,1.250000,2,3,Gesture,OpenHand,Pointer", "UTC, decimal e gesto reconhecido devem ser preservados independentemente do esperado.");
            }
            string originalContents = File.ReadAllText(firstPath);
            using (var writer = TrainingCsvWriter.Create(directory))
                Check(Path.GetFileName(writer.FilePath) == "COLETA_2.csv", "Nova sessão deve incrementar o número.");
            File.WriteAllText(Path.Combine(directory, "COLETA_8.csv"), "coleta anterior");
            File.WriteAllText(Path.Combine(directory, "COLETA_invalida.csv"), "ignorar");
            using (var writer = TrainingCsvWriter.Create(directory))
                Check(Path.GetFileName(writer.FilePath) == "COLETA_9.csv", "Numeração deve continuar após o maior número existente.");
            Check(File.ReadAllText(firstPath) == originalContents, "Coletas anteriores não podem ser sobrescritas.");

            controller = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TrainingInterfaceController>(true)).Single();
            Check((bool)Call("ValidateConfiguration"), "Cena deve ter referências, IDs de gestos e fonte de reconhecimento válidos.");
            var settings = new SerializedObject(controller);
            var source = (HeadsetGestureSource)settings.FindProperty("headsetSource").objectReferenceValue;
            var sourceSettings = new SerializedObject(source);
            var bindings = sourceSettings.FindProperty("bindings");
            Check(bindings.arraySize == 6, "Treinamento deve reconhecer os seis gestos, inclusive Rest.");
            Check(Enumerable.Range(0, bindings.arraySize).All(i =>
                ((Component)bindings.GetArrayElementAtIndex(i).FindPropertyRelative("activeState").objectReferenceValue).gameObject.scene == scene),
                "Todas as poses devem pertencer à cena de treinamento.");
            var gestures = settings.FindProperty("gestures");
            Check(Enumerable.Range(0, gestures.arraySize).All(i =>
                bindings.GetArrayElementAtIndex(i).FindPropertyRelative("gesture").intValue == gestures.GetArrayElementAtIndex(i).FindPropertyRelative("gesture").intValue &&
                bindings.GetArrayElementAtIndex(i).FindPropertyRelative("image").objectReferenceValue == gestures.GetArrayElementAtIndex(i).FindPropertyRelative("image").objectReferenceValue),
                "O gesto esperado deve corresponder à imagem exibida.");

            // Substitui apenas as leituras físicas: mantém a seleção de gesto de produção.
            fakePoses = new GameObject("TemporaryCollectionPoses") { hideFlags = HideFlags.HideAndDontSave };
            var states = new VirtualActiveState[bindings.arraySize];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = fakePoses.AddComponent<VirtualActiveState>();
                bindings.GetArrayElementAtIndex(i).FindPropertyRelative("activeState").objectReferenceValue = states[i];
            }
            sourceSettings.ApplyModifiedPropertiesWithoutUndo();
            Check(source.GetRecognizedGesture() == GestureType.None, "Ausência de reconhecimento deve produzir None, nunca o gesto esperado.");
            for (int i = 0; i < states.Length; i++)
            {
                states[i].Active = true;
                var expected = (GestureType)bindings.GetArrayElementAtIndex(i).FindPropertyRelative("gesture").intValue;
                Check(source.GetRecognizedGesture() == expected && source.IsGestureActive(expected), "Detecção compartilhada divergente para " + expected);
                states[i].Active = false;
            }
            states[5].Active = true;
            states[0].Active = true;
            Check(source.GetRecognizedGesture() == GestureType.OpenHand, "Repouso permissivo não deve ocultar uma pose específica.");
            states[3].Active = true;
            Check(source.GetRecognizedGesture() == GestureType.Tripod, "Prioridade deve ser igual à do indicador da partida.");
            states[3].Active = false;
            states[0].Active = false;

            Call("Awake");
            var startButton = (UnityEngine.UI.Button)settings.FindProperty("startButton").objectReferenceValue;
            var backButton = (UnityEngine.UI.Button)settings.FindProperty("backButton").objectReferenceValue;
            Check(backButton.interactable && startButton.interactable, "Antes de iniciar, Iniciar e Voltar devem estar disponíveis.");
            Check(!scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<UnityEngine.UI.Button>(true)).Any(b => b.name == "PauseSequenceButton"),
                "Pausar deve ter sido removido da interface.");
            Set("cycle", 1);
            Set("gestureOrder", Enumerable.Range(0, gestures.arraySize).ToArray());
            recording = TrainingCsvWriter.Create(directory);
            string collectionPath = recording.FilePath;
            Set("recording", recording);
            Set("recordingStartTime", Time.realtimeSinceStartupAsDouble);
            Set("nextFlushTime", double.MaxValue);
            Call("BeginPhase", TrainingInterfaceController.TrainingPhase.Rest);
            Call("RecordSample");
            recording.Flush();
            Check(ReadLines(collectionPath).Last().EndsWith(",1,1,Rest,Rest,Rest"), "Descanso deve registrar Rest como esperado e reconhecer repouso.");
            Check(!backButton.interactable && !startButton.interactable, "Durante o descanso, Voltar e Iniciar devem estar desabilitados.");
            controller.ReturnToMenu();
            Check(controller.Phase == TrainingInterfaceController.TrainingPhase.Rest &&
                typeof(TrainingInterfaceController).GetField("recording", privateInstance).GetValue(controller) == recording,
                "Voltar não deve encerrar a coleta nem carregar o menu durante o treinamento.");
            states[5].Active = false;
            states[1].Active = true;
            Call("AdvancePhase");
            Check(!backButton.interactable, "Voltar também deve estar desabilitado na execução do gesto.");
            Call("RecordSample");
            recording.Flush();
            Check(ReadLines(collectionPath).Last().EndsWith(",1,1,Gesture,OpenHand,Pointer"), "Coleta deve manter o gesto feito mesmo quando diferente do esperado.");
            states[1].Active = false;
            Set("nextSampleTime", 0d);
            Call("RecordSample");
            recording.Flush();
            Check(ReadLines(collectionPath).Last().EndsWith(",Gesture,OpenHand,None"), "Perda de pose deve ser registrada explicitamente.");

            Call("OnApplicationPause", true);
            int beforeSuspend = recording.SampleCount;
            Call("LateUpdate");
            Check(controller.IsPaused && recording.SampleCount == beforeSuspend, "Suspensão do sistema não deve registrar amostras.");
            Call("OnApplicationPause", false);
            Check(!controller.IsPaused && !backButton.interactable, "Voltar ao app deve retomar automaticamente, mantendo Voltar bloqueado.");
            Call("RecordSample");
            Check(recording.SampleCount == beforeSuspend + 1, "Após suspensão, amostras devem continuar no mesmo CSV sem botão Continuar.");
            int finalCount = recording.SampleCount;
            Call("BeginPhase", TrainingInterfaceController.TrainingPhase.Completed);
            Check(backButton.interactable && startButton.interactable, "Após concluir, Voltar e Repetir devem ficar disponíveis.");
            recording = null;
            using (File.Open(collectionPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            Check(File.ReadAllLines(collectionPath).Length == finalCount + 1, "Conclusão deve fechar o arquivo e preservar todas as amostras.");
            Call("LateUpdate");
            Check(controller.Phase == TrainingInterfaceController.TrainingPhase.Completed, "Treinamento concluído não deve continuar coletando.");

            recording = TrainingCsvWriter.Create(directory);
            string partialPath = recording.FilePath;
            Set("recording", recording);
            Call("BeginPhase", TrainingInterfaceController.TrainingPhase.Rest);
            Call("RecordSample");
            Call("OnDisable");
            recording = null;
            Check(File.ReadAllLines(partialPath).Length == 2 && controller.Phase == TrainingInterfaceController.TrainingPhase.Ready,
                "Desativar o treinamento deve preservar coleta parcial e permitir nova sequência.");
            using (File.Open(partialPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            Check(true, "Coleta parcial deve estar fechada.");
            return $"{count} verificações aprovadas: CSV, numeração, UTC, poses, cena, amostras, bloqueio de Voltar, retomada automática e fechamento.";
        }
        finally
        {
            if (controller != null) Call("OnDisable");
            recording?.Dispose();
            if (fakePoses != null) UnityEngine.Object.DestroyImmediate(fakePoses);
            EditorSceneManager.ClosePreviewScene(scene);
            CultureInfo.CurrentCulture = originalCulture;
            // Apenas a pasta exclusiva criada por esta execução; nunca a pasta de coletas do usuário.
            Directory.Delete(directory, true);
        }
    }
}
