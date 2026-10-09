using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyoGuess.Training
{
    [Serializable]
    public sealed class TrainingGesture
    {
        public string displayName;
        public Sprite image;
    }

    // Conduz somente a apresentacao do protocolo; nao le sensores nem salva dados.
    [DisallowMultipleComponent]
    public sealed class TrainingInterfaceController : MonoBehaviour
    {
        public enum TrainingPhase { Ready, Rest, Gesture, Completed }

        [Header("Protocol")]
        [SerializeField] private TrainingGesture[] gestures;
        [SerializeField] private Sprite restImage;
        [Min(1)] [SerializeField] private int repeatCycles = 5;
        [Min(0.1f)] [SerializeField] private float restDuration = 3f;
        [Min(0.1f)] [SerializeField] private float gestureDuration = 5f;

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
        [SerializeField] private UnityEngine.UI.Button pauseButton;
        [SerializeField] private UnityEngine.UI.Button backButton;
        [SerializeField] private TMP_Text startButtonText;
        [SerializeField] private TMP_Text pauseButtonText;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private int[] gestureOrder;
        private int gestureIndex;
        private int cycle;
        private float elapsedTime;
        private readonly Color restColor = new Color(0f, 1f, 0.86f);
        private readonly Color gestureColor = new Color(0.9f, 0.4f, 1f);

        public TrainingPhase Phase { get; private set; }
        public bool IsPaused { get; private set; }
        public int CompletedGestures { get; private set; }
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
            pauseButton.onClick.AddListener(TogglePause);
            backButton.onClick.AddListener(ReturnToMenu);
            Phase = TrainingPhase.Ready;
            UpdateInterface();
        }

        private void Update()
        {
            if (!IsRunning || IsPaused)
                return;

            elapsedTime = Mathf.Min(elapsedTime + Time.deltaTime, PhaseDuration);
            if (elapsedTime >= PhaseDuration)
                AdvancePhase();

            UpdateTimer();
        }

        public void StartSequence()
        {
            if (IsRunning || gestureOrder == null)
                return;

            cycle = 1;
            gestureIndex = 0;
            CompletedGestures = 0;
            IsPaused = false;
            ShuffleGestures();
            BeginPhase(TrainingPhase.Rest);
        }

        public void TogglePause()
        {
            if (!IsRunning)
                return;

            IsPaused = !IsPaused;
            UpdateInterface();
        }

        public void ReturnToMenu()
        {
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
            UpdateInterface();
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
                phaseText.text = IsPaused ? "PAUSADO"
                    : Phase == TrainingPhase.Rest ? "DESCANSO" : "MANTENHA O GESTO";
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
            pauseButton.interactable = running;
            startButtonText.text = completed ? "REPETIR" : "INICIAR";
            pauseButtonText.text = IsPaused ? "CONTINUAR" : "PAUSAR";
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
            if (gestures == null || gestures.Length == 0 || restImage == null || repeatCycles < 1 ||
                restDuration <= 0f || gestureDuration <= 0f || gestureImage == null || phaseText == null ||
                gestureText == null || instructionText == null || timerText == null || cycleText == null ||
                stepText == null || progressText == null || timerFill == null || progressFill == null ||
                startButton == null || pauseButton == null || backButton == null ||
                startButtonText == null || pauseButtonText == null)
                return false;

            foreach (TrainingGesture gesture in gestures)
                if (gesture == null || gesture.image == null || string.IsNullOrWhiteSpace(gesture.displayName))
                    return false;

            return true;
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(StartSequence);
            if (pauseButton != null) pauseButton.onClick.RemoveListener(TogglePause);
            if (backButton != null) backButton.onClick.RemoveListener(ReturnToMenu);
        }
    }
}
