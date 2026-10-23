using System;
using System.IO;
using MyoGuess.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyoGuess.Training
{
    [Serializable]
    public sealed class TrainingGesture
    {
        public GestureType gesture;
        public string displayName;
        public Sprite image;
    }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class TrainingInterfaceController : MonoBehaviour
    {
        public enum TrainingPhase { Ready, Rest, Gesture, Completed }

        [Header("Protocol")]
        [SerializeField] private TrainingGesture[] gestures;
        [SerializeField] private Sprite restImage;
        [Min(1)] [SerializeField] private int repeatCycles = 5;
        [Min(0.1f)] [SerializeField] private float restDuration = 3f;
        [Min(0.1f)] [SerializeField] private float gestureDuration = 5f;

        [Header("Coleta")]
        [SerializeField] private HeadsetGestureSource headsetSource;
        [Tooltip("Intervalo entre amostras reais, sem duplicar leituras para compensar frames lentos.")]
        [Min(0.01f)] [SerializeField] private float sampleInterval = 0.1f;

        [Header("Interface")]
        [SerializeField] private UnityEngine.UI.Image gestureImage;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text gestureText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text cycleText;
        [SerializeField] private TMP_Text stepText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private UnityEngine.UI.Image timerFill;
        [SerializeField] private UnityEngine.UI.Image progressFill;
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private UnityEngine.UI.Button backButton;
        [SerializeField] private TMP_Text startButtonText;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private int[] gestureOrder;
        private int gestureIndex;
        private int cycle;
        private float elapsedTime;
        private TrainingCsvWriter recording;
        private double recordingStartTime;
        private double nextSampleTime;
        private double nextFlushTime;
        private bool applicationPaused;
        private readonly Color restColor = new Color(0f, 1f, 0.86f);
        private readonly Color gestureColor = new Color(0.9f, 0.4f, 1f);

        public TrainingPhase Phase { get; private set; }
        public bool IsPaused => applicationPaused;
        public int CompletedGestures { get; private set; }
        public string LastCollectionPath { get; private set; }
        public static string CollectionDirectory
        {
            get
            {
#if UNITY_EDITOR
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "data_collect"));
#else
                // No Quest o projeto do computador não está disponível; usa a pasta gravável do app.
                return Path.Combine(Application.persistentDataPath, "data_collect");
#endif
            }
        }
        public GestureType ExpectedGesture => Phase == TrainingPhase.Gesture
            ? gestures[gestureOrder[gestureIndex]].gesture : GestureType.Rest;
        public int TotalGestures => gestures.Length * repeatCycles;
        public float RemainingTime => IsRunning ? Mathf.Max(0f, PhaseDuration - elapsedTime) : 0f;
        private bool IsRunning => Phase == TrainingPhase.Rest || Phase == TrainingPhase.Gesture;
        private float PhaseDuration => Phase == TrainingPhase.Rest ? restDuration : gestureDuration;

        private void Awake()
        {
            if (!ValidateConfiguration())
            {
                Debug.LogError("Configure os gestos, tempos e referencias da TrainingInterfaceController.", this);
                enabled = false;
                return;
            }

            gestureOrder = new int[gestures.Length];
            startButton.onClick.AddListener(StartSequence);
            backButton.onClick.AddListener(ReturnToMenu);
            Phase = TrainingPhase.Ready;
            UpdateInterface();
        }

        // Lê os ActiveStateGroups depois dos reconhecedores do Meta SDK, como na partida.
        private void LateUpdate()
        {
            if (!IsRunning || IsPaused)
                return;

            if (!RecordSample()) return;
            elapsedTime = Mathf.Min(elapsedTime + Time.deltaTime, PhaseDuration);
            if (elapsedTime >= PhaseDuration)
                AdvancePhase();

            UpdateTimer();
        }

        public void StartSequence()
        {
            if (!isActiveAndEnabled || IsRunning || gestureOrder == null)
                return;

            if (applicationPaused) return;
            MyoGuess.Audio.GameAudio.PlayClick();
            try
            {
                if (headsetSource == null || !headsetSource.IsAvailable)
                    throw new InvalidOperationException("Reconhecimento do Meta Quest 3 indisponível.");
                recording = TrainingCsvWriter.Create(CollectionDirectory);
                LastCollectionPath = recording.FilePath;
                recordingStartTime = Time.realtimeSinceStartupAsDouble;
                nextFlushTime = recordingStartTime + 1d;
                Debug.Log("[Coleta] Arquivo iniciado: " + LastCollectionPath, this);
            }
            catch (Exception exception) when (IsRecordingError(exception) || exception is InvalidOperationException)
            {
                HandleRecordingError(exception);
                return;
            }

            cycle = 1;
            gestureIndex = 0;
            CompletedGestures = 0;
            ShuffleGestures();
            BeginPhase(TrainingPhase.Rest);
        }

        public void ReturnToMenu()
        {
            if (IsRunning) return;
            MyoGuess.Audio.GameAudio.PlayBack();
            CloseRecording();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void AdvancePhase()
        {
            if (Phase == TrainingPhase.Rest)
            {
                BeginPhase(TrainingPhase.Gesture);
                return;
            }

            CompletedGestures++;
            gestureIndex++;
            if (gestureIndex == gestures.Length)
            {
                if (cycle == repeatCycles)
                {
                    BeginPhase(TrainingPhase.Completed);
                    return;
                }

                cycle++;
                gestureIndex = 0;
                ShuffleGestures();
            }

            BeginPhase(TrainingPhase.Rest);
        }

        private void BeginPhase(TrainingPhase phase)
        {
            Phase = phase;
            elapsedTime = 0f;
            nextSampleTime = 0d;
            if (phase == TrainingPhase.Completed && !CloseRecording()) return;
            UpdateInterface();
        }

        private bool RecordSample()
        {
            if (recording == null) return false;
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextSampleTime) return true;
            try
            {
                recording.WriteSample(DateTimeOffset.UtcNow, now - recordingStartTime,
                    cycle, gestureIndex + 1, Phase, ExpectedGesture, headsetSource.GetRecognizedGesture());
                nextSampleTime = now + sampleInterval;
                if (now >= nextFlushTime)
                {
                    recording.Flush();
                    nextFlushTime = now + 1d;
                }
                return true;
            }
            catch (Exception exception) when (IsRecordingError(exception))
            {
                HandleRecordingError(exception);
                return false;
            }
        }

        private void FlushRecording()
        {
            try { recording?.Flush(); }
            catch (Exception exception) when (IsRecordingError(exception)) { HandleRecordingError(exception); }
        }

        private bool CloseRecording()
        {
            var closing = recording;
            recording = null;
            if (closing == null) return true;
            try
            {
                closing.Dispose();
                Debug.Log($"[Coleta] {closing.SampleCount} amostras salvas em: {closing.FilePath}", this);
                return true;
            }
            catch (Exception exception) when (IsRecordingError(exception))
            {
                HandleRecordingError(exception);
                return false;
            }
        }

        private static bool IsRecordingError(Exception exception) =>
            exception is IOException || exception is UnauthorizedAccessException;

        private void HandleRecordingError(Exception exception)
        {
            CloseRecording();
            Phase = TrainingPhase.Ready;
            if (isActiveAndEnabled)
            {
                UpdateInterface();
                phaseText.text = "ERRO NA COLETA";
                instructionText.text = "Não foi possível gravar a coleta. Verifique o armazenamento e tente novamente.";
            }
            Debug.LogError("[Coleta] " + exception.Message, this);
        }

        private void OnApplicationPause(bool paused)
        {
            applicationPaused = paused;
            // Suspensão do sistema não oferece pausa manual; a coleta retoma ao voltar ao app.
            nextSampleTime = 0d;
            if (paused) FlushRecording();
        }

        private void OnApplicationQuit() => CloseRecording();

        private void OnEnable()
        {
            if (gestureOrder != null) UpdateInterface();
        }

        private void OnDisable()
        {
            CloseRecording();
            if (IsRunning)
            {
                Phase = TrainingPhase.Ready;
            }
        }

        private void ShuffleGestures()
        {
            for (int i = 0; i < gestureOrder.Length; i++)
                gestureOrder[i] = i;

            for (int i = gestureOrder.Length - 1; i > 0; i--)
            {
                int other = UnityEngine.Random.Range(0, i + 1);
                int temporary = gestureOrder[i];
                gestureOrder[i] = gestureOrder[other];
                gestureOrder[other] = temporary;
            }
        }

        private void UpdateInterface()
        {
            bool running = IsRunning;
            bool completed = Phase == TrainingPhase.Completed;
            Color color = Phase == TrainingPhase.Gesture ? gestureColor : restColor;
            phaseText.color = color;
            timerFill.color = color;
            gestureImage.color = color;
            gestureImage.sprite = Phase == TrainingPhase.Gesture
                ? gestures[gestureOrder[gestureIndex]].image : restImage;

            if (Phase == TrainingPhase.Ready)
            {
                phaseText.text = "PRONTO PARA COMEÇAR";
                gestureText.text = "PREPARE A MÃO";
                instructionText.text = "Siga as imagens e mantenha cada gesto até o tempo terminar.";
            }
            else if (completed)
            {
                phaseText.text = "SEQUÊNCIA CONCLUÍDA";
                gestureText.text = "FINALIZADO";
                instructionText.text = "Você concluiu todos os ciclos. Pode descansar a mão.";
            }
            else
            {
                phaseText.text = Phase == TrainingPhase.Rest ? "DESCANSO" : "MANTENHA O GESTO";
                gestureText.text = Phase == TrainingPhase.Rest ? "RELAXE A MÃO"
                    : gestures[gestureOrder[gestureIndex]].displayName;
                instructionText.text = Phase == TrainingPhase.Rest
                    ? "Próximo gesto: " + gestures[gestureOrder[gestureIndex]].displayName
                    : "Mantenha esta posição até o timer terminar.";
            }

            cycleText.SetText("CICLO {0:0} / {1:0}", cycle, repeatCycles);
            stepText.SetText("GESTO {0:0} / {1:0}", completed ? gestures.Length : running ? gestureIndex + 1 : 0, gestures.Length);
            progressText.SetText("{0:0} / {1:0} GESTOS CONCLUÍDOS", CompletedGestures, TotalGestures);
            progressFill.fillAmount = (float)CompletedGestures / TotalGestures;
            startButton.interactable = !running;
            backButton.interactable = !running;
            startButtonText.text = completed ? "REPETIR" : "INICIAR";
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            if (IsRunning)
                timerText.SetText("{0:0}", Mathf.CeilToInt(RemainingTime));
            else
                timerText.text = Phase == TrainingPhase.Completed ? "0" : "—";

            timerFill.fillAmount = IsRunning ? RemainingTime / PhaseDuration : Phase == TrainingPhase.Ready ? 1f : 0f;
        }

        private bool ValidateConfiguration()
        {
            if (headsetSource == null || sampleInterval < 0.01f || !headsetSource.CanRecognize(GestureType.Rest) ||
                gestures == null || gestures.Length == 0 || restImage == null || repeatCycles < 1 ||
                restDuration <= 0f || gestureDuration <= 0f || gestureImage == null || phaseText == null ||
                gestureText == null || instructionText == null || timerText == null || cycleText == null ||
                stepText == null || progressText == null || timerFill == null || progressFill == null ||
                startButton == null || backButton == null || startButtonText == null)
                return false;

            foreach (TrainingGesture gesture in gestures)
                if (gesture == null || gesture.image == null || string.IsNullOrWhiteSpace(gesture.displayName) ||
                    !headsetSource.CanRecognize(gesture.gesture))
                    return false;

            return true;
        }

        private void OnDestroy()
        {
            CloseRecording();
            if (startButton != null) startButton.onClick.RemoveListener(StartSequence);
            if (backButton != null) backButton.onClick.RemoveListener(ReturnToMenu);
        }
    }
}
