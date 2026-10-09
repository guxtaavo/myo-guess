using TMPro;
using UnityEngine;

namespace MyoGuess.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GestureRoundController : MonoBehaviour
    {
        [Header("Gesture")]
        [SerializeField] private UnityEngine.UI.Image gestureImage;
        [SerializeField] private Sprite[] gestureSprites;

        [Header("Approach")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform endPoint;
        [Tooltip("Tempo da aproximacao e da rodada, em segundos.")]
        [Min(0.1f)]
        [SerializeField] private float roundDuration = 5f;

        [Header("Timer")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private UnityEngine.UI.Image timerFill;

        private float elapsedTime;
        private bool configured;
        private bool roundActive;
        private int displayedSeconds = -1;

        public int RoundNumber { get; private set; }
        public float RemainingTime => Mathf.Max(0f, roundDuration - elapsedTime);

        private void Awake()
        {
            configured = ValidateConfiguration();
            if (!configured)
            {
                if (gestureImage != null)
                    gestureImage.gameObject.SetActive(false);

                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (configured)
                BeginRound();
        }

        private void Update()
        {
            if (!roundActive)
            {
                // Um frame oculto separa o fim do gesto do proximo sorteio.
                BeginRound();
                return;
            }

            elapsedTime = Mathf.Min(elapsedTime + Time.deltaTime, roundDuration);
            float progress = elapsedTime / roundDuration;

            // O mesmo progresso controla a posicao, os segundos e a barra.
            gestureImage.rectTransform.position = Vector3.Lerp(
                spawnPoint.position, endPoint.position, progress);
            UpdateTimer();

            if (progress >= 1f)
                EndRound();
        }

        private void OnDisable()
        {
            roundActive = false;
            if (configured)
                gestureImage.gameObject.SetActive(false);
        }

        private void BeginRound()
        {
            elapsedTime = 0f;
            gestureImage.sprite = gestureSprites[Random.Range(0, gestureSprites.Length)];
            gestureImage.rectTransform.position = spawnPoint.position;
            gestureImage.gameObject.SetActive(true);
            roundActive = true;
            RoundNumber++;
            UpdateTimer();
        }

        private void EndRound()
        {
            if (!roundActive)
                return;

            roundActive = false;
            // Reutiliza a imagem existente; nao instancia objetos a cada rodada.
            gestureImage.gameObject.SetActive(false);
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

        private bool ValidateConfiguration()
        {
            if (gestureImage == null || spawnPoint == null || endPoint == null ||
                timerText == null || timerFill == null || roundDuration <= 0f ||
                gestureSprites == null || gestureSprites.Length == 0)
            {
                Debug.LogError("Configure a imagem, os gestos, os pontos de inicio/fim e o timer no GestureRoundController.", this);
                return false;
            }

            foreach (Sprite sprite in gestureSprites)
            {
                if (sprite == null)
                {
                    Debug.LogError("A lista de gestos contem uma imagem vazia.", this);
                    return false;
                }
            }

            if (spawnPoint == gestureImage.transform || endPoint == gestureImage.transform ||
                spawnPoint.IsChildOf(gestureImage.transform) || endPoint.IsChildOf(gestureImage.transform) ||
                Vector3.SqrMagnitude(spawnPoint.position - endPoint.position) < 0.000001f)
            {
                Debug.LogError("Use pontos de inicio/fim distintos e independentes da imagem animada.", this);
                return false;
            }

            return true;
        }
    }
}
