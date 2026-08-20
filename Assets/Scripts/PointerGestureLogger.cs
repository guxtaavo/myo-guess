using UnityEngine;

public class PointerGestureLogger : MonoBehaviour
{
    public void OnPointerActivated()
    {
        Debug.Log("[PointerRight] Gesto detectado!");
    }

    public void OnPointerDeactivated()
    {
        Debug.Log("[PointerRight] Gesto encerrado.");
    }
}