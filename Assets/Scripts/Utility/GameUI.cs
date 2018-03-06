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
        private bool _gameIsOver;
        private GuardUtil[] _guardUtils;

        [UsedImplicitly]
        private void Start()
        {
            GuardUtil.OnGuardCaughtPlayer += ShowGameLoseUi;
            FindObjectOfType<PlayerController>().OnReachedEndOfLevel += ShowGameWinUi;
            _guardUtils = FindObjectsOfType<GuardUtil>();
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
            EnableStateUi();
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

        private void DisableAllStateUi()
        {
            if(StateAlert != null) StateAlert.SetActive(false);
            if(StateInvestigate != null) StateInvestigate.SetActive(false);
            if(StateChase != null) StateChase.SetActive(false);
        }

        private void EnableStateUi()
        {
            var largestVal = _guardUtils.Select(guardUtil => (int) guardUtil.state).Concat(new[] {0}).Max();

            DisableAllStateUi();
            switch (largestVal)
            {
                case (int)GuardUtil.State.Patrol:
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
                default:
                    DisableAllStateUi();
                    break;
            }
        }
    }
}