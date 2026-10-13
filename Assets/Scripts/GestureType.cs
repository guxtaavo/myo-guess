namespace MyoGuess.Gameplay
{
    public enum GestureType { None, OpenHand, Pointer, Rock, Tripod, ThumbsUp }

    public enum GestureInputMode
    {
        [UnityEngine.InspectorName("Meta Quest 3")] Headset,
        [UnityEngine.InspectorName("Sensor FBG")] FBG
    }
}
