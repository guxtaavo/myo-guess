using UnityEngine;

namespace MyoGuess.Audio
{
    [CreateAssetMenu(menuName = "Myo Guess/Audio Settings")]
    public sealed class GameAudioSettings : ScriptableObject
    {
        [Header("Music")]
        public AudioClip menuMusic;
        public AudioClip gameplayMusic;
        public AudioClip trainingMusic;
        [Range(0f, 1f)] public float menuVolume = 0.25f;
        [Range(0f, 1f)] public float gameplayVolume = 0.2f;
        [Range(0f, 1f)] public float trainingVolume = 0.12f;
        [Min(0f)] public float musicFadeDuration = 0.4f;

        [Header("Feedback")]
        public AudioClip click;
        public AudioClip back;
        public AudioClip mistake;
        public AudioClip defeat;
        [Range(0f, 1f)] public float uiVolume = 0.55f;
        [Range(0f, 1f)] public float feedbackVolume = 0.65f;
        [Range(0f, 1f)] public float gameOverMusicMultiplier = 0.25f;

        public AudioClip MusicForScene(string sceneName)
        {
            switch (sceneName)
            {
                case "MainMenu": return menuMusic;
                case "Gameplay": return gameplayMusic;
                case "Training": return trainingMusic;
                default: return null;
            }
        }

        public float VolumeForScene(string sceneName)
        {
            switch (sceneName)
            {
                case "MainMenu": return menuVolume;
                case "Gameplay": return gameplayVolume;
                case "Training": return trainingVolume;
                default: return 0f;
            }
        }
    }
}
