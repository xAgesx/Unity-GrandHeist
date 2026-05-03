using UnityEngine;

public class PlayerPlaceHolder : MonoBehaviour {
    
    public PlayerController player;
    public Vector3 offset;

    // Update is called once per frame
    void Update() {
        transform.position = player.transform.position + offset;
    }
}
