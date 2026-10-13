using UnityEngine;

namespace MyoGuess.Gameplay
{
    public abstract class GestureSource : MonoBehaviour
    {
        public abstract bool IsAvailable { get; }
        public abstract bool CanRecognize(GestureType gesture);
        public abstract bool IsGestureActive(GestureType gesture);
    }
}
