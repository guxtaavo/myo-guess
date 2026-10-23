using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyoGuess.Gameplay
{
    [Serializable]
    public sealed class GameplayGesture
    {
        public GestureType gesture;
        public Sprite image;
    }

    [DisallowMultipleComponent]
    public sealed class GestureRoundController : MonoBehaviour
    {
        private const string HighScoreKey = "MyoGuess.HighScore";

        [Header("Entrada de gestos")]
        [SerializeField] private GestureInputMode inputMode = GestureInputMode.Headset;
        [SerializeField] private HeadsetGestureSource headsetSource;
        [SerializeField] private FbgGestureSource fbgSource;
        [Tooltip("Registra a decisão e a leitura do sensor uma única vez por rodada.")]
        [SerializeField] private bool logRoundResults = true;

        [Header("Gestos")]
        [SerializeField] private UnityEngine.UI.Image gestureImage;
        [SerializeField] private GameplayGesture[] gestures;

        [Header("Regras")]
        [Min(1)] [SerializeField] private int startingLives = 3;
        [Min(1)] [SerializeField] private int pointsPerGesture = 100;
        [Tooltip("Confere o gesto este tempo antes do fim. 0,1 s numa rodada de 5 s = aos 4,9 s.")]
        [Min(0f)] [SerializeField] private float evaluationLeadTime = 0.1f;
        [Min(0.1f)] [SerializeField] private float roundDuration = 5f;
        [Min(0f)] [SerializeField] private float feedbackDuration = 0.75f;

        [Header("Approach")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform endPoint;

        [Header("HUD")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private UnityEngine.UI.Image timerFill;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private UnityEngine.UI.Image[] lifeImages;
        [SerializeField] private TMP_Text feedbackText;

        [Header("Game Over")]
        [SerializeField] private GameOverPanel gameOverPanel;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private GameplaySession session;
        private GestureSource source;
        private GameplayGesture currentGesture;
        private float feedbackRemaining;
        private int displayedSeconds = -1;
        private Color gestureColor;

        public int RoundNumber { get; private set; }
        public int Score => session?.Score ?? 0;
        public int Lives => session?.Lives ?? startingLives;
        public int HighScore { get; private set; }
        public bool IsGameOver => session != null && session.IsGameOver;
        public float RemainingTime => session?.RemainingTime ?? roundDuration;
        public GestureType CurrentGesture => currentGesture?.gesture ?? GestureType.None;

        private void Awake()
        {
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            gestureColor = gestureImage != null ? gestureImage.color : Color.white;
            if (gameOverPanel != null)
            {
                gameOverPanel.RestartRequested += RestartGame;
                gameOverPanel.MenuRequested += ReturnToMenu;
                gameOverPanel.Hide();
            }
        }

        private void Start() => RestartGame();

        public void RestartGame()
        {
            MyoGuess.Audio.GameAudio.ResetGameplayFeedback();
            source = inputMode == GestureInputMode.Headset ? (GestureSource)headsetSource : fbgSource;
            if (gameOverPanel != null) gameOverPanel.Hide();
            if (!ValidateConfiguration())
            {
                session = null;
                if (gestureImage != null) gestureImage.gameObject.SetActive(false);
                SetFeedback(inputMode == GestureInputMode.FBG ? "SENSOR FBG: A IMPLEMENTAR" : "CONFIGURE A ENTRADA DE GESTOS", Color.white);
                return;
            }

            session = new GameplaySession(startingLives, pointsPerGesture, roundDuration, evaluationLeadTime);
            RoundNumber = 0;
            feedbackRemaining = 0f;
            UpdateHud();
            BeginRound();
        }

        // Avalia após os Update dos reconhecedores de pose e dos eventos do Meta SDK.
        private void LateUpdate()
        {
            if (session == null || session.IsGameOver) return;
            if (!session.IsRoundActive)
            {
                feedbackRemaining -= Time.deltaTime;
                if (feedbackRemaining <= 0f) BeginRound();
                return;
            }

            RoundResult result = session.Advance(Time.deltaTime, source.IsGestureActive(CurrentGesture));
            float progress = 1f - RemainingTime / roundDuration;
            gestureImage.rectTransform.position = Vector3.Lerp(spawnPoint.position, endPoint.position, progress);
            UpdateTimer();
            if (result != RoundResult.None) EndRound(result);
        }

        private void BeginRound()
        {
            session.BeginRound();
            currentGesture = gestures[UnityEngine.Random.Range(0, gestures.Length)];
            gestureImage.sprite = currentGesture.image;
            gestureImage.color = gestureColor;
            gestureImage.rectTransform.position = spawnPoint.position;
            gestureImage.gameObject.SetActive(true);
            RoundNumber++;
            SetFeedback("COPIE O GESTO", Color.white);
            UpdateTimer();
        }

        private void EndRound(RoundResult result)
        {
            bool correct = result == RoundResult.Correct;
            if (!correct) MyoGuess.Audio.GameAudio.PlayMistake(session.IsGameOver);
            if (logRoundResults)
            {
                string reading = source is HeadsetGestureSource headset
                    ? headset.DescribeLastReading() : $"entrada={inputMode}, alvo={CurrentGesture}";
                Debug.Log($"[Gameplay] Rodada {RoundNumber}, imagem={currentGesture.image.name}, " +
                    $"tempo={roundDuration - RemainingTime:0.00}s, resultado={(correct ? "ACERTO" : "ERRO")}: {reading}", this);
            }
            gestureImage.color = correct ? new Color(0f, 1f, 0.86f) : new Color(1f, 0.3f, 0.45f);
            SetFeedback(correct ? $"ACERTO! +{pointsPerGesture}" : "GESTO INCORRETO — PERDEU UMA VIDA", gestureImage.color);
            feedbackRemaining = feedbackDuration;
            UpdateHud();
            if (session.IsGameOver)
            {
                HighScore = Math.Max(HighScore, Score);
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
                PlayerPrefs.Save();
                // Só as rodadas param. Tracking e interação VR continuam ativos no painel.
                gameOverPanel.Show(Score, HighScore);
            }
        }

        public void ReturnToMenu()
        {
            MyoGuess.Audio.GameAudio.PlayBack();
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void UpdateHud()
        {
            scoreText.SetText("{0:0}", Score);
            for (int i = 0; i < lifeImages.Length; i++)
                lifeImages[i].color = i < Lives ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.25f);
        }

        private void UpdateTimer()
        {
            int seconds = Mathf.CeilToInt(RemainingTime);
            if (seconds != displayedSeconds)
            {
                timerText.SetText("{0:0}", seconds);
                displayedSeconds = seconds;
            }
            timerFill.fillAmount = RemainingTime / roundDuration;
        }

        private void SetFeedback(string message, Color color)
        {
            if (feedbackText == null) return;
            feedbackText.text = message;
            feedbackText.color = color;
        }

        private bool ValidateConfiguration()
        {
            if (source == null || !source.IsAvailable)
            {
                Debug.LogWarning(inputMode == GestureInputMode.FBG ? "Sensor FBG: implementação pendente. A partida não será iniciada." : "Configure a fonte de gestos do óculos.", this);
                return false;
            }
            if (gestureImage == null || spawnPoint == null || endPoint == null || timerText == null ||
                timerFill == null || scoreText == null || gameOverPanel == null || !gameOverPanel.IsConfigured ||
                startingLives < 1 || pointsPerGesture < 1 || roundDuration <= 0f ||
                evaluationLeadTime < 0f || evaluationLeadTime >= roundDuration || lifeImages == null || lifeImages.Length < startingLives ||
                gestures == null || gestures.Length == 0)
            {
                Debug.LogError("Configure os gestos, regras, HUD e painel de game over no GestureRoundController.", this);
                return false;
            }
            foreach (var life in lifeImages) if (life == null) return false;
            foreach (var gesture in gestures)
                if (gesture == null || gesture.image == null || !source.CanRecognize(gesture.gesture))
                {
                    Debug.LogError("Cada imagem sorteada precisa ter uma pose correspondente na fonte de gestos.", this);
                    return false;
                }
            if (spawnPoint == gestureImage.transform || endPoint == gestureImage.transform ||
                spawnPoint.IsChildOf(gestureImage.transform) || endPoint.IsChildOf(gestureImage.transform) ||
                Vector3.SqrMagnitude(spawnPoint.position - endPoint.position) < 0.000001f)
                return false;
            return true;
        }

        private void OnDestroy()
        {
            if (gameOverPanel == null) return;
            gameOverPanel.RestartRequested -= RestartGame;
            gameOverPanel.MenuRequested -= ReturnToMenu;
        }
    }
}
