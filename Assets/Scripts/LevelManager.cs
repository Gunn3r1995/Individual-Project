using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts
{
    public class LevelManager : MonoBehaviour {
        
        public void LoadLevel(string sceneName) {
            Debug.Log("Level load requested for: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }

        public void LoadNextLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex+1);
        }

        public void ReloadCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitRequest()
        {
            Debug.Log("I want to quit!");
            Application.Quit();
        }
    }
}