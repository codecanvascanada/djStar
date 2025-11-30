using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class MissionManager : MonoBehaviour
{
    public static MissionManager instance;

    // Mission Definitions
    [Serializable]
    public class Mission
    {
        public string id;
        public string description;
        public MissionType type;
        public int targetValue; // e.g., 3 for "Play 3 songs", 1 for "1 FC"
        public int rewardCoins;
        public bool isCumulative; // True for "Play X songs", "Cumulative Combo"
        public bool isCompleted;
        public int currentProgress; // For cumulative missions
        public int currentComboProgress; // For cumulative combo missions
    }

    public enum MissionType
    {
        PlaySongs,
        FullCombo,
        CumulativeCombo,
        AchieveScore,
        PerfectJudgments
    }

    [Header("Mission Settings")]
    public List<Mission> allPossibleMissions; // All missions the game can offer
    public int missionsPerDay = 3;
    public int allMissionsCompleteBonusCoins = 150; // Bonus for completing all daily missions

    [Header("Current Daily Missions")]
    public List<Mission> currentDailyMissions;

    private const string LastMissionResetDateKey = "LastMissionResetDate";
    private const string DailyMissionPrefix = "DailyMission_"; // Prefix for saving daily mission states

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMissions();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeMissions()
    {
        // Define all possible missions here (can be loaded from ScriptableObject/JSON later)
        if (allPossibleMissions == null || allPossibleMissions.Count == 0)
        {
            allPossibleMissions = new List<Mission>
            {
                new Mission { id = "play_3_songs", description = "Play 3 songs", type = MissionType.PlaySongs, targetValue = 3, rewardCoins = 150, isCumulative = true, isCompleted = false, currentProgress = 0 },
                new Mission { id = "1_fc_song", description = "Clear 1 song with a FULL COMBO", type = MissionType.FullCombo, targetValue = 1, rewardCoins = 250, isCumulative = false, isCompleted = false, currentProgress = 0 },
                new Mission { id = "cumulative_300_combo", description = "Achieve a cumulative 300 combo", type = MissionType.CumulativeCombo, targetValue = 300, rewardCoins = 250, isCumulative = true, isCompleted = false, currentProgress = 0 }
            };
        }

        CheckAndResetDailyMissions();
    }

    private void CheckAndResetDailyMissions()
    {
        string lastResetDateString = PlayerPrefs.GetString(LastMissionResetDateKey, DateTime.MinValue.ToString());
        DateTime lastResetDate = DateTime.Parse(lastResetDateString);

        if (lastResetDate.Date < DateTime.Today)
        {
            // It's a new day, reset missions
            // UnityEngine.Debug.Log("New day detected. Resetting daily missions.");
            ResetDailyMissions();
            PlayerPrefs.SetString(LastMissionResetDateKey, DateTime.Today.ToString());
            PlayerPrefs.Save();
        }
        else
        {
            // Same day, load existing missions
            // UnityEngine.Debug.Log("Same day. Loading existing daily missions.");
            LoadDailyMissions();
        }
    }

    private void ResetDailyMissions()
    {
        currentDailyMissions = new List<Mission>();
        // Select random missions from allPossibleMissions
        // For now, just take the first 'missionsPerDay' missions
        for (int i = 0; i < missionsPerDay && i < allPossibleMissions.Count; i++)
        {
            Mission newMission = allPossibleMissions[i]; // Clone to avoid modifying original
            newMission.isCompleted = false;
            newMission.currentProgress = 0;
            newMission.currentComboProgress = 0; // Reset combo progress
            currentDailyMissions.Add(newMission);
        }
        SaveDailyMissions();
    }

    private void LoadDailyMissions()
    {
        currentDailyMissions = new List<Mission>();
        for (int i = 0; i < missionsPerDay; i++)
        {
            string missionJson = PlayerPrefs.GetString(DailyMissionPrefix + i, string.Empty);
            if (!string.IsNullOrEmpty(missionJson))
            {
                Mission loadedMission = JsonUtility.FromJson<Mission>(missionJson);
                currentDailyMissions.Add(loadedMission);
            }
            else
            {
                // Fallback if a mission wasn't saved (e.g., first time loading today)
                ResetDailyMissions(); // Re-select and save
                return;
            }
        }
    }

    private void SaveDailyMissions()
    {
        for (int i = 0; i < currentDailyMissions.Count; i++)
        {
            string missionJson = JsonUtility.ToJson(currentDailyMissions[i]);
            PlayerPrefs.SetString(DailyMissionPrefix + i, missionJson);
        }
        PlayerPrefs.Save();
    }

    // --- Mission Progress Tracking ---
    public void OnSongPlayed()
    {
        foreach (Mission mission in currentDailyMissions)
        {
            if (mission.type == MissionType.PlaySongs && !mission.isCompleted)
            {
                mission.currentProgress++;
                if (mission.currentProgress >= mission.targetValue)
                {
                    CompleteMission(mission);
                }
            }
        }
        SaveDailyMissions();
    }

    public void OnComboAchieved(int currentCombo)
    {
        foreach (Mission mission in currentDailyMissions)
        {
            if (mission.type == MissionType.CumulativeCombo && !mission.isCompleted)
            {
                mission.currentComboProgress += currentCombo; // Add current combo to cumulative progress
                if (mission.currentComboProgress >= mission.targetValue)
                {
                    CompleteMission(mission);
                }
            }
        }
        SaveDailyMissions();
    }

    public void OnSongCleared(bool isFullCombo, float finalAchievementRate, int finalCombo)
    {
        foreach (Mission mission in currentDailyMissions)
        {
            if (!mission.isCompleted)
            {
                switch (mission.type)
                {
                    case MissionType.FullCombo:
                        if (isFullCombo && finalAchievementRate >= 90f) // FC + high achievement rate
                        {
                            CompleteMission(mission);
                        }
                        break;
                    case MissionType.AchieveScore:
                        // Placeholder for future score missions
                        break;
                    case MissionType.PerfectJudgments:
                        // Placeholder for future perfect judgment missions
                        break;
                }
            }
        }
        SaveDailyMissions();
    }

    // Placeholder for future perfect judgment tracking
    public void OnPerfectJudgment(int count)
    {
        // Implement logic for perfect judgment missions
    }

    // Placeholder for future score tracking
    public void OnScoreAchieved(int score)
    {
        // Implement logic for score missions
    }

    private void CompleteMission(Mission mission)
    {
        if (!mission.isCompleted)
        {
            mission.isCompleted = true;
            CurrencyManager.instance.AddCoins(mission.rewardCoins);
            // UnityEngine.Debug.Log(string.Format("Mission Completed: {0}! Rewarded {1} coins.", mission.description, mission.rewardCoins));
            CheckAllMissionsCompleted();
        }
    }

    private void CheckAllMissionsCompleted()
    {
        if (currentDailyMissions.All(m => m.isCompleted))
        {
            CurrencyManager.instance.AddCoins(allMissionsCompleteBonusCoins);
            // UnityEngine.Debug.Log(string.Format("All daily missions completed! Rewarded {0} bonus coins.", allMissionsCompleteBonusCoins));
        }
    }

    // For debugging/testing: Reset missions
    public void ResetMissions()
    {
        PlayerPrefs.DeleteKey(LastMissionResetDateKey);
        for (int i = 0; i < missionsPerDay; i++)
        {
            PlayerPrefs.DeleteKey(DailyMissionPrefix + i);
        }
        PlayerPrefs.Save();
        InitializeMissions(); // Re-initialize to get new missions
        // UnityEngine.Debug.Log("All missions reset.");
    }
}
