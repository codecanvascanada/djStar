using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class MissionItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI rewardText;
    public Button claimRewardButton;
    public Image missionStatusImage; // Optional: to show completed/in-progress status

    [Header("Status Colors/Sprites")]
    public Color completedColor = Color.green;
    public Color inProgressColor = Color.yellow;
    public Sprite completedSprite;
    public Sprite inProgressSprite;

    public void SetupMissionItem(MissionManager.Mission mission, UnityAction claimAction)
    {
        if (descriptionText != null) descriptionText.text = mission.description;
        if (rewardText != null) rewardText.text = $"{mission.rewardCoins} Coins";

        UpdateProgressDisplay(mission);

        if (claimRewardButton != null)
        {
            claimRewardButton.onClick.RemoveAllListeners();
            claimRewardButton.onClick.AddListener(claimAction);
            claimRewardButton.interactable = mission.isCompleted; // Only interactable if completed
        }

        if (missionStatusImage != null)
        {
            missionStatusImage.color = mission.isCompleted ? completedColor : inProgressColor;
            missionStatusImage.sprite = mission.isCompleted ? completedSprite : inProgressSprite;
        }
    }

    public void UpdateProgressDisplay(MissionManager.Mission mission)
    {
        if (progressText != null)
        {
            if (mission.isCompleted)
            {
                progressText.text = "Complete!";
            }
            else if (mission.isCumulative)
            {
                progressText.text = $"{mission.currentProgress}/{mission.targetValue}";
            }
            else // Non-cumulative, e.g., FC
            {
                progressText.text = "In Progress...";
            }
        }
        if (claimRewardButton != null)
        {
            claimRewardButton.interactable = mission.isCompleted;
        }
        if (missionStatusImage != null)
        {
            missionStatusImage.color = mission.isCompleted ? completedColor : inProgressColor;
            missionStatusImage.sprite = mission.isCompleted ? completedSprite : inProgressSprite;
        }
    }
}
