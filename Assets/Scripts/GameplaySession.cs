using System;

namespace MyoGuess.Gameplay
{
    public enum RoundResult { None, Correct, Missed }

    // Regras independentes de UI e sensores, para validar a partida sem um headset.
    public sealed class GameplaySession
    {
        private readonly int startingLives;
        private readonly int pointsPerGesture;
        private readonly float roundDuration;
        private readonly float evaluationLeadTime;
        private float elapsed;
        private bool evaluated;

        public int Score { get; private set; }
        public int Lives { get; private set; }
        public bool IsGameOver => Lives == 0;
        public bool IsRoundActive { get; private set; }
        public float RemainingTime => Math.Max(0f, roundDuration - elapsed);
        public float EvaluationTime => roundDuration - evaluationLeadTime;

        public GameplaySession(int lives, int points, float duration, float leadTime)
        {
            if (lives < 1 || points < 1 || duration <= 0f || leadTime < 0f || leadTime >= duration)
                throw new ArgumentOutOfRangeException(nameof(lives), "Regras de partida inválidas.");
            startingLives = lives;
            pointsPerGesture = points;
            roundDuration = duration;
            evaluationLeadTime = leadTime;
            Reset();
        }

        public void Reset()
        {
            Score = 0;
            Lives = startingLives;
            elapsed = 0f;
            evaluated = false;
            IsRoundActive = false;
        }

        public void BeginRound()
        {
            if (IsGameOver) return;
            elapsed = 0f;
            evaluated = false;
            IsRoundActive = true;
        }

        public RoundResult Advance(float deltaTime, bool correctGesture)
        {
            if (!IsRoundActive || IsGameOver || deltaTime <= 0f) return RoundResult.None;
            float step = Math.Min(deltaTime, RemainingTime);
            elapsed += step;
            RoundResult result = RoundResult.None;
            // Uma única amostra no instante configurado, sem acumular tempo de pose.
            if (!evaluated && elapsed >= EvaluationTime)
            {
                evaluated = true;
                if (correctGesture)
                {
                    Score += pointsPerGesture;
                    result = RoundResult.Correct;
                }
                else
                {
                    Lives--;
                    result = RoundResult.Missed;
                }
            }
            if (RemainingTime <= 0f || IsGameOver)
                IsRoundActive = false;
            return result;
        }
    }
}
