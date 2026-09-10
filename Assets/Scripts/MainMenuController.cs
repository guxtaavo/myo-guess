using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyoGuess.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private string gameplaySceneName = "Gameplay";

        private void Awake()
        {
            if (playButton == null)
            {
                Debug.LogError("Arraste o botao Jogar para o campo Play Button.", this);
                return;
            }

            playButton.onClick.AddListener(OpenGameplay);
        }

        private void OnDestroy()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OpenGameplay);
            }
        }

        public void OpenGameplay()
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}
