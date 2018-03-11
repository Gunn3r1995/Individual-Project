using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Utility
{
    public class LevelManager : MonoBehaviour {
        public static event Action OnHandlePauseMenu;

        /// <summary>
        /// Loads the level with name 'sceneName'.
        /// </summary>
        /// <param name="sceneName">Scene name.</param>
        public void LoadLevel(string sceneName) {
            print("Level load requested for: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Loads the next level.
        /// </summary>
        public void LoadNextLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex+1);
        }

        /// <summary>
        /// Reloads the current scene.
        /// </summary>
        public void ReloadCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Quit request, won't work for builds like WebGL.
        /// </summary>
        public void QuitRequest()
        {
            Debug.Log("I want to quit!");
            Application.Quit();
        }

        public void HandlePauseMenu() {
            if (OnHandlePauseMenu != null)
                OnHandlePauseMenu();
        }
    }
}