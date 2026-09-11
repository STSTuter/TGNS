using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FYP.Steam;

namespace FYP.PlayFabIntegration
{
    /// <summary>
    /// Gate on the login screen. There are no accounts anymore - Steam (via SteamBootstrap) is the
    /// only identity, so both "Register" and "Login" just confirm Steam is running, load the local
    /// character save, and continue past the screen. The email/username/password fields and the
    /// register/login split are kept only so the existing UI layout doesn't need to be reworked;
    /// the username field still doubles as a locally-remembered display name via PlayerPrefs.
    /// </summary>
    public class PlayfabAccountMenu : MonoBehaviour
    {
        [Header("Login GameObjects")]
        [SerializeField]
        GameObject rememberAccountButton;
        [SerializeField]
        GameObject loginButton;
        [SerializeField]
        GameObject switchToRegisterButton;

        [Header("Register GameObjects")]
        [SerializeField]
        GameObject emailField;
        [SerializeField]
        GameObject registerButton;
        [SerializeField]
        GameObject switchToLogInButton;

        [Header("Inputs")]
        [SerializeField]
        TMP_InputField emailInput;
        [SerializeField]
        TMP_InputField usernameInput;
        [SerializeField]
        TMP_InputField passwordInput;
        [SerializeField]
        Toggle rememberUserNameToggle;

        [Header("Misc")]
        [SerializeField]
        TMP_Text outputText;
        [SerializeField]
        GameObject transitionRegister;
        [SerializeField]
        GameObject transitionLogin;
        [SerializeField]
        PlayfabCharacter character;

        const string PREFS_USER_NAME_KEY = "UserName";
        const string PREFS_USER_NAME_REMEMBER_KEY = "RememberUserName";

        private void Start()
        {
            CheckForStoredUserNameToggle();
            CheckForStoredUserName();
            ShowSteamStatus();
        }

        private void ShowSteamStatus()
        {
            if (outputText == null) return;

            outputText.text = SteamBootstrap.Instance != null && SteamBootstrap.Instance.Initialized
                ? $"Signed in as {SteamBootstrap.Instance.UserName} (Steam)"
                : "Steam is not running - start Steam and restart the game.";
        }

        #region ButtonCallableMethods
        public void Register()
        {
            if (!EnsureSteamReady()) return;

            character.GetData();
            transitionRegister.SetActive(true);
        }

        public void LogIn()
        {
            if (!EnsureSteamReady()) return;

            character.GetData();
            StoreUserNameLocally();
            StartCoroutine(DelayTransition());
        }

        private bool EnsureSteamReady()
        {
            if (SteamBootstrap.Instance != null && SteamBootstrap.Instance.Initialized)
            {
                return true;
            }

            if (outputText != null)
            {
                outputText.text = "Steam is not running - cannot continue.";
            }
            return false;
        }

        public void SwitchBetweenRegisterAndLogin(bool toRegister)
        {
            emailField.gameObject.SetActive(toRegister);
            registerButton.gameObject.SetActive(toRegister);
            switchToLogInButton.gameObject.SetActive(toRegister);

            loginButton.gameObject.SetActive(!toRegister);
            rememberAccountButton.gameObject.SetActive(!toRegister);
            switchToRegisterButton.gameObject.SetActive(!toRegister);
        }
        #endregion

        IEnumerator DelayTransition()
        {
            yield return new WaitForSeconds(1f);
            transitionLogin.gameObject.SetActive(true);
        }

        #region Rember User Name
        void CheckForStoredUserName()
        {
            if (!rememberUserNameToggle.isOn) return;
            if (!PlayerPrefs.HasKey(PREFS_USER_NAME_KEY)) return;
            string storedUserName = PlayerPrefs.GetString(PREFS_USER_NAME_KEY);
            if (storedUserName != "")
            {
                usernameInput.text = storedUserName;
            }
        }

        void CheckForStoredUserNameToggle()
        {
            if (!PlayerPrefs.HasKey(PREFS_USER_NAME_REMEMBER_KEY)) return;
            string storedRememberUserNameToggle = PlayerPrefs.GetString(PREFS_USER_NAME_REMEMBER_KEY);
            rememberUserNameToggle.isOn = storedRememberUserNameToggle == "true";
        }

        void StoreUserNameLocally()
        {
            if (rememberUserNameToggle.isOn)
            {
                PlayerPrefs.SetString(PREFS_USER_NAME_KEY, usernameInput.text);
            }
            else
            {
                PlayerPrefs.SetString(PREFS_USER_NAME_KEY, "");
            }
            PlayerPrefs.Save();
        }

        public void HandleRememberUserNameToggle()
        {
            string rememberUserName = rememberUserNameToggle.isOn ? "true" : "false";
            PlayerPrefs.SetString(PREFS_USER_NAME_REMEMBER_KEY, rememberUserName);
            PlayerPrefs.Save();
        }
        #endregion

        void OnApplicationQuit()
        {
            HandleRememberUserNameToggle();
            StoreUserNameLocally();
        }
    }
}
