using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionManager : MonoBehaviour {
    public static MissionManager Instance { get; private set; }

    [System.Serializable]
    public struct Mission {
        public string text;
        public Sprite icon;
    }

    [Header("Missions")]
    public Mission[] missions;

    [Header("UI References")]
    public TextMeshProUGUI missionText;
    public Image missionIcon;
    public Image missionToggle;

    [Header("Animation")]
    public float punchScale = 1.15f;
    public float animationDuration = 0.3f;

    int currentMission = -1;

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start() {
        if (missionText == null)
            missionText = transform.Find("MissionTxt")?.GetComponent<TextMeshProUGUI>();
        if (missionIcon == null)
            missionIcon = transform.Find("Image")?.GetComponent<Image>();
        if (missionToggle == null)
            missionToggle = transform.Find("Image Toggled")?.GetComponent<Image>();

        if (missions.Length > 0)
            SetMission(0, false);
    }

    public void SetMission(int index, bool animate = true) {
        if (index < 0 || index >= missions.Length) return;
        currentMission = index;
        Mission m = missions[index];

        if (missionText != null) missionText.text = m.text;
        if (missionIcon != null && m.icon != null) missionIcon.sprite = m.icon;

        if (animate && gameObject.activeInHierarchy)
            StartCoroutine(AnimateUpdate());
    }

    public void AdvanceMission() {
        SetMission(currentMission + 1);
    }

    IEnumerator AnimateUpdate() {
        if (missionToggle != null)
            missionToggle.gameObject.SetActive(true);

        Vector3 original = transform.localScale;
        Vector3 target = original * punchScale;
        float half = animationDuration * 0.5f;

        float elapsed = 0f;
        while (elapsed < half) {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            t = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(original, target, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half) {
            elapsed += Time.deltaTime;
            float t = elapsed / half;
            t = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(target, original, t);
            yield return null;
        }

        transform.localScale = original;

        if (missionToggle != null)
            missionToggle.gameObject.SetActive(false);
    }
}
