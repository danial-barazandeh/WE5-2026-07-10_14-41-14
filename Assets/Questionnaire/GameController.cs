using System;
using System.Collections.Generic; // Required for List<T>
using UnityEngine;
using UnityEngine.Android;
using System.IO;

public class GameController : MonoBehaviour
{
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject phase1Panel;
    [SerializeField] private GameObject phase2Panel;
    [SerializeField] private GameObject phase3Panel;
    
    private string fileName;

    // 1. Create a persistent variable to hold all data for the entire session
    private GameSessionData currentSession;

    [System.Serializable]
    public class GameSessionData
    {
        public string playerAge;
        public string playerGender;
        public string datePlayed;
        // 2. Change this to a List so it can hold multiple rounds
        public List<SessionStats> rounds = new List<SessionStats>();     
    }

    [System.Serializable]
    public class SessionStats
    {
        public int roundNumber; // Helpful to track which of the 8 experiments this is
        public int trustLevel;
        public string NPCGender;
        public string NPCTattooCategory;
    }

    private string GetDownloadsPath()
    {
    #if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment"))
        {
            string directoryDownloads = environment.GetStatic<string>("DIRECTORY_DOWNLOADS");
            using (AndroidJavaObject downloadsDir = environment.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", directoryDownloads))
            {
                return downloadsDir.Call<string>("getAbsolutePath");
            }
        }
    #else
        return Application.persistentDataPath;
    #endif
    }

    void Start()
    {
        // 3. Initialize the session data ONCE when the app starts
        fileName = "Experiment_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
        
        currentSession = new GameSessionData();
        currentSession.playerAge = "25";
        currentSession.playerGender = "Female";
        currentSession.datePlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        startPanel.SetActive(true);

        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }
    }

    void Update()
    {
        
    }

    public void goToPhase1()
    {
        startPanel.SetActive(false);
        phase2Panel.SetActive(false); 
        phase3Panel.SetActive(false); 

        phase1Panel.SetActive(true);
    }

    public void goToPhase2()
    {
        phase1Panel.SetActive(false);
        phase2Panel.SetActive(true);
    }

    public void goToPhase3()
    {
        phase2Panel.SetActive(false);
        phase3Panel.SetActive(true);
    }

 public void finishScene(int score)
    {
        Debug.Log("Final Score: " + score);

        // 1. Save the data FIRST. This adds the new stat record to the list.
        SaveSharedString(score);

        // 2. Check how many rounds have been recorded
        if (currentSession.rounds.Count < 8)
        {
            Debug.Log("Round " + currentSession.rounds.Count + " complete. Looping back to Phase 1...");
            // Not the last round! Restart the flow.
            goToPhase1();
        }
        else
        {
            Debug.Log("Experiment Complete! All 8 rounds finished.");
            // It IS the last round. Turn off all gameplay panels.
            // (You could also activate a "Thank You" panel here if you have one)
            phase1Panel.SetActive(false);
            phase2Panel.SetActive(false);
            phase3Panel.SetActive(false);
        }
    }

    public void SaveSharedString(int dataToShare)
    {
        // 4. Create a NEW stats object just for this round
        SessionStats thisRoundStats = new SessionStats();
        thisRoundStats.roundNumber = currentSession.rounds.Count + 1; // Automatically counts 1, 2, 3...
        thisRoundStats.trustLevel = dataToShare;
        thisRoundStats.NPCGender = "Male";
        thisRoundStats.NPCTattooCategory = "Tattoo Category";
    
        // 5. Add this round's stats to the main session's list
        currentSession.rounds.Add(thisRoundStats);

        // 6. Convert the ENTIRE session (with all rounds) to pretty JSON
        string jsonString = JsonUtility.ToJson(currentSession, true);

        try
        {
            string downloadsPath = GetDownloadsPath();
            
            if (!Directory.Exists(downloadsPath))
            {
                Directory.CreateDirectory(downloadsPath);
            }

            string filePath = Path.Combine(downloadsPath, fileName);

            // 7. Overwrite the file with the updated JSON. 
            // Because the JSON string contains all previous rounds too, no data is lost!
            File.WriteAllText(filePath, jsonString);
            
            Debug.Log("Saved JSON round " + thisRoundStats.roundNumber + " to: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("CRASH in SaveSharedString: " + e.Message);
            Debug.LogError(e.StackTrace);
        }
    }
}