using System;
using System.Collections.Generic; // Required for List<T>
using UnityEngine;
using UnityEngine.Android;
using System.IO;
using TMPro;
using System.Collections;

public class GameController : MonoBehaviour
{
    [SerializeField] private GameObject agePanel;
    [SerializeField] private GameObject tattooPanel;
    [SerializeField] private GameObject othersTattooPanel;
    [SerializeField] private GameObject genderPanel;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject phase1Panel;
    [SerializeField] private GameObject phase2Panel;
    [SerializeField] private GameObject phase3Panel;
    [SerializeField] private AvatarInteractionManager avatarManager;
    
    private string fileName;

    // 1. Create a persistent variable to hold all data for the entire session
    private GameSessionData currentSession;

    [System.Serializable]
    public class GameSessionData
    {
        public string playerAge;
        public string playerGender;
        public string datePlayed;
        public string playerTattoo;
        public string othersTattoo;
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
        currentSession.datePlayed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        agePanel.SetActive(true);

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


public void confirmAge()
    {
        TMPro.TMP_Dropdown ageDropdownFirstDigit = System.Array.Find(agePanel.GetComponentsInChildren<TMPro.TMP_Dropdown>(true), d => d.name == "AgeFirstDigit");
        TMPro.TMP_Dropdown ageDropdownSecondDigit = System.Array.Find(agePanel.GetComponentsInChildren<TMPro.TMP_Dropdown>(true), d => d.name == "AgeSecondDigit");

        // --- SAFETY CHECKS ---
        if (ageDropdownFirstDigit == null)
        {
            Debug.LogError("CRASH PREVENTED: Could not find 'AgeFirstDigit'. Check spelling and ensure it's a child of agePanel.");
            return; // Stops the function here so it doesn't crash
        }
        if (ageDropdownSecondDigit == null)
        {
            Debug.LogError("CRASH PREVENTED: Could not find 'AgeSecondDigit'. Check spelling and ensure it's a child of agePanel.");
            return;
        }

        // Safely get the actual TEXT of the selected option
        string firstDigit = ageDropdownFirstDigit.options[ageDropdownFirstDigit.value].text;
        string secondDigit = ageDropdownSecondDigit.options[ageDropdownSecondDigit.value].text;
        
        // Save it to your class-level variable
        currentSession.playerAge = firstDigit + secondDigit;

        // Hide all panels
        agePanel.SetActive(false);
        startPanel.SetActive(false);
        phase2Panel.SetActive(false); 
        phase3Panel.SetActive(false); 
        phase1Panel.SetActive(false);
        tattooPanel.SetActive(false);
        othersTattooPanel.SetActive(false);

        // Show the next phase
        genderPanel.SetActive(true);
    }


    public void confirmGender(  string gender){
        currentSession.playerGender = gender;

        agePanel.SetActive(false);
        startPanel.SetActive(false);
        phase2Panel.SetActive(false); 
        phase3Panel.SetActive(false); 
        phase1Panel.SetActive(false);
        othersTattooPanel.SetActive(false);
        genderPanel.SetActive(false);


        tattooPanel.SetActive(true);

    }

      public void confirmTattoo(  string tattoo){
        currentSession.playerTattoo = tattoo;

        agePanel.SetActive(false);
        startPanel.SetActive(false);
        phase2Panel.SetActive(false); 
        phase3Panel.SetActive(false); 
        phase1Panel.SetActive(false);
        genderPanel.SetActive(false);
        tattooPanel.SetActive(false);

        othersTattooPanel.SetActive(true);

    }

    public void confirmOthersTattoo(  string tattoo){
        currentSession.othersTattoo = tattoo;

        agePanel.SetActive(false);
        phase2Panel.SetActive(false); 
        phase3Panel.SetActive(false); 
        phase1Panel.SetActive(false);
        genderPanel.SetActive(false);
        tattooPanel.SetActive(false);
        othersTattooPanel.SetActive(false);


        startPanel.SetActive(true);

    }
    

    public void goToPhase1()
    {
        disableAllPanels();
        avatarManager.StartExperimentWithUI();
        StartCoroutine(activatePanelAfterDelay(2.5f, phase1Panel));
    } 

    

    public void goToPhase2()
    {
        disableAllPanels();
        avatarManager.NotifyYesHeard();
        StartCoroutine(activatePanelAfterDelay(2.5f, phase2Panel));
    }

    public void goToPhase3()
    {

        phase1Panel.SetActive(false);
        startPanel.SetActive(false);
        phase2Panel.SetActive(false);
        genderPanel.SetActive(false);
        tattooPanel.SetActive(false);
        othersTattooPanel.SetActive(false);
        agePanel.SetActive(false);

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
        // Fetch the data from your other script
        AvatarData currentAvatar = null;
        if (avatarManager != null)
        {
            currentAvatar = avatarManager.GetAvatarData();
        }
        else
        {
            Debug.LogWarning("AvatarManager is not linked in GameController!");
        }

        SessionStats thisRoundStats = new SessionStats();
        thisRoundStats.roundNumber = currentSession.rounds.Count + 1; 
        thisRoundStats.trustLevel = dataToShare;
        
        // Grab the strings from the AvatarData object. 
        // We use a fallback ("Unknown") just in case something went wrong.
        thisRoundStats.NPCGender = (currentAvatar != null) ? currentAvatar.gender : "Unknown";
        thisRoundStats.NPCTattooCategory = (currentAvatar != null) ? currentAvatar.tatoo : "Unknown";
    
        currentSession.rounds.Add(thisRoundStats);

        string jsonString = JsonUtility.ToJson(currentSession, true);

        avatarManager.AdvanceToNextAvatar();

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







    private IEnumerator activatePanelAfterDelay(float delayTime, GameObject panel)
    {
        Debug.Log("Delay yime: "+delayTime.ToString()+" Activated panel: " + panel.name);
        yield return new WaitForSeconds(delayTime);
        panel.SetActive(true);
        Debug.Log("Activated panel: " + panel.name);
    }

    private void disableAllPanels()
    {
        phase1Panel.SetActive(false);
        startPanel.SetActive(false);
        phase2Panel.SetActive(false);
        genderPanel.SetActive(false);
        tattooPanel.SetActive(false);
        othersTattooPanel.SetActive(false);
        agePanel.SetActive(false);
        phase3Panel.SetActive(false);
    }
}