using Unity.MP_FPS.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Unity.MP_FPS
{
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenu : MonoBehaviour
    {
        static class UIElementNames
        {
            public const string PauseMainPanel = "PauseMainPanel";
            public const string PauseSettingsPanel = "PauseSettingsPanel";
            public const string ResumeButton = "ResumeButton";
            public const string SettingsButton = "SettingsButton";
            public const string SettingsBackButton = "SettingsBackButton";
            public const string MainMenuButton = "MainMenuButton";
            public const string QuitButton = "QuitButton";
        }

        VisualElement m_MainPanel;
        VisualElement m_SettingsPanel;
        Button m_ResumeButton;
        Button m_SettingsButton;
        Button m_SettingsBackButton;
        Button m_MainMenuButton;
        Button m_QuitButton;
        int m_LastToggleFrame = -1;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            GameInput.Actions.UI.TogglePauseMenu.performed += OnTogglePauseAction;

            root.SetBinding("style.display", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(GameSettings.PauseMenuStylePropertyName),
                bindingMode = BindingMode.ToTarget,
            });

            m_MainPanel = root.Q<VisualElement>(UIElementNames.PauseMainPanel);
            m_SettingsPanel = root.Q<VisualElement>(UIElementNames.PauseSettingsPanel);
            AudioSettingsPanel.Bind(m_SettingsPanel);

            m_ResumeButton = root.Q<Button>(UIElementNames.ResumeButton);
            m_ResumeButton.clicked += OnResumePressed;

            m_SettingsButton = root.Q<Button>(UIElementNames.SettingsButton);
            m_SettingsButton.clicked += OnSettingsPressed;

            m_SettingsBackButton = root.Q<Button>(UIElementNames.SettingsBackButton);
            m_SettingsBackButton.clicked += OnSettingsBackPressed;

            m_MainMenuButton = root.Q<Button>(UIElementNames.MainMenuButton);
            m_MainMenuButton.clicked += OnMainMenuPressed;
            m_MainMenuButton.SetEnabled(GameManager.CanUseMainMenu);

            m_QuitButton = root.Q<Button>(UIElementNames.QuitButton);
            m_QuitButton.clicked += OnQuitPressed;

            ShowMainPanel();
        }

        void OnDisable()
        {
            GameInput.Actions.UI.TogglePauseMenu.performed -= OnTogglePauseAction;
            m_ResumeButton.clicked -= OnResumePressed;
            m_SettingsButton.clicked -= OnSettingsPressed;
            m_SettingsBackButton.clicked -= OnSettingsBackPressed;
            m_MainMenuButton.clicked -= OnMainMenuPressed;
            m_QuitButton.clicked -= OnQuitPressed;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                HandlePauseShortcut();
            }
        }

        void OnTogglePauseAction(InputAction.CallbackContext context)
        {
            if (context.control != null && context.control.device is Keyboard)
            {
                return;
            }

            HandlePauseShortcut();
        }

        void HandlePauseShortcut()
        {
            if (Time.frameCount == m_LastToggleFrame)
            {
                return;
            }

            if (GameSettings.Instance.GameState != GlobalGameState.InGame)
            {
                return;
            }

            m_LastToggleFrame = Time.frameCount;

            if (m_SettingsPanel.style.display == DisplayStyle.Flex)
            {
                ShowMainPanel();
                m_ResumeButton?.Focus();
                return;
            }

            GameSettings.Instance.IsPauseMenuOpen = !GameSettings.Instance.IsPauseMenuOpen;

            if (!GameSettings.Instance.IsPauseMenuOpen)
            {
                ShowMainPanel();
            }
            else
            {
                m_ResumeButton?.Focus();
            }
        }

        void ShowMainPanel()
        {
            m_MainPanel.style.display = DisplayStyle.Flex;
            m_SettingsPanel.style.display = DisplayStyle.None;
        }

        void ShowSettingsPanel()
        {
            m_MainPanel.style.display = DisplayStyle.None;
            m_SettingsPanel.style.display = DisplayStyle.Flex;
        }

        void OnResumePressed()
        {
            ShowMainPanel();
            GameSettings.Instance.IsPauseMenuOpen = false;
        }

        void OnSettingsPressed() => ShowSettingsPanel();

        void OnSettingsBackPressed() => ShowMainPanel();

        static void OnMainMenuPressed()
        {
            GameManager.Instance.ReturnToMainMenuAsync();
            Utils.SetCursorVisible(true);
        }

        static void OnQuitPressed() => GameManager.Instance.QuitAsync();
    }
}
