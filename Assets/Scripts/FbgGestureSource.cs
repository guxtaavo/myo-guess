namespace MyoGuess.Gameplay
{
    public sealed class FbgGestureSource : GestureSource
    {
        // Implementação: conectar o sensor FBG e converter suas leituras em GestureType.
        public override bool IsAvailable => false;
        public override bool CanRecognize(GestureType gesture) => false;
        public override bool IsGestureActive(GestureType gesture) => false;
    }
}
