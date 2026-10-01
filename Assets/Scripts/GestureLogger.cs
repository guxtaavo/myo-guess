using UnityEngine;

public class GestureLogger : MonoBehaviour
{
    [SerializeField] private string gestureName = "PointerPoseRight";

    public void OnGestureActivated()
    {
        Debug.Log($"[{gestureName}] Gesto detectado!");
    }

    public void OnGestureDeactivated()
    {
        Debug.Log($"[{gestureName}] Gesto encerrado.");
    }
}