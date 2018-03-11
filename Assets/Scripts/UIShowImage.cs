using UnityEngine;
using UnityEngine.EventSystems;

public class UIShowImage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public GameObject DisplayGameObject;

	// Use this for initialization
	private void Start () {
        print("Game Object");
        DisplayGameObject.SetActive(false);
	}

    public void OnPointerEnter(PointerEventData eventData)
    {
		print("Mouse Enter");
		DisplayGameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        print("Mouse Exit");
		DisplayGameObject.SetActive(false);
    }
}
