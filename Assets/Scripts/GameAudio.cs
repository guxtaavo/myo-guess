using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyoGuess.Audio
{
    // Um único player persiste entre cenas para não cortar os sons dos botões ao navegar.
    [DisallowMultipleComponent]
    public sealed class GameAudio : MonoBehaviour
    {
        private static GameAudio instance;
        private GameAudioSettings settings;
        private AudioSource musicSource;
        private AudioSource uiSource;
        private AudioSource feedbackSource;
        private Coroutine musicTransition;
        private string musicScene;
        private bool gameOver;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => GetOrCreate();

        private static GameAudio GetOrCreate()
        {
            // Validações da coleta no Editor não devem criar players nem reproduzir áudio.
            if (!Application.isPlaying) return null;
            if (instance != null) return instance;
            var config = Resources.Load<GameAudioSettings>("GameAudioSettings");
            if (config == null)
            {
                Debug.LogWarning("[Audio] Configure Assets/Resources/GameAudioSettings.asset.");
                return null;
            }
            var root = new GameObject("GameAudio");
            instance = root.AddComponent<GameAudio>();
            instance.settings = config;
            instance.musicSource = instance.CreateSource(true, 128);
            instance.uiSource = instance.CreateSource(false, 32);
            instance.feedbackSource = instance.CreateSource(false, 16);
            DontDestroyOnLoad(root);
            SceneManager.sceneLoaded += instance.OnSceneLoaded;
            return instance;
        }

        private AudioSource CreateSource(bool loop, int priority)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.priority = priority;
            return source;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            feedbackSource.Stop();
            gameOver = false;
            musicScene = scene.name;
            SwitchMusic(settings.MusicForScene(musicScene));
        }

        private float MusicVolume => settings.VolumeForScene(musicScene) *
            (gameOver ? settings.gameOverMusicMultiplier : 1f);

        private void SwitchMusic(AudioClip clip)
        {
            if (musicTransition != null) StopCoroutine(musicTransition);
            musicTransition = StartCoroutine(ChangeMusic(clip));
        }

        private IEnumerator ChangeMusic(AudioClip clip)
        {
            if (musicSource.clip != clip)
            {
                if (musicSource.isPlaying) yield return FadeMusicTo(0f);
                musicSource.Stop();
                musicSource.clip = clip;
                musicSource.volume = 0f;
                if (clip != null) musicSource.Play();
            }
            if (clip != null) yield return FadeMusicTo(MusicVolume);
            musicTransition = null;
        }

        private IEnumerator FadeMusicTo(float target)
        {
            float initial = musicSource.volume;
            float duration = settings.musicFadeDuration;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                musicSource.volume = Mathf.Lerp(initial, target, elapsed / duration);
                yield return null;
            }
            musicSource.volume = target;
        }

        public static void PlayClick()
        {
            var audio = GetOrCreate();
            if (audio != null && audio.settings.click != null)
                audio.uiSource.PlayOneShot(audio.settings.click, audio.settings.uiVolume);
        }

        public static void PlayBack()
        {
            var audio = GetOrCreate();
            if (audio != null && audio.settings.back != null)
                audio.uiSource.PlayOneShot(audio.settings.back, audio.settings.uiVolume);
        }

        public static void PlayMistake(bool finalLife)
        {
            var audio = GetOrCreate();
            if (audio == null) return;
            audio.gameOver = finalLife;
            var clip = finalLife ? audio.settings.defeat : audio.settings.mistake;
            audio.feedbackSource.Stop();
            if (clip != null) audio.feedbackSource.PlayOneShot(clip, audio.settings.feedbackVolume);
            if (finalLife) audio.SwitchMusic(audio.musicSource.clip);
        }

        public static void ResetGameplayFeedback()
        {
            var audio = GetOrCreate();
            if (audio == null) return;
            audio.feedbackSource.Stop();
            audio.gameOver = false;
            if (audio.musicScene == "Gameplay") audio.SwitchMusic(audio.settings.gameplayMusic);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this) instance = null;
        }
    }
}
