using JohnStairs.RPG.Character.Cam;
using JohnStairs.RPG.Character.Motor;
using UnityEngine;
using UnityEngine.UI;

namespace JohnStairs.PPC.UI {
    public class DemoUI : MonoBehaviour {
        public GameObject CharacterMmo;
        public GameObject CharacterArpg;
        public GameObject CharacterIso;

        public Transform StartSpawn;
        public Transform PillarsSpawn;
        public Transform HouseSpawn;
        public Transform WaterSpawn;
        public Transform ClimbingSpawn;

        public Color SelectedButtonColor;
        public GameObject SelectedCharacterButton;
        public GameObject SelectedLocationButton;
        public GameObject MenuCursorHint;

        protected GameObject[] _characters;
        protected bool _teleport;
        protected Transform _teleportTarget;

        protected void Awake() {
        }

        protected void Start() {
            _characters = new[] { CharacterMmo, CharacterArpg, CharacterIso };
            SetHighlightedButton(SelectedCharacterButton, true);
            SetHighlightedButton(SelectedLocationButton, false);
            ShowMenuCursorHint(false);
        }

        protected void FixedUpdate() { // required to prevent interference with Character Controller component physics
            if (_teleport) {
                _teleport = false;
                foreach (GameObject character in _characters) {
                    character.transform.position = _teleportTarget.position;
                }
            }
        }

        public void ChangeCharacter(GameObject buttonObject) {
            SetHighlightedButton(buttonObject, true);
            ShowMenuCursorHint(false);

            foreach (GameObject character in _characters) {
                character.SetActive(false);
            }

            switch (buttonObject.name) {
                case "MMO":
                    CharacterMmo.SetActive(true);
                    break;
                case "ARPG":
                    ShowMenuCursorHint(true);
                    CharacterArpg.SetActive(true);
                    break;
                case "Iso":
                    CharacterIso.SetActive(true);
                    break;
            }
        }

        public void ChangeLocation(GameObject buttonObject) {
            SetHighlightedButton(buttonObject, false);

            switch (buttonObject.name) {
                case "Start":
                    Teleport(StartSpawn);
                    MotorReset();
                    CameraResetAndYaw(0);
                    break;
                case "Pillars":
                    Teleport(PillarsSpawn);
                    MotorReset();
                    CameraResetAndYaw(90.0f);
                    break;
                case "House":
                    Teleport(HouseSpawn);
                    MotorReset();
                    CameraResetAndYaw(HouseSpawn.rotation.eulerAngles.y);
                    break;
                case "Water":
                    Teleport(WaterSpawn);
                    CameraResetAndPitch(35);
                    break;
                case "Climbing":
                    Teleport(ClimbingSpawn);
                    MotorReset();
                    CameraResetAndYaw(ClimbingSpawn.rotation.eulerAngles.y);
                    break;
            }
        }

        protected void SetHighlightedButton(GameObject buttonObject, bool isCharacterButton) {
            if (isCharacterButton) {
                SetButtonColor(SelectedCharacterButton, Color.black);
                SelectedCharacterButton = buttonObject;
            } else {
                SetButtonColor(SelectedLocationButton, Color.black);
                SelectedLocationButton = buttonObject;
            }
            SetButtonColor(buttonObject, SelectedButtonColor);
        }

        protected void SetButtonColor(GameObject buttonObject, Color color) {
            if (buttonObject == null) {
                return;
            }
            ColorBlock colors = buttonObject.GetComponent<Button>().colors;
            colors.normalColor = color;
            colors.selectedColor = color;
            buttonObject.GetComponent<Button>().colors = colors;
        }

        protected void ShowMenuCursorHint(bool show) {
            MenuCursorHint?.SetActive(show);
        }

        protected void Teleport(Transform targetTransform) {
            _teleport = true;
            _teleportTarget = targetTransform;
            foreach (GameObject character in _characters) {
                character.transform.rotation = targetTransform.rotation;
            }
        }

        protected void MotorReset() {
            foreach (GameObject character in _characters) {
                character.GetComponent<RPGMotor>().Reset();
            }
        }

        protected void CameraResetAndYaw(float yaw) {
            foreach (GameObject character in _characters) {
                character.GetComponent<RPGCamera>().ResetView();
                character.GetComponent<RPGCamera>().SetYaw(yaw);
            }
        }

        protected void CameraResetAndPitch(float pitch) {
            foreach (GameObject character in _characters) {
                character.GetComponent<RPGCamera>().ResetView();
                character.GetComponent<RPGCamera>().SetPitch(pitch);
            }
        }
    }
}
