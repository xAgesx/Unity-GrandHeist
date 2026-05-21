using UnityEngine;

public class Billboard : MonoBehaviour {
    public Vector3 offset = new Vector3(0, 2.5f, 0);

    Transform cam;

    void Awake() {
        cam = Camera.main.transform;
        transform.localPosition = offset;
    }

    void LateUpdate() {
        transform.rotation = Quaternion.LookRotation(transform.position - cam.position);
    }
}
