using UnityEngine;

namespace Assets.Scripts.Utility
{
    public class FinishPointSpawner : MonoBehaviour
    {
        public GameObject[] FinishPoints;

        private void Start ()
        {
            DisableAllFinishPoints();
            EnableRandomFinishPoint();
        }

        private void DisableAllFinishPoints()
        {
            if(FinishPoints == null) return;
            if(FinishPoints.Length <= 0) return;

            foreach (var finishPoint in FinishPoints)
            {
                finishPoint.SetActive(false);
            }
        }

        private void EnableRandomFinishPoint()
        {
            if (FinishPoints == null) return;
            if(FinishPoints.Length <= 0) return;

            FinishPoints[Random.Range(0, FinishPoints.Length)].SetActive(true);
        }
    }
}
