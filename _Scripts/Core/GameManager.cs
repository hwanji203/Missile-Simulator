using UnityEngine;

namespace Core
{
    public class GameManager : MonoBehaviour
    {
        private void Awake()
        {
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
