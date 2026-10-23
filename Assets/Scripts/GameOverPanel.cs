using System;
using TMPro;
using UnityEngine;

namespace MyoGuess.Gameplay
{
    public sealed class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private UnityEngine.UI.Button restartButton;
        [SerializeField] private UnityEngine.UI.Button menuButton;

        public event Action RestartRequested;
        public event Action MenuRequested;
        public bool IsConfigured => panelRoot != null && scoreText != null && highScoreText != null &&
            restartButton != null && menuButton != null;

        private void Awake()
        {
            if (!IsConfigured) return;
            restartButton.onClick.AddListener(RequestRestart);
            menuButton.onClick.AddListener(RequestMenu);
        }

        public void Show(int score, int highScore)
        {
            scoreText.SetText("SEU SCORE\n{0:0}", score);
            highScoreText.SetText("RECORDE\n{0:0}", highScore);
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void RequestRestart()
        {
            MyoGuess.Audio.GameAudio.PlayClick();
            RestartRequested?.Invoke();
        }
        private void RequestMenu() => MenuRequested?.Invoke();

        private void OnDestroy()
        {
            if (restartButton != null) restartButton.onClick.RemoveListener(RequestRestart);
            if (menuButton != null) menuButton.onClick.RemoveListener(RequestMenu);
        }
    }
}
