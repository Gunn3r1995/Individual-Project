using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts
{
    public class GameUI : MonoBehaviour
    {
        public GameObject GameLoseUi;
        public GameObject GameWinUi;
        public Texture AlertedTexture;
        private bool _gameIsOver;

        [UsedImplicitly]
        private void Start()
        {
            GuardUtil.OnGuardCaughtPlayer += ShowGameLoseUi;
            FindObjectOfType<PlayerController>().OnReachedEndOfLevel += ShowGameWinUi;
        }

        [UsedImplicitly]
        private void Update()
        {
            if (_gameIsOver)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    FindObjectOfType<LevelManager>().ReloadCurrentScene();
                }
            }
        }

        private void ShowGameWinUi()
        {
            OnGameOver(GameWinUi);
        }

        private void ShowGameLoseUi()
        {
            OnGameOver(GameLoseUi);
        }

        private void OnGameOver(GameObject gameOverUi)
        {
            gameOverUi.SetActive(true);
            _gameIsOver = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            GuardUtil.OnGuardCaughtPlayer -= ShowGameLoseUi;
            FindObjectOfType<PlayerController>().OnReachedEndOfLevel -= ShowGameWinUi;
        }
    }
}