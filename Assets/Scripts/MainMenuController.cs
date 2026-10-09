using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyoGuess.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private string gameplaySceneName = "Gameplay";
        [SerializeField] private Button trainingButton;
        [SerializeField] private string trainingSceneName = "Training";

        private void Awake()
        {
            if (playButton == null)
            {
                Debug.LogError("Arraste o botao Jogar para o campo Play Button.", this);
            }
            else
            {
                playButton.onClick.AddListener(OpenGameplay);
            }

            if (trainingButton != null)
                trainingButton.onClick.AddListener(OpenTraining);
        }

        private void OnDestroy()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OpenGameplay);
            }

            if (trainingButton != null)
                trainingButton.onClick.RemoveListener(OpenTraining);
        }

        public void OpenGameplay()
        {
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void OpenTraining()
        {
            SceneManager.LoadScene(trainingSceneName);
        }
    }
}
