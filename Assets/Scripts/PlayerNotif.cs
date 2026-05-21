using TMPro;
using UnityEngine;

public class PlayerNotif : MonoBehaviour {
    public static PlayerNotif Instance { get; private set; }

    public TextMeshProUGUI promptText;
    public Vector3 offset = new Vector3(0, 2.2f, 0);

    Transform cam;

    void Awake() {
        Instance = this;
        cam = Camera.main.transform;
        transform.localPosition = offset;
    }

    public void Show(string message) {
        promptText.text = message;
    }

    public void Hide() {
        promptText.text = "";
    }
}
