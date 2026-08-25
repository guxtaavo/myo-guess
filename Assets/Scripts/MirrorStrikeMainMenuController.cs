using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyoGuess.MainMenu
{
    [AddComponentMenu("Myo Guess/Main Menu/Mirror Strike Controller")]
    public sealed class MirrorStrikeMainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private CanvasGroup content;
        [SerializeField] private string gameplaySceneName = "Gameplay";

        private void Awake()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(StartGame);
            }
        }

        private IEnumerator Start()
        {
            if (content == null)
            {
                yield break;
            }

            content.alpha = 0f;
            float elapsed = 0f;
            const float duration = 0.55f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                content.alpha = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                yield return null;
            }

            content.alpha = 1f;
        }

        public void StartGame()
        {
            if (string.IsNullOrWhiteSpace(gameplaySceneName) || !Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                Debug.LogWarning($"[Mirror Strike] A cena '{gameplaySceneName}' ainda não está no Build Profiles.");
                return;
            }

            SceneManager.LoadScene(gameplaySceneName);
        }

        public void Configure(Button button, CanvasGroup canvasGroup)
        {
            startButton = button;
            content = canvasGroup;
        }
    }
}
