using UnityEngine;
using System;

public class GemManager : MonoBehaviour
{
    public static GemManager instance;

    private const string GemBalanceKey = "GemBalance";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Get current Gem balance
    public int GetGemBalance()
    {
        return PlayerPrefs.GetInt(GemBalanceKey, 0);
    }

    // Add Gems to the player's balance
    public void AddGems(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Attempted to add negative Gems. Use SpendGems for spending.");
            return;
        }
        int currentBalance = GetGemBalance();
        PlayerPrefs.SetInt(GemBalanceKey, currentBalance + amount);
        PlayerPrefs.Save();
        Debug.Log($"Added {amount} Gems. New balance: {GetGemBalance()}");
    }

    // Spend Gems from the player's balance
    public bool SpendGems(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Attempted to spend negative Gems. Use AddGems for adding.");
            return false;
        }
        int currentBalance = GetGemBalance();
        if (currentBalance >= amount)
        {
            PlayerPrefs.SetInt(GemBalanceKey, currentBalance - amount);
            PlayerPrefs.Save();
            Debug.Log($"Spent {amount} Gems. New balance: {GetGemBalance()}");
            return true;
        }
        else
        {
            Debug.Log("Not enough Gems to spend.");
            return false;
        }
    }

    // For debugging/testing: Reset Gem balance
    public void ResetGems()
    {
        PlayerPrefs.SetInt(GemBalanceKey, 0);
        PlayerPrefs.Save();
        Debug.Log("Gem balance reset to 0.");
    }
}
