using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Michsky.UI.Dark
{
    public class GamepadChecker : MonoBehaviour
    {
        // Resources
        public MainPanelManager defaultPanelManager;
        public List<MainPanelManager> panelManagers = new List<MainPanelManager>();

        // Settings
        [Tooltip("Always update input device. If you turn off this feature, you won't able to change the input device after start, but it might increase the performance.")]
        public bool alwaysUpdate = true;
        public bool affectCursor = true;
        public InputAction gamepadHotkey;

        // Core
        [Tooltip("Objects in this list will be enabled when the gamepad is un-plugged.")]
        public List<GameObject> keyboardObjects = new List<GameObject>();
        [Tooltip("Objects in this list will be enabled when the gamepad is plugged.")]
        public List<GameObject> gamepadObjects = new List<GameObject>();
        [Tooltip("Buttons in this list will be prepared for the current input device.")]
        public List<Button> buttons = new List<Button>();

        // Helpers
        Vector3 cursorPos;
        Vector3 lastCursorPos;
        Navigation customNav = new Navigation();

        [HideInInspector] public bool gamepadConnected;
        [HideInInspector] public bool gamepadEnabled;
        [HideInInspector] public bool keyboardEnabled;

        [HideInInspector] public float hAxis;
        [HideInInspector] public float vAxis;

        void Start()
        {
            if (alwaysUpdate == false) { this.enabled = false; }
            else { this.enabled = true; }

            gamepadHotkey.Enable();

            if (Gamepad.current == null)
            {
                gamepadConnected = false;
                SwitchToKeyboard();
            }

            else
            {
                gamepadConnected = true;
                SwitchToGamepad();
            }
        }

        void Update()
        {
            if (Gamepad.current == null)
                gamepadConnected = false;
            else
            {
                gamepadConnected = true;
                hAxis = Gamepad.current.rightStick.x.ReadValue();
                vAxis = Gamepad.current.rightStick.y.ReadValue();
            }

            cursorPos = Mouse.current.position.ReadValue();

            if (gamepadConnected == true && gamepadEnabled == true
                && keyboardEnabled == false && cursorPos != lastCursorPos)
                SwitchToKeyboard();
            else if (gamepadConnected == true && gamepadEnabled == false
                && keyboardEnabled == true && gamepadHotkey.triggered)
                SwitchToGamepad();
            else if (gamepadConnected == false && keyboardEnabled == false)
                SwitchToKeyboard();
        }

        public void SwitchToGamepad()
        {
            for (int i = 0; i < keyboardObjects.Count; i++)
                keyboardObjects[i].SetActive(false);

            for (int i = 0; i < gamepadObjects.Count; i++)
            {
                gamepadObjects[i].SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(gamepadObjects[i].GetComponentInParent<RectTransform>());
            }

            customNav.mode = Navigation.Mode.Automatic;

            for (int i = 0; i < buttons.Count; i++)
                if (buttons[i] != null)
                    buttons[i].navigation = customNav;

            gamepadEnabled = true;
            keyboardEnabled = false;
            lastCursorPos = Mouse.current.position.ReadValue();

            if (affectCursor == true)
                Cursor.visible = false;

            if (defaultPanelManager != null)
            {
                defaultPanelManager.gamepadEnabled = true;
                SelectUIObject(defaultPanelManager.panels[defaultPanelManager.currentPanelIndex].defaultSelected);
            }

            for (int i = 0; i < panelManagers.Count; i++)
            {
                if (panelManagers[i] == null)
                    continue;

                panelManagers[i].gamepadEnabled = true;
            }
        }

        public void SwitchToKeyboard()
        {
            for (int i = 0; i < gamepadObjects.Count; i++)
            {
                if (gamepadObjects[i] == null)
                    continue;

                gamepadObjects[i].SetActive(false);
            }

            for (int i = 0; i < keyboardObjects.Count; i++)
            {
                if (keyboardObjects[i] == null)
                    continue;

                keyboardObjects[i].SetActive(true);
                LayoutRebuilder.ForceRebuildLayoutImmediate(keyboardObjects[i].GetComponentInParent<RectTransform>());
            }

            customNav.mode = Navigation.Mode.None;

            for (int i = 0; i < buttons.Count; i++)
                if (buttons[i] != null)
                    buttons[i].navigation = customNav;

            gamepadEnabled = false;
            keyboardEnabled = true;

            if (affectCursor == true)
                Cursor.visible = true;

            if (defaultPanelManager != null)
                defaultPanelManager.gamepadEnabled = false;
        }

        public void SelectUIObject(GameObject tempObj)
        {
            if (gamepadEnabled == false)
                return;

            EventSystem.current.SetSelectedGameObject(tempObj.gameObject);
        }

        public void SelectDefaultPanelObject()
        {
            if (gamepadEnabled == false)
                return;

            EventSystem.current.SetSelectedGameObject(defaultPanelManager.panels[defaultPanelManager.currentPanelIndex].defaultSelected);
        }
    }
}