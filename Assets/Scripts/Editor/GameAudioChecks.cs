using System;
using System.Linq;
using MyoGuess.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameAudioChecks
{
    [MenuItem("Myo Guess/Validar áudio")]
    public static void RunFromMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Valide fora do Play Mode.");
        int count = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            count++;
        }
        var settings = Resources.Load<GameAudioSettings>("GameAudioSettings");
        Check(settings != null, "Configuração de áudio deve existir em Resources.");
        var scenes = new[] { "MainMenu", "Gameplay", "Training" };
        var clips = new[] { settings.menuMusic, settings.gameplayMusic, settings.trainingMusic };
        for (int i = 0; i < scenes.Length; i++)
        {
            string sceneName = scenes[i];
            Check(clips[i] != null && clips[i].length > 5f, "Música ausente ou inválida para " + sceneName);
            Check(settings.MusicForScene(sceneName) == clips[i], "Música incorreta para " + sceneName);
            Check(settings.VolumeForScene(sceneName) > 0f && settings.VolumeForScene(sceneName) <= 1f, "Volume inválido para " + sceneName);
            var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clips[i]));
            Check(importer.defaultSampleSettings.loadType == AudioClipLoadType.Streaming &&
                importer.GetOverrideSampleSettings("Android").loadType == AudioClipLoadType.Streaming,
                "Música deve usar streaming no Editor e no Android.");
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/" + sceneName + ".unity");
            try
            {
                var listeners = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<AudioListener>(true));
                Check(listeners.Count(l => l.enabled && l.gameObject.activeInHierarchy) == 1, "A cena deve ter um AudioListener ativo: " + sceneName);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        Check(settings.MusicForScene("Unknown") == null, "Cenas desconhecidas não devem receber música incorreta.");
        Check(settings.click != null && settings.back != null && settings.mistake != null && settings.defeat != null,
            "Todos os efeitos devem estar configurados.");
        Check(settings.click != settings.back, "Clique e Voltar devem usar os sons escolhidos separadamente.");
        Check(settings.defeat.length > 4f && settings.defeat.length > settings.mistake.length,
            "Game over deve usar a vinheta longa de trombone.");
        foreach (var clip in new[] { settings.click, settings.back, settings.mistake, settings.defeat })
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
            Check(clip.length > 0f && importer.defaultSampleSettings.loadType == AudioClipLoadType.DecompressOnLoad,
                "Feedback curto deve estar pronto para reprodução: " + clip.name);
        }
        Check(settings.uiVolume > 0f && settings.feedbackVolume > 0f && settings.gameOverMusicMultiplier < 1f,
            "Feedback deve ser audível e música deve baixar no game over.");
        return $"{count} verificações de áudio aprovadas: músicas, efeitos, importação, volumes e listeners.";
    }
}
