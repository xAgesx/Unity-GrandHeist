using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class LeaderboardMenu : MonoBehaviour
{
    [Header("Dreamlo Settings")]
    private string publicCode = "0TrV-wKaEkWx8ocKqNbGoAoFEqax_hN0OgBB8cLn1Myw";
    public int inputScore;
    public TextMeshProUGUI inputName;

    public string webURL = "http://dreamlo.com/lb/";
    public string proxy = "https://penguin-avalanche-proxy.thamer-douss.workers.dev/?url=";

    public List<TextMeshProUGUI> names;
    public List<TextMeshProUGUI> scores;

    // Call this from a button
    public void Start()
    {	
		inputScore = PlayerPrefs.GetInt("LastScore", 0);
        StartCoroutine(DownloadScores());
    }



    public void SubmitScore(){
        inputScore = PlayerPrefs.GetInt("LastScore", 0);
        StartCoroutine(UploadScore(inputName.text, inputScore));
        
        FindAnyObjectByType<MenuManager>().GetComponent<MenuManager>().Back();
    }
    IEnumerator UploadScore(string name, int score)
    {
        Debug.Log("Uploading ...");
        string url = $"{proxy}{webURL}{publicCode}/add/{name}/"+ score.ToString();
        Debug.Log(url);
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Score uploaded successfully!");
        }
        else
        {
            Debug.LogError("Error: " + www.error);
        }
    }

    // Example: Fetch scores (optional)
    IEnumerator DownloadScores()
    {
        string url = $"{proxy}{webURL}{publicCode}/json";
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            ProcessAndDisplayScores(www.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error fetching scores: " + www.error);
        }
    }
    void ProcessAndDisplayScores(string json)
    {
        //Clear all slots first
        foreach (var slot in names) slot.text = "";
        foreach (var slot in scores) slot.text = "";

        try
        {
            //JSON parsing
            var entries = json.Split(new[] { "{\"name\":" }, System.StringSplitOptions.RemoveEmptyEntries).Skip(1).Take(5).ToList();
            for (int i = 0; i < entries.Count && i < 5; i++)
            {
                string[] parts = entries[i].Split(',');
                string name = parts[0].Trim('"');
                string score = parts[1].Split(':')[1].Trim('"');
                
                names[i].text = name;
                scores[i].text = score;
                
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing leaderboard: " + e.Message);
        }
    }
}