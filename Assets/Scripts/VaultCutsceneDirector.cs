using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class VaultCutsceneDirector : MonoBehaviour
{
    [Header("References (set by VaultCutsceneSetup)")]
    public PlayableDirector vaultDoorTimeline;
    public CameraController camCtrl;
    public PlayerController player;
    public Transform hudGroup;

    [Header("Door Frame (parent of the vault door lid)")]
    public Transform vaultDoorFrame;

    [Header("Camera Positions (relative to door frame - +Z = toward player)")]
    public Vector3 camPos_Exterior = new Vector3(-3f, 2f, 3f);
    public Vector3 camLook_Exterior = new Vector3(0f, 1f, 0f);
    public Vector3 camPos_Interior = new Vector3(2f, 1.5f, -4f);
    public Vector3 camLook_Interior = new Vector3(0f, 1f, -2f);
    public Vector3 camPos_Escape = new Vector3(0f, 2.5f, 8f);
    public Vector3 camLook_Escape = new Vector3(0f, 1f, 0f);

    [Header("Timing")]
    public float phase1_DoorOpen = 2.5f;
    public float phase2_LootNarrate = 4f;
    public float phase3_Alarm = 2f;
    public float phase4_Escape = 3f;
    public float cameraMoveDuration = 1.2f;
    public float textFadeDuration = 0.5f;

    [Header("Audio (set by VaultCutsceneSetup)")]
    public AudioClip sfxVaultOpen;
    public AudioClip sfxGoldClink;
    public AudioClip sfxAlarm;

    Transform vaultDoorTransform;
    bool cutscenePlaying;

    Image letterboxTop;
    Image letterboxBottom;
    TextMeshProUGUI cutsceneText;
    Image alarmFlash;

    Canvas canvas;
    bool uiCreated;

    public bool IsPlaying => cutscenePlaying;

    enum Phase { Idle, Opening, Looting, Alarm, Escape, Done }
    Phase currentPhase;

    void Awake()
    {
        if (vaultDoorFrame == null && transform.parent != null)
            vaultDoorFrame = transform.parent;
        if (vaultDoorFrame == null)
            vaultDoorFrame = transform;
        vaultDoorTransform = vaultDoorFrame;
    }

    void EnsureUI()
    {
        if (uiCreated) return;
        uiCreated = true;

        canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        if (hudGroup == null && GameManager.Instance != null)
            hudGroup = GameManager.Instance.overlayUI?.transform;

        letterboxTop = CreateLetterbox("Letterbox_Top",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(2000f, 120f));
        letterboxBottom = CreateLetterbox("Letterbox_Bottom",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(2000f, 120f));
        cutsceneText = CreateCutsceneText();
        alarmFlash = CreateAlarmFlash();
    }

    Image CreateLetterbox(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = go.GetComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        go.SetActive(false);
        return img;
    }

    TextMeshProUGUI CreateCutsceneText()
    {
        GameObject go = new GameObject("CutsceneText", typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(800f, 200f);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 36f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = "";
        tmp.gameObject.SetActive(false);
        return tmp;
    }

    Image CreateAlarmFlash()
    {
        GameObject go = new GameObject("AlarmFlash", typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        Image img = go.GetComponent<Image>();
        img.color = new Color(1f, 0f, 0f, 0f);
        img.raycastTarget = false;
        go.SetActive(false);
        return img;
    }

    public void BeginCutscene()
    {
        if (cutscenePlaying) return;

        EnsureUI();
        vaultDoorTimeline.time = 0d;
        StartCoroutine(CutsceneSequence());
    }

    IEnumerator CutsceneSequence()
    {
        cutscenePlaying = true;
        currentPhase = Phase.Opening;

        if (player != null) player.SetCutsceneMode(true);
        if (camCtrl != null) camCtrl.SetCutsceneActive(true);

        SetLetterbox(true);
        SetHUDVisible(false);

        // --- Phase 1: Vault Opens ---
        Vector3 exteriorPos = vaultDoorTransform.TransformPoint(camPos_Exterior);
        Vector3 exteriorLook = vaultDoorTransform.TransformPoint(camLook_Exterior);
        StartCoroutine(MoveCameraTo(exteriorPos, exteriorLook, cameraMoveDuration));

        vaultDoorTimeline.Play();
        if (sfxVaultOpen != null) SoundManager.Instance.PlaySFX(sfxVaultOpen);

        yield return new WaitForSeconds(phase1_DoorOpen);

        // --- Phase 2: Looting ---
        currentPhase = Phase.Looting;

        Vector3 interiorPos = vaultDoorTransform.TransformPoint(camPos_Interior);
        Vector3 interiorLook = vaultDoorTransform.TransformPoint(camLook_Interior);
        StartCoroutine(MoveCameraTo(interiorPos, interiorLook, cameraMoveDuration));

        yield return new WaitForSeconds(cameraMoveDuration * 0.5f);

        yield return StartCoroutine(ShowNarration("You step into the vault...", 1.5f));
        if (sfxGoldClink != null) SoundManager.Instance.PlaySFX(sfxGoldClink);
        yield return StartCoroutine(ShowNarration("Gold bars... stacks of cash...\nyou fill your bag to the brim.", 2.5f));

        // --- Phase 3: Alarm ---
        currentPhase = Phase.Alarm;

        StartCoroutine(AlarmFlashRoutine());
        if (sfxAlarm != null) SoundManager.Instance.PlaySFX(sfxAlarm);
        StartCoroutine(CameraShake(0.3f, phase3_Alarm));
        yield return StartCoroutine(ShowNarration("ALARM! They know you're here!", 1.5f));

        yield return new WaitForSeconds(phase3_Alarm - 1.5f);

        // --- Phase 4: Escape ---
        currentPhase = Phase.Escape;

        Vector3 escapeCamPos = vaultDoorTransform.TransformPoint(camPos_Escape);
        Vector3 escapeCamLook = vaultDoorTransform.TransformPoint(camLook_Escape);
        StartCoroutine(MoveCameraTo(escapeCamPos, escapeCamLook, cameraMoveDuration));

        yield return StartCoroutine(ShowNarration("You sprint for the exit!", 1.5f));

        yield return new WaitForSeconds(phase4_Escape - 1.5f);

        // --- End ---
        currentPhase = Phase.Done;

        SetLetterbox(false);
        SetHUDVisible(true);
        HideNarration();
        SetAlarmFlashOff();

        if (camCtrl != null) camCtrl.SetCutsceneActive(false);
        if (player != null) player.SetCutsceneMode(false);
        cutscenePlaying = false;
    }

    void SetLetterbox(bool show)
    {
        if (letterboxTop != null) letterboxTop.gameObject.SetActive(show);
        if (letterboxBottom != null) letterboxBottom.gameObject.SetActive(show);
    }

    void SetHUDVisible(bool visible)
    {
        if (hudGroup != null) hudGroup.gameObject.SetActive(visible);
    }

    void HideNarration()
    {
        if (cutsceneText != null)
        {
            cutsceneText.gameObject.SetActive(false);
            cutsceneText.text = "";
        }
    }

    void SetAlarmFlashOff()
    {
        if (alarmFlash != null) alarmFlash.gameObject.SetActive(false);
    }

    IEnumerator MoveCameraTo(Vector3 targetPos, Vector3 lookTarget, float duration)
    {
        Vector3 startPos = camCtrl.transform.position;
        Quaternion startRot = camCtrl.transform.rotation;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            t = Mathf.Clamp01(t);
            float smooth = t * t * (3f - 2f * t);

            Vector3 pos = Vector3.Lerp(startPos, targetPos, smooth);
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - pos);
            Quaternion rot = Quaternion.Slerp(startRot, targetRot, smooth);
            camCtrl.SetCutsceneTransform(pos, rot);
            startRot = camCtrl.transform.rotation;
            yield return null;
        }
    }

    IEnumerator ShowNarration(string text, float holdDuration)
    {
        cutsceneText.text = text;
        cutsceneText.gameObject.SetActive(true);

        float t = 0f;
        while (t < textFadeDuration)
        {
            t += Time.deltaTime;
            SetTextAlpha(Mathf.Clamp01(t / textFadeDuration));
            yield return null;
        }

        yield return new WaitForSeconds(holdDuration);

        t = textFadeDuration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            SetTextAlpha(Mathf.Clamp01(t / textFadeDuration));
            yield return null;
        }

        cutsceneText.gameObject.SetActive(false);
    }

    void SetTextAlpha(float alpha)
    {
        Color c = cutsceneText.color;
        c.a = Mathf.Clamp01(alpha);
        cutsceneText.color = c;
    }

    IEnumerator AlarmFlashRoutine()
    {
        alarmFlash.gameObject.SetActive(true);
        float t = 0f;
        while (currentPhase == Phase.Alarm)
        {
            t += Time.deltaTime * 6f;
            float alpha = (Mathf.Sin(t) + 1f) * 0.3f;
            Color c = alarmFlash.color;
            c.a = Mathf.Clamp01(alpha);
            alarmFlash.color = c;
            yield return null;
        }
        alarmFlash.gameObject.SetActive(false);
    }

    IEnumerator CameraShake(float intensity, float duration)
    {
        Vector3 originalPos = camCtrl.transform.localPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float ox = Random.Range(-intensity, intensity);
            float oy = Random.Range(-intensity, intensity);
            camCtrl.transform.localPosition = originalPos + new Vector3(ox, oy, 0f);
            yield return null;
        }
        camCtrl.transform.localPosition = originalPos;
    }

    Transform GizmoFrame()
    {
        if (vaultDoorFrame != null) return vaultDoorFrame;
        if (transform.parent != null) return transform.parent;
        return transform;
    }

    void OnDrawGizmosSelected()
    {
        Transform frame = GizmoFrame();

        var cameras = new (Vector3 pos, Vector3 look, Color color, string label)[]
        {
            (camPos_Exterior, camLook_Exterior, Color.green, "Exterior"),
            (camPos_Interior, camLook_Interior, Color.cyan,  "Interior"),
            (camPos_Escape,   camLook_Escape,   Color.yellow,"Escape"),
        };

        foreach (var (pos, look, color, label) in cameras)
        {
            Vector3 worldPos = frame.TransformPoint(pos);
            Vector3 worldLook = frame.TransformPoint(look);

            Gizmos.color = color;
            Gizmos.DrawLine(frame.position, worldPos);
            Gizmos.DrawWireSphere(worldPos, 0.15f);
            Gizmos.DrawLine(worldPos, worldLook);

            DrawCameraCone(worldPos, worldLook - worldPos, color);

#if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.Label(worldPos + Vector3.up * 0.3f, label);
#endif
        }
    }

    void DrawCameraCone(Vector3 position, Vector3 direction, Color color)
    {
        if (direction.sqrMagnitude < 0.0001f) return;
        Vector3 fwd = direction.normalized;
        float coneLength = Mathf.Min(direction.magnitude * 0.5f, 1.5f);
        float coneRadius = 0.3f;

        Vector3 end = position + fwd * coneLength;
        Gizmos.DrawLine(position, end);

        Vector3 right = Vector3.Cross(fwd, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(fwd, Vector3.forward).normalized;
        Vector3 up = Vector3.Cross(right, fwd).normalized;

        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Vector3 offset = (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * coneRadius;
            Vector3 p1 = end + offset;
            Vector3 p2 = end + (right * Mathf.Cos(a + 45f * Mathf.Deg2Rad) + up * Mathf.Sin(a + 45f * Mathf.Deg2Rad)) * coneRadius;
            Gizmos.DrawLine(position, p1);
            Gizmos.DrawLine(p1, p2);
        }
    }
}
