using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class DreamloEntry {
    public string name;
    public string score;
}

[System.Serializable]
public class DreamloLeaderboard {
    public DreamloEntry[] entry;
}

[System.Serializable]
public class DreamloData {
    public DreamloLeaderboard leaderboard;
}

[System.Serializable]
public class DreamloResponse {
    public DreamloData dreamlo;
}

public class LeaderboardMenu : MonoBehaviour {
    [Header("Dreamlo")]
    private string privateKey = "AMs378zae0Gz8FARWBoe4wfgZPf0dT40mQv26s126CHQ";
    public string webURL = "http://dreamlo.com/lb/";
    public string proxy = "https://penguin-avalanche-proxy.thamer-douss.workers.dev/?url=";

    public List<TextMeshProUGUI> names;
    public List<TextMeshProUGUI> scores;

    [Header("Submit")]
    public GameObject submitSection;
    public TextMeshProUGUI inputName;

    float playerTime;
    int lowestQualifyingSeconds;
    public string raw;

    void Start() {
        playerTime = PlayerPrefs.GetFloat("LastTime", -1f);
        if (submitSection != null) submitSection.SetActive(false);
        StartCoroutine(DownloadScores());
    }

    public void SubmitScore() {
        string name = inputName != null ? inputName.text : "Player";
        if (string.IsNullOrWhiteSpace(name)) name = "Player";
        int seconds = Mathf.RoundToInt(playerTime);
        StartCoroutine(UploadTime(name, seconds));
    }

    IEnumerator UploadTime(string name, int timeSeconds) {
        string url = $"{proxy}{webURL}{privateKey}/add/{name}/{timeSeconds}";
        Debug.Log("[Leaderboard] Uploading time to: " + url);
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
            Debug.Log("[Leaderboard] Upload response: " + www.downloadHandler.text);
        else
            Debug.LogError("[Leaderboard] Upload error (" + www.result + "): " + www.error);

        StartCoroutine(DownloadScores());

        // if (MenuManager.Instance != null)
        //     MenuManager.Instance.BackToMenu();
        // else
        //     UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    IEnumerator DownloadScores() {
        string url = $"{proxy}{webURL}{privateKey}/json/5";
        Debug.Log("[Leaderboard] Downloading from: " + url);
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success) {
            raw = www.downloadHandler.text;
            Debug.Log("[Leaderboard] Raw response (" + raw.Length + " chars): " + raw);
            if (!string.IsNullOrEmpty(raw))
                ProcessAndDisplayScores(raw);
        } else {
            Debug.LogError("[Leaderboard] Download error (" + www.result + "): " + www.error);

            string fallbackUrl = $"{webURL}{privateKey}/json/5/asc";
            Debug.Log("[Leaderboard] Trying direct (no proxy): " + fallbackUrl);
            UnityWebRequest fallback = UnityWebRequest.Get(fallbackUrl);
            yield return fallback.SendWebRequest();

            if (fallback.result == UnityWebRequest.Result.Success) {
                string raw = fallback.downloadHandler.text;
                Debug.Log("[Leaderboard] Direct response: " + raw);
                if (!string.IsNullOrEmpty(raw))
                    ProcessAndDisplayScores(raw);
            } else {
                Debug.LogError("[Leaderboard] Direct also failed (" + fallback.result + "): " + fallback.error);
            }
        }
    }

    void ProcessAndDisplayScores(string json) {
        foreach (var slot in names) slot.text = "";
        foreach (var slot in scores) slot.text = "";

        try {
            DreamloResponse response = JsonUtility.FromJson<DreamloResponse>(json);
            if (response?.dreamlo?.leaderboard?.entry == null) {
                Debug.Log("[Leaderboard] Parsed OK but entry array is null");
                return;
            }

            DreamloEntry[] entries = response.dreamlo.leaderboard.entry;
            Debug.Log("[Leaderboard] Parsed " + entries.Length + " entries");

            if (entries.Length == 0) {
                Debug.Log("[Leaderboard] Entry array is empty");
                return;
            }

            int displayCount = Mathf.Min(entries.Length, names.Count);

            for (int i = 0; i < displayCount; i++) {
                int totalSec = int.Parse(entries[i].score);
                int min = totalSec / 60;
                int sec = totalSec % 60;

                string displayName = entries[i].name;
                string displayTime = string.Format("{0:00}:{1:00}", min, sec);
                Debug.Log("[Leaderboard] Entry " + i + ": " + displayName + " - " + displayTime);

                names[i].text = $"{i + 1}. {displayName}";
                scores[i].text = displayTime;
            }

            if (entries.Length >= 5)
                lowestQualifyingSeconds = int.Parse(entries[4].score);
            else if (entries.Length > 0)
                lowestQualifyingSeconds = int.Parse(entries[entries.Length - 1].score);

            Debug.Log("[Leaderboard] Qualifying threshold: " + lowestQualifyingSeconds + "s");
            CheckQualification();
        } catch (System.Exception e) {
            Debug.LogError("[Leaderboard] Parse error: " + e.Message + "\nJSON: " + json);
        }
    }

    void CheckQualification() {
        if (playerTime < 0f || submitSection == null) return;

        int playerSeconds = Mathf.RoundToInt(playerTime);
        bool qualifies = lowestQualifyingSeconds <= 0 || playerSeconds <= lowestQualifyingSeconds;
        submitSection.SetActive(qualifies);
    }
}
