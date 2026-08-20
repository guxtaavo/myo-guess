using UnityEngine;

public class RestGestureLogger : MonoBehaviour
{
    public void OnPointerActivated()
    {
        Debug.Log("[RestRight] Gesto detectado!");
    }

    public void OnPointerDeactivated()
    {
        Debug.Log("[RestRight] Gesto encerrado.");
    }
}