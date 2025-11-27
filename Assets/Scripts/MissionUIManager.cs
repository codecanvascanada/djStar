using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections;

public class MissionUIManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject missionItemPrefab; // Prefab for a single mission item
    public Transform missionsContentParent; // Parent for mission items
    public TextMeshProUGUI allMissionsCompleteBonusText;
    public Button allMissionsCompleteButton;
    public GameObject missionPanel; // Reference to the mission panel itself

    void Start()
    {
        // Defer the UI refresh until the manager is ready
        StartCoroutine(WaitForManagerAndRefresh());
    }

    private IEnumerator WaitForManagerAndRefresh()
    {
        // Wait until the MissionManager has been initialized.
        yield return new WaitUntil(() => MissionManager.instance != null);

        // Now it's safe to refresh the UI if the panel is active.
        if (gameObject.activeInHierarchy)
        {
            RefreshMissionUI();
        }
    }

    public void RefreshMissionUI()
    {
        if (MissionManager.instance == null)
        {
            UnityEngine.Debug.LogError("MissionManager instance not found! Cannot refresh UI.");
            return;
        }

        // --- Start of New Diagnostic Logs ---
        if (missionItemPrefab == null)
        {
            UnityEngine.Debug.LogError("MissionItemPrefab is NOT ASSIGNED in the MissionUIManager inspector!");
            return;
        }
        MissionItemUI prefabScript = missionItemPrefab.GetComponent<MissionItemUI>();
        if (prefabScript == null)
        {
            UnityEngine.Debug.LogError("PREFAB ITSELF is missing the MissionItemUI.cs script!");
            return;
        }
        if (prefabScript.descriptionText == null)
        {
            UnityEngine.Debug.LogError("PREFAB's 'Description Text' field is NOT ASSIGNED in its inspector!");
            return;
        }
                    UnityEngine.Debug.Log(string.Format("Found {0} missions to display.", MissionManager.instance.currentDailyMissions.Count));        // --- End of New Diagnostic Logs ---

        // Clear existing mission items
        foreach (Transform child in missionsContentParent)
        {
            Destroy(child.gameObject);
        }

        // Populate current daily missions
        foreach (var mission in MissionManager.instance.currentDailyMissions)
        {
            GameObject itemGO = Instantiate(missionItemPrefab, missionsContentParent);
            MissionItemUI itemUI = itemGO.GetComponent<MissionItemUI>();

            if (itemUI == null)
            {
                // This check is now redundant due to the checks above, but kept as a safeguard
                UnityEngine.Debug.LogError("Instantiated MissionItemPrefab is MISSING the MissionItemUI.cs script! Please check the prefab.", itemGO);
                continue; 
            }
            
            itemUI.SetupMissionItem(mission, () => ClaimMissionReward(mission));
        }

        // Update all missions complete bonus display
        if (allMissionsCompleteBonusText != null)
        {
            allMissionsCompleteBonusText.text = $"All Missions Clear Bonus: {MissionManager.instance.allMissionsCompleteBonusCoins} Coins";
        }

        // Update all missions complete button interactability
        if (allMissionsCompleteButton != null)
        {
            bool allCompleted = MissionManager.instance.currentDailyMissions.All(m => m.isCompleted);
            allMissionsCompleteButton.interactable = allCompleted;
            allMissionsCompleteButton.onClick.RemoveAllListeners();
            allMissionsCompleteButton.onClick.AddListener(ClaimAllMissionsBonus);
        }
    }

    private void ClaimMissionReward(MissionManager.Mission mission)
    {
        // MissionManager already handles adding coins when mission is completed
        // This button would just trigger the check and UI refresh
        UnityEngine.Debug.Log(string.Format("Attempting to claim reward for mission: {0}", mission.description));
        // MissionManager.instance.CompleteMission(mission); // This is called internally by MissionManager
        RefreshMissionUI(); // Refresh UI to show updated status
    }

    private void ClaimAllMissionsBonus()
    {
        if (MissionManager.instance == null) return;

        bool allCompleted = MissionManager.instance.currentDailyMissions.All(m => m.isCompleted);
        if (allCompleted && allMissionsCompleteButton.interactable)
        {
            // MissionManager.CheckAllMissionsCompleted() is called internally when a mission is completed
            // So, this button just needs to refresh the UI and disable itself
            UnityEngine.Debug.Log("Claiming all missions complete bonus.");
            allMissionsCompleteButton.interactable = false; // Disable after claiming
            RefreshMissionUI(); // Refresh UI to show updated status
        }
        else
        {
            UnityEngine.Debug.Log("Not all missions are completed yet, or bonus already claimed.");
        }
    }

    public void ToggleMissionPanel()
    {
        if (missionPanel != null)
        {
            missionPanel.SetActive(!missionPanel.activeSelf);
            if (missionPanel.activeSelf)
            {
                if (MissionManager.instance != null)
                {
                    RefreshMissionUI();
                }
                else
                {
                    // If manager is not ready yet, start the waiting coroutine
                    StartCoroutine(WaitForManagerAndRefresh());
                }
            }
        }
    }
}
