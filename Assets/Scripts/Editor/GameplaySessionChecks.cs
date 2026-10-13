using System;
using MyoGuess.Gameplay;
using UnityEditor;
using UnityEngine;

public static class GameplaySessionChecks
{
    [MenuItem("Myo Guess/Validar regras da partida")]
    public static void RunFromMenu() => Debug.Log(Run());

    public static string Run()
    {
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        GameplaySession Create() => new GameplaySession(3, 100, 5f, 0.1f);

        var game = Create();
        game.BeginRound();
        Check(game.Advance(4.89f, true) == RoundResult.None && game.Score == 0, "Gesto antecipado não deve pontuar.");
        Check(game.Advance(0.02f, false) == RoundResult.Missed && game.Lives == 2, "Vale a pose aos 4,9 s, não a pose anterior.");
        Check(game.Advance(0.1f, true) == RoundResult.None && game.Score == 0 && game.Lives == 2, "A rodada só pode ser avaliada uma vez.");
        game.Advance(1f, false);
        Check(!game.IsRoundActive && game.RemainingTime == 0f, "A animação deve concluir aos 5 s.");

        game = Create();
        game.BeginRound();
        game.Advance(4.89f, false);
        Check(game.Advance(0.02f, true) == RoundResult.Correct && game.Score == 100 && game.Lives == 3, "Acerto instantâneo aos 4,9 s deve pontuar sem tempo de sustentação.");
        Check(game.IsRoundActive && game.RemainingTime > 0f, "A comparação deve ocorrer antes de zerar.");
        Check(game.Advance(1f, true) == RoundResult.None && game.Score == 100, "Não duplicar pontos no fim da rodada.");

        for (int i = 0; i < 3; i++)
        {
            game.BeginRound();
            Check(game.Advance(5f, false) == RoundResult.Missed, "Rodada sem reconhecimento deve descontar uma vida.");
        }
        Check(game.IsGameOver && game.Lives == 0 && game.Score == 100, "Terceira falha deve encerrar a partida preservando o score.");
        game.BeginRound();
        Check(!game.IsRoundActive && game.Advance(10f, true) == RoundResult.None && game.Score == 100, "Game over deve bloquear novas rodadas e pontuação.");
        game.Reset();
        game.BeginRound();
        Check(game.Lives == 3 && game.Score == 0 && game.IsRoundActive && game.RemainingTime == 5f, "Reinício deve restaurar vidas, score e tempo.");
        Check(game.Advance(0f, true) == RoundResult.None && game.Score == 0, "Não avaliar sem passagem de tempo.");

        var boundary = new GameplaySession(3, 100, 5f, 0f);
        boundary.BeginRound();
        Check(boundary.Advance(5f, true) == RoundResult.Correct && !boundary.IsRoundActive, "A configuração de avaliação no fim também deve funcionar.");
        bool rejected = false;
        try { new GameplaySession(3, 100, 5f, 5f); }
        catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "Rejeitar um instante de comparação fora da rodada.");
        return "16 verificações aprovadas: comparação aos 4,9 s, pontuação única, três vidas, game over e reinício.";
    }
}
