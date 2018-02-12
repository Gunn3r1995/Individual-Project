using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts
{
    public class GameUI : MonoBehaviour
    {
        public GameObject gameLoseUI;
        public GameObject gameWinUI;
        bool gameIsOver;

        // Use this for initialization
        void Start()
        {
            Guard.OnGuardCaughtPlayer += ShowGameLoseUI;
            FindObjectOfType<PlayerController>().OnReachedEndOfLevel += ShowGameWinUI;
        }

        private void Update()
        {
            if (gameIsOver)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    FindObjectOfType<LevelManager>().ReloadCurrentScene();
                }
            }
        }

        void ShowGameWinUI()
        {
            OnGameOver(gameWinUI);
        }

        void ShowGameLoseUI()
        {
            OnGameOver(gameLoseUI);
        }

        void OnGameOver(GameObject gameOverUI)
        {
            gameOverUI.SetActive(true);
            gameIsOver = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Guard.OnGuardCaughtPlayer -= ShowGameLoseUI;
            FindObjectOfType<PlayerController>().OnReachedEndOfLevel -= ShowGameWinUI;
        }

    }
}