using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts.Utility
{
    public class GameUI : MonoBehaviour
    {
        public GameObject GameLoseUi;
        public GameObject GameWinUi;
        public GameObject StateAlert;
        public GameObject StateInvestigate;
        public GameObject StateChase;
        public GameObject PauseMenu;

        private bool _gameIsOver;
        private bool _gameIsPaused;
        private GuardUtil[] _guardUtils;

        [UsedImplicitly]
        private void Start()
        {
            Time.timeScale = 1;

            // Action handles for Ui
            FindObjectOfType<PlayerController>().OnReachedEndOfLevel += ShowGameWinUi;
            GuardUtil.OnGuardCaughtPlayer += ShowGameLoseUi;
            LevelManager.OnHandlePauseMenu += HandlePauseMenu;

            // Get all Guards utiities
            _guardUtils = FindObjectsOfType<GuardUtil>();
        }

        [UsedImplicitly]
        private void Update()
        {
            // If game is over and space is pressed, reload current scene (Restart Level)
            if (_gameIsOver && Input.GetKeyDown(KeyCode.Space))
                FindObjectOfType<LevelManager>().ReloadCurrentScene();

            // Pause menu to restart or go back to main menu
            if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) && !_gameIsOver)
                HandlePauseMenu();

            HandleStateUi();
        }

        /// <summary>
        /// Shows the game win user interface.
        /// </summary>
        private void ShowGameWinUi()
        {
            OnGameOver(GameWinUi);
        }

        /// <summary>
        /// Shows the game lose user interface.
        /// </summary>
        private void ShowGameLoseUi()
        {
            OnGameOver(GameLoseUi);
        }

        /// <summary>
        /// Handles the supplied gameOverUi game object 
        /// </summary>
        /// <param name="gameOverUi">Game over user interface.</param>
        private void OnGameOver(GameObject gameOverUi)
        {
            gameOverUi.SetActive(true);
            _gameIsOver = true;

            // Handle the cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;


            GuardUtil.OnGuardCaughtPlayer -= ShowGameLoseUi;
            FindObjectOfType<PlayerController>().OnReachedEndOfLevel -= ShowGameWinUi;
        }

        /// <summary>
        /// Disables all state user interface.
        /// </summary>
        private void DisableAllStateUi()
        {
            if(StateAlert != null) StateAlert.SetActive(false);
            if(StateInvestigate != null) StateInvestigate.SetActive(false);
            if(StateChase != null) StateChase.SetActive(false);
        }

        /// <summary>
        /// Handles the state user interface.
        /// </summary>
        private void HandleStateUi()
        {
            var largestVal = _guardUtils.Select(guardUtil => (int) guardUtil.state).Concat(new[] {0}).Max();

            DisableAllStateUi();
            switch (largestVal)
            {
                case (int)GuardUtil.State.Patrol:
                case (int)GuardUtil.State.Stand:
                default:
                    DisableAllStateUi();
                    break;
                case (int)GuardUtil.State.Alert:
                    if(StateAlert != null) StateAlert.SetActive(true);
                    break;
                case (int)GuardUtil.State.Investigate:
                    if (StateInvestigate != null) StateInvestigate.SetActive(true);
                    break;
                case (int)GuardUtil.State.Chase:
                    if (StateChase != null) StateChase.SetActive(true);
                    break;
            }
        }

        /// <summary>
        /// Handles the pause menu.
        /// </summary>
        private void HandlePauseMenu() {
            if (!_gameIsPaused)
                ShowPauseMenu();
            else
                HidePauseMenu();
        }

        /// <summary>
        /// Shows the pause menu.
        /// </summary>
        private void ShowPauseMenu()
        {
            if (PauseMenu == null)
                return;

            Time.timeScale = 0;
            _gameIsPaused = true;
            PauseMenu.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Hides the pause menu.
        /// </summary>
        private void HidePauseMenu() {
            if (PauseMenu == null)
                return;

            Time.timeScale = 1;
            _gameIsPaused = false;
            PauseMenu.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}