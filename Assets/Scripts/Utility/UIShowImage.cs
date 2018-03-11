using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.Utility
{
    public class UIShowImage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject DisplayGameObject;

        // Use this for initialization
        private void Start()
        {
            // Display nothing by default
            DisplayGameObject.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Display game object on mouse enter
            DisplayGameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Hide game object on mouse exit
            DisplayGameObject.SetActive(false);
        }
    }
}