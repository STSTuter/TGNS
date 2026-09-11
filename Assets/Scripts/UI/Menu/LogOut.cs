using Unity.Netcode;
using UnityEngine;

namespace Menu
{
    public class LogOut : MonoBehaviour
    {
        [SerializeField]
        GameObject logOutTransition;

        // There's no account to log out of anymore (Steam is the only identity) - this now just
        // drops any active network session and returns to the login/menu screen. The method name
        // is kept as-is because the login screen's button still calls it by name.
        public void LogOutOfPlayfab()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            logOutTransition.SetActive(true);
        }
    }
}
