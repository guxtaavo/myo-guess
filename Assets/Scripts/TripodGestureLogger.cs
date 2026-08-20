using UnityEngine;

public class TripodGestureLogger : MonoBehaviour
{
    public void OnPointerActivated()
    {
        Debug.Log("[TripodRight] Gesto detectado!");
    }

    public void OnPointerDeactivated()
    {
        Debug.Log("[TripodRight] Gesto encerrado.");
    }
}